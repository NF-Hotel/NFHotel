#!/usr/bin/env bash
# Start Alloy from deploy/alloy/docker-compose.yml with its pinned image and its hardening, on
# a Linux host, as scripts/deploy-services.sh does (ADR-0020, MIL-023 task 8). It checks that
# Alloy turns healthy and stays up, runs as 473 with the host's systemd-journal group, keeps
# the hardening, reads the journal and the job reports, logs no permission error, and is
# healthy again after a restart (its data volume holds across one).
#
# It also checks the path that matters: the log lines of a real container are found through the
# socket proxy and reach a real Loki (the pinned image and the configuration of deploy/monitoring).
#
# The socket proxy (ADR-0042, MIL-081) lets through only the calls of its allowlist. The test
# reads every call that Alloy makes from the log of the proxy, which runs at the level info here,
# and expects each to pass; it prints the list. Then it sends the matrix of ADR-0042 from a
# container on the internal network: each call of the list answers 200, and each call that the
# proxy must refuse answers 403 (archive, export, attach over a WebSocket, top, stats, changes,
# events, info, version, the image, volume, system, secret, plugin, swarm, node, service, task,
# config and distribution sections, and the writes), with and without an API version in the path,
# with an encoded slash, a dot segment and an upgrade header. Last, the runtime check of MIL-080
# reads both containers and asks the proxy from inside Alloy.
#
# It found two failures of 2026-10-23, both with a healthy-looking Alloy:
#   - as root without DAC_OVERRIDE, Alloy could not enter /var/lib/alloy and stopped with
#     "mkdir /var/lib/alloy/data: permission denied";
#   - the proxy refused GET /networks, which discovery.docker needs, so no container log
#     reached Loki ("error while computing network labels ... 403 Forbidden").
#
# Run it as root on a Linux host with Docker and systemd:
#   sudo bash ops/tests/test-alloy-start.sh [--upstream]
#   --upstream  run the proxy with the configuration of the upstream image and the switches that
#               the compose file had until MIL-081 (CONTAINERS=1, NETWORKS=1, POST=0). The matrix
#               and the runtime check then fail on archive, export and attach: this shows that the
#               test catches finding 2 of MIL-081. The run is expected to exit 1.
# It starts the compose project t-alloy, a Loki (t-alloy-loki) and a container that logs
# (t-alloy-logger), and removes them, their volumes and its test report at the end. Metrics go
# to a closed local port and logs to that Loki, so nothing leaves the host.
# Exit:  0 every check passed; 1 a check failed; 2 usage error
# Needs: bash 4+, root, docker with the compose plugin (2.24 or later for --upstream), getent

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

upstream=0
case "${1:-}" in
  "") ;;
  --upstream) upstream=1 ;;
  *)
    echo "usage: test-alloy-start.sh [--upstream]" >&2
    exit 2
    ;;
esac

OPS="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
W="$(mktemp -d)"
report=/var/lib/nfhotel-metrics/t-alloy.prom
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
# The internal network of the applications, which push their metrics to Alloy (MIL-034). The
# proxy logs every request, so the test can read the calls of Alloy.
export NFHOTEL_JOURNAL_GID ALLOY_ENV_FILE="$W/alloy.env" NFHOTEL_API_NETWORK=t-alloy-api NFHOTEL_PROXY_LOG_LEVEL=info
NFHOTEL_JOURNAL_GID="$(getent group systemd-journal | cut -d: -f3)"
[[ "$NFHOTEL_JOURNAL_GID" =~ ^[0-9]+$ ]] || {
  echo "error: this host has no group systemd-journal" >&2
  exit 1
}
files=(-f "$OPS/deploy/alloy/docker-compose.yml")
if ((upstream)); then
  # The upstream image of the FROM line, with the switches of the compose file before MIL-081.
  upstream_image="$(sed -n 's/^FROM //p' "$OPS/deploy/alloy/socket-proxy/Dockerfile" | head -n 1)"
  printf '%s\n' 'services:' '  socket-proxy:' '    build: !reset null' "    image: $upstream_image" \
    '    environment:' '      CONTAINERS: "1"' '      NETWORKS: "1"' '      POST: "0"' >"$W/upstream.yml"
  files+=(-f "$W/upstream.yml")
fi
compose() { docker compose -p t-alloy "${files[@]}" "$@"; }
image() { sed -n "s|^    image: \(.*$1.*\)$|\1|p" "$2" | head -n 1; }
LOKI="$(image grafana/loki "$OPS/deploy/monitoring/docker-compose.yml")"
readonly LOKI CURL=curlimages/curl:8.10.1
marker="t-alloy-marker-$$-$RANDOM"
# curl_on <network> <curl arguments>: curl from a throwaway container on that network.
curl_on() {
  local net="$1"
  shift
  docker run --rm --network "$net" "$CURL" -s "$@"
}
cleanup() {
  docker rm -f t-alloy-loki t-alloy-logger >/dev/null 2>&1
  docker volume rm -f t-alloy-loki-data >/dev/null 2>&1
  docker network rm t-alloy-api >/dev/null 2>&1
  compose down -v >/dev/null 2>&1
  rm -f "$report"
  rm -rf "$W"
}
trap cleanup EXIT

# As on nf-hotel: the journal folder of systemd, and a job report as backup-database.sh writes it.
[[ -d /var/log/journal ]] || install -d -o root -g systemd-journal -m 2755 /var/log/journal
install -d -m 0755 /var/lib/nfhotel-metrics
printf '%s\n' 'nfhotel_task_last_success_timestamp_seconds{task="t_alloy"} 1761210000' >"$report"
chmod 0644 "$report"
printf '%s\n' "PROMETHEUS_REMOTE_WRITE_URL=http://127.0.0.1:9/api/v1/write" \
  "LOKI_PUSH_URL=http://t-alloy-loki:3100/loki/api/v1/push" >"$ALLOY_ENV_FILE"

# A real Loki on Alloy's egress network, and a container that logs a marker every second.
docker network create --internal t-alloy-api >/dev/null
compose up --no-start >/dev/null 2>&1
docker run -d --name t-alloy-loki --network t-alloy_egress -v t-alloy-loki-data:/loki \
  -v "$OPS/deploy/monitoring/loki:/etc/loki:ro" "$LOKI" -config.file=/etc/loki/config.yml >/dev/null
docker run -d --name t-alloy-logger --entrypoint sh "$CURL" \
  -c "while :; do echo $marker; sleep 1; done" >/dev/null
for _ in $(seq 1 30); do
  [[ "$(curl_on t-alloy_egress http://t-alloy-loki:3100/ready)" == ready* ]] && break
  sleep 3
done

# --build: the proxy is built from its Dockerfile and haproxy.cfg at every run, so an image of an
# earlier run on the same host cannot stand in for the configuration under test.
compose up -d --build --wait --wait-timeout 180 >"$W/up" 2>&1 && ok "Alloy turns healthy with the pinned image" ||
  ko "up: $(tail -3 "$W/up"); $(compose logs --tail 10 alloy 2>&1)"
c="$(compose ps -q alloy)"
get() {
  docker exec "$c" bash -c 'exec 3<>/dev/tcp/127.0.0.1/12345 && printf "GET %s HTTP/1.0\r\n\r\n" "$1" >&3 && cat <&3' _ "$1" 2>/dev/null
}

[[ "$(docker inspect -f '{{.RestartCount}} {{.State.Status}}' "$c" 2>/dev/null)" == "0 running" ]] &&
  ok "Alloy stays up, with no restart" || ko "state: $(docker inspect -f '{{.RestartCount}} {{.State.Status}}' "$c")"
ids="$(docker exec "$c" id 2>/dev/null)"
[[ "$ids" == "uid=473(alloy) gid=473(alloy)"* && "$ids" == *"$NFHOTEL_JOURNAL_GID"* ]] &&
  ok "Alloy runs as 473 with the journal group $NFHOTEL_JOURNAL_GID" || ko "id: $ids"
hardening="$(docker inspect -f '{{.HostConfig.ReadonlyRootfs}} {{.HostConfig.CapDrop}} {{.HostConfig.SecurityOpt}}' "$c")"
[[ "$hardening" == "true [ALL] [no-new-privileges:true]" ]] &&
  ok "the hardening holds: read-only, no capabilities, no new privileges" || ko "hardening: $hardening"

components="$(get /api/v0/web/components)"
[[ "$components" == *loki.source.journal* && "$components" == *prometheus.exporter.unix* ]] &&
  ok "the journal and host components are loaded" || ko "components: ${components:0:200}"
[[ "$components" != *'"state":"unhealthy"'* ]] && ok "no component is unhealthy" ||
  ko "unhealthy: $(grep -o '"localID":"[^"]*"[^}]*"state":"unhealthy"' <<<"$components" | head -3)"
# Read into a variable first: under pipefail, grep -q would end the pipe early and fail it.
metrics="$(get /api/v0/component/prometheus.exporter.unix.host/metrics)"
[[ "$metrics" == *'task="t_alloy"'* ]] &&
  ok "Alloy reads the job reports in /var/lib/nfhotel-metrics" ||
  ko "the job report is not in the host metrics: $(grep -E 'textfile' <<<"$metrics" | head -3)"
logs="$(compose logs alloy 2>&1)"
[[ "${logs,,}" != *"permission denied"* ]] && ok "Alloy logs no permission error" ||
  ko "logs: $(grep -i 'permission denied' <<<"$logs" | head -3)"

# The path that a healthy Alloy alone does not prove: discovery through the proxy, then Loki.
found=""
for _ in $(seq 1 24); do
  found="$(curl_on t-alloy_egress -G http://t-alloy-loki:3100/loki/api/v1/query_range \
    --data-urlencode 'query={container="t-alloy-logger", host="nf-hotel"}' --data-urlencode limit=5)"
  [[ "$found" == *"$marker"* ]] && break
  sleep 5
done
[[ "$found" == *"$marker"* ]] && ok "the log lines of a real container reach Loki through the proxy" ||
  ko "no log line of t-alloy-logger in Loki: ${found:0:200}"
logs="$(compose logs alloy 2>&1)"
[[ "$logs" != *"Unable to refresh target groups"* && "$logs" != *"403 Forbidden"* ]] &&
  ok "the Docker discovery logs no refused call" || ko "discovery: $(grep -m 2 -E 'target groups|403 Forbidden' <<<"$logs")"

# The restart ends the log streams of Alloy, so the proxy logs them now, also with the configuration
# of the upstream image, which logs a request only when its answer ends.
compose restart alloy >/dev/null 2>&1
for _ in $(seq 1 24); do
  [[ "$(docker inspect -f '{{.State.Health.Status}}' "$c" 2>/dev/null)" == healthy ]] && break
  sleep 5
done
[[ "$(docker inspect -f '{{.State.Health.Status}}' "$c")" == healthy ]] &&
  ok "Alloy is healthy again after a restart, with its data volume" || ko "after restart: $(compose logs --tail 10 alloy 2>&1)"

# The calls of Alloy (MIL-081 task 1), read before the runtime check asks the proxy from Alloy's
# address. Each line of the proxy's log from Alloy's address on the internal network gives the
# status ($6) and the request; HAProxy listens on IPv6 too, so the address comes as ::ffff:<IPv4>.
# The list keeps the status, the method and the path, with the container id as <id>, the API
# version as /v<api> and no query.
alloy_ip="$(docker inspect -f '{{with index .NetworkSettings.Networks "t-alloy_internal"}}{{.IPAddress}}{{end}}' "$c")"
calls="$(compose logs --no-log-prefix socket-proxy 2>&1 | awk -v ip="$alloy_ip:" '{ source = $1; sub(/^::ffff:/, "", source) }
  index(source, ip) == 1 && NF >= 9 {
    method = $(NF - 2); path = $(NF - 1); sub(/^"/, "", method); sub(/\?.*/, "", path); print $6, method, path
  }' | sed -E 's#[0-9a-f]{64}#<id>#g; s#^([0-9]+ [A-Z]+ )/v1\.[0-9]+/#\1/v<api>/#' | sort -u)"
echo "      the calls of Alloy through the proxy (status, method, path):"
mapfile -t call_lines <<<"${calls:-none}"
printf '        %s\n' "${call_lines[@]}"
[[ -n "$calls" ]] && ok "the log of the proxy holds the calls of Alloy" || ko "no call of Alloy ($alloy_ip) in the log of the proxy"
refused="$(grep -v '^200 ' <<<"$calls")"
[[ -n "$calls" && -z "$refused" ]] && ok "every call of Alloy passes the proxy" ||
  ko "the proxy refused calls of Alloy: $(tr '\n' ';' <<<"$refused")"
level="$(env -u NFHOTEL_PROXY_LOG_LEVEL docker compose -p t-alloy -f "$OPS/deploy/alloy/docker-compose.yml" config socket-proxy 2>/dev/null |
  sed -n 's/^ *LOG_LEVEL: //p')"
[[ "$level" == warning ]] && ok "on the VM the proxy logs at the level warning, not each request of Alloy" ||
  ko "the default log level of the proxy is '$level', not warning"

# An application on its internal network pushes a metric over OTLP/HTTP, as the web application
# and the API do (MIL-034); Alloy accepts it.
now="$(date +%s)000000000"
metric='{"resourceMetrics":[{"resource":{"attributes":[{"key":"service.name","value":{"stringValue":"t-app"}}]},'
metric+='"scopeMetrics":[{"metrics":[{"name":"t_alloy_requests","sum":{"aggregationTemporality":2,"isMonotonic":true,'
metric+='"dataPoints":[{"asInt":"1","timeUnixNano":"'"$now"'"}]}}]}]}]}'
otlp="$(curl_on t-alloy-api -o /dev/null -w '%{http_code}' -H 'Content-Type: application/json' -d "$metric" http://alloy:4318/v1/metrics)"
[[ "$otlp" == 200 ]] && ok "Alloy accepts OTLP metrics from the applications' network" || ko "OTLP push answered $otlp"

# The matrix of ADR-0042, sent from a container on the internal network of the proxy. A request is
# "METHOD PATH [HEADER|HEADER...]"; the answers are the status codes, one line per request, in
# order. The refused calls name a container that does not exist, so a proxy that lets one through
# gets 404 from Docker, and no data; the calls of the list name the logger.
probe_proxy() {
  docker run --rm -i --network t-alloy_internal --entrypoint sh "$CURL" -c '
    while read -r method path headers; do
      set -- -s -o /dev/null -w "%{http_code}" --path-as-is --max-time 10
      if [ "$method" = HEAD ]; then set -- "$@" -I; else set -- "$@" -X "$method"; fi
      old_ifs="$IFS"; IFS="|"
      for header in $headers; do set -- "$@" -H "$header"; done
      IFS="$old_ifs"
      echo "$(curl "$@" "http://socket-proxy:2375$path")"
    done'
}
logger="$(docker inspect -f '{{.Id}}' t-alloy-logger)"
zero=0000000000000000000000000000000000000000000000000000000000000000
# A valid key of 16 bytes (the sample of RFC 6455): HAProxy answers a shorter key 400 as a bad
# request before any rule, in both configurations, and the upgrade rule would go untested.
ws='Upgrade:websocket|Connection:Upgrade|Sec-WebSocket-Version:13|Sec-WebSocket-Key:dGhlIHNhbXBsZSBub25jZQ=='
allowed=()
for v in "" /v1.44; do
  allowed+=("GET $v/_ping" "HEAD $v/_ping" "GET $v/containers/json" "GET $v/networks"
    "GET $v/containers/$logger/json" "GET $v/containers/$logger/logs?stdout=1&tail=1")
done
refused=()
for v in "" /v1.44; do
  refused+=("GET $v/containers/$zero/archive?path=/" "HEAD $v/containers/$zero/archive?path=/"
    "GET $v/containers/$zero/export" "GET $v/containers/$zero/attach/ws?stream=1&stdin=1 $ws"
    "GET $v/containers/$zero/top" "GET $v/containers/$zero/stats?stream=0" "GET $v/containers/$zero/changes"
    "GET $v/events" "GET $v/info" "GET $v/version" "GET $v/images/json" "GET $v/volumes" "GET $v/system/df"
    "GET $v/secrets" "GET $v/plugins" "GET $v/swarm" "GET $v/nodes" "GET $v/services" "GET $v/tasks"
    "GET $v/configs" "GET $v/distribution/alpine/json"
    "POST $v/containers/create" "POST $v/containers/$zero/start" "POST $v/containers/$zero/stop"
    "POST $v/containers/$zero/kill" "POST $v/containers/$zero/exec" "POST $v/build" "POST $v/commit"
    "POST $v/auth" "POST $v/networks/create" "DELETE $v/containers/$zero" "PUT $v/containers/$zero/archive?path=/")
done
# The path as it arrives: an encoded slash, a dot segment, a trailing slash and an upgrade of a
# call of the list are refused too.
refused+=("GET /containers%2Fjson" "GET /containers/$logger%2Farchive?path=/"
  "GET /v1.44/containers/$logger/json/../archive?path=/" "GET /containers/json/"
  "GET /containers/json $ws")
requests=("${allowed[@]}" "${refused[@]}")
mapfile -t codes < <(printf '%s\n' "${requests[@]}" | probe_proxy)
# wrong_answers <first> <count> <code>: the requests from index first on that did not get code.
# The headers of an upgrade are left out of the line, which names the upgrade instead.
wrong_answers() {
  local i request label
  for ((i = $1; i < $1 + $2; i++)); do
    request="${requests[i]}"
    label="${request%% Upgrade*}"
    [[ "$label" == "$request" ]] || label+=" (an upgrade to a WebSocket)"
    [[ "${codes[i]:-}" == "$3" ]] || printf '%s answered %s; ' "$label" "${codes[i]:-nothing}"
  done
}
wrong="$(wrong_answers 0 "${#allowed[@]}" 200)"
[[ -z "$wrong" ]] && ok "the proxy allows the ${#allowed[@]} calls of the list, with and without an API version" ||
  ko "the proxy refused calls of the list: $wrong"
wrong="$(wrong_answers "${#allowed[@]}" "${#refused[@]}" 403)"
[[ -z "$wrong" ]] && ok "the proxy refuses the other ${#refused[@]} calls with 403" ||
  ko "the proxy let through calls it must refuse: $wrong"

# The runtime check of MIL-080 over the two containers, with its probe of the proxy from inside
# Alloy (MIL-081 task 4).
bash "$OPS/scripts/check-container-runtime.sh" --filter label=com.docker.compose.project=t-alloy >"$W/check" 2>&1
code=$?
[[ "$code" == 0 ]] && grep -q 'proxy list 200, networks 200, archive 403, export 403, attach 403' "$W/check" &&
  ok "the runtime check passes both containers, and the proxy answers its probe from inside Alloy" ||
  ko "runtime check (exit $code): $(grep -E '^(FAIL|WARN)|proxy' "$W/check" | head -6)"
grep -E '^(PASS|FAIL|WARN)|^      ' "$W/check" | sed 's/^/      /'

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
