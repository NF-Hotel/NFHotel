#!/usr/bin/env bash
# Live test of scripts/check-container-runtime.sh (MIL-080 task 3), with real Docker and the
# kernel's view of real containers. Containers that keep their test profile pass, and one container
# per broken rule fails with that rule: privileged, an added capability, root where the profile says
# nonroot, a writable host folder, the Docker socket, the host's network, seccomp=unconfined, no
# no-new-privileges, no profile, and the group of the socket next to a mount of /. Then the profiles
# of the repository are compared with the compose files that docker compose config renders, for
# every service, also the ones that CI does not start: the capabilities, the root filesystem, the
# user, no-new-privileges, the limits, the sensitive host paths and the writable host folders.
#
# Run it as root on a Linux host with Docker:
#   sudo bash ops/tests/test-container-runtime-live.sh
# It starts containers named t-live-*, with the label nfhotel.test=live-<pid>, and a folder in /tmp,
# and removes them at the end. Every container has no network, except the one on the host's network.
# Env:   LIVE_IMAGE (default curlimages/curl:8.10.1), an image with sleep
# Exit:  0 every check passed; 1 a check failed
# Needs: bash 4+, root, docker with the compose plugin, jq

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

OPS="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CHECK="$OPS/scripts/check-container-runtime.sh"
PROFILES="$OPS/security/container-profiles.tsv"
IMAGE="${LIVE_IMAGE:-curlimages/curl:8.10.1}"
LABEL="nfhotel.test=live-$$"
W="$(mktemp -d)"
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

[[ "$(id -u)" == 0 ]] || {
  echo "error: run it as root" >&2
  exit 1
}
cleanup() {
  docker ps -aq --filter "label=$LABEL" | xargs -r docker rm -f >/dev/null 2>&1
  rm -rf "$W"
}
trap cleanup EXIT

printf 'live\tok\tnonroot\t-\tro\t-\t-\t-\nlive\treader\tnonroot\t-\tro\t/proc\t-\t-\nlive\tagent\tnonroot\t-\tro\t/\t-\t-\n' \
  >"$W/profiles.tsv"
mkdir -p "$W/data"
socket_gid="$(stat -L -c %g /var/run/docker.sock)"

# start <name> <service or -> [docker run options...]: a container that keeps the profile
# live/ok unless the options change it, sleeping as user 10001 with no network. The option
# --no-hardening leaves out no-new-privileges.
start() {
  local name="$1" service="$2" a
  shift 2
  local labels=(--label "$LABEL") hardening=(--cap-drop ALL --read-only --memory 32m --cpus 0.1 --pids-limit 16)
  local defaults=(--user 10001:10001 --network none) options=() nnp=(--security-opt no-new-privileges:true)
  if [[ "$service" != - ]]; then
    labels+=(--label "com.docker.compose.service=$service"
      --label "com.docker.compose.project.config_files=/live/ops/deploy/live/docker-compose.yml")
  fi
  for a in "$@"; do
    case "$a" in
      --no-hardening) nnp=() ;;
      *) options+=("$a") ;;
    esac
  done
  [[ " ${options[*]} " == *" --user "* ]] && defaults=(--network none)
  [[ " ${options[*]} " == *" --network "* ]] && defaults=(--user 10001:10001)
  docker run -d --name "t-live-$name" "${labels[@]}" "${hardening[@]}" "${nnp[@]}" "${defaults[@]}" \
    "${options[@]}" --entrypoint sleep "$IMAGE" 600 >/dev/null 2>"$W/start-$name" ||
    ko "t-live-$name did not start: $(cat "$W/start-$name")"
}

docker image inspect "$IMAGE" >/dev/null 2>&1 || docker pull -q "$IMAGE" >/dev/null || {
  echo "error: cannot pull $IMAGE" >&2
  exit 1
}
echo "== the check over containers that keep or break their profile"
start ok ok
start reader reader -v /proc:/host/proc:ro
start agent agent -v /:/host/root:ro
start privileged ok --privileged
start capability ok --cap-add NET_ADMIN
start root ok --user 0:0
start writable ok -v "$W/data:/data"
start socket ok -v /var/run/docker.sock:/var/run/docker.sock:ro
start host-network ok --network host
start seccomp ok --security-opt seccomp=unconfined
start no-nnp ok --no-hardening
start no-profile -
start socket-group agent -v /:/host/root:ro --group-add "$socket_gid"

bash "$CHECK" --profiles "$W/profiles.tsv" --filter "label=$LABEL" >"$W/out" 2>&1
code=$?
sed 's/^/      /' "$W/out"
# expect <name> <status> [<rule>]: the line of the container has the status and, if given, the rule.
expect() {
  local line rules
  line="$(awk -v c="t-live-$1" '$2 == c { print $1, $4 }' "$W/out")"
  rules="${line#* }"
  if [[ "${line%% *}" == "$2" && (-z "${3:-}" || ",$rules," == *",$3,"*) ]]; then
    ok "t-live-$1: $2${3:+ with $3}"
  else
    ko "t-live-$1: expected $2${3:+ with $3}, got '$line'"
  fi
}
expect ok PASS
expect reader PASS
expect agent PASS
grep -q '^PASS  t-live-agent .*socket seen through /, cannot open it' "$W/out" &&
  ok "the container that mounts / sees the socket and cannot open it" || ko "agent: $(grep t-live-agent "$W/out")"
expect privileged FAIL privileged
expect capability FAIL cap_add
expect capability FAIL cap_bounding
expect root FAIL user
expect writable FAIL writable_mount
expect socket FAIL docker_socket
expect host-network FAIL host_namespace
expect seccomp FAIL seccomp
expect no-nnp FAIL no_new_privileges
expect no-profile FAIL profile
expect socket-group FAIL docker_socket
[[ "$code" == 1 ]] && ok "the check exits 1" || ko "the check exited $code"
[[ "$(grep -cE '^(PASS|FAIL|WARN)  t-live-' "$W/out")" == 13 ]] && ok "the check read the 13 containers of the test" ||
  ko "the check read $(grep -cE '^(PASS|FAIL|WARN)  t-live-' "$W/out") containers"
grep -qE '^PASS  t-live-ok .*uid 10001, caps none, seccomp filter' "$W/out" &&
  ok "the kernel's view: uid 10001, no capability, seccomp in filter mode" || ko "kernel view: $(grep 't-live-ok ' "$W/out")"
if docker info --format '{{json .SecurityOptions}}' | grep -q 'name=apparmor'; then
  grep -qE '^PASS  t-live-ok .*apparmor docker-default \(enforce\)' "$W/out" &&
    ok "the kernel's view: AppArmor docker-default in enforce mode" || ko "AppArmor: $(grep 't-live-ok ' "$W/out")"
fi
bash "$CHECK" --profiles "$W/profiles.tsv" --filter "label=$LABEL" --deploy >"$W/deploy" 2>&1
[[ "$(awk '$2 == "t-live-no-profile" { print $1 }' "$W/deploy")" == WARN ]] &&
  ok "--deploy: the container without a profile is a warning" || ko "--deploy: $(grep no-profile "$W/deploy")"

echo "== the profiles of the repository against the compose files"
# What each compose file needs to be read: the example file in place of the git-ignored settings
# file and the values the deploy passes, as tests/test-service-networks.sh reads them. proxmox is a
# test lab of CI, not a host the check reads.
declare -A SETTINGS=(
  ["nginx"]="NGINX_ENV_FILE=nginx.env.example"
  ["nfhotel-web"]="NFHOTEL_WEB_ENV_FILE=nfhotel-web.env.example"
  ["nfhotel-api"]="NFHOTEL_API_ENV_FILE=nfhotel-api.env.example"
  ["market-analysis"]="MARKET_ANALYSIS_ENV_FILE=market-analysis.env.example"
  ["benchmark"]="BENCHMARK_ENV_FILE=benchmark.env.example API_TOKEN=test-token"
  ["ollama"]="OLLAMA_ENV_FILE=ollama.env.example"
  ["postgres"]="POSTGRES_ENV_FILE=postgres.env.example"
  ["postgres-tailnet"]="NFHOTEL_TAILNET_IP=100.64.0.1"
  ["cloudflared"]=""
  ["alloy"]="ALLOY_ENV_FILE=alloy.env.example NFHOTEL_JOURNAL_GID=999"
  ["monitoring"]="MONITORING_BIND=127.0.0.1"
  ["backup-target"]="BACKUP_PUBLIC_KEY=test BACKUP_TARGET_BIND=127.0.0.1"
)
# The sensitive host paths as the check defines them, read from the check itself.
eval "$(sed -n '/^readonly SENSITIVE=(/,/)$/p; /^within() {/,/^}$/p; /^is_sensitive() {/,/^}$/p' "$CHECK")"
if ! declare -F is_sensitive >/dev/null; then
  echo "error: cannot read is_sensitive from $CHECK" >&2
  exit 1
fi
# profile_field <folder> <service> <n>: a column of a profile row, empty if there is no row.
profile_field() { awk -F'\t' -v f="$1" -v s="$2" -v n="$3" '$1 == f && $2 == s { print $n }' "$PROFILES"; }
# sorted <comma list>: the list sorted, - for none.
sorted() {
  local out
  out="$(tr ',' '\n' <<<"$1" | sed '/^-*$/d' | LC_ALL=C sort | paste -sd, -)"
  echo "${out:--}"
}
# differences <folder> <service> <service JSON>: what the compose file sets beyond or below the
# profile, one clause per difference; nothing when they match.
differences() {
  local folder="$1" service="$2" s="$3" user caps rootfs paths writable exempt
  local drops_all added root compose_user uid host="" source rw
  user="$(profile_field "$folder" "$service" 3)" caps="$(profile_field "$folder" "$service" 4)"
  rootfs="$(profile_field "$folder" "$service" 5)" paths="$(profile_field "$folder" "$service" 6)"
  writable="$(profile_field "$folder" "$service" 7)" exempt=",$(profile_field "$folder" "$service" 8),"
  drops_all="$(jq -r '(.cap_drop // []) | map(sub("^CAP_"; "")) | index("ALL") != null' <<<"$s")"
  if [[ "$caps" == default ]]; then
    [[ "$drops_all" == false ]] || echo "the profile keeps the default capabilities, the compose file drops them"
  else
    added="$(jq -r '(.cap_add // []) | map(sub("^CAP_"; "")) | join(",")' <<<"$s")"
    [[ "$(sorted "$added")" == "$(sorted "$caps")" ]] || echo "cap_add ${added:--}, the profile's $caps"
    [[ "$drops_all" == true || "$exempt" == *,cap_drop,* ]] || echo "no cap_drop ALL"
  fi
  root="$(jq -r 'if .read_only == true then "ro" else "rw" end' <<<"$s")"
  [[ "$root" == "$rootfs" ]] || echo "the root filesystem is $root, the profile's $rootfs"
  compose_user="$(jq -r '.user // ""' <<<"$s")"
  uid="${compose_user%%:*}"
  if [[ "$uid" == 0 || "$uid" == root ]]; then
    [[ "$user" == root ]] || echo "user $compose_user, the profile's $user"
  elif [[ -n "$uid" ]]; then
    # A user ID must be the profile's; a name can only be told from root.
    [[ "$user" == nonroot || "$user" == "$uid" || (! "$uid" =~ ^[0-9]+$ && "$user" =~ ^[0-9]+$) ]] ||
      echo "user $compose_user, the profile's $user"
  fi
  [[ "$exempt" == *,no_new_privileges,* ||
    "$(jq -r '[(.security_opt // [])[] | select(test("^no-new-privileges([:=]true)?$"))] | length' <<<"$s")" != 0 ]] ||
    echo "no no-new-privileges"
  [[ "$(jq -r '.privileged // false' <<<"$s")" == false ]] || echo "privileged"
  [[ "$(jq -r '[(.security_opt // [])[] | select(test("^(seccomp|apparmor)[:=]unconfined$"))] | length' <<<"$s")" == 0 ]] ||
    echo "seccomp or AppArmor unconfined"
  [[ "$exempt" == *,host_namespace,* ||
    "$(jq -r '[.network_mode, .pid, .ipc, .uts, .userns_mode] | map(select(. == "host")) | length' <<<"$s")" == 0 ]] ||
    echo "a namespace of the host"
  [[ "$exempt" == *,device,* || "$(jq -r '(.devices // []) | length' <<<"$s")" == 0 ]] || echo "a device"
  [[ "$exempt" == *,limits,* || "$(jq -r '[(.mem_limit // .deploy.resources.limits.memory),
      (.cpus // .deploy.resources.limits.cpus), (.pids_limit // .deploy.resources.limits.pids)]
      | map(select(. != null and . != 0 and . != "0")) | length' <<<"$s")" == 3 ]] ||
    echo "a limit is missing (mem_limit, cpus, pids_limit)"
  while IFS= read -r source; do
    is_sensitive "$source" && host+="${host:+,}$source"
  done < <(jq -r '(.volumes // [])[] | select(.type == "bind") | .source' <<<"$s")
  [[ "$(sorted "$host")" == "$(sorted "$paths")" ]] || echo "sensitive host paths ${host:--}, the profile's $paths"
  rw="$(jq -r '[(.volumes // [])[] | select(.type == "bind" and .read_only != true) | .target] | join(",")' <<<"$s")"
  [[ "$(sorted "$rw")" == "$(sorted "$writable")" ]] || echo "writable host folders ${rw:--}, the profile's $writable"
}
for file in "$OPS"/deploy/*/docker-compose.yml; do
  folder="$(basename "$(dirname "$file")")"
  [[ "$folder" == proxmox ]] && continue
  if [[ ! -v "SETTINGS[$folder]" ]]; then
    ko "$folder: this test has no settings to read its compose file"
    continue
  fi
  # shellcheck disable=SC2086 # the settings are several assignments, split on purpose
  if ! json="$(cd "$OPS/deploy/$folder" && env ${SETTINGS[$folder]} docker compose -f docker-compose.yml config --format json 2>"$W/config")"; then
    ko "$folder: docker compose config failed: $(head -2 "$W/config")"
    continue
  fi
  for service in $(jq -r '.services | keys[]' <<<"$json"); do
    if [[ -z "$(profile_field "$folder" "$service" 3)" ]]; then
      ko "$folder/$service has no profile"
      continue
    fi
    found="$(differences "$folder" "$service" "$(jq -c --arg s "$service" '.services[$s]' <<<"$json")" | paste -sd ';' -)"
    [[ -z "$found" ]] && ok "$folder/$service matches its profile" || ko "$folder/$service: $found"
  done
done
# Every row names a service of a compose file.
while IFS=$'\t' read -r folder service _; do
  [[ -z "$folder" || "$folder" == \#* ]] && continue
  grep -qE "^  $service:" "$OPS/deploy/$folder/docker-compose.yml" 2>/dev/null ||
    ko "the profile $folder/$service names no compose service"
done <"$PROFILES"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
