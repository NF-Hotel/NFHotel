#!/usr/bin/env bash
# Tests of scripts/report-health.sh (ADR-0020, MIL-023 task 9): a running and a healthy
# container are 1, an unhealthy and a stopped one are 0, a starting one is 1, a one-off
# container is left out, no container gives an empty report, and a failed docker keeps
# the old report. A fake stands in for docker, so no container runs.
#
# Run it in Debian 12 with the repository at /src:
#   docker run --rm -v "$PWD:/src:ro" debian:12-slim bash /src/tests/test-report-health.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 12 (coreutils)

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
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

# The fake docker answers from $W/containers: one "<name> <policy> <state> <health>" line each.
cat >"$W/docker" <<'EOF'
#!/bin/bash
[[ -n "${FAKE_FAIL:-}" ]] && exit 1
case "$1" in
  ps) if [[ -s "$FAKE_CONTAINERS" ]]; then awk '{print NR}' "$FAKE_CONTAINERS"; fi ;;
  inspect) sed 's|^|/|' "$FAKE_CONTAINERS" ;;
esac
EOF
chmod +x "$W/docker"
export NFHOTEL_DOCKER="$W/docker" NFHOTEL_METRICS_DIR="$W/metrics" FAKE_CONTAINERS="$W/containers"
report="$W/metrics/containers.prom"
run() { bash "$REPO/scripts/report-health.sh" >"$W/out" 2>&1; }
value() { sed -n "s/^nfhotel_container_healthy{container=\"$1\"} \([01]\)$/\1/p" "$report"; }

cat >"$W/containers" <<'EOF'
nfhotel-web-1 unless-stopped running healthy
nfhotel-api-1 unless-stopped running unhealthy
nfhotel-nginx-1 always running starting
nfhotel-ollama-1 unless-stopped running none
nfhotel-analysis-1 unless-stopped exited none
nfhotel-migrate-1 no exited none
EOF
run && ok "the report is written" || ko "run: $(cat "$W/out")"
[[ "$(value nfhotel-web-1)" == 1 && "$(value nfhotel-ollama-1)" == 1 ]] &&
  ok "a healthy container and one without a health check are 1" || ko "running: $(cat "$report")"
[[ "$(value nfhotel-api-1)" == 0 ]] && ok "an unhealthy container is 0" || ko "unhealthy"
[[ "$(value nfhotel-analysis-1)" == 0 ]] && ok "a stopped container is 0" || ko "stopped"
[[ "$(value nfhotel-nginx-1)" == 1 ]] && ok "a starting container is 1, so a deploy raises no alert" || ko "starting"
! grep -q migrate "$report" && ok "a one-off container is left out" || ko "one-off"
grep -q '^# TYPE nfhotel_container_healthy gauge$' "$report" && [[ "$(stat -c %a "$report")" == 644 ]] &&
  ok "the report is a 0644 gauge" || ko "format: $(stat -c %a "$report")"

cp "$report" "$W/before"
FAKE_FAIL=1 run && ko "a failed docker passed" || ok "a failed docker fails the run"
cmp -s "$report" "$W/before" && ok "a failed docker keeps the old report" || ko "the old report changed"

: >"$W/containers"
run && [[ "$(grep -vc '^#' "$report")" == 0 ]] && ok "no container gives an empty report" || ko "empty: $(cat "$W/out")"
[[ -z "$(find "$W/metrics" -name '.containers.*')" ]] && ok "no temporary file is left" || ko "temporary file left"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
