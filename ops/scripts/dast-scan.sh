#!/usr/bin/env bash
# Dynamic application security testing (DAST) of the throwaway NF Hotel stack with OWASP
# ZAP (MIL-027 tasks 6 and 7, MIL-077 task 3 of NFHotel). ZAP runs in a container on the
# stack's own Docker networks and attacks https://nginx and the API of the stack only: never
# the VM, the tailnet or a public NF Hotel host.
#
#   baseline  the passive scan, twice: signed out from https://nginx/, and signed in as the
#             test Manager from https://nginx/admin (the staff cookie as a header)
#   full      the active scan, signed in, from https://nginx/; then the API scan: the active
#             scan of the API itself with its OpenAPI document (--api-document), from the
#             stack's internal API network, with the API token and the staff token of the
#             stack's API scan account on every request (the weekly run and a run by hand)
#
# The gate fails on any High alert whose rule is not set to IGNORE in the rules file; on any
# answer 500 of the API to the API scan, because a 500 is a fault of the API (503 is its
# answer when a service that it calls is missing, as in this stack); and when the API scan
# was not signed in. Each IGNORE or WARN line of the rules file states its reason and an
# expiry at most 30 days ahead, in its third column: "(<reason>; expires YYYY-MM-DD)". Per
# scan, the results folder gets the HTML and JSON report of ZAP and a SARIF file for code
# scanning.
#
# Usage: dast-scan.sh baseline|full --stack-env <stack.env> --rules <zap-rules.tsv> --results <folder>
#                     [--api-document <OpenAPI document>]   (full needs it, baseline refuses it)
#        dast-scan.sh --check-rules <zap-rules.tsv> [<today as YYYY-MM-DD>]
#        dast-scan.sh --to-sarif <ZAP report.json> [<zap-rules.tsv>]   (print the SARIF of a report)
#        dast-scan.sh --gate <zap-rules.tsv> <ZAP report.json>...      (the gate over reports)
# Exit:  0 the gate passes; 1 it fails or a scan could not run; 2 usage error
# Needs: bash 4+, docker, jq, GNU date

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=ops/scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"

# ZAP 2.17.0 (the stable release); bump the tag and the digest together.
readonly ZAP="ghcr.io/zaproxy/zaproxy:2.17.0@sha256:5ccfff9a411743ab2615136a2edd7416bec9ce41572fa94e935df38c14adb2b6"
readonly CURL="curlimages/curl:8.10.1"
readonly TARGET="https://nginx"
# The API on the stack's internal API network, and the limit of the active part of the API
# scan, well inside the 90 minutes of the weekly job (MIL-077).
readonly API_TARGET="http://nfhotel-api:8080"
readonly API_SCAN_MINUTES=45
# The API scan of ZAP reports each answer of the API in the 400 or 500 range as this alert,
# with the status code as its evidence; the scans of the web pages do not raise it.
readonly ANSWER_ALERT="100000"
readonly MAX_DAYS=30

# check_rules <file> <today>: every IGNORE or WARN line has a reason and an expiry at
# most MAX_DAYS ahead; an expired one is refused, so the rule comes back to the gate.
check_rules() {
  local file="$1" today="$2" bad=0 id action note expires days today_s
  [[ -f "$file" ]] || die "no rules file $file"
  today_s="$(date -u -d "$today" +%s)" || die "'$today' is not a date"
  while IFS=$'\t' read -r id action note; do
    [[ -z "$id" || "$id" == \#* ]] && continue
    [[ "$id" =~ ^[0-9]+$ && "$action" =~ ^(IGNORE|WARN|FAIL)$ ]] || {
      echo "FAIL  '$id $action': a line is <rule id><tab>IGNORE|WARN|FAIL<tab>(<reason>; expires YYYY-MM-DD)"
      bad=1
      continue
    }
    [[ "$action" == "FAIL" ]] && continue
    expires="$(sed -nE 's/.*expires ([0-9]{4}-[0-9]{2}-[0-9]{2}).*/\1/p' <<<"$note")"
    if [[ ! "$note" =~ ^\(.+\;\ expires\ [0-9-]+\)$ || -z "$expires" ]]; then
      echo "FAIL  rule $id ($action) needs \"(<reason>; expires YYYY-MM-DD)\""
      bad=1
      continue
    fi
    days=$((($(date -u -d "$expires" +%s) - today_s) / 86400))
    if ((days > MAX_DAYS || days < 0)); then
      echo "FAIL  rule $id ($action) expires on $expires, $days days from $today (allowed 0 to $MAX_DAYS)"
      bad=1
    fi
  done <"$file"
  ((bad == 0)) || exit 1
  echo "==> $file: every IGNORE and WARN rule has a reason and expires within $MAX_DAYS days"
}

# ignored_rules <rules file>: the ids of its IGNORE rules as a JSON array.
ignored_rules() {
  awk -F'\t' '$2 == "IGNORE" {print $1}' "$1" | jq -R . | jq -s .
}

# to_sarif <report.json> [<rules>]: the alerts of a ZAP JSON report as SARIF 2.1.0 for code scanning.
# Code scanning takes only repository paths as locations, so a location is the path of
# the URL under dast/ (https://nginx/admin/revenue becomes dast/admin/revenue), and the
# full URL is in the message. With a rules file, the alerts of its IGNORE rules are left out:
# ZAP still reports them, but code scanning shows only what someone acts on (ADR-0032). For
# the same reason, of the answers of the API only the answers 500 go to code scanning, as
# errors: an answer in the 400 range is the API refusing bad input, and a 503 is by design.
to_sarif() {
  local ignored="[]"
  if [[ -n "${2:-}" ]]; then
    ignored="$(ignored_rules "$2")"
  fi
  jq --argjson ignored "$ignored" --arg answer "$ANSWER_ALERT" '
    [.site[]?.alerts[]? | select(.pluginid as $p | $ignored | index($p) | not)
      | if .pluginid == $answer then .instances = [(.instances // [])[] | select(.evidence == "500")] else . end
      | select(.pluginid != $answer or (.instances | length) > 0)] as $alerts | {
    version: "2.1.0",
    "$schema": "https://json.schemastore.org/sarif-2.1.0.json",
    runs: [{
      tool: {driver: {name: "OWASP ZAP", informationUri: "https://www.zaproxy.org/",
        rules: [$alerts[] | {id: .pluginid, name: .alert,
          shortDescription: {text: .alert}, helpUri: ("https://www.zaproxy.org/docs/alerts/" + .pluginid + "/")}]
          | unique_by(.id)}},
      results: [$alerts[] | . as $a | .instances[]? | {
        ruleId: $a.pluginid,
        level: (if $a.pluginid == $answer then "error"
          else ({"3": "error", "2": "warning", "1": "note"}[$a.riskcode] // "none") end),
        message: {text: ($a.alert + " (" + $a.riskdesc + ") at " + .method + " " + .uri)},
        locations: [{physicalLocation: {artifactLocation: {
          uri: ("dast/" + (.uri | sub("^[a-z]+://[^/]+/?"; "") | sub("[?#].*$"; "")
            | if . == "" then "index" else . end))}}}]}]
    }]}' "$1"
}

# gate <rules> <report.json>...: fail on a High alert whose rule is not set to IGNORE
# (MIL-027 criterion 3); on an answer 500 of the API, unless the rules file sets the answer
# alert to IGNORE (MIL-077); and when the API scan was not signed in: the staff list takes no
# input, so with the two tokens it never answers 401.
gate() {
  local rules="$1" ignored findings errors signed_out answers line
  shift
  ignored="$(ignored_rules "$rules")"
  findings="$(jq -r --argjson ignored "$ignored" '
    .site[]?.alerts[]? | select(.riskcode == "3" and ((.pluginid) as $id | $ignored | index($id) | not))
    | "High  \(.pluginid)  \(.alert) (\(.instances | length) instances)"' "$@")"
  errors="$(jq -r --argjson ignored "$ignored" --arg answer "$ANSWER_ALERT" '
    .site[]?.alerts[]? | select(.pluginid == $answer and ($ignored | index($answer) | not))
    | .instances[]? | select(.evidence == "500") | "500  \(.method) \(.uri)"' "$@" | sort -u)"
  signed_out="$(jq -r --arg answer "$ANSWER_ALERT" '
    .site[]?.alerts[]? | select(.pluginid == $answer) | .instances[]?
    | select(.evidence == "401" and .method == "GET" and (.uri // "" | test("^[a-z]+://[^/]+/api/staff([?#].*)?$")))
    | .uri' "$@")"
  answers="$(jq -r --arg answer "$ANSWER_ALERT" '.site[]?.alerts[]? | select(.pluginid == $answer)
    | .instances[]?.evidence' "$@" | sort | uniq -c | awk '{printf "%s%s: %s", sep, $2, $1; sep = ", "}')"
  [[ -z "$answers" ]] || echo "INFO  the answers of the API in the 400 and 500 range (status: instances): $answers"
  if [[ -n "$findings" ]]; then
    echo "FAIL  ZAP found High alerts that block the change (MIL-027 criterion 3):"
    while IFS= read -r line; do echo "      $line"; done <<<"$findings"
    echo "      Fix them, or accept one in the rules file with a reason and an expiry."
  fi
  if [[ -n "$errors" ]]; then
    echo "FAIL  the API answered 500, a fault, to the API scan (MIL-077):"
    while IFS= read -r line; do echo "      $line"; done <<<"$errors"
    echo "      Fix each one with a regression test; the report holds the request."
  fi
  if [[ -n "$signed_out" ]]; then
    echo "FAIL  the API scan was not signed in: GET /api/staff answered 401, so the scan met the"
    echo "      sign-in check instead of the endpoints (the replacer rules of ZAP or the tokens of stack.env)."
  fi
  [[ -z "$findings" && -z "$errors" && -z "$signed_out" ]] || return 1
  echo "PASS  no High alert and no answer 500; the other alerts are in the reports"
}

if [[ "${1:-}" == "--to-sarif" ]]; then
  [[ ($# -eq 2 || ($# -eq 3 && -f "$3")) && -f "$2" ]] || {
    usage >&2
    exit 2
  }
  to_sarif "$2" "${3:-}"
  exit 0
fi

if [[ "${1:-}" == "--check-rules" ]]; then
  [[ $# -ge 2 ]] || {
    usage >&2
    exit 2
  }
  check_rules "$2" "${3:-$(date -u +%F)}"
  exit 0
fi

if [[ "${1:-}" == "--gate" ]]; then
  [[ $# -ge 3 && -f "$2" ]] || {
    usage >&2
    exit 2
  }
  for report in "${@:3}"; do
    [[ -f "$report" ]] || {
      usage >&2
      exit 2
    }
  done
  gate "$2" "${@:3}"
  exit 0
fi

mode="${1:-}"
[[ "$mode" == "baseline" || "$mode" == "full" ]] || {
  usage >&2
  exit 2
}
shift
stack_env="" rules="" results="" api_document=""
while (($#)); do
  case "$1" in
    --stack-env)
      stack_env="${2:?--stack-env needs a value}"
      shift
      ;;
    --rules)
      rules="${2:?--rules needs a value}"
      shift
      ;;
    --results)
      results="${2:?--results needs a value}"
      shift
      ;;
    --api-document)
      api_document="${2:?--api-document needs a value}"
      shift
      ;;
    *)
      usage >&2
      exit 2
      ;;
  esac
  shift
done
[[ -n "$stack_env" && -f "$stack_env" ]] || die "give --stack-env, the stack.env that e2e-stack.sh wrote"
[[ -n "$rules" ]] || die "give --rules, the ZAP rules file"
[[ -n "$results" ]] || die "give --results, the folder for the reports"
check_rules "$rules" "$(date -u +%F)" >/dev/null || die "$rules is not valid (run dast-scan.sh --check-rules on it)"
network="$(sed -n 's/^E2E_NETWORK=//p' "$stack_env")"
[[ -n "$network" ]] || die "$stack_env has no E2E_NETWORK"
api_network=""
if [[ "$mode" == "full" ]]; then
  [[ -n "$api_document" && -f "$api_document" ]] ||
    die "give --api-document, the OpenAPI document of the API (g/openapi/nfhotel-api.json after a build)"
  for key in E2E_API_NETWORK E2E_API_TOKEN E2E_API_USER E2E_API_PASSWORD; do
    grep -qE "^$key=.+" "$stack_env" || die "$stack_env has no $key (e2e-stack.sh writes it since MIL-077)"
  done
  api_network="$(sed -n 's/^E2E_API_NETWORK=//p' "$stack_env")"
elif [[ -n "$api_document" ]]; then
  die "only the full scan takes --api-document"
fi

# A native path for the Docker daemon: Git Bash on Windows has cygpath, Linux does not.
native() {
  if command -v cygpath >/dev/null 2>&1; then cygpath -m "$1"; else echo "$1"; fi
}

mkdir -p "$results"
results="$(cd "$results" && pwd)"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
# ZAP runs as its own user and writes its reports into the work folder.
chmod 777 "$work"
cp "$rules" "$work/rules.tsv"

# sign_in: print the Cookie header of a signed-in test Manager. The login form is a static
# server-rendered form with an antiforgery token; the password goes in by the env file.
sign_in() {
  # shellcheck disable=SC2016 # expanded by the shell inside the curl container
  MSYS_NO_PATHCONV=1 docker run --rm --network "$network" --env-file "$(native "$stack_env")" \
    --entrypoint sh "$CURL" -c '
      set -e
      jar=/tmp/jar
      curl -sk -c "$jar" -o /tmp/login https://nginx/admin/login
      token="$(sed -n "s/.*name=\"__RequestVerificationToken\" value=\"\([^\"]*\)\".*/\1/p" /tmp/login)"
      curl -sk -b "$jar" -c "$jar" -o /dev/null \
        --data-urlencode "_handler=staff-login" --data-urlencode "__RequestVerificationToken=$token" \
        --data-urlencode "Input.UserName=$E2E_MANAGER_USER" --data-urlencode "Input.Password=$E2E_MANAGER_PASSWORD" \
        https://nginx/admin/login
      sed -e "s/^#HttpOnly_//" -e "/^#/d" -e "/^$/d" "$jar" | awk -F"\t" "{printf \"%s%s=%s\", sep, \$6, \$7; sep=\"; \"}"'
}

# keep_report <name> <exit status of ZAP>: 0 clean, 1 a FAIL rule, 2 warnings (-I ignores
# them); 3 means ZAP itself failed. Keep the JSON and HTML report in the results folder, with
# its SARIF.
keep_report() {
  local name="$1" status="$2"
  ((status <= 2)) || die "ZAP ($name) failed to run (exit $status)"
  [[ -f "$work/$name.json" ]] || die "ZAP ($name) wrote no report"
  cp "$work/$name.json" "$work/$name.html" "$results/"
  to_sarif "$results/$name.json" "$work/rules.tsv" >"$results/$name.sarif"
}

# zap <name> <script> <start url> [signed-in]: run one ZAP scan of the web pages into
# <name>.json and .html.
zap() {
  local name="$1" script="$2" url="$3" signed_in="${4:-}" status=0
  local -a env=()
  if [[ -n "$signed_in" ]]; then
    local cookie
    cookie="$(sign_in)" || die "signing in as the test Manager failed"
    [[ "$cookie" == *AspNetCore* ]] || die "signing in as the test Manager gave no session cookie"
    umask 077
    printf 'ZAP_AUTH_HEADER=Cookie\nZAP_AUTH_HEADER_VALUE=%s\nZAP_AUTH_HEADER_SITE=nginx\n' "$cookie" >"$work/auth.env"
    umask 022
    env=(--env-file "$(native "$work/auth.env")")
  fi
  echo "==> ZAP $script ($name) from $url"
  MSYS_NO_PATHCONV=1 docker run --rm --network "$network" "${env[@]}" \
    -v "$(native "$work"):/zap/wrk:rw" "$ZAP" \
    "$script" -t "$url" -c rules.tsv -J "$name.json" -r "$name.html" -I -m 2 || status=$?
  rm -f "$work/auth.env"
  keep_report "$name" "$status"
}

# api_sign_in <API token> <user name> <password>: print the staff token of the API scan
# account, signed in at the API itself, once the two tokens have opened the staff list. The
# secrets reach the curl container by its environment and curl by files, never by a command
# line; the user name and the password are letters, digits and hyphens made by e2e-stack.sh,
# so the JSON needs no escaping.
api_sign_in() {
  # shellcheck disable=SC2016 # expanded by the shell inside the curl container
  E2E_API_URL="$API_TARGET" E2E_API_TOKEN="$1" E2E_API_USER="$2" E2E_API_PASSWORD="$3" MSYS_NO_PATHCONV=1 \
    docker run --rm --network "$api_network" -e E2E_API_URL -e E2E_API_TOKEN -e E2E_API_USER -e E2E_API_PASSWORD \
    --entrypoint sh "$CURL" -c '
      set -e
      cd /tmp
      printf "Authorization: Bearer %s\n" "$E2E_API_TOKEN" >headers
      printf "{\"userName\":\"%s\",\"password\":\"%s\"}" "$E2E_API_USER" "$E2E_API_PASSWORD" >login.json
      curl -sf -H @headers -H "Content-Type: application/json" --data @login.json -o session "$E2E_API_URL/api/auth/login"
      token="$(sed -n "s/.*\"token\":\"\([^\"]*\)\".*/\1/p" session)"
      printf "X-Staff-Token: %s\n" "$token" >>headers
      curl -sf -H @headers -o /dev/null "$E2E_API_URL/api/staff"
      printf "%s" "$token"'
}

# zap_api: the API scan (MIL-077). ZAP joins the stack's internal API network with -silent, so
# it sends nothing outside the stack; it imports the OpenAPI document with the API as its
# target, and the replacer of ZAP puts the API token and the staff token on every request.
# ZAP runs as its own user: it reads the file of the two headers through the group of the user
# of this script (--group-add), and the file is removed when the scan ends.
zap_api() {
  local api_token user password staff_token status=0
  api_token="$(sed -n 's/^E2E_API_TOKEN=//p' "$stack_env")"
  user="$(sed -n 's/^E2E_API_USER=//p' "$stack_env")"
  password="$(sed -n 's/^E2E_API_PASSWORD=//p' "$stack_env")"
  staff_token="$(api_sign_in "$api_token" "$user" "$password")" ||
    die "signing in at the API as $user, or opening the staff list with its tokens, failed"
  [[ "$staff_token" =~ ^[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$ ]] ||
    die "signing in at the API as $user gave no staff token"
  cp "$api_document" "$work/nfhotel-api.json"
  (
    umask 027
    printf '%s\n' \
      "replacer.full_list(0).description=API token" \
      "replacer.full_list(0).enabled=true" \
      "replacer.full_list(0).matchtype=REQ_HEADER" \
      "replacer.full_list(0).matchstr=Authorization" \
      "replacer.full_list(0).regex=false" \
      "replacer.full_list(0).replacement=Bearer $api_token" \
      "replacer.full_list(1).description=Staff token" \
      "replacer.full_list(1).enabled=true" \
      "replacer.full_list(1).matchtype=REQ_HEADER" \
      "replacer.full_list(1).matchstr=X-Staff-Token" \
      "replacer.full_list(1).regex=false" \
      "replacer.full_list(1).replacement=$staff_token" >"$work/api-headers.conf"
  )
  echo "==> ZAP zap-api-scan.py (api) with the OpenAPI document against $API_TARGET"
  MSYS_NO_PATHCONV=1 docker run --rm --network "$api_network" --group-add "$(id -g)" \
    -v "$(native "$work"):/zap/wrk:rw" "$ZAP" \
    zap-api-scan.py -t nfhotel-api.json -f openapi -O "$API_TARGET" -c rules.tsv -J api.json -r api.html -I \
    -z "-silent -configfile /zap/wrk/api-headers.conf -config scanner.maxScanDurationInMins=$API_SCAN_MINUTES" ||
    status=$?
  rm -f "$work/api-headers.conf"
  keep_report api "$status"
}

if [[ "$mode" == "baseline" ]]; then
  zap baseline-public zap-baseline.py "$TARGET/"
  zap baseline-admin zap-baseline.py "$TARGET/admin" signed-in
  gate "$work/rules.tsv" "$results/baseline-public.json" "$results/baseline-admin.json"
else
  zap full zap-full-scan.py "$TARGET/" signed-in
  zap_api
  gate "$work/rules.tsv" "$results/full.json" "$results/api.json"
fi
