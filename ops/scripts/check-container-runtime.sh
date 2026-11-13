#!/usr/bin/env bash
# Check that every running container has the rights and the security profiles of its compose
# service (MIL-080; CIS-001 rows 4.1 and 5.x). Each container is read two ways: its
# configuration (docker inspect) and the kernel's view of its main process (/proc/<pid>/status
# and its AppArmor label). Both are compared with the profile of its compose service in
# ops/security/container-profiles.tsv. It changes nothing.
#
# A container is matched by the folder of its compose file under ops/deploy and its compose
# service (the compose labels), so the VM, the throwaway stack of CI and the monitoring LXC share
# the profiles. The rules, each reported by its name:
#   profile            the container has a profile; one without is checked against the strictest
#                      profile (nonroot, no capability, read-only, no host path)
#   privileged         not privileged
#   cap_drop           cap_drop: ALL
#   cap_add            no added capability outside the profile
#   cap_bounding       no capability outside the profile in the bounding set of the main process
#   no_new_privileges  set in the configuration and in force for the main process (NoNewPrivs 1)
#   seccomp            not unconfined, and the main process in filter mode where the daemon has seccomp
#   apparmor           not unconfined, and the main process confined in enforce mode where the
#                      daemon has AppArmor
#   read_only          a read-only root filesystem, unless the profile says rw
#   limits             a memory, a CPU and a PID limit
#   host_namespace     not in the network, PID, IPC, UTS or user namespace of the host
#   device             no device, device rule or device request
#   writable_mount     no host folder mounted writable, except at a folder the profile names
#   host_path          no sensitive host path (SENSITIVE) outside the profile
#   docker_socket      the Docker socket is mounted only where the profile says so, and a container
#                      that sees it through a parent folder cannot open it: not its owner, not in
#                      its group, no DAC_OVERRIDE
#   user               the effective user of the main process is the one of the profile
#   kernel_view        the kernel's view of the main process can be read
# The exempt column of a profile names rules that do not apply to its service.
#
# It prints one line per container: PASS, FAIL or WARN, its name, its profile, the rules it breaks
# (- for none), then what it can reach (user, capabilities, seccomp, AppArmor, root filesystem,
# sensitive host paths, writable mounts, the socket, networks, published ports), and a line per
# broken rule; then the versions of Docker Engine, containerd and runc. It never prints an
# environment, a label value other than the compose service, or the content of a file.
#
# Usage: check-container-runtime.sh [--profiles <file>] [--deploy] [--metrics <file>]
#                                   [--lock <file>] [--filter <docker ps filter>]...
#   --profiles  the profiles (default: ../security/container-profiles.tsv from this script)
#   --deploy    a container without a profile is a warning, not a violation: the deploy checks
#               its release and must not fail, or block a rollback, on a container outside it
#   --metrics   also write the result for Prometheus to <file>, a text file that Alloy reads
#   --lock      skip the run, and exit 0, when another process holds <file> (the lock of the
#               deploy command) before or after the containers are read, so a check never compares
#               a release with the containers of the one before it in the middle of a deploy. The
#               check only tests the lock and gives it back at once: a deploy never finds it held
#   --filter    check only the containers of this filter of docker ps (deploy-monitoring.sh passes
#               its compose project; the tests pass their label)
# Env:   NFHOTEL_DOCKER (default docker), NFHOTEL_PROC (default /proc) and NFHOTEL_DOCKER_SOCKET
#        (default /var/run/docker.sock); for the tests
# Exit:  0 no violation; 1 a violation, or docker could not be read; 2 usage error or a profile
#        that is not valid
# Needs: bash 4+, docker, GNU stat (-c), flock (for --lock), root (to read the main process of
#        every container)

# No -e: every container is checked and counted, and the exit code says whether one broke its
# profile; each call that can fail is handled where it is made.
set -uo pipefail

docker="${NFHOTEL_DOCKER:-docker}"
proc="${NFHOTEL_PROC:-/proc}"
socket="${NFHOTEL_DOCKER_SOCKET:-/var/run/docker.sock}"
profiles="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)/security/container-profiles.tsv"
deploy=0
metrics=""
lock=""
filters=()

# The capabilities by their number in the kernel (linux/capability.h).
readonly CAPS=(CHOWN DAC_OVERRIDE DAC_READ_SEARCH FOWNER FSETID KILL SETGID SETUID SETPCAP LINUX_IMMUTABLE
  NET_BIND_SERVICE NET_BROADCAST NET_ADMIN NET_RAW IPC_LOCK IPC_OWNER SYS_MODULE SYS_RAWIO SYS_CHROOT
  SYS_PTRACE SYS_PACCT SYS_ADMIN SYS_BOOT SYS_NICE SYS_RESOURCE SYS_TIME SYS_TTY_CONFIG MKNOD LEASE
  AUDIT_WRITE AUDIT_CONTROL SETFCAP MAC_OVERRIDE MAC_ADMIN SYSLOG WAKE_ALARM BLOCK_SUSPEND AUDIT_READ
  PERFMON BPF CHECKPOINT_RESTORE)
# The capabilities that Docker gives a container that drops none.
readonly DOCKER_DEFAULT_CAPS=(CHOWN DAC_OVERRIDE FSETID FOWNER MKNOD NET_RAW SETGID SETUID SETFCAP SETPCAP
  NET_BIND_SERVICE SYS_CHROOT KILL AUDIT_WRITE)
# A bind mount whose source is one of these, is inside one, or holds one, reaches the host's
# system, its devices, the runtime or the logs; it must be named in the profile. A host folder
# elsewhere (the compose folders of the checkout, with the secret files) is not sensitive.
readonly SENSITIVE=(/proc /sys /dev /etc /boot /root /run /var/run /var/lib/docker /var/lib/containerd
  /var/log /usr /bin /sbin /lib /lib64)
readonly SOCKETS=(/var/run/docker.sock /run/docker.sock)
readonly RULES=(profile privileged cap_drop cap_add cap_bounding no_new_privileges seccomp apparmor read_only
  limits host_namespace device writable_mount host_path docker_socket user kernel_view)
# The patterns of the columns of a profile.
readonly RE_KEY='^[a-z0-9-]+/[a-z0-9_-]+$' RE_USER='^(root|nonroot|[0-9]+)$' RE_CAPS='^(-|default|[A-Z_]+(,[A-Z_]+)*)$'
readonly RE_ROOTFS='^(ro|rw)$' RE_PATHS='^(-|/[^,[:space:]]*(,/[^,[:space:]]*)*)$' RE_EXEMPT='^(-|[a-z_]+(,[a-z_]+)*)$'
# One docker inspect per container, one value per line; empty values keep their line. A list or
# map that Docker leaves null, and a label the container does not have, are read through with,
# since len refuses null and index prints a missing key as <no value>.
# shellcheck disable=SC2016 # a Go template, not a shell expansion
readonly FORMAT='{{.Name}}
{{.State.Pid}}
{{.HostConfig.Privileged}}
{{json .HostConfig.CapDrop}}
{{json .HostConfig.CapAdd}}
{{json .HostConfig.SecurityOpt}}
{{.HostConfig.ReadonlyRootfs}}
{{.HostConfig.Memory}}|{{.HostConfig.NanoCpus}}|{{.HostConfig.CpuQuota}}|{{.HostConfig.PidsLimit}}
{{.HostConfig.NetworkMode}}|{{.HostConfig.PidMode}}|{{.HostConfig.IpcMode}}|{{.HostConfig.UTSMode}}|{{.HostConfig.UsernsMode}}
{{with .HostConfig.Devices}}{{len .}}{{else}}0{{end}}|{{with .HostConfig.DeviceCgroupRules}}{{len .}}{{else}}0{{end}}|{{with .HostConfig.DeviceRequests}}{{len .}}{{else}}0{{end}}
{{.AppArmorProfile}}
{{with .Config.Labels}}{{with index . "com.docker.compose.service"}}{{.}}{{end}}{{end}}
{{with .Config.Labels}}{{with index . "com.docker.compose.project.config_files"}}{{.}}{{end}}{{end}}
{{range .Mounts}}{{.Type}}|{{.Source}}|{{.Destination}}|{{.RW}};{{end}}
{{range $name, $net := .NetworkSettings.Networks}}{{$name}},{{end}}
{{range $port, $bindings := .NetworkSettings.Ports}}{{if $bindings}}{{$port}},{{end}}{{end}}'

usage() {
  sed -n '2,/^set -uo/p' "${BASH_SOURCE[0]}" | sed '$d' | sed 's/^# \{0,1\}//'
}

die() {
  echo "error: $*" >&2
  exit 1
}

while (($#)); do
  case "$1" in
    --profiles)
      profiles="${2:?--profiles needs a file}"
      shift
      ;;
    --deploy) deploy=1 ;;
    --metrics)
      metrics="${2:?--metrics needs a file}"
      shift
      ;;
    --lock)
      lock="${2:?--lock needs a file}"
      shift
      ;;
    --filter)
      filters+=(--filter "${2:?--filter needs a value}")
      shift
      ;;
    -h | --help)
      usage
      exit 0
      ;;
    *)
      usage >&2
      exit 2
      ;;
  esac
  shift
done

# deploy_running: whether another process holds the lock. The test takes the lock and closes it at
# once, so a deploy that starts never finds it held by this check.
deploy_running() {
  local fd
  exec {fd}>>"$lock" || die "cannot open the lock $lock"
  if flock --nonblock "$fd"; then
    exec {fd}>&-
    return 1
  fi
  exec {fd}>&-
  return 0
}

if [[ -n "$lock" ]] && deploy_running; then
  echo "skipped: a deploy holds $lock; the next run checks the containers"
  exit 0
fi

# --- the profiles -----------------------------------------------------------------------------

declare -A P_USER=() P_CAPS=() P_ROOTFS=() P_PATHS=() P_WRITABLE=() P_EXEMPT=()

# in_list <word> <word...>: whether the first word is one of the others.
in_list() {
  local word="$1" item
  shift
  for item in "$@"; do
    [[ "$item" == "$word" ]] && return 0
  done
  return 1
}

# read_profiles: load the profiles file; a row that is not valid stops the check with exit 2.
read_profiles() {
  local n=0 folder service user caps rootfs paths writable exempt extra rule key
  [[ -f "$profiles" ]] || {
    echo "error: the profiles $profiles do not exist" >&2
    exit 2
  }
  while IFS=$'\t' read -r folder service user caps rootfs paths writable exempt extra || [[ -n "$folder" ]]; do
    n=$((n + 1))
    [[ -z "$folder" || "$folder" == \#* ]] && continue
    if [[ -n "$extra" || -z "$exempt" ]] || [[ ! "$folder/$service" =~ $RE_KEY ]] ||
      [[ ! "$user" =~ $RE_USER ]] || [[ ! "$caps" =~ $RE_CAPS ]] || [[ ! "$rootfs" =~ $RE_ROOTFS ]] ||
      [[ ! "$paths" =~ $RE_PATHS ]] || [[ ! "$writable" =~ $RE_PATHS ]] || [[ ! "$exempt" =~ $RE_EXEMPT ]]; then
      echo "error: line $n of $profiles is not a valid profile (eight tab-separated columns)" >&2
      exit 2
    fi
    if [[ "$caps" != - && "$caps" != default ]]; then
      for rule in ${caps//,/ }; do
        in_list "$rule" "${CAPS[@]}" || {
          echo "error: line $n of $profiles names the unknown capability $rule" >&2
          exit 2
        }
      done
    fi
    if [[ "$exempt" != - ]]; then
      for rule in ${exempt//,/ }; do
        in_list "$rule" "${RULES[@]}" || {
          echo "error: line $n of $profiles exempts the unknown rule $rule" >&2
          exit 2
        }
      done
    fi
    key="$folder/$service"
    [[ -z "${P_USER[$key]:-}" ]] || {
      echo "error: line $n of $profiles repeats the profile $key" >&2
      exit 2
    }
    P_USER[$key]="$user"
    P_CAPS[$key]="$caps"
    P_ROOTFS[$key]="$rootfs"
    P_PATHS[$key]="$paths"
    P_WRITABLE[$key]="$writable"
    P_EXEMPT[$key]="$exempt"
  done <"$profiles"
}

# --- capabilities -----------------------------------------------------------------------------

# cap_number <name>: the number of a capability, or nothing.
cap_number() {
  local i
  for i in "${!CAPS[@]}"; do
    [[ "${CAPS[$i]}" == "$1" ]] && echo "$i" && return 0
  done
  return 1
}

# mask_of <caps column>: the bit mask of the capabilities of a profile.
mask_of() {
  local mask=0 name n names=()
  case "$1" in
    -) ;;
    default) names=("${DOCKER_DEFAULT_CAPS[@]}") ;;
    *) IFS=, read -r -a names <<<"$1" ;;
  esac
  for name in "${names[@]}"; do
    n="$(cap_number "$name")" && mask=$((mask | (1 << n)))
  done
  echo "$mask"
}

# names_of <mask>: the capabilities of a bit mask, comma separated, or none.
names_of() {
  local mask="$1" i out=""
  for ((i = 0; i < 64; i++)); do
    if (((mask >> i) & 1)); then
      out+="${CAPS[$i]:-CAP_$i},"
    fi
  done
  out="${out%,}"
  echo "${out:-none}"
}

# json_list <JSON array of strings>: its items, one per line, without CAP_.
json_list() {
  local s="${1#[}" item
  s="${s%]}"
  [[ "$s" == null || -z "$s" ]] && return 0
  local items=()
  IFS=, read -r -a items <<<"$s"
  for item in "${items[@]}"; do
    item="${item//\"/}"
    item="${item#CAP_}"
    echo "${item^^}"
  done
}

# --- paths ------------------------------------------------------------------------------------

# within <path> <folder>: whether the path is the folder or inside it.
within() {
  [[ "$2" == / || "$1" == "$2" || "$1" == "$2"/* ]]
}

# is_sensitive <source>: whether a host path reaches the host's system (see SENSITIVE).
is_sensitive() {
  local p
  [[ "$1" == / ]] && return 0
  for p in "${SENSITIVE[@]}"; do
    within "$1" "$p" && return 0
    within "$p" "$1" && return 0
  done
  return 1
}

# is_socket <source>: whether a host path is the Docker socket.
is_socket() {
  in_list "$1" "${SOCKETS[@]}" "$socket"
}

# shows_socket <source>: whether a mounted host folder holds the Docker socket.
shows_socket() {
  local s
  for s in "${SOCKETS[@]}" "$socket"; do
    [[ "$1" != "$s" ]] && within "$s" "$1" && return 0
  done
  return 1
}

# allowed <path> <comma list of paths>: whether a path is one of the profile, or inside one. The
# root folder allows itself only, so a profile with / names every other path it mounts.
allowed() {
  local p items=()
  [[ "$2" == - ]] && return 1
  IFS=, read -r -a items <<<"$2"
  for p in "${items[@]}"; do
    [[ "$1" == "$p" ]] && return 0
    [[ "$p" != / && "$1" == "$p"/* ]] && return 0
  done
  return 1
}

# folder_of <config_files label>: the folder of the first compose file, the key of the profiles.
folder_of() {
  local file="${1%%,*}"
  file="${file//\\//}"
  [[ "$file" == */* ]] || return 0
  file="${file%/*}"
  echo "${file##*/}"
}

# --- one container ----------------------------------------------------------------------------

violations=()
# violate <rule> <detail>: record a broken rule unless the profile exempts it.
violate() {
  [[ ",$exempt," == *",$1,"* ]] && return 0
  violations+=("$1: $2")
}

# positive <value>: whether the value is a whole number above 0 (Docker prints 0, -1 or <nil> for
# no limit).
positive() {
  [[ "$1" =~ ^[0-9]+$ ]] && (($1 > 0))
}

# check_container <id>: print the line of the container and its broken rules; sets status.
check_container() {
  local id="$1" out lines=() name pid privileged cap_drop cap_add security_opt readonly_rootfs limits namespaces
  local devices apparmor_profile service config_files mounts networks ports folder key known=1
  local user caps rootfs paths writable allowed_mask
  out="$("$docker" inspect --format "$FORMAT" "$id" 2>/dev/null)" || {
    echo "WARN  ${id:0:12}  -  -  removed while it was checked"
    status=warn
    return 0
  }
  mapfile -t lines <<<"$out"
  name="${lines[0]#/}" pid="${lines[1]}" privileged="${lines[2]}" cap_drop="${lines[3]}" cap_add="${lines[4]}"
  security_opt="${lines[5]}" readonly_rootfs="${lines[6]}" limits="${lines[7]}" namespaces="${lines[8]}"
  devices="${lines[9]}" apparmor_profile="${lines[10]}" service="${lines[11]}" config_files="${lines[12]}"
  mounts="${lines[13]:-}" networks="${lines[14]:-}" ports="${lines[15]:-}"

  folder="$(folder_of "$config_files")"
  key="${folder:--}/${service:--}"
  # exempt is global on purpose: violate reads it.
  if [[ -n "${P_USER[$key]:-}" ]]; then
    user="${P_USER[$key]}" caps="${P_CAPS[$key]}" rootfs="${P_ROOTFS[$key]}"
    paths="${P_PATHS[$key]}" writable="${P_WRITABLE[$key]}" exempt="${P_EXEMPT[$key]}"
  else
    known=0
    user=nonroot caps=- rootfs=ro paths=- writable=- exempt=-
  fi
  violations=()
  ((known)) || violate profile "no profile for ${key}; add a row to $(basename "$profiles") or remove the container"

  # The configuration.
  [[ "$privileged" == true ]] && violate privileged "the container runs privileged"
  [[ "$cap_drop" =~ \"(CAP_)?ALL\" ]] || violate cap_drop "cap_drop does not hold ALL"
  allowed_mask="$(mask_of "$caps")"
  local cap n
  while read -r cap; do
    [[ -n "$cap" ]] || continue
    n="$(cap_number "$cap")" || n=""
    if [[ -z "$n" ]] || ((((allowed_mask >> n) & 1) == 0)); then
      violate cap_add "adds $cap, which the profile does not name"
    fi
  done < <(json_list "$cap_add")
  [[ "$security_opt" =~ \"no-new-privileges([:=]true)?\" ]] ||
    violate no_new_privileges "no-new-privileges is not set in the configuration"
  [[ "$security_opt" =~ \"seccomp[:=]unconfined\" ]] && violate seccomp "seccomp is unconfined in the configuration"
  [[ "$security_opt" =~ \"apparmor[:=]unconfined\" ]] && violate apparmor "AppArmor is unconfined in the configuration"
  if ((has_apparmor)) && [[ -z "$apparmor_profile" || "$apparmor_profile" == unconfined ]]; then
    violate apparmor "the container has no AppArmor profile"
  fi
  [[ "$rootfs" == ro && "$readonly_rootfs" != true ]] && violate read_only "the root filesystem is writable"
  local memory nano quota pids
  IFS='|' read -r memory nano quota pids <<<"$limits"
  positive "$memory" || violate limits "no memory limit"
  positive "$nano" || positive "$quota" || violate limits "no CPU limit"
  positive "$pids" || violate limits "no PID limit"
  local net_mode pid_mode ipc_mode uts_mode userns_mode
  IFS='|' read -r net_mode pid_mode ipc_mode uts_mode userns_mode <<<"$namespaces"
  [[ "$net_mode" == host ]] && violate host_namespace "the network namespace of the host"
  [[ "$pid_mode" == host ]] && violate host_namespace "the PID namespace of the host"
  [[ "$ipc_mode" == host ]] && violate host_namespace "the IPC namespace of the host"
  [[ "$uts_mode" == host ]] && violate host_namespace "the UTS namespace of the host"
  [[ "$userns_mode" == host ]] && violate host_namespace "the user namespace of the host"
  local n_devices n_rules n_requests
  IFS='|' read -r n_devices n_rules n_requests <<<"$devices"
  ((${n_devices:-0} + ${n_rules:-0} + ${n_requests:-0} > 0)) &&
    violate device "$n_devices device(s), $n_rules device rule(s), $n_requests device request(s)"

  # The mounts.
  local mount type source destination rw items=() sensitive_seen="" writable_seen="" socket_state=none
  local seen_through=""
  IFS=';' read -r -a items <<<"$mounts"
  for mount in "${items[@]}"; do
    [[ -n "$mount" ]] || continue
    IFS='|' read -r type source destination rw <<<"$mount"
    [[ "$type" == bind ]] || continue
    if [[ "$rw" == true ]]; then
      writable_seen+="$destination,"
      allowed "$destination" "$writable" || violate writable_mount "$source is mounted writable at $destination"
    fi
    if is_socket "$source"; then
      socket_state=mounted
      allowed "$source" "$paths" || violate docker_socket "mounts the Docker socket $source"
      continue
    fi
    if is_sensitive "$source"; then
      sensitive_seen+="$source,"
      allowed "$source" "$paths" || violate host_path "mounts the host path $source, which the profile does not name"
    fi
    shows_socket "$source" && seen_through="$source"
  done

  # The kernel's view of the main process.
  local status_text="" euid="" egid="" groups="" cap_bounding="" cap_effective="" nnp="" seccomp_mode="" label=""
  if [[ "$pid" =~ ^[1-9][0-9]*$ ]] && status_text="$(cat "$proc/$pid/status" 2>/dev/null)" && [[ -n "$status_text" ]]; then
    euid="$(awk '$1 == "Uid:" { print $3 }' <<<"$status_text")"
    egid="$(awk '$1 == "Gid:" { print $3 }' <<<"$status_text")"
    groups="$(awk '$1 == "Groups:" { $1 = ""; print }' <<<"$status_text")"
    cap_bounding="$(awk '$1 == "CapBnd:" { print $2 }' <<<"$status_text")"
    cap_effective="$(awk '$1 == "CapEff:" { print $2 }' <<<"$status_text")"
    nnp="$(awk '$1 == "NoNewPrivs:" { print $2 }' <<<"$status_text")"
    seccomp_mode="$(awk '$1 == "Seccomp:" { print $2 }' <<<"$status_text")"
    # A kernel with more than one security module keeps the label of AppArmor in its own folder.
    local attr="$proc/$pid/attr/current"
    [[ -r "$proc/$pid/attr/apparmor/current" ]] && attr="$proc/$pid/attr/apparmor/current"
    label="$({ tr -d '\0' <"$attr"; } 2>/dev/null)"
  fi
  local bounding=0 effective=0 extra
  if [[ "$euid" =~ ^[0-9]+$ && "$cap_bounding" =~ ^[0-9a-f]+$ && "$cap_effective" =~ ^[0-9a-f]+$ ]]; then
    bounding=$((16#$cap_bounding))
    effective=$((16#$cap_effective))
    extra=$((bounding & ~allowed_mask))
    ((extra == 0)) || violate cap_bounding "the main process may hold $(names_of "$extra"), which the profile does not name"
    [[ "$nnp" == 1 ]] || violate no_new_privileges "no-new-privileges is not in force for the main process"
    if ((has_seccomp)) && [[ "$seccomp_mode" != 2 ]]; then
      violate seccomp "the main process is not in seccomp filter mode (Seccomp ${seccomp_mode:-?})"
    fi
    if ((has_apparmor)) && [[ "$label" == unconfined* || "$label" != *"(enforce)" ]]; then
      violate apparmor "the main process is not confined in enforce mode (${label:-no label})"
    fi
    case "$user" in
      nonroot) ((euid != 0)) || violate user "the main process runs as root; the profile says nonroot" ;;
      root) ;;
      *) ((euid == user)) || violate user "the main process runs as $euid; the profile says $user" ;;
    esac
    if [[ -n "$seen_through" ]]; then
      local opener=""
      if ((euid == 0)); then
        opener="root"
      elif [[ "$euid" == "$socket_uid" ]]; then
        opener="the owner of the socket"
      elif [[ "$egid" == "$socket_gid" || " $groups " == *" $socket_gid "* ]]; then
        opener="in the group $socket_gid of the socket"
      elif (((effective >> 1) & 1)); then
        opener="DAC_OVERRIDE"
      fi
      if [[ -n "$opener" ]]; then
        socket_state="seen through $seen_through, can open it"
        violate docker_socket "sees the Docker socket through $seen_through and can open it ($opener)"
      else
        socket_state="seen through $seen_through, cannot open it"
      fi
    fi
  else
    violate kernel_view "the kernel's view of the main process ($proc/$pid) cannot be read; run the check as root on the Docker host"
  fi

  # The line of the container, then one line per broken rule.
  local rules="" v seccomp_text
  for v in "${violations[@]}"; do
    [[ ",$rules," == *",${v%%:*},"* ]] || rules+="${v%%:*},"
  done
  rules="${rules%,}"
  case "$seccomp_mode" in
    0) seccomp_text=disabled ;;
    1) seccomp_text=strict ;;
    2) seccomp_text=filter ;;
    *) seccomp_text=unknown ;;
  esac
  networks="${networks%,}" ports="${ports%,}" sensitive_seen="${sensitive_seen%,}" writable_seen="${writable_seen%,}"
  local info
  info="uid ${euid:-?}, caps $(names_of "$bounding"), seccomp $seccomp_text, apparmor ${label:-none}"
  info+=", root $([[ "$readonly_rootfs" == true ]] && echo ro || echo rw), host paths ${sensitive_seen:-none}"
  info+=", writable mounts ${writable_seen:-none}, socket $socket_state, networks ${networks:-none}, ports ${ports:-none}"
  if ((${#violations[@]} == 0)); then
    status=pass
    echo "PASS  $name  $key  -  $info"
  elif ((! known && deploy)); then
    status=warn
    echo "WARN  $name  $key  $rules  $info"
    printf '      %s\n' "${violations[@]}" "the deploy checks its release only; this container is outside it"
  else
    status=fail
    echo "FAIL  $name  $key  $rules  $info"
    printf '      %s\n' "${violations[@]}"
    for v in "${violations[@]}"; do
      metric_lines+=("nfhotel_container_runtime_violation{container=\"$name\",rule=\"${v%%:*}\"} 1")
    done
  fi
}

# --- main -------------------------------------------------------------------------------------

read_profiles
info="$("$docker" info --format '{{json .SecurityOptions}}' 2>/dev/null)" || die "docker info failed"
has_apparmor=0 has_seccomp=0
[[ "$info" == *'"name=apparmor'* ]] && has_apparmor=1
[[ "$info" == *'"name=seccomp'* ]] && has_seccomp=1
socket_uid="" socket_gid=""
if read -r socket_uid socket_gid < <(stat -L -c '%u %g' "$socket" 2>/dev/null); then :; else
  socket_uid=0 socket_gid=0
fi
ids="$("$docker" ps --quiet --no-trunc "${filters[@]}")" || die "docker ps failed"

echo "==> the runtime of the containers against $(basename "$profiles") (MIL-080)"
passed=0 failed=0 warned=0 status="" exempt=-
metric_lines=()
while read -r id; do
  [[ -n "$id" ]] || continue
  check_container "$id"
  case "$status" in
    pass) passed=$((passed + 1)) ;;
    fail) failed=$((failed + 1)) ;;
    warn) warned=$((warned + 1)) ;;
  esac
done <<<"$ids"
# A deploy that started while the containers were read may have changed them halfway: this run is
# not counted, and the next one reads the containers of the release.
if [[ -n "$lock" ]] && deploy_running; then
  echo "skipped: a deploy took $lock during the check; the next run checks the containers"
  exit 0
fi

components="$("$docker" version --format '{{.Server.Version}}|{{range .Server.Components}}{{.Name}}={{.Version}} {{end}}' 2>/dev/null)" ||
  components="?|"
engine="${components%%|*}" containerd="?" runc="?"
for part in ${components#*|}; do
  case "$part" in
    containerd=*) containerd="${part#*=}" ;;
    runc=*) runc="${part#*=}" ;;
  esac
done
echo "==> $((passed + failed + warned)) container(s): $passed passed, $failed failed, $warned warned"
echo "==> Docker Engine $engine, containerd $containerd, runc $runc; AppArmor $( ((has_apparmor)) && echo on || echo off), seccomp $( ((has_seccomp)) && echo on || echo off)"

if [[ -n "$metrics" ]]; then
  dir="$(dirname "$metrics")"
  install -d -m 0755 "$dir" || die "cannot make $dir"
  tmp="$(mktemp "$dir/.container-runtime.XXXXXX")" || die "cannot write in $dir"
  trap 'rm -f "$tmp"' EXIT
  {
    echo '# HELP nfhotel_container_runtime_violation 1 for each rule that a running container breaks.'
    echo '# TYPE nfhotel_container_runtime_violation gauge'
    ((${#metric_lines[@]} == 0)) || printf '%s\n' "${metric_lines[@]}"
    echo '# HELP nfhotel_container_runtime_containers The running containers that the runtime check read.'
    echo '# TYPE nfhotel_container_runtime_containers gauge'
    echo "nfhotel_container_runtime_containers $((passed + failed + warned))"
    echo '# HELP nfhotel_container_runtime_failed The containers that break their profile.'
    echo '# TYPE nfhotel_container_runtime_failed gauge'
    echo "nfhotel_container_runtime_failed $failed"
    echo '# HELP nfhotel_task_last_success_timestamp_seconds When the task last succeeded.'
    echo '# TYPE nfhotel_task_last_success_timestamp_seconds gauge'
    echo "nfhotel_task_last_success_timestamp_seconds{task=\"container_check\"} $(date +%s)"
  } >"$tmp"
  chmod 0644 "$tmp"
  mv "$tmp" "$metrics" || die "cannot write $metrics"
fi

((failed == 0))
