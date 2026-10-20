#!/usr/bin/env bash
# Dynamic application security testing (DAST) of the throwaway NF Hotel stack with OWASP
# ZAP (MIL-027 tasks 6 and 7 of NFHotel). ZAP runs in a container on the stack's own
# Docker network and attacks https://nginx only: never the VM, the tailnet or a public
# NF Hotel host.
#
#   baseline  the passive scan, twice: signed out from https://nginx/, and signed in as the
#             test Manager from https://nginx/admin (the staff cookie as a header)
#   full      the active scan, signed in, from https://nginx/ (the weekly run)
#
# The gate fails on any High alert whose rule is not set to IGNORE in the rules file.
# Each IGNORE or WARN line of the rules file states its reason and an expiry at most 30
# days ahead, in its third column: "(<reason>; expires YYYY-MM-DD)". Per scan, the results
# folder gets the HTML and JSON report of ZAP and a SARIF file for code scanning.
#
# Usage: dast-scan.sh baseline|full --stack-env <stack.env> --rules <zap-rules.tsv> --results <folder>
#        dast-scan.sh --check-rules <zap-rules.tsv> [<today as YYYY-MM-DD>]
#        dast-scan.sh --to-sarif <ZAP report.json>   (print the SARIF of a report)
# Exit:  0 the gate passes; 1 it fails or a scan could not run; 2 usage error
# Needs: bash 4+, docker, jq, GNU date

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"

# ZAP 2.17.0 (the stable release); bump the tag and the digest together.
readonly ZAP="ghcr.io/zaproxy/zaproxy:2.17.0@sha256:5ccfff9a411743ab2615136a2edd7416bec9ce41572fa94e935df38c14adb2b6"
readonly CURL="curlimages/curl:8.10.1"
readonly TARGET="https://nginx"
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

# to_sarif <report.json>: the alerts of a ZAP JSON report as SARIF 2.1.0 for code scanning.
# Code scanning takes only repository paths as locations, so a location is the path of
# the URL under dast/ (https://nginx/admin/revenue becomes dast/admin/revenue), and the
# full URL is in the message.
to_sarif() {
  jq '{
    version: "2.1.0",
    "$schema": "https://json.schemastore.org/sarif-2.1.0.json",
    runs: [{
      tool: {driver: {name: "OWASP ZAP", informationUri: "https://www.zaproxy.org/",
        rules: [.site[]?.alerts[]? | {id: .pluginid, name: .alert,
          shortDescription: {text: .alert}, helpUri: ("https://www.zaproxy.org/docs/alerts/" + .pluginid + "/")}]
          | unique_by(.id)}},
      results: [.site[]?.alerts[]? | . as $a | .instances[]? | {
        ruleId: $a.pluginid,
        level: ({"3": "error", "2": "warning", "1": "note"}[$a.riskcode] // "none"),
        message: {text: ($a.alert + " (" + $a.riskdesc + ") at " + .method + " " + .uri)},
        locations: [{physicalLocation: {artifactLocation: {
          uri: ("dast/" + (.uri | sub("^[a-z]+://[^/]+/?"; "") | sub("[?#].*$"; "")
            | if . == "" then "index" else . end))}}}]}]
    }]}' "$1"
}

if [[ "${1:-}" == "--to-sarif" ]]; then
  [[ $# -eq 2 && -f "$2" ]] || {
    usage >&2
    exit 2
  }
  to_sarif "$2"
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

mode="${1:-}"
[[ "$mode" == "baseline" || "$mode" == "full" ]] || {
  usage >&2
  exit 2
}
shift
stack_env="" rules="" results=""
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

# zap <name> <script> <start url> [signed-in]: run one ZAP scan into <name>.json and .html.
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
  # 0 clean, 1 a FAIL rule, 2 warnings (-I ignores them); 3 means ZAP itself failed.
  ((status <= 2)) || die "ZAP ($name) failed to run (exit $status)"
  [[ -f "$work/$name.json" ]] || die "ZAP ($name) wrote no report"
  cp "$work/$name.json" "$work/$name.html" "$results/"
  to_sarif "$results/$name.json" >"$results/$name.sarif"
}

# gate <report.json>...: fail on a High alert whose rule is not set to IGNORE.
gate() {
  local ignored findings
  ignored="$(awk -F'\t' '$2 == "IGNORE" {print $1}' "$work/rules.tsv" | jq -R . | jq -s .)"
  findings="$(jq -r --argjson ignored "$ignored" '
    .site[]?.alerts[]? | select(.riskcode == "3" and ((.pluginid) as $id | $ignored | index($id) | not))
    | "High  \(.pluginid)  \(.alert) (\(.instances | length) instances)"' "$@")"
  if [[ -n "$findings" ]]; then
    echo "FAIL  ZAP found High alerts that block the change (MIL-027 criterion 3):"
    while IFS= read -r line; do echo "      $line"; done <<<"$findings"
    echo "      Fix them, or accept one in the rules file with a reason and an expiry."
    exit 1
  fi
  echo "PASS  no High alert; the other alerts are in the reports"
}

if [[ "$mode" == "baseline" ]]; then
  zap baseline-public zap-baseline.py "$TARGET/"
  zap baseline-admin zap-baseline.py "$TARGET/admin" signed-in
  gate "$results/baseline-public.json" "$results/baseline-admin.json"
else
  zap full zap-full-scan.py "$TARGET/" signed-in
  gate "$results/full.json"
fi
