#!/usr/bin/env bash
# Tests of scripts/dora-metrics.sh (MIL-023 task 5): the five DORA metrics of a
# hand-made month in tests/fixtures/dora (September 2026: a dry run, two failed CD runs
# in a row, a cancelled run, a rollback, a hotfix and a change not deployed yet), the
# window of a month that is not over, the fields of a CD job log, and the usage errors.
# The fixtures stand in for the GitHub API; fetch itself is not run here.
#
# Run it in Debian 12 with the repository at /src:
#   docker run --rm -v "$PWD:/src:ro" debian:12-slim bash /src/tests/test-dora-metrics.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 12 (apt-get, jq)

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

command -v jq >/dev/null || { apt-get -qq update >/dev/null && apt-get -qq install -y jq >/dev/null 2>&1; }
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DORA="$REPO/scripts/dora-metrics.sh"
DATA="$REPO/tests/fixtures/dora"
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

# is <jq path> <expected> <name>: the metric at that path of the September report.
is() {
  local got
  got="$(jq -c "$1" "$W/september.json")"
  [[ "$got" == "$2" ]] && ok "$3" || ko "$3 (got $got, want $2)"
}

bash "$DORA" report --month 2026-09 --data "$DATA" --json >"$W/september.json" &&
  ok "the report of September runs" || ko "the report of September runs"

is '.change_lead_time.median_hours' 24 "change lead time: the median of 4 h, 24 h and 24 h"
is '.change_lead_time.not_deployed' '[3]' "a change merged after the last deployment is not deployed"
is '.deployment_frequency.deployments' 4 "deployment frequency: four successful deployments"
is '.deployment_frequency.per_week' 0.9 "deployment frequency: 0.9 per week in 30 days"
is '.deployment_frequency.days_with_deployment' 3 "deployment frequency: three days with a deployment"
is '[.attempts[].run_id]' '[101,103,104,105,106,107]' \
  "a dry run, a cancelled run and a run after the window are not deployments"
is '.change_failure_rate.percent' 50 "change failure rate: 3 of 6 deployments failed"
is '.change_failure_rate.rolled_back' 1 "a deployment that the next one rolled back failed"
is '[.attempts[] | select(.failed) | .run_id]' '[103,104,106]' "the failed deployments are named"
is '.failed_deployment_recovery_time.incidents' 2 "two failed runs in a row are one incident"
is '.failed_deployment_recovery_time.median_hours' 2 "recovery time: the median of 3 h and 1 h"
is '.deployment_rework_rate.percent' 50 "rework rate: 3 of 6 deployments unplanned"
is '.deployment_rework_rate.rework' 3 "the two recovering deployments and the hotfix are rework"
is '.releases.not_deployed' '[202,207]' "successful releases that never reached production are named"

out="$(bash "$DORA" report --month 2026-09 --data "$DATA")"
[[ "$out" == *"| Change failure rate | 50 % | 3 of 6 deployments failed (1 rolled back) |"* ]] &&
  ok "the Markdown report has the change failure rate row" ||
  ko "the Markdown report has the change failure rate row"

NOW=2026-09-05T00:00:00Z bash "$DORA" report --month 2026-09 --data "$DATA" --json >"$W/partial.json"
[[ "$(jq -c '[.until, .days, .deployment_frequency.deployments]' "$W/partial.json")" == '["2026-09-05T00:00:00Z",4,2]' ]] &&
  ok "a month that is not over ends now" || ko "a month that is not over ends now"
[[ "$(jq -c '[.change_lead_time.changes, .change_lead_time.median_hours]' "$W/partial.json")" == '[2,14]' ]] &&
  ok "changes merged after now are left out" || ko "changes merged after now are left out"

[[ "$(bash "$DORA" job-fields <"$DATA/cd-job.log")" == '{"release_run":202,"apply":true}' ]] &&
  ok "a CD job log gives the first release run and apply input" ||
  ko "a CD job log gives the first release run and apply input"
[[ "$(sed 's/$/\r/' "$DATA/cd-job.log" | bash "$DORA" job-fields)" == '{"release_run":202,"apply":true}' ]] &&
  ok "a job log with CRLF line ends gives the same fields" ||
  ko "a job log with CRLF line ends gives the same fields"
[[ "$(echo "no inputs here" | bash "$DORA" job-fields)" == '{"release_run":null,"apply":null}' ]] &&
  ok "a log without the inputs gives nulls" || ko "a log without the inputs gives nulls"

mkdir "$W/empty"
bash "$DORA" report --month 2026-09 --data "$W/empty" >/dev/null 2>&1
[[ $? -eq 1 ]] && ok "missing data files fail with 1" || ko "missing data files fail with 1"
bash "$DORA" report --month 2026-13 --data "$DATA" >/dev/null 2>&1
[[ $? -eq 1 ]] && ok "an invalid month fails with 1" || ko "an invalid month fails with 1"
bash "$DORA" report --data "$DATA" >/dev/null 2>&1
[[ $? -eq 2 ]] && ok "a missing --month is a usage error" || ko "a missing --month is a usage error"
bash "$DORA" deploy >/dev/null 2>&1
[[ $? -eq 2 ]] && ok "an unknown subcommand is a usage error" || ko "an unknown subcommand is a usage error"

echo "$pass passed, $fail failed"
((fail == 0))
