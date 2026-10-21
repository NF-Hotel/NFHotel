#!/usr/bin/env bash
# Tests of the rules file check and the SARIF conversion of scripts/dast-scan.sh (MIL-027
# task 6 of NFHotel): an IGNORE or WARN rule needs a reason and an expiry at most 30 days
# ahead and not past, a FAIL rule needs neither, comments and blank lines are skipped, and
# a malformed line fails; the SARIF of a ZAP report has repository paths as locations,
# which code scanning requires, and the levels of the ZAP risks. No ZAP runs here; the
# scan itself is tested by the NFHotel workflow.
#
# Run it in Debian 12 with the repository at /src:
#   docker run --rm -v "$PWD:/src:ro" debian:12-slim bash /src/tests/test-dast-rules.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 12 (apt-get, jq)

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

apt-get -qq update >/dev/null && apt-get -qq install -y jq >/dev/null 2>&1
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SCAN="$REPO/scripts/dast-scan.sh"
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

rules() { printf '%s\n' "$@" >"$W/rules.tsv"; }
check() { bash "$SCAN" --check-rules "$W/rules.tsv" 2026-10-20 >/dev/null; }
T=$'\t'

rules "# a comment" "" "10038${T}IGNORE${T}(The CSP is set by MIL-030; expires 2026-11-19)"
check && ok "an IGNORE rule with a reason and a 30-day expiry passes" || ko "an IGNORE rule with a reason and a 30-day expiry passes"
rules "10038${T}WARN${T}(Tracked in issue 12; expires 2026-11-20)"
check && ko "an expiry of 31 days fails" || ok "an expiry of 31 days fails"
rules "10038${T}IGNORE${T}(Old; expires 2026-10-19)"
check && ko "an expired rule fails" || ok "an expired rule fails"
rules "10038${T}IGNORE${T}(No expiry)"
check && ko "a rule without an expiry fails" || ok "a rule without an expiry fails"
rules "10038${T}IGNORE"
check && ko "a rule without a reason fails" || ok "a rule without a reason fails"
rules "10038${T}FAIL${T}(Must never come back)"
check && ok "a FAIL rule needs no expiry" || ko "a FAIL rule needs no expiry"
rules "10038 IGNORE (spaces; expires 2026-11-01)"
check && ko "a line without tabs fails" || ok "a line without tabs fails"
rules "abc${T}IGNORE${T}(Not an id; expires 2026-11-01)"
check && ko "a rule id that is not a number fails" || ok "a rule id that is not a number fails"
bash "$SCAN" --check-rules "$W/missing.tsv" >/dev/null 2>&1 && ko "a missing file fails" || ok "a missing file fails"
bash "$SCAN" nonsense >/dev/null 2>&1
[[ $? -eq 2 ]] && ok "an unknown mode is a usage error" || ko "an unknown mode is a usage error"

cat >"$W/report.json" <<'JSON'
{"site": [{"alerts": [
  {"pluginid": "10038", "alert": "CSP Header Not Set", "riskcode": "2", "riskdesc": "Medium (High)",
   "instances": [{"method": "GET", "uri": "https://nginx/admin/revenue?x=1"}, {"method": "GET", "uri": "https://nginx/"}]},
  {"pluginid": "40012", "alert": "Cross Site Scripting", "riskcode": "3", "riskdesc": "High (Medium)",
   "instances": [{"method": "POST", "uri": "https://nginx/book"}]}]}]}
JSON
sarif="$(bash "$SCAN" --to-sarif "$W/report.json")"
uris="$(jq -r '[.runs[0].results[].locations[0].physicalLocation.artifactLocation.uri] | join(",")' <<<"$sarif")"
[[ "$uris" == "dast/admin/revenue,dast/index,dast/book" ]] &&
  ok "SARIF locations are repository paths under dast/" || ko "SARIF locations are repository paths under dast/"
[[ "$(jq -r '[.runs[0].results[].level] | join(",")' <<<"$sarif")" == "warning,warning,error" ]] &&
  ok "SARIF levels follow the ZAP risk" || ko "SARIF levels follow the ZAP risk"
[[ "$(jq -r '.runs[0].results[0].message.text' <<<"$sarif")" == *"GET https://nginx/admin/revenue?x=1" ]] &&
  ok "the SARIF message keeps the full URL" || ko "the SARIF message keeps the full URL"
[[ "$(jq '.runs[0].tool.driver.rules | length' <<<"$sarif")" == 2 ]] && ok "one SARIF rule per ZAP rule" || ko "one SARIF rule per ZAP rule"

echo "==> $pass passed, $fail failed"
((fail == 0))
