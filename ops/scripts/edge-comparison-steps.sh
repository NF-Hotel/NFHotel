#!/usr/bin/env bash
# The steps of the workflow edge-comparison.yaml (MIL-078 task 3, ADR-0046; MIL-084 task 5,
# ADR-0048), one subcommand per step, so that the workflow keeps one line of shell per step
# (QC-YML-001 criterion 7), as running-image-scan-steps.sh does for the daily scan of the running
# images.
#
# Usage: edge-comparison-steps.sh check|compare|report|look|look-report
#   check    whether every secret of the environment cloudflare-read is set, from HAS_<NAME>
#            (true or false) for each name of SECRETS below; writes ready=true or ready=false to
#            GITHUB_OUTPUT. A missing secret is not a failure: the step names the missing ones in
#            a warning and in the step summary, and the workflow skips the other steps
#   compare  run edge-tofu.sh plan-json and reduce its JSON stream with jq to the counts of the
#            plan and the address and action of each object that differs; the stream holds the
#            identifiers and is never printed or kept. Each address of TF_VAR_team_emails is
#            masked in the log first. Writes results/summary.md, the step summary and
#            differs=true or differs=false to GITHUB_OUTPUT; a difference is not a failure. A
#            plan that fails, fails the step with the titles and details of its errors, in which
#            identifiers and addresses are replaced
#   report   open the issue "The public edge differs from its code", or comment on the open
#            one, when DIFFERS is true; close the open one with the summary when it is false
#   look     run check-security-txt.sh on the public address of security.txt with the file of
#            the checkout, the Canonical and the addresses that the file names (RFC 9116,
#            section 5.1); writes
#            results/summary.md, the step summary and failed=true or failed=false to
#            GITHUB_OUTPUT. A finding of the check is not a failure of the step; a check that
#            cannot run (exit 2 or more) is
#   look-report  open the issue "The public security.txt fails its weekly check", or comment
#            on the open one, when FAILED is true; close the open one with the summary when it
#            is false
# Each runs in the folder of the checkout, which holds results/.
#
# Env:   GITHUB_OUTPUT and GITHUB_STEP_SUMMARY (check, compare, look); HAS_<NAME> (check);
#        CLOUDFLARE_API_TOKEN and TF_VAR_* (compare, for edge-tofu.sh); REPO, SERVER, RUN_ID,
#        GH_TOKEN and DIFFERS (report) or FAILED (look-report); for the tests NFHOTEL_GH
#        (default gh), NFHOTEL_EDGE_TOFU (default scripts/edge-tofu.sh),
#        NFHOTEL_CHECK_SECURITY_TXT (default scripts/check-security-txt.sh), NFHOTEL_NOW (the
#        clock of the check, default now) and NFHOTEL_CURL (read by the check)
# Exit:  0 done; 1 the workflow passed no HAS_<NAME>, the addresses are not a JSON list, the
#        plan failed or gave no summary, the check could not run, or report or look-report
#        found no summary; 2 usage error
# Needs: bash 4+, jq, gh (report, look-report), docker (compare, through edge-tofu.sh), curl,
#        iconv and cmp (look, through check-security-txt.sh)

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=ops/scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"
gh="${NFHOTEL_GH:-gh}"
edge_tofu="${NFHOTEL_EDGE_TOFU:-$ROOT/scripts/edge-tofu.sh}"
check_security_txt="${NFHOTEL_CHECK_SECURITY_TXT:-$ROOT/scripts/check-security-txt.sh}"
readonly SUMMARY=results/summary.md
readonly ISSUE_TITLE="The public edge differs from its code"
# The look at the public security.txt (ADR-0048, decision 6): its address, the warnings that
# ADR-0048 lists as the exceptions E1 to E3, the title of its issue, and the most lines of the
# check that the issue quotes (the body of an issue holds 65,536 characters; the log keeps every
# line). It leaves out the Strict-Transport-Security of the front page: the edge answers the front
# page to a runner of GitHub by itself, without the headers of the site, so the comparison is
# made by hand (wiki page Public-Access).
readonly SECURITY_TXT_URL=https://nfhotel.devmatiker.com/.well-known/security.txt
readonly SECURITY_TXT_WARNINGS=unsigned,no-encryption,no-role-address
readonly LOOK_TITLE="The public security.txt fails its weekly check"
readonly MAX_QUOTED_LINES=50
# The secrets of the environment cloudflare-read, named in edge-comparison.yaml and in the wiki
# page Public-Access.
readonly SECRETS=(CLOUDFLARE_READ_TOKEN CLOUDFLARE_ACCOUNT_ID CLOUDFLARE_TUNNEL_ID
  CLOUDFLARE_ACCESS_APP_ID CLOUDFLARE_ACCESS_POLICY_ID CLOUDFLARE_TEAM_NAME CLOUDFLARE_TEAM_EMAILS)

# What a message of tofu or docker may hold that the public log must not show: e-mail addresses,
# UUIDs and runs of 32 or more hexadecimal characters (the account identifier, digests).
readonly JQ_REDACT='def redact: gsub("[^\\s\"<>(),;]+@[^\\s\"<>(),;]+"; "<address>")
  | gsub("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}"; "<id>")
  | gsub("[0-9a-fA-F]{32,}"; "<id>");'
# The JSON stream of tofu plan, reduced to the last summary of the changes, the objects that
# differ (every action but the import itself) and the errors, redacted.
# shellcheck disable=SC2016 # $messages is a variable of the jq program, not of the shell
readonly JQ_REDUCE="$JQ_REDACT"'
[split("\n")[] | select(length > 0) | (fromjson? // empty)] as $messages
| {
    summary: ([$messages[] | select(.type == "change_summary") | .changes] | last),
    changes: [$messages[] | select(.type == "planned_change") | .change
      | select(.action | IN("import", "noop", "read") | not)
      | {address: (.resource.addr | redact), action}],
    errors: [$messages[] | select(.type == "diagnostic" and .diagnostic.severity == "error")
      | .diagnostic
      | {summary: (.summary // "" | redact),
         detail: (.detail // "" | redact | gsub("\\s+"; " ") | .[:400]),
         address: (.address // "" | redact)}]
  }'

fail() {
  echo "::error::$*" >&2
  exit 1
}

# join_names <name>...: the names, separated by a comma and a space.
join_names() {
  local IFS=,
  local list="$*"
  echo "${list//,/, }"
}

check() {
  local name has missing=()
  : "${GITHUB_OUTPUT:?GITHUB_OUTPUT is not set}" "${GITHUB_STEP_SUMMARY:?GITHUB_STEP_SUMMARY is not set}"
  for name in "${SECRETS[@]}"; do
    has="HAS_$name"
    case "${!has:-}" in
      true) ;;
      false) missing+=("$name") ;;
      *) fail "$has is not true or false; the workflow must pass it for the secret $name" ;;
    esac
  done
  if ((${#missing[@]} == 0)); then
    echo "ready=true" >>"$GITHUB_OUTPUT"
    return 0
  fi
  echo "ready=false" >>"$GITHUB_OUTPUT"
  echo "::warning::not compared: the environment cloudflare-read lacks the secrets $(join_names "${missing[@]}"); S01 adds them as the wiki page Public-Access says"
  # shellcheck disable=SC2016 # the backticks are Markdown of the step summary, not a command
  printf 'Not compared: the environment `cloudflare-read` lacks the secrets %s. S01 adds them as the wiki page Public-Access says ("The edge as code").\n' \
    "$(join_names "${missing[@]}")" >>"$GITHUB_STEP_SUMMARY"
}

# mask_addresses: have the runner mask each address of TF_VAR_team_emails in the log. GitHub
# masks the secret as a whole, not each address in it. jq's own message is dropped, because it
# would quote the value.
mask_addresses() {
  local address addresses
  addresses="$(jq -r '.[] | strings' <<<"${TF_VAR_team_emails:-}" 2>/dev/null)" ||
    fail "TF_VAR_team_emails, from the secret CLOUDFLARE_TEAM_EMAILS, is not a JSON list of addresses"
  while IFS= read -r address; do
    [[ -z "$address" ]] || echo "::add-mask::$address"
  done <<<"$addresses"
}

# write_summary <reduced plan> <differs>: results/summary.md from the counts and the objects
# that differ; the counts also go to the log.
write_summary() {
  local reduced="$1" differs="$2" counts
  counts="$(jq -r '.summary | "\(.import // 0) to import, \(.add) to add, \(.change) to change, \(.remove) to destroy"' "$reduced")"
  {
    echo "## The public edge against its code"
    echo
    if [[ "$differs" == true ]]; then
      echo "The plan against the live account found a difference: $counts."
      echo
      echo "These objects differ from the code in \`ops/infra/cloudflare/\`:"
      echo
      jq -r '.changes[] | "- `\(.address)`: \(.action)"' "$reduced"
      echo
      echo "The plan itself is not printed, because the log of this public repository is public." \
        "To see what differs, run \`bash ops/scripts/edge-tofu.sh plan\` on your own machine with" \
        "the token that reads, as the wiki page Public-Access says (\"Read the weekly issue\")."
    else
      echo "The live account matches the code in \`ops/infra/cloudflare/\`: $counts."
    fi
  } >"$SUMMARY"
  echo "Plan: $counts."
}

compare() {
  local statuses differs=false
  : "${GITHUB_OUTPUT:?GITHUB_OUTPUT is not set}" "${GITHUB_STEP_SUMMARY:?GITHUB_STEP_SUMMARY is not set}"
  mask_addresses
  mkdir -p results
  # Not local: the trap runs after the function has returned.
  tmp="$(mktemp -d)"
  trap 'rm -rf "$tmp"' EXIT
  set +e
  bash "$edge_tofu" plan-json 2>"$tmp/stderr" | jq -R -s -c "$JQ_REDUCE" >"$tmp/reduced"
  statuses=("${PIPESTATUS[@]}")
  set -e
  # The output of docker and of tofu init, which is not the plan, with the same replacements.
  jq -R -r "$JQ_REDACT redact" <"$tmp/stderr" >&2 || echo "(the output of tofu init was not text)" >&2
  ((statuses[1] == 0)) || fail "jq could not read the output of tofu plan"
  if ((statuses[0] != 0)) || [[ "$(jq '.errors | length' "$tmp/reduced")" != 0 ]]; then
    jq -r '.errors[] | "::error::\(.summary)\(if .address != "" then " (\(.address))" else "" end): \(.detail)"' \
      "$tmp/reduced"
    echo "The plan failed; its errors are in the log of the run, with identifiers and addresses replaced." \
      >>"$GITHUB_STEP_SUMMARY"
    fail "tofu plan failed (exit ${statuses[0]}); run edge-tofu.sh plan on your own machine for the whole message"
  fi
  [[ "$(jq -r '.summary | type' "$tmp/reduced")" == object ]] || fail "tofu plan gave no summary of its changes"
  [[ "$(jq '.summary | .add + .change + .remove' "$tmp/reduced")" == 0 ]] || differs=true
  write_summary "$tmp/reduced" "$differs"
  # The names of the objects that differ, never their attributes.
  jq -r '.changes[] | "Differs: \(.address) (\(.action))"' "$tmp/reduced"
  cat "$SUMMARY" >>"$GITHUB_STEP_SUMMARY"
  echo "differs=$differs" >>"$GITHUB_OUTPUT"
}

# file_issue <title> <found>: add the address of the run to results/summary.md; then, when found
# is true, comment with it on the open issue of that title or open one, and when it is false,
# close the open one with it. A finding is reported by the issue, not by a red run: the run is a
# check of the commit on main, and the CD gate (ADR-0021) refuses a commit with a failed check,
# as the daily scan of the running images says.
file_issue() {
  local title="$1" found="$2" open
  printf '\nRun: %s/%s/actions/runs/%s\n' "$SERVER" "$REPO" "$RUN_ID" >>"$SUMMARY"
  open="$(TITLE="$title" "$gh" issue list --repo "$REPO" --state open --search "in:title \"$title\"" \
    --json number,title --jq "map(select(.title == env.TITLE))[0].number // empty")"
  if [[ "$found" == true ]]; then
    if [[ -n "$open" ]]; then
      "$gh" issue comment "$open" --repo "$REPO" --body-file "$SUMMARY"
    else
      "$gh" issue create --repo "$REPO" --title "$title" --body-file "$SUMMARY"
    fi
  elif [[ -n "$open" ]]; then
    "$gh" issue close "$open" --repo "$REPO" --comment "$(cat "$SUMMARY")"
  fi
}

report() {
  : "${REPO:?REPO is not set}" "${SERVER:?SERVER is not set}" "${RUN_ID:?RUN_ID is not set}"
  [[ "${DIFFERS:-}" == true || "${DIFFERS:-}" == false ]] || fail "DIFFERS is not true or false"
  [[ -f "$SUMMARY" ]] || fail "no $SUMMARY: the step compare did not write it"
  file_issue "$ISSUE_TITLE" "$DIFFERS"
  if [[ "$DIFFERS" == true ]]; then
    echo "::warning::the public edge differs from its code; see the issue \"$ISSUE_TITLE\""
  fi
}

# write_look_summary <output of the check> <failed>: results/summary.md, which the issue quotes:
# what the look found, and the first lines of the check in a block of text without backticks, so
# that nothing in them is read as Markdown, a link or a mention.
write_look_summary() {
  local found="$1" failed="$2" lines
  lines="$(wc -l <"$found")"
  {
    echo "## The public security.txt"
    echo
    if [[ "$failed" == true ]]; then
      echo "The weekly look at $SECURITY_TXT_URL failed (RFC 9116, section 5.1; ADR-0048): the file is" \
        "missing or differs from \`ops/deploy/nginx/well-known/security.txt\` on main, is not served as" \
        "RFC 9116 asks, expires within 30 days, or names an address that fails or ends on another host." \
        "The wiki page Public-Access says what to do (\"The security.txt\"). The check found:"
    else
      echo "The weekly look at $SECURITY_TXT_URL passed (RFC 9116, section 5.1; ADR-0048). The check found:"
    fi
    echo
    echo '```text'
    head -n "$MAX_QUOTED_LINES" "$found" | tr -d '`'
    echo '```'
    if ((lines > MAX_QUOTED_LINES)); then
      echo
      echo "$((lines - MAX_QUOTED_LINES)) more lines are in the log of the run."
    fi
  } >"$SUMMARY"
}

look() {
  local status=0 failed
  local args=(--url "$SECURITY_TXT_URL" --canonical "$SECURITY_TXT_URL"
    --same-as "$ROOT/deploy/nginx/well-known/security.txt" --links
    --expect-warnings "$SECURITY_TXT_WARNINGS")
  : "${GITHUB_OUTPUT:?GITHUB_OUTPUT is not set}" "${GITHUB_STEP_SUMMARY:?GITHUB_STEP_SUMMARY is not set}"
  if [[ -n "${NFHOTEL_NOW:-}" ]]; then
    args+=(--now "$NFHOTEL_NOW")
  fi
  mkdir -p results
  # Not local: the trap runs after the function has returned.
  tmp="$(mktemp -d)"
  trap 'rm -rf "$tmp"' EXIT
  bash "$check_security_txt" "${args[@]}" >"$tmp/found" 2>&1 || status=$?
  cat "$tmp/found"
  case "$status" in
    0) failed=false ;;
    1) failed=true ;;
    *) fail "check-security-txt.sh stopped with exit $status, so nothing was checked; what it said is above" ;;
  esac
  write_look_summary "$tmp/found" "$failed"
  cat "$SUMMARY" >>"$GITHUB_STEP_SUMMARY"
  echo "failed=$failed" >>"$GITHUB_OUTPUT"
}

look_report() {
  : "${REPO:?REPO is not set}" "${SERVER:?SERVER is not set}" "${RUN_ID:?RUN_ID is not set}"
  [[ "${FAILED:-}" == true || "${FAILED:-}" == false ]] || fail "FAILED is not true or false"
  [[ -f "$SUMMARY" ]] || fail "no $SUMMARY: the step look did not write it"
  file_issue "$LOOK_TITLE" "$FAILED"
  if [[ "$FAILED" == true ]]; then
    echo "::warning::the public security.txt fails its weekly check; see the issue \"$LOOK_TITLE\""
  fi
}

(($# == 1)) || {
  usage >&2
  exit 2
}
case "$1" in
  check) check ;;
  compare) compare ;;
  report) report ;;
  look) look ;;
  look-report) look_report ;;
  -h | --help) usage ;;
  *)
    usage >&2
    exit 2
    ;;
esac
