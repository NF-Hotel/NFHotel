#!/usr/bin/env bash
# Start Alloy from deploy/alloy/docker-compose.yml with its pinned image and its hardening, on
# a Linux host, as scripts/deploy-services.sh does (ADR-0020, MIL-023 task 8). It checks that
# Alloy turns healthy and stays up, runs as 473 with the host's systemd-journal group, keeps
# the hardening, reads the journal and the job reports, logs no permission error, and is
# healthy again after a restart (its data volume holds across one).
#
# It also checks the path that matters: the log lines of a real container are found through the
# restricted socket proxy and reach a real Loki (the pinned image and the configuration of
# deploy/monitoring), and the proxy still refuses writes and the calls it does not allow.
#
# It found two failures of 2026-10-23, both with a healthy-looking Alloy:
#   - as root without DAC_OVERRIDE, Alloy could not enter /var/lib/alloy and stopped with
#     "mkdir /var/lib/alloy/data: permission denied";
#   - the proxy refused GET /networks, which discovery.docker needs, so no container log
#     reached Loki ("error while computing network labels ... 403 Forbidden").
#
# Run it as root on a Linux host with Docker and systemd:
#   sudo bash ops/tests/test-alloy-start.sh
# It starts the compose project t-alloy, a Loki (t-alloy-loki) and a container that logs
# (t-alloy-logger), and removes them, their volumes and its test report at the end. Metrics go
# to a closed local port and logs to that Loki, so nothing leaves the host.
# Exit:  0 every check passed; 1 a check failed
# Needs: bash 4+, root, docker with the compose plugin, getent

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

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
export NFHOTEL_JOURNAL_GID ALLOY_ENV_FILE="$W/alloy.env"
NFHOTEL_JOURNAL_GID="$(getent group systemd-journal | cut -d: -f3)"
[[ "$NFHOTEL_JOURNAL_GID" =~ ^[0-9]+$ ]] || {
  echo "error: this host has no group systemd-journal" >&2
  exit 1
}
compose() { docker compose -p t-alloy -f "$OPS/deploy/alloy/docker-compose.yml" "$@"; }
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
compose up --no-start >/dev/null 2>&1
docker run -d --name t-alloy-loki --network t-alloy_egress -v t-alloy-loki-data:/loki \
  -v "$OPS/deploy/monitoring/loki:/etc/loki:ro" "$LOKI" -config.file=/etc/loki/config.yml >/dev/null
docker run -d --name t-alloy-logger --entrypoint sh "$CURL" \
  -c "while :; do echo $marker; sleep 1; done" >/dev/null
for _ in $(seq 1 30); do
  [[ "$(curl_on t-alloy_egress http://t-alloy-loki:3100/ready)" == ready* ]] && break
  sleep 3
done

compose up -d --wait --wait-timeout 180 >"$W/up" 2>&1 && ok "Alloy turns healthy with the pinned image" ||
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

# The proxy allows what discovery needs, and still refuses the rest.
code() { curl_on t-alloy_internal -o /dev/null -w '%{http_code}' "$@"; }
[[ "$(code http://socket-proxy:2375/containers/json)" == 200 && "$(code http://socket-proxy:2375/networks)" == 200 ]] &&
  ok "the proxy allows listing containers and networks" || ko "the proxy refuses a read that discovery needs"
refused="$(code -X POST http://socket-proxy:2375/networks/create) $(code -X POST http://socket-proxy:2375/containers/t-alloy-logger/stop)"
refused+=" $(code http://socket-proxy:2375/images/json) $(code -X POST http://socket-proxy:2375/containers/t-alloy-logger/exec)"
[[ "$refused" == "403 403 403 403" ]] && ok "the proxy refuses network creation, stop, exec and the image list" ||
  ko "the proxy answered $refused (network create, stop, image list, exec); each must be 403"

proxy_log="$(compose logs socket-proxy 2>&1)"
[[ "$proxy_log" != *dockerfrontend* ]] && ok "the proxy does not log each request of Alloy" ||
  ko "the proxy logs requests: $(grep -c dockerfrontend <<<"$proxy_log") lines"

compose restart alloy >/dev/null 2>&1
for _ in $(seq 1 24); do
  [[ "$(docker inspect -f '{{.State.Health.Status}}' "$c" 2>/dev/null)" == healthy ]] && break
  sleep 5
done
[[ "$(docker inspect -f '{{.State.Health.Status}}' "$c")" == healthy ]] &&
  ok "Alloy is healthy again after a restart, with its data volume" || ko "after restart: $(compose logs --tail 10 alloy 2>&1)"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
