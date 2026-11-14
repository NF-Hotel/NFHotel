#!/usr/bin/env bash
# Tests of the rules file check, the SARIF conversion and the gate of scripts/dast-scan.sh
# (MIL-027 task 6 and MIL-077 task 3 of NFHotel): an IGNORE or WARN rule needs a reason and
# an expiry at most 30 days ahead and not past, a FAIL rule needs neither, comments and
# blank lines are skipped, and a malformed line fails; the SARIF of a ZAP report has
# repository paths as locations, which code scanning requires, and the levels of the ZAP
# risks, and of the answers of the API only the answers 500; the gate fails on a High alert
# that is not IGNORE, on an answer 500 of the API and on an API scan that was not signed in;
# and the full scan needs the OpenAPI document and the API settings of stack.env, which the
# baseline refuses. No ZAP runs here; the scan itself is tested by the NFHotel workflow.
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

# With the rules file, the alerts of an IGNORE rule stay out of the SARIF (ADR-0032).
printf '10038\tIGNORE\t(The CSP is set elsewhere; expires 2026-11-19)\n40012\tFAIL\t\n' >"$W/ignore.tsv"
sarif="$(bash "$SCAN" --to-sarif "$W/report.json" "$W/ignore.tsv")"
[[ "$(jq -r '[.runs[0].results[].ruleId] | join(",")' <<<"$sarif")" == "40012" ]] &&
  ok "the alerts of an IGNORE rule are left out of the SARIF" || ko "IGNORE in SARIF: $(jq -c '[.runs[0].results[].ruleId]' <<<"$sarif")"
[[ "$(jq -r '[.runs[0].tool.driver.rules[].id] | join(",")' <<<"$sarif")" == "40012" ]] &&
  ok "an IGNORE rule is not listed as a SARIF rule" || ko "IGNORE rule listed"
bash "$SCAN" --to-sarif "$W/report.json" "$W/missing.tsv" >/dev/null 2>&1
[[ $? -eq 2 ]] && ok "a missing rules file is a usage error" || ko "a missing rules file is a usage error"

# The answers of the API in a report of the API scan (MIL-077): alert 100000, Info in the 400
# range and Low in the 500 range, with the status code as its evidence.
api_report() { # api_report <file> <instances in the 400 range> <instances in the 500 range>
  jq -n --argjson client "$2" --argjson server "$3" '{site: [{alerts: [
    {pluginid: "100000", alert: "A Client Error response code was returned by the server",
     riskcode: "0", riskdesc: "Informational (High)", instances: $client},
    {pluginid: "100000", alert: "A Server Error response code was returned by the server",
     riskcode: "1", riskdesc: "Low (High)", instances: $server}]}]}' >"$1"
}
answer() { printf '{"method": "%s", "uri": "http://nfhotel-api:8080%s", "evidence": "%s"}' "$1" "$2" "$3"; }
api_report "$W/api-ok.json" "[$(answer POST /api/rooms 400),$(answer POST /api/auth/login 401)]" \
  "[$(answer POST /api/market-analysis/runs 503)]"
api_report "$W/api-500.json" "[$(answer POST /api/rooms 400)]" \
  "[$(answer POST /api/rooms/search 500),$(answer POST /api/market-analysis/runs 503)]"
api_report "$W/api-signed-out.json" "[$(answer GET /api/staff 401)]" "[]"
printf '40012\tFAIL\t\n' >"$W/fail.tsv"
printf '40012\tIGNORE\t(Accepted for the test; expires 2026-11-19)\n' >"$W/ignore-high.tsv"
printf '100000\tIGNORE\t(Accepted for the test; expires 2026-11-19)\n' >"$W/ignore-answers.tsv"

sarif="$(bash "$SCAN" --to-sarif "$W/api-500.json")"
[[ "$(jq -r '[.runs[0].results[] | .ruleId + " " + .level + " " + .locations[0].physicalLocation.artifactLocation.uri]
  | join(",")' <<<"$sarif")" == "100000 error dast/api/rooms/search" ]] &&
  ok "of the answers of the API only the 500 goes to the SARIF, as an error" ||
  ko "answers in the SARIF: $(jq -c '[.runs[0].results[] | [.ruleId, .level]]' <<<"$sarif")"
sarif="$(bash "$SCAN" --to-sarif "$W/api-ok.json")"
[[ "$(jq '(.runs[0].results | length) + (.runs[0].tool.driver.rules | length)' <<<"$sarif")" == 0 ]] &&
  ok "answers in the 400 range and a 503 stay out of the SARIF" || ko "answers in the 400 range and a 503 stay out of the SARIF"

gate() { bash "$SCAN" --gate "$@" >"$W/gate.log" 2>&1; }
gate "$W/fail.tsv" "$W/api-ok.json" && ok "answers in the 400 range and a 503 pass the gate" ||
  ko "answers in the 400 range and a 503 pass the gate: $(cat "$W/gate.log")"
grep -q "(status: instances): 400: 1, 401: 1, 503: 1" "$W/gate.log" &&
  ok "the gate counts the answers of the API by status" || ko "the gate counts the answers: $(cat "$W/gate.log")"
gate "$W/fail.tsv" "$W/api-500.json"
[[ $? -eq 1 ]] && grep -q "500  POST http://nfhotel-api:8080/api/rooms/search" "$W/gate.log" &&
  ok "an answer 500 of the API fails the gate and names the request" || ko "an answer 500 of the API fails the gate"
[[ "$(grep -c '^      500  ' "$W/gate.log")" == 1 ]] &&
  ok "a 503, the answer of the API by design, is not a fault" || ko "a 503 is counted as a fault"
gate "$W/ignore-answers.tsv" "$W/api-500.json" &&
  ok "an IGNORE rule for the answers accepts an answer 500" || ko "an IGNORE rule for the answers accepts an answer 500"
gate "$W/fail.tsv" "$W/api-signed-out.json"
[[ $? -eq 1 ]] && grep -q "not signed in" "$W/gate.log" &&
  ok "a 401 to GET /api/staff fails the gate: the API scan was not signed in" || ko "a 401 to GET /api/staff fails the gate"
gate "$W/ignore-answers.tsv" "$W/api-signed-out.json" &&
  ko "an IGNORE rule does not hide an API scan that was not signed in" ||
  ok "an IGNORE rule does not hide an API scan that was not signed in"
gate "$W/fail.tsv" "$W/report.json" "$W/api-ok.json"
[[ $? -eq 1 ]] && grep -q "High  40012" "$W/gate.log" &&
  ok "a High alert in any of the reports fails the gate" || ko "a High alert in any of the reports fails the gate"
gate "$W/ignore-high.tsv" "$W/report.json" && ok "a High alert of an IGNORE rule passes the gate" ||
  ko "a High alert of an IGNORE rule passes the gate: $(cat "$W/gate.log")"
bash "$SCAN" --gate "$W/fail.tsv" >/dev/null 2>&1
[[ $? -eq 2 ]] && ok "the gate without a report is a usage error" || ko "the gate without a report is a usage error"
bash "$SCAN" --gate "$W/fail.tsv" "$W/missing.json" >/dev/null 2>&1
[[ $? -eq 2 ]] && ok "a missing report is a usage error" || ko "a missing report is a usage error"

# The full scan needs the OpenAPI document and the API settings of stack.env; the baseline
# refuses the document. Each is refused before a container starts.
printf 'E2E_NETWORK=test-proxy\n' >"$W/stack.env"
printf '{}\n' >"$W/nfhotel-api.json"
scan() { bash "$SCAN" "$@" --stack-env "$W/stack.env" --rules "$W/fail.tsv" --results "$W/results" >"$W/scan.log" 2>&1; }
scan full
[[ $? -eq 1 ]] && grep -q -- "--api-document" "$W/scan.log" &&
  ok "the full scan needs the OpenAPI document" || ko "the full scan needs the OpenAPI document: $(cat "$W/scan.log")"
scan baseline --api-document "$W/nfhotel-api.json"
[[ $? -eq 1 ]] && grep -q "only the full scan" "$W/scan.log" &&
  ok "the baseline refuses the OpenAPI document" || ko "the baseline refuses the OpenAPI document: $(cat "$W/scan.log")"
scan full --api-document "$W/nfhotel-api.json"
[[ $? -eq 1 ]] && grep -q "has no E2E_API_NETWORK" "$W/scan.log" &&
  ok "the full scan needs the API settings of stack.env" || ko "the full scan needs the API settings: $(cat "$W/scan.log")"

echo "==> $pass passed, $fail failed"
((fail == 0))
