#!/usr/bin/env bash
# Check the monitoring configuration with the tools of the pinned images (ADR-0020,
# MIL-023 criterion 8), so a broken rule, route or pipeline fails the pull request and
# not the LXC: promtool check config and test rules, amtool check-config, loki
# -verify-config, blackbox_exporter --config.check, alloy fmt, and the Grafana dashboards
# as JSON. Stand-in secret files are made for amtool; no real secret is read.
#
# Usage: check-monitoring.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: bash 4+, docker, python3

set -uo pipefail

OPS="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
mon="$OPS/deploy/monitoring"
alloy="$OPS/deploy/alloy"
W="$(mktemp -d)"
trap 'rm -rf "$W"' EXIT
failed=0

check() {
  local name="$1"
  shift
  if "$@" >"$W/out" 2>&1; then
    echo "PASS  $name"
  else
    echo "FAIL  $name"
    sed 's/^/      /' "$W/out"
    failed=1
  fi
}
# image <name> <compose file>: the pinned image of that name.
image() { sed -n "s|^    image: \(.*$1.*\)$|\1|p" "$2" | head -n 1; }

echo "https://discord.com/api/webhooks/0/stand-in" >"$W/discord-webhook-url"
echo "https://hc-ping.com/stand-in" >"$W/healthchecks-watchdog-url"

check "promtool check config" docker run --rm --entrypoint promtool -v "$mon/prometheus:/p:ro" -w /p \
  "$(image prom/prometheus "$mon/docker-compose.yml")" check config /p/prometheus.yml
check "promtool test rules" docker run --rm --entrypoint promtool -v "$mon/prometheus:/p:ro" -w /p \
  "$(image prom/prometheus "$mon/docker-compose.yml")" test rules rules-test.yml
check "amtool check-config" docker run --rm --entrypoint amtool -v "$mon/alertmanager:/etc/alertmanager:ro" \
  -v "$W:/etc/alertmanager-secrets:ro" "$(image prom/alertmanager "$mon/docker-compose.yml")" \
  check-config /etc/alertmanager/alertmanager.yml
check "loki -verify-config" docker run --rm -v "$mon/loki:/etc/loki:ro" \
  "$(image grafana/loki "$mon/docker-compose.yml")" -config.file=/etc/loki/config.yml -verify-config
check "blackbox_exporter --config.check" docker run --rm -v "$mon/blackbox:/etc/blackbox:ro" \
  "$(image prom/blackbox-exporter "$mon/docker-compose.yml")" --config.file=/etc/blackbox/blackbox.yml --config.check
# alloy fmt prints the formatted file; a difference means the file is not formatted.
alloy_fmt() {
  docker run --rm --entrypoint /bin/alloy -v "$alloy:/a:ro" "$(image grafana/alloy "$alloy/docker-compose.yml")" \
    fmt /a/config.alloy >"$W/formatted" && diff -u "$alloy/config.alloy" "$W/formatted"
}
check "alloy fmt" alloy_fmt
check "Grafana dashboards are JSON" python3 -c \
  'import json, sys; [json.load(open(f, encoding="utf-8")) for f in sys.argv[1:]]' \
  "$mon"/grafana/dashboards/*.json

((failed == 0))
