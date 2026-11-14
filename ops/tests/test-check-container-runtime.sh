#!/usr/bin/env bash
# Unit test of scripts/check-container-runtime.sh (MIL-080 task 2): for every rule a container
# that keeps it and one that breaks it, each failing with exactly its rule; the exceptions of a
# profile; a daemon without AppArmor or seccomp; the deploy mode, where a container without a
# profile is a warning; the report for Prometheus; the floor of runc (MIL-083 task 5); the lock of
# the deploy; the profiles that are not valid; and that the profiles of the repository name every
# compose service under deploy/.
# A stand-in answers for docker and a folder stands in for /proc, so no container runs.
#
# Run it in Debian 12 with the operations folder at /src:
#   docker run --rm -v "$PWD/ops:/src:ro" debian:12-slim bash /src/tests/test-check-container-runtime.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: bash 4+, awk, flock, stat

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

OPS="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CHECK="$OPS/scripts/check-container-runtime.sh"
W="$(mktemp -d)"
trap 'rm -rf "$W"' EXIT
pass=0
fail=0
ok() {
  echo "PASS  $1"
  pass=$((pass + 1))
}
ko() {
  echo "FAIL  $1"
  fail=$((fail + 1))
}

mkdir -p "$W/bin" "$W/docker" "$W/proc"
# The stand-in for docker answers from the files in $FAKE_DOCKER and records the arguments of ps;
# the Alloy of a socket proxy comes from ps-alloy, and exec in it answers with exec-answers.
cat >"$W/bin/docker" <<'EOF'
#!/bin/bash
d="$FAKE_DOCKER"
case "$1" in
  info) cat "$d/info" ;;
  version) cat "$d/version" ;;
  ps)
    if [[ "$*" == *label=com.docker.compose.service=alloy* ]]; then
      echo "$*" >"$d/ps-alloy-args"; cat "$d/ps-alloy" 2>/dev/null; exit 0
    fi
    echo "$*" >"$d/ps-args"; [[ ! -e "$d/ps-fails" ]] || exit 1; cat "$d/ps" 2>/dev/null; exit 0 ;;
  exec) echo "$2" >"$d/exec-target"; [[ ! -e "$d/exec-fails" ]] || exit 1; cat "$d/exec-answers" ;;
  inspect)
    # FAKE_TAKE_LOCK: a deploy takes that lock while the containers are read (held for 3 seconds).
    if [[ -n "${FAKE_TAKE_LOCK:-}" && ! -e "$d/lock-taken" ]]; then
      touch "$d/lock-taken"
      (flock "$FAKE_TAKE_LOCK" sleep 3 </dev/null >/dev/null 2>&1 &)
      sleep 0.5
    fi
    f="$d/inspect-${!#}"; [[ -f "$f" ]] || exit 1; cat "$f" ;;
  *) exit 1 ;;
esac
EOF
chmod +x "$W/bin/docker"
apparmor_and_seccomp='["name=apparmor","name=seccomp,profile=builtin","name=cgroupns"]'
echo "$apparmor_and_seccomp" >"$W/docker/info"
readonly VERSIONS='29.0.1|Engine=29.0.1 containerd=v2.1.5 runc=1.3.4 docker-init=0.19.0 '
echo "$VERSIONS" >"$W/docker/version"
# The socket belongs to root and the group 998, as the docker group of a host.
touch "$W/docker.sock" && chown 0:998 "$W/docker.sock"

# The profiles of the test: the same columns as ops/security/container-profiles.tsv.
printf '%s\n' '# test profiles' \
  "$(printf '%s\t' t app nonroot - ro - - | sed 's/\t$//')	-" \
  "$(printf '%s\t' t web root CHOWN,NET_BIND_SERVICE ro - - | sed 's/\t$//')	-" \
  "$(printf '%s\t' t agent 473 - ro /,/proc - | sed 's/\t$//')	-" \
  "$(printf '%s\t' t proxy root - ro /var/run/docker.sock - | sed 's/\t$//')	-" \
  "$(printf '%s\t' t store nonroot - rw - /data | sed 's/\t$//')	-" \
  "$(printf '%s\t' t legacy root default rw - - | sed 's/\t$//')	cap_drop,limits" \
  "$(printf '%s\t' alloy socket-proxy root - ro /var/run/docker.sock - | sed 's/\t$//')	-" \
  "$(printf '%s\t' alloy alloy 473 - ro /,/proc - | sed 's/\t$//')	-" >"$W/profiles.tsv"

n=0
# container <id> [<field>=<value>...]: a container that keeps the profile t/app, changed by the
# fields: the lines of the docker inspect format of the check, and the kernel's view of its main
# process (uid, gid, groups, capbnd, capeff, nnp, seccomp, label; proc=no leaves /proc out).
container() {
  local id="$1" field pid
  shift
  n=$((n + 1))
  pid=$((1000 + n))
  local -A f=(
    [name]="/t-$id" [pid]="$pid" [privileged]=false [cap_drop]='["ALL"]' [cap_add]=null
    [security_opt]='["no-new-privileges:true"]' [readonly]=true [limits]='67108864|250000000|0|32'
    [namespaces]='bridge||||' [devices]='0|0|0' [apparmor_profile]=docker-default [service]=app
    [config_files]=/srv/nfhotel/ops/deploy/t/docker-compose.yml [mounts]='' [networks]='t-net,' [ports]=''
    [system_paths]='12|5'
    [uid]=10001 [gid]=10001 [groups]='' [capbnd]=0000000000000000 [capeff]=0000000000000000
    [nnp]=1 [seccomp]=2 [label]='docker-default (enforce)' [proc]=yes
  )
  for field in "$@"; do
    f[${field%%=*}]="${field#*=}"
  done
  printf '%s\n' "${f[name]}" "${f[pid]}" "${f[privileged]}" "${f[cap_drop]}" "${f[cap_add]}" \
    "${f[security_opt]}" "${f[readonly]}" "${f[limits]}" "${f[namespaces]}" "${f[devices]}" \
    "${f[apparmor_profile]}" "${f[service]}" "${f[config_files]}" "${f[mounts]}" "${f[networks]}" \
    "${f[ports]}" "${f[system_paths]}" >"$W/docker/inspect-$id"
  echo "$id" >>"$W/docker/ps"
  if [[ "${f[proc]}" == yes ]]; then
    mkdir -p "$W/proc/${f[pid]}/attr"
    printf 'Name:\tmain\nUid:\t%s\t%s\t%s\t%s\nGid:\t%s\t%s\t%s\t%s\nGroups:\t%s\nCapEff:\t%s\nCapBnd:\t%s\nNoNewPrivs:\t%s\nSeccomp:\t%s\n' \
      "${f[uid]}" "${f[uid]}" "${f[uid]}" "${f[uid]}" "${f[gid]}" "${f[gid]}" "${f[gid]}" "${f[gid]}" \
      "${f[groups]}" "${f[capeff]}" "${f[capbnd]}" "${f[nnp]}" "${f[seccomp]}" >"$W/proc/${f[pid]}/status"
    printf '%s\n' "${f[label]}" >"$W/proc/${f[pid]}/attr/current"
  fi
}
reset() {
  rm -f "$W/docker/ps" "$W/docker/inspect-"* "$W/docker/ps-fails" "$W/docker/ps-alloy"* "$W/docker/exec-"*
  rm -rf "${W:?}/proc/"*
  echo "$apparmor_and_seccomp" >"$W/docker/info"
}
check() {
  PATH="$W/bin:$PATH" FAKE_DOCKER="$W/docker" NFHOTEL_PROC="$W/proc" NFHOTEL_DOCKER_SOCKET="$W/docker.sock" \
    FAKE_TAKE_LOCK="${FAKE_TAKE_LOCK:-}" bash "$CHECK" --profiles "${PROFILES:-$W/profiles.tsv}" "$@" >"$W/out" 2>&1
}
# rules_of <id>: the status and the broken rules of a container in the output.
rules_of() { awk -v c="t-$1" '$2 == c { print $1, $4 }' "$W/out"; }
expect() {
  local id="$1" want="$2" desc="$3" got
  got="$(rules_of "$id")"
  [[ "$got" == "$want" ]] && ok "$desc" || ko "$desc (got '$got', want '$want'): $(grep -A3 "t-$id " "$W/out" | head -4)"
}

# Every rule, kept and broken; each breaking container changes one thing.
reset
SYS_ADMIN=0000000000200000 WEB_CAPS=0000000000000401 SOCKET_GROUP=998
container ok
container profile service=nope
container privileged privileged=true
container cap-drop cap_drop=null
container legacy service=legacy cap_drop=null readonly=false limits='0|0|0|<nil>' uid=0 gid=0 \
  capbnd=00000000a80425fb capeff=00000000a80425fb
container web service=web cap_add='["CAP_CHOWN","NET_BIND_SERVICE"]' uid=0 gid=0 capbnd="$WEB_CAPS" capeff="$WEB_CAPS"
container cap-add service=web cap_add='["CAP_CHOWN","CAP_NET_ADMIN"]' uid=0 gid=0 capbnd="$WEB_CAPS" capeff="$WEB_CAPS"
container cap-bounding capbnd="$SYS_ADMIN"
container nnp-short security_opt='["no-new-privileges"]'
container nnp-config security_opt='[]'
container nnp-kernel nnp=0
container seccomp-config security_opt='["no-new-privileges:true","seccomp=unconfined"]'
container seccomp-kernel seccomp=0
container apparmor-config security_opt='["no-new-privileges:true","apparmor=unconfined"]'
container apparmor-none apparmor_profile=''
container apparmor-kernel label=unconfined
container apparmor-complain label='docker-default (complain)'
container read-only readonly=false
container store service=store readonly=false mounts='bind|/srv/nfhotel/store|/data|true;volume|/var/lib/docker/volumes/x/_data|/cache|true;'
container no-memory limits='0|250000000|0|32'
container no-cpu limits='67108864|0|0|32'
container quota limits='67108864|0|50000|32'
container no-pids limits='67108864|250000000|0|<nil>'
container zero-pids limits='67108864|250000000|0|0'
container minus-pids limits='67108864|250000000|0|-1'
container host-net namespaces='host||||'
container host-all namespaces='host|host|host|host|host'
container own-net namespaces='none||||'
container shared-net namespaces='container:abc||||'
container device devices='1|0|0'
container device-rule devices='0|1|0'
container system-paths system_paths='0|0'
container system-paths-masked system_paths='0|5'
container system-paths-read-only system_paths='12|0'
container writable mounts='bind|/srv/nfhotel/data|/data|true;'
container volume mounts='volume|/var/lib/docker/volumes/x/_data|/data|true;'
container sensitive mounts='bind|/etc/passwd|/etc/passwd|false;'
container parent mounts='bind|/var|/host/var|false;'
container checkout mounts='bind|/srv/nfhotel/ops/deploy/t/templates|/etc/t|false;'
container agent service=agent uid=473 gid=473 groups=999 mounts='bind|/|/host/root|false;bind|/proc|/host/proc|false;'
container agent-sys service=agent uid=473 gid=473 groups=999 mounts='bind|/sys|/host/sys|false;'
container socket mounts='bind|/var/run/docker.sock|/var/run/docker.sock|false;'
container proxy service=proxy uid=0 gid=0 mounts='bind|/var/run/docker.sock|/var/run/docker.sock|false;'
container socket-group service=agent uid=473 gid=473 groups="999 $SOCKET_GROUP" mounts='bind|/|/host/root|false;'
container socket-root service=agent uid=0 gid=0 mounts='bind|/|/host/root|false;'
container socket-dac service=agent uid=473 gid=473 capeff=0000000000000002 capbnd=0000000000000002 mounts='bind|/|/host/root|false;'
container user-root uid=0 gid=0
container user-other service=agent uid=1000 gid=1000
container web-root service=web uid=0 gid=0 cap_add='["CHOWN"]' capbnd=0000000000000001 capeff=0000000000000001
container kernel proc=no
check
code=$?
expect ok "PASS -" "profile: a container with a profile passes"
expect profile "FAIL profile" "profile: a container without a profile fails"
expect privileged "FAIL privileged" "privileged: a privileged container fails"
expect cap-drop "FAIL cap_drop" "cap_drop: a container without cap_drop ALL fails"
expect legacy "PASS -" "the exceptions of a profile: cap_drop and limits do not apply to legacy"
expect web "PASS -" "cap_add: the capabilities of the profile pass, with or without CAP_"
expect cap-add "FAIL cap_add" "cap_add: a capability outside the profile fails"
expect cap-bounding "FAIL cap_bounding" "cap_bounding: SYS_ADMIN in the bounding set of the main process fails"
grep -A2 '^FAIL  t-cap-bounding ' "$W/out" | grep -q 'may hold SYS_ADMIN' && ok "cap_bounding: the line names the capability" ||
  ko "cap_bounding detail: $(grep -A2 'FAIL  t-cap-bounding' "$W/out")"
expect nnp-short "PASS -" "no_new_privileges: the short form passes"
expect nnp-config "FAIL no_new_privileges" "no_new_privileges: missing from the configuration fails"
expect nnp-kernel "FAIL no_new_privileges" "no_new_privileges: not in force for the main process fails"
expect seccomp-config "FAIL seccomp" "seccomp: unconfined in the configuration fails"
expect seccomp-kernel "FAIL seccomp" "seccomp: a main process outside filter mode fails"
expect apparmor-config "FAIL apparmor" "apparmor: unconfined in the configuration fails"
expect apparmor-none "FAIL apparmor" "apparmor: no profile where the daemon has AppArmor fails"
expect apparmor-kernel "FAIL apparmor" "apparmor: an unconfined main process fails"
expect apparmor-complain "FAIL apparmor" "apparmor: a profile in complain mode fails"
expect read-only "FAIL read_only" "read_only: a writable root filesystem fails"
expect store "PASS -" "read_only and writable_mount: a profile with rw and /data passes; a volume is no host folder"
expect no-memory "FAIL limits" "limits: no memory limit fails"
expect no-cpu "FAIL limits" "limits: no CPU limit fails"
expect quota "PASS -" "limits: a CPU quota counts as a CPU limit"
expect no-pids "FAIL limits" "limits: no PID limit fails"
expect zero-pids "FAIL limits" "limits: a PID limit of 0 (none) fails"
expect minus-pids "FAIL limits" "limits: a PID limit of -1 (none) fails"
expect host-net "FAIL host_namespace" "host_namespace: the host's network fails"
[[ "$(grep -A6 '^FAIL  t-host-all ' "$W/out" | grep -c 'namespace of the host')" == 5 ]] &&
  ok "host_namespace: network, PID, IPC, UTS and user are each named" || ko "host_namespace detail: $(grep -A6 'FAIL  t-host-all' "$W/out")"
expect own-net "PASS -" "host_namespace: no network passes"
expect shared-net "PASS -" "host_namespace: the network of another container passes"
expect device "FAIL device" "device: a device fails"
expect device-rule "FAIL device" "device: a device rule fails"
expect system-paths "FAIL system_paths" "system_paths: systempaths=unconfined (no masked, no read-only path) fails"
expect system-paths-masked "FAIL system_paths" "system_paths: no masked path fails"
expect system-paths-read-only "FAIL system_paths" "system_paths: no read-only path fails"
expect writable "FAIL writable_mount" "writable_mount: a host folder mounted writable fails"
expect volume "PASS -" "writable_mount: a writable volume passes"
expect sensitive "FAIL host_path" "host_path: /etc outside the profile fails"
expect parent "FAIL host_path" "host_path: a folder that holds /var/run fails"
expect checkout "PASS -" "host_path: a folder of the checkout passes"
expect agent "PASS -" "host_path: / and /proc of the profile pass; the socket is seen and cannot be opened"
grep -q '^PASS  t-agent .*socket seen through /, cannot open it' "$W/out" && ok "docker_socket: the line says the socket is seen and closed" ||
  ko "agent line: $(grep 't-agent ' "$W/out")"
expect agent-sys "FAIL host_path" "host_path: a sensitive path that the profile does not name fails"
expect socket "FAIL docker_socket" "docker_socket: the socket outside the profile fails"
expect proxy "PASS -" "docker_socket: the socket of the profile passes"
expect socket-group "FAIL docker_socket" "docker_socket: in the group of the socket, seen through /, fails"
expect socket-root "FAIL user,docker_socket" "docker_socket: root that sees the socket fails (and breaks its user)"
expect socket-dac "FAIL cap_bounding,docker_socket" "docker_socket: DAC_OVERRIDE, seen through /, fails"
expect user-root "FAIL user" "user: root where the profile says nonroot fails"
expect user-other "FAIL user" "user: another user than the profile's uid fails"
expect web-root "PASS -" "user: root where the profile says root passes"
expect kernel "FAIL kernel_view" "kernel_view: a main process that cannot be read fails"
[[ "$code" == 1 ]] && ok "a violation exits 1" || ko "exit $code with violations"
[[ "$(grep -cE '^(PASS|FAIL|WARN)  ' "$W/out")" == "$n" ]] && ok "one line per container ($n)" ||
  ko "lines: $(grep -cE '^(PASS|FAIL|WARN)  ' "$W/out") for $n containers"
grep -q '^==> Docker Engine 29.0.1, containerd v2.1.5, runc 1.3.4; AppArmor on, seccomp on$' "$W/out" &&
  ok "the versions of Docker Engine, containerd and runc are printed" || ko "versions: $(tail -2 "$W/out")"
! grep -q 'ops/deploy/t/docker-compose.yml' "$W/out" && ok "no label value but the service is printed" || ko "a label value is in the output"
grep -q "ps --quiet --no-trunc$" "$W/docker/ps-args" && ok "it reads the running containers only" || ko "ps: $(cat "$W/docker/ps-args")"

# The kernel's rules of a daemon without AppArmor and seccomp, as in some LXC containers.
reset
echo '["name=cgroupns"]' >"$W/docker/info"
container no-lsm seccomp=0 label=unconfined apparmor_profile=''
container no-lsm-config security_opt='["no-new-privileges:true","seccomp=unconfined"]'
check
expect no-lsm "PASS -" "a daemon without AppArmor and seccomp: the kernel's view of them is not required"
expect no-lsm-config "FAIL seccomp" "a daemon without seccomp: unconfined in the configuration still fails"
grep -q 'AppArmor off, seccomp off' "$W/out" && ok "the line of the versions says what the daemon has" || ko "daemon: $(tail -1 "$W/out")"

# Every container passes: exit 0. The deploy mode: a container without a profile is a warning.
reset
container ok
check && ok "no violation exits 0" || ko "exit with no violation: $(cat "$W/out")"
container unknown service=nope privileged=true
check --deploy
code=$?
expect unknown "WARN profile,privileged" "--deploy: a container without a profile is a warning with what it breaks"
[[ "$code" == 0 ]] && ok "--deploy: a container without a profile does not fail the check" || ko "--deploy exit $code"
container broken readonly=false
check --deploy
code=$?
expect broken "FAIL read_only" "--deploy: a container with a profile still fails"
[[ "$code" == 1 ]] && ok "--deploy: a violation of a container with a profile exits 1" || ko "--deploy exit $code with a violation"

# The report for Prometheus, written whole or not at all.
reset
container ok
container bad privileged=true
check --metrics "$W/metrics/container-runtime.prom"
m="$W/metrics/container-runtime.prom"
grep -qx 'nfhotel_container_runtime_violation{container="t-bad",rule="privileged"} 1' "$m" &&
  grep -qx 'nfhotel_container_runtime_containers 2' "$m" && grep -qx 'nfhotel_container_runtime_failed 1' "$m" &&
  grep -qE '^nfhotel_task_last_success_timestamp_seconds\{task="container_check"\} [0-9]{10}$' "$m" &&
  ok "the report names each broken rule, the counts and the time of the run" || ko "report: $(cat "$m" 2>&1)"
grep -q '# HELP nfhotel_task_last_success_timestamp_seconds When the task last succeeded.' "$m" &&
  ok "the time of the run has the HELP line of the other task reports" || ko "HELP line of the task report"
[[ "$(stat -c %a "$m")" == 644 ]] && [[ -z "$(find "$W/metrics" -name '.container-runtime.*')" ]] &&
  ok "the report is 0644 and leaves no temporary file" || ko "report mode $(stat -c %a "$m"); $(ls -A "$W/metrics")"
grep -qx 'nfhotel_container_runtime_info{engine="29.0.1",containerd="v2.1.5",runc="1.3.4"} 1' "$m" &&
  ok "the report names the versions of Docker Engine, containerd and runc (MIL-082)" || ko "versions: $(grep runtime_info "$m")"
# A quote or a backslash in a version cannot break the line of the report.
printf '%s\n' '29.0.1|containerd=v2"1 runc=1.3\4 ' >"$W/docker/version"
check --metrics "$m"
grep -qx 'nfhotel_container_runtime_info{engine="29.0.1",containerd="v2_1",runc="1.3_4"} 1' "$m" &&
  ok "a quote or a backslash in a version becomes _ in the report" || ko "versions: $(grep runtime_info "$m")"
echo "$VERSIONS" >"$W/docker/version"

# The floor of runc (MIL-083, ADR-0043): a runc below the first version of its line without the
# known escapes fails the check, one at or above it passes, and one that cannot be read is a
# warning; in the deploy mode a runc below the floor is a warning; the report says which.
reset
container ok
readonly ESCAPES="CVE-2025-31133, CVE-2025-52565 and CVE-2025-52881"
# with_runc <version> [<argument of the check>...]: run the check on a daemon with that runc.
with_runc() {
  local version="$1"
  shift
  printf '29.9.3|Engine=29.9.3 containerd=v2.4.2 runc=%s docker-init=0.19.0 \n' "$version" >"$W/docker/version"
  check "$@"
}
# Below it (<version>=<its floor>): lines older than every fixed one, Debian 13's runc among them,
# and the version just below the fix of each line.
for pair in 1.1.15+ds1=1.2.8 1.0.0-rc95=1.2.8 0.9.9=1.2.8 1.2.7=1.2.8 1.3.2=1.3.3; do
  with_runc "${pair%=*}"
  code=$?
  [[ "$code" == 1 ]] && grep -qxF "FAIL  runc ${pair%=*}: below ${pair#*=}, the first version without $ESCAPES" "$W/out" &&
    ok "floor: runc ${pair%=*} is below ${pair#*=} and fails" ||
    ko "floor: runc ${pair%=*}, exit $code: $(grep 'runc ' "$W/out" | tail -1)"
done
# At or above it: the fixed versions, later ones of their lines (10 is above 8, not below), later
# lines (1.10 is above 1.3), a leading v, and the runc of the VM (1.5.2 on 2026-11-13).
for version in 1.2.8 1.2.10 1.3.3 1.3.4 1.4.0 1.5.2 1.10.0 2.0.0 v1.3.3; do
  with_runc "$version"
  code=$?
  [[ "$code" == 0 ]] && grep -qxF "==> runc $version: at or above the floor of its line" "$W/out" &&
    ok "floor: runc $version passes" || ko "floor: runc $version, exit $code: $(grep 'runc ' "$W/out" | tail -1)"
done
# A version that cannot be read is a warning, not a failure.
for version in '?' abc 1.3 ''; do
  with_runc "$version"
  code=$?
  [[ "$code" == 0 ]] && grep -qxF "WARN  runc $version: the version cannot be read, so its floor is not checked" "$W/out" &&
    ok "floor: a runc version '$version' that cannot be read is a warning" ||
    ko "floor: runc '$version', exit $code: $(grep 'runc ' "$W/out" | tail -1)"
done
# The deploy cannot change the runtime, and neither can its rollback: there it is a warning.
with_runc 1.1.15+ds1 --deploy
code=$?
[[ "$code" == 0 ]] &&
  grep -qxF "WARN  runc 1.1.15+ds1: below 1.2.8, the first version without $ESCAPES; the deploy goes on, the timer's check fails" "$W/out" &&
  ok "floor: --deploy warns on a runc below the floor and exits 0" || ko "floor --deploy, exit $code: $(grep 'runc ' "$W/out" | tail -1)"
# A container that breaks its profile still fails, whatever runc is.
container broken privileged=true
with_runc 1.5.2
[[ $? == 1 ]] && ok "floor: a runc at or above the floor does not hide a broken container" || ko "floor with a broken container"
reset
container ok
with_runc 1.3.2 --metrics "$m"
grep -qx 'nfhotel_container_runtime_runc_below_floor 1' "$m" && ok "floor: the report says 1 for a runc below the floor" ||
  ko "report below: $(grep runc_below "$m")"
with_runc 1.1.15+ds1 --deploy --metrics "$m"
grep -qx 'nfhotel_container_runtime_runc_below_floor 1' "$m" && ok "floor: the report says 1 in the deploy mode too" ||
  ko "report --deploy: $(grep runc_below "$m")"
with_runc 1.5.2 --metrics "$m"
grep -qx 'nfhotel_container_runtime_runc_below_floor 0' "$m" && ok "floor: the report says 0 for a runc at or above it" ||
  ko "report at or above: $(grep runc_below "$m")"
with_runc abc --metrics "$m"
grep -q '^# HELP nfhotel_container_runtime_runc_below_floor ' "$m" && ! grep -q '^nfhotel_container_runtime_runc_below_floor' "$m" &&
  ok "floor: the report has no value for a runc that cannot be read" || ko "report unknown: $(grep runc_below "$m")"
echo "$VERSIONS" >"$W/docker/version"

# The lock of the deploy: a run while a deploy holds it is skipped and writes nothing.
rm -f "$m"
exec 8>"$W/lock"
flock 8
check --lock "$W/lock" --metrics "$m"
code=$?
grep -q '^skipped: a deploy holds' "$W/out" && [[ "$code" == 0 && ! -e "$m" ]] &&
  ok "--lock: a run during a deploy is skipped" || ko "--lock held: exit $code, $(cat "$W/out")"
exec 8>&-
check --lock "$W/lock" --metrics "$m"
[[ -s "$m" ]] && ok "--lock: a run without a deploy checks and writes the report" || ko "--lock free: $(cat "$W/out")"
flock --nonblock "$W/lock" true && ok "--lock: the check gives the lock back, so a deploy can start" ||
  ko "the lock is held after the check"
# A deploy that takes the lock while the containers are read: the run is not counted.
rm -f "$m"
FAKE_TAKE_LOCK="$W/lock" check --lock "$W/lock" --metrics "$m"
code=$?
grep -q '^skipped: a deploy took' "$W/out" && [[ "$code" == 0 && ! -e "$m" ]] &&
  ok "--lock: a deploy that starts during the check discards the run" || ko "lock taken: exit $code, $(cat "$W/out")"
flock --wait 10 "$W/lock" true
rm -f "$W/docker/lock-taken"

# The filter of docker ps, and a docker that cannot be read.
check --filter label=com.docker.compose.project=t
grep -q -- '--filter label=com.docker.compose.project=t' "$W/docker/ps-args" && ok "--filter goes to docker ps" ||
  ko "filter: $(cat "$W/docker/ps-args")"
touch "$W/docker/ps-fails"
check
[[ $? == 1 ]] && grep -q 'docker ps failed' "$W/out" && ok "a failed docker ps exits 1" || ko "docker ps failure: $(cat "$W/out")"
rm -f "$W/docker/ps-fails"

# A container that docker ps lists and that is gone before docker inspect is a warning.
reset
container ok
echo 0123456789abcdef >>"$W/docker/ps"
check
code=$?
grep -qx 'WARN  0123456789ab  -  -  removed while it was checked' "$W/out" && [[ "$code" == 0 ]] &&
  ok "a container removed during the check is a warning, not a failure" || ko "removed container: exit $code, $(cat "$W/out")"

# The socket proxy of Alloy, asked from inside the Alloy of the same compose files (MIL-081).
reset
alloy_files=/srv/nfhotel/ops/deploy/alloy/docker-compose.yml
container socket-proxy service=socket-proxy config_files="$alloy_files" uid=0 gid=0 \
  mounts='bind|/var/run/docker.sock|/var/run/docker.sock|false;'
echo 0a11a0 >"$W/docker/ps-alloy"
answers() { printf '%s\n' "$@" >"$W/docker/exec-answers"; }
answers "list 200" "networks 200" "archive 403" "export 403" "attach 403"
check
expect socket-proxy "PASS -" "proxy_allowlist: a proxy that refuses archive, export and attach passes"
grep -q 'proxy list 200, networks 200, archive 403, export 403, attach 403$' "$W/out" &&
  [[ "$(cat "$W/docker/exec-target")" == 0a11a0 ]] &&
  grep -q -- "--filter label=com.docker.compose.project.config_files=$alloy_files" "$W/docker/ps-alloy-args" &&
  ok "the proxy is asked from inside the Alloy of its compose files; the line shows the status codes" ||
  ko "probe: $(cat "$W/out") $(cat "$W/docker/ps-alloy-args" "$W/docker/exec-target" 2>&1)"
answers "list 200" "networks 200" "archive 404" "export 403" "attach 403"
check
expect socket-proxy "FAIL proxy_allowlist" "proxy_allowlist: a proxy that lets archive through fails"
grep -q 'the proxy answered archive from inside Alloy with 404, not 403' "$W/out" &&
  ok "the broken rule names the call and its answer" || ko "probe detail: $(cat "$W/out")"
answers "list 403" "networks 200" "archive 403" "export 403" "attach 403"
check
expect socket-proxy "FAIL proxy_allowlist" "proxy_allowlist: a proxy that refuses the list of Alloy fails"
touch "$W/docker/exec-fails"
check
expect socket-proxy "FAIL proxy_allowlist" "proxy_allowlist: a probe that gets no answer fails"
rm -f "$W/docker/exec-fails" "$W/docker/ps-alloy"
check
expect socket-proxy "PASS -" "proxy_allowlist: a proxy with no Alloy beside it is not asked"
grep -q 'proxy not asked (no Alloy)$' "$W/out" && ok "the line says that the proxy was not asked" ||
  ko "no Alloy: $(cat "$W/out")"

# Profiles that are not valid stop the check with exit 2.
bad_profiles() {
  printf '%s\n' "$1" >"$W/bad.tsv"
  PROFILES="$W/bad.tsv" check
  [[ $? == 2 ]] && ok "$2" || ko "$2: $(cat "$W/out")"
}
bad_profiles "$(printf 't\tapp\tnonroot\t-\tro\t-\t-')" "a row with seven columns is refused"
bad_profiles "$(printf 't\tapp\tnobody\t-\tro\t-\t-\t-')" "a user that is not nonroot, root or a number is refused"
bad_profiles "$(printf 't\tapp\tnonroot\tSYS_EVERYTHING\tro\t-\t-\t-')" "an unknown capability is refused"
bad_profiles "$(printf 't\tapp\tnonroot\t-\tro\t-\t-\tprivileges')" "an unknown rule in exempt is refused"
bad_profiles "$(printf 't\tapp\tnonroot\t-\tro\tetc\t-\t-')" "a host path that is not absolute is refused"
bad_profiles "$(printf 't\tapp\tnonroot\t-\tro\t-\t-\t-\nt\tapp\troot\t-\tro\t-\t-\t-')" "a profile named twice is refused"
PROFILES="$W/missing.tsv" check
[[ $? == 2 ]] && ok "a missing profiles file is refused" || ko "missing profiles: $(cat "$W/out")"

# The profiles of the repository are valid, and name every compose service under deploy/ that
# runs on the VM, in the monitoring LXC or on the backup host (proxmox is a test lab of CI).
reset
PROFILES="$OPS/security/container-profiles.tsv" check && ok "the profiles of the repository are valid" ||
  ko "repository profiles: $(head -3 "$W/out")"
missing=""
for file in "$OPS"/deploy/*/docker-compose.yml; do
  folder="$(basename "$(dirname "$file")")"
  [[ "$folder" == proxmox ]] && continue
  while read -r service; do
    awk -F'\t' -v f="$folder" -v s="$service" '$1 == f && $2 == s { found = 1 } END { exit !found }' \
      "$OPS/security/container-profiles.tsv" || missing+=" $folder/$service"
  done < <(awk '/^services:/ { s = 1; next } /^[^ #]/ { s = 0 }
    s && /^  [a-z0-9_-]+:/ { sub(/:.*/, ""); sub(/^ +/, ""); print }' "$file")
done
[[ -z "$missing" ]] && ok "every compose service under deploy/ has a profile" || ko "services without a profile:$missing"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
