#!/usr/bin/env bash
# Tests of scripts/edge-tofu.sh and scripts/edge-comparison-steps.sh (MIL-078 tasks 2 and 3,
# ADR-0046). edge-tofu.sh: each subcommand runs the pinned image with the options it needs (no
# network to format, the code read-only except to format and to lock, the lock file read-only at
# init, the state in memory), hands the token and the values to docker by name only, refuses to
# run without a certificate bundle, without a token, or apply without a terminal, and removes
# its work folder. The steps of the workflow edge-comparison.yaml: check names the missing
# secrets and lets the run stay green; compare masks each address, reduces the plan to its counts
# and the names of the objects that differ, never prints an identifier or an address, and fails
# a plan that fails, with its errors redacted; report opens, comments on or closes the issue as
# the daily scan of the running images does. The steps of the job look (MIL-084 task 5,
# ADR-0048): look runs check-security-txt.sh on the public address of security.txt with the file
# of the checkout, the addresses that the file names and the front page, through a fake curl
# that answers as the public listener and GitHub will after the deploy; a missing file, a file
# that differs from main, an Expires fewer than 30 days ahead, a named address that fails or
# ends on another host, and another Strict-Transport-Security each open the issue, and a look
# that passes closes it. Fakes stand in for docker, gh, curl, edge-tofu.sh and
# check-security-txt.sh, so nothing reaches Cloudflare, GitHub or the public site. The
# identifiers and the e-mail addresses below are made up; the web addresses of the look are
# those of the public site.
#
# Run it in Debian 12 with the repository's ops/ at /src:
#   docker run --rm -v "$PWD/ops:/src:ro" debian:12-slim bash /src/tests/test-edge-comparison.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 12 (jq is installed by the test)

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

OPS="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
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

command -v jq >/dev/null || { apt-get update -qq && apt-get install -y -qq jq >/dev/null; } || exit 1

ACCOUNT=0123456789abcdef0123456789abcdef
TUNNEL=11111111-2222-3333-4444-555555555555
APP=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee
POLICY=99999999-8888-7777-6666-555555555555
TOKEN=made-up-token-0001
EMAILS='["anna@example.com","bo@example.org"]'
# leaks <file>...: whether a made-up identifier, address or token is in the files.
leaks() { grep -q -e "$ACCOUNT" -e "$TUNNEL" -e "$APP" -e "$POLICY" -e "$TOKEN" -e "anna@" -e "bo@" "$@"; }

echo "== edge-tofu.sh"
# The fake docker logs each call on one line, whether it got a token, and the work folder.
cat >"$W/docker" <<'EOF'
#!/bin/bash
echo "docker $*" >>"$FAKE_LOG"
echo "token ${CLOUDFLARE_API_TOKEN:+set}" >>"$FAKE_LOG"
for arg in "$@"; do
  [[ "$arg" != *:/work ]] || echo "work ${arg%:/work}" >>"$FAKE_LOG"
done
case "$*" in
  *" init "*) echo "Initializing the backend..." ;;
  *" plan "*-json*) echo '{"type":"change_summary","changes":{"add":0,"change":0,"import":3,"remove":0}}' ;;
esac
exit "${FAKE_DOCKER_EXIT:-0}"
EOF
chmod +x "$W/docker"
: >"$W/ca.pem"
CODE="$OPS/infra/cloudflare"
IMAGE_RE='ghcr\.io/opentofu/opentofu:[0-9.]+-minimal@sha256:[0-9a-f]{64}'

# edge <argument>...: edge-tofu.sh with the fake docker, without a terminal.
edge() {
  : >"$W/log"
  NFHOTEL_DOCKER="$W/docker" FAKE_LOG="$W/log" CA_BUNDLE="${CA_BUNDLE:-$W/ca.pem}" \
    EDGE_ENV_FILE="${EDGE_ENV_FILE:-$W/edge.env}" bash "$OPS/scripts/edge-tofu.sh" "$@" \
    >"$W/stdout" 2>"$W/stderr" </dev/null
}
calls() { grep -c '^docker ' "$W/log"; }
call() { grep '^docker ' "$W/log" | sed -n "${1}p"; }

edge
[[ $? == 2 ]] && ok "no subcommand is a usage error" || ko "no subcommand: $(cat "$W/stderr")"
edge deploy
[[ $? == 2 ]] && ok "an unknown subcommand is a usage error" || ko "unknown: $(cat "$W/stderr")"
edge --help && grep -q "^Usage: edge-tofu.sh fmt|format|validate|lock|plan|plan-json|apply" "$W/stdout" &&
  ok "--help prints the header" || ko "help: $(cat "$W/stdout")"

edge fmt && [[ "$(calls)" == 1 ]] && call 1 | grep -qE -- "--network none -v $CODE:/src:ro $IMAGE_RE fmt -check -recursive$" &&
  ok "fmt checks the format in the pinned image, without a network, the code read-only" || ko "fmt: $(cat "$W/log")"
call 1 | grep -q -- "--rm --read-only --tmpfs /tmp --user [0-9]*:[0-9]* --cap-drop ALL --security-opt no-new-privileges" &&
  ok "tofu runs with a read-only root, as the user, without capabilities" || ko "hardening: $(call 1)"
! grep -q -e "CLOUDFLARE_API_TOKEN" -e "TF_VAR_" "$W/log" && grep -q "^token $" "$W/log" &&
  ok "fmt hands on no token and no value" || ko "fmt token: $(cat "$W/log")"
FAKE_DOCKER_EXIT=3 edge fmt
[[ $? == 3 ]] && ok "a file to format fails fmt with the exit status of tofu" || ko "fmt exit: $?"
edge format && call 1 | grep -qE -- "--network none -v $CODE:/src $IMAGE_RE fmt -recursive$" &&
  ok "format rewrites the files, the code writable, without a network" || ko "format: $(cat "$W/log")"

edge validate && [[ "$(calls)" == 2 ]] &&
  call 1 | grep -qE -- "-v $CODE:/src:ro $IMAGE_RE init -backend=false -input=false -lockfile=readonly$" &&
  call 2 | grep -qE -- "-v $CODE:/src:ro $IMAGE_RE validate$" && ! grep -q CLOUDFLARE_API_TOKEN "$W/log" &&
  ok "validate installs the provider against the lock file, then validates, without a token" ||
  ko "validate: $(cat "$W/log")"
work="$(sed -n 's/^work //p' "$W/log" | head -n 1)"
[[ -n "$work" && ! -e "$work" ]] && ok "the work folder of the provider is removed after the run" || ko "work: $work"
edge lock && [[ "$(calls)" == 1 ]] &&
  call 1 | grep -qE -- "-v $CODE:/src $IMAGE_RE init -backend=false -input=false -upgrade$" &&
  ok "lock runs init -upgrade with the code writable, for the lock file" || ko "lock: $(cat "$W/log")"

edge plan-json && ko "plan-json ran without a token" ||
  { grep -q "error: CLOUDFLARE_API_TOKEN is not set" "$W/stderr" && [[ "$(calls)" == 0 ]] &&
    ok "plan-json without a token stops before docker"; } || ko "no token: $(cat "$W/stderr" "$W/log")"
CLOUDFLARE_API_TOKEN="$TOKEN" TF_VAR_account_id="$ACCOUNT" TF_VAR_team_emails="$EMAILS" edge plan-json &&
  [[ "$(calls)" == 2 ]] && [[ "$(cat "$W/stdout")" == '{"type":"change_summary","changes":{"add":0,"change":0,"import":3,"remove":0}}' ]] &&
  grep -q "Initializing the backend" "$W/stderr" &&
  ok "plan-json writes only the plan stream to standard output, and init to standard error" ||
  ko "plan-json: $(cat "$W/stdout" "$W/stderr")"
call 2 | grep -q -- "--tmpfs /state " && call 2 | grep -q -- " -e CLOUDFLARE_API_TOKEN " &&
  call 2 | grep -q -- " -e TF_VAR_account_id " && call 2 | grep -q -- " -e TF_VAR_team_emails " &&
  call 2 | grep -q -- "plan -input=false -state=/state/terraform.tfstate -json$" && grep -q "^token set$" "$W/log" &&
  ok "the plan gets the token and the values by name, and keeps its state in memory" || ko "plan options: $(call 2)"
! leaks "$W/log" "$W/stdout" "$W/stderr" && ok "no token, identifier or address is on a command line" ||
  ko "values on the command line: $(cat "$W/log")"
! grep -q -- "--env-file" "$W/log" && ok "without edge.env no settings file is handed on" || ko "env file: $(cat "$W/log")"
echo "TF_VAR_account_id=$ACCOUNT" >"$W/edge.env"
CLOUDFLARE_API_TOKEN="$TOKEN" edge plan && call 2 | grep -q -- "--env-file $W/edge.env " &&
  call 2 | grep -q -- "plan -input=false -state=/state/terraform.tfstate$" && ! leaks "$W/log" &&
  ok "plan hands on the settings file edge.env by its name" || ko "plan env file: $(cat "$W/log")"
rm -f "$W/edge.env"
CLOUDFLARE_API_TOKEN="$TOKEN" edge apply && ko "apply ran without a terminal" ||
  { grep -q "error: apply needs a terminal" "$W/stderr" && [[ "$(calls)" == 0 ]] &&
    ok "apply without a terminal stops before docker"; } || ko "apply: $(cat "$W/stderr")"
CA_BUNDLE="$W/missing.pem" edge validate && ko "validate ran without a certificate bundle" ||
  { grep -q "error: no certificate bundle at $W/missing.pem" "$W/stderr" && [[ "$(calls)" == 0 ]] &&
    ok "without a certificate bundle nothing runs"; } || ko "bundle: $(cat "$W/stderr")"

echo "== the steps of the workflow (edge-comparison-steps.sh)"
# The fake edge-tofu.sh writes FAKE_STDERR to standard error and the file FAKE_STREAM to standard
# output, and exits with FAKE_EXIT; the fake gh logs its calls and lists FAKE_OPEN as open.
cat >"$W/edge-tofu" <<'EOF'
#!/bin/bash
echo "edge-tofu $*" >>"$FAKE_LOG"
[[ -z "${FAKE_STDERR:-}" ]] || echo "$FAKE_STDERR" >&2
[[ -z "${FAKE_STREAM:-}" ]] || cat "$FAKE_STREAM"
exit "${FAKE_EXIT:-0}"
EOF
cat >"$W/gh" <<'EOF'
#!/bin/bash
echo "gh $*" >>"$FAKE_LOG"
case "$1 $2" in
  "issue list") echo "${FAKE_OPEN:-}" ;;
  "issue create" | "issue comment" | "issue close") ;;
  *) exit 1 ;;
esac
EOF
chmod +x "$W/edge-tofu" "$W/gh"

# The JSON stream of tofu plan -json, as OpenTofu 1.13 writes it, for three imports and one
# warning; a line that is not JSON is ignored.
stream() {
  local app_action="$1" change="$2"
  cat <<EOF
{"@level":"info","@message":"OpenTofu 1.13.9","type":"version","tofu":"1.13.9","ui":"1.2"}
{"@level":"info","@message":"cloudflare_zero_trust_access_policy.team: Refreshing state... [id=$POLICY]","hook":{"resource":{"addr":"cloudflare_zero_trust_access_policy.team"},"id_key":"id","id_value":"$POLICY"},"type":"refresh_start"}
not a line of JSON
{"@level":"info","@message":"cloudflare_zero_trust_access_policy.team: Plan to import","change":{"resource":{"addr":"cloudflare_zero_trust_access_policy.team"},"action":"import","importing":{"id":"$ACCOUNT/$POLICY"}},"type":"planned_change"}
{"@level":"info","@message":"cloudflare_zero_trust_access_application.back_office: Plan to $app_action","change":{"resource":{"addr":"cloudflare_zero_trust_access_application.back_office"},"action":"$app_action","importing":{"id":"accounts/$ACCOUNT/$APP"}},"type":"planned_change"}
{"@level":"info","@message":"cloudflare_zero_trust_tunnel_cloudflared_config.nf_hotel: Plan to import","change":{"resource":{"addr":"cloudflare_zero_trust_tunnel_cloudflared_config.nf_hotel"},"action":"import","importing":{"id":"$ACCOUNT/$TUNNEL"}},"type":"planned_change"}
{"@level":"warn","@message":"Warning: a deprecated attribute","diagnostic":{"severity":"warning","summary":"A deprecated attribute","detail":"Seen for anna@example.com"},"type":"diagnostic"}
{"@level":"info","@message":"Plan: 3 to import, 0 to add, $change to change, 0 to destroy.","changes":{"add":0,"change":$change,"import":3,"remove":0,"forget":0,"operation":"plan"},"type":"change_summary"}
EOF
}
stream import 0 >"$W/clean.jsonl"
stream update 1 >"$W/differs.jsonl"
cat >"$W/failed.jsonl" <<EOF
{"@level":"info","@message":"OpenTofu 1.13.9","type":"version","tofu":"1.13.9","ui":"1.2"}
{"@level":"error","@message":"Error: Cannot import non-existent remote object","diagnostic":{"severity":"error","summary":"Cannot import non-existent remote object","detail":"No object accounts/$ACCOUNT/$APP\nfor anna@example.com\n(\"$TUNNEL\").","address":"cloudflare_zero_trust_access_application.back_office","snippet":{"values":[{"traversal":"var.account_id","statement":"is \"$ACCOUNT\""}]}},"type":"diagnostic"}
EOF
echo '{"@level":"info","@message":"OpenTofu 1.13.9","type":"version"}' >"$W/no-summary.jsonl"

# steps <subcommand>: a step in an empty job folder, with the variables of the runner and those
# of the array step_env; with SUMMARY_TEXT, results/summary.md holds it first.
step_env=()
steps() {
  rm -rf "$W/job" && mkdir -p "$W/job" && : >"$W/log"
  (
    cd "$W/job" || exit 1
    if [[ -n "${SUMMARY_TEXT:-}" ]]; then mkdir -p results && echo "$SUMMARY_TEXT" >results/summary.md; fi
    export GITHUB_OUTPUT="$W/job/output" GITHUB_STEP_SUMMARY="$W/job/step-summary" FAKE_LOG="$W/log"
    export NFHOTEL_GH="$W/gh" NFHOTEL_EDGE_TOFU="$W/edge-tofu"
    ((${#step_env[@]} == 0)) || export "${step_env[@]}"
    bash "$OPS/scripts/edge-comparison-steps.sh" "$@"
  ) >"$W/stdout" 2>&1
}
# The names of the secrets, as the workflow passes them to check.
names=(READ_TOKEN ACCOUNT_ID TUNNEL_ID ACCESS_APP_ID ACCESS_POLICY_ID TEAM_NAME TEAM_EMAILS)
has() {
  local name
  step_env=()
  for name in "${names[@]}"; do step_env+=("HAS_CLOUDFLARE_$name=${1:-true}"); done
}

has true
steps check && [[ "$(cat "$W/job/output")" == ready=true ]] && ! grep -q '::warning::' "$W/stdout" &&
  ok "check with every secret is ready, without a warning" || ko "check all: $(cat "$W/stdout")"
has true
step_env[0]=HAS_CLOUDFLARE_READ_TOKEN=false step_env[6]=HAS_CLOUDFLARE_TEAM_EMAILS=false
steps check && [[ "$(cat "$W/job/output")" == ready=false ]] &&
  grep -q "::warning::not compared: the environment cloudflare-read lacks the secrets CLOUDFLARE_READ_TOKEN, CLOUDFLARE_TEAM_EMAILS;" "$W/stdout" &&
  grep -q "lacks the secrets CLOUDFLARE_READ_TOKEN, CLOUDFLARE_TEAM_EMAILS" "$W/job/step-summary" &&
  ok "check with missing secrets names them in a warning and the summary, and stays green" ||
  ko "check missing: $(cat "$W/stdout")"
has true
unset 'step_env[2]'
step_env=("${step_env[@]}")
steps check && ko "check accepted a secret the workflow did not pass" ||
  { grep -q "::error::HAS_CLOUDFLARE_TUNNEL_ID is not true or false" "$W/stdout" && [[ ! -s "$W/job/output" ]] &&
    ok "check fails when the workflow passes no yes or no for a secret"; } || ko "check unset: $(cat "$W/stdout")"

step_env=("TF_VAR_team_emails=$EMAILS" "CLOUDFLARE_API_TOKEN=$TOKEN")
FAKE_STREAM="$W/clean.jsonl" FAKE_STDERR="Digest: sha256:$ACCOUNT$ACCOUNT pulled for anna@example.com" steps compare &&
  [[ "$(cat "$W/job/output")" == differs=false ]] && grep -q "^edge-tofu plan-json$" "$W/log" &&
  grep -qF "The live account matches the code in \`ops/infra/cloudflare/\`: 3 to import, 0 to add, 0 to change, 0 to destroy." "$W/job/results/summary.md" &&
  grep -q "^Plan: 3 to import, 0 to add, 0 to change, 0 to destroy.$" "$W/stdout" &&
  cmp -s "$W/job/results/summary.md" "$W/job/step-summary" &&
  ok "compare of a clean plan writes the counts and differs=false" || ko "compare clean: $(cat "$W/stdout")"
grep -q "^::add-mask::anna@example.com$" "$W/stdout" && grep -q "^::add-mask::bo@example.org$" "$W/stdout" &&
  ok "compare masks each address in the log first" || ko "masks: $(cat "$W/stdout")"
grep -q "Digest: sha256:<id> pulled for <address>" "$W/stdout" &&
  ok "the output of docker and tofu init is shown with identifiers and addresses replaced" ||
  ko "stderr: $(cat "$W/stdout")"
! leaks <(grep -v '^::add-mask::' "$W/stdout") "$W/job/results/summary.md" "$W/job/step-summary" &&
  ok "the clean run prints no identifier and no address" || ko "clean leaks: $(cat "$W/stdout")"

FAKE_STREAM="$W/differs.jsonl" steps compare && [[ "$(cat "$W/job/output")" == differs=true ]] &&
  grep -qF -- "- \`cloudflare_zero_trust_access_application.back_office\`: update" "$W/job/results/summary.md" &&
  grep -q "found a difference: 3 to import, 0 to add, 1 to change, 0 to destroy." "$W/job/results/summary.md" &&
  grep -q "^Differs: cloudflare_zero_trust_access_application.back_office (update)$" "$W/stdout" &&
  ! grep -q -e "policy.team (" -e "nf_hotel (" "$W/stdout" &&
  ok "compare of a difference names the object and its action, not the imports" || ko "compare differs: $(cat "$W/stdout")"
! leaks <(grep -v '^::add-mask::' "$W/stdout") "$W/job/results/summary.md" "$W/job/step-summary" &&
  ok "the run with a difference prints no identifier and no address" || ko "differs leaks: $(cat "$W/stdout")"

FAKE_STREAM="$W/failed.jsonl" FAKE_EXIT=1 steps compare && ko "compare accepted a failed plan" ||
  { grep -q "^::error::Cannot import non-existent remote object (cloudflare_zero_trust_access_application.back_office): No object accounts/<id>/<id> for <address> (\"<id>\").$" "$W/stdout" &&
    grep -q "::error::tofu plan failed (exit 1)" "$W/stdout" && [[ ! -s "$W/job/output" ]] &&
    ok "compare fails a failed plan, with its errors redacted"; } || ko "compare failed: $(cat "$W/stdout")"
! leaks <(grep -v '^::add-mask::' "$W/stdout") "$W/job/step-summary" &&
  ok "the failed run prints no identifier and no address" || ko "failed leaks: $(cat "$W/stdout")"
FAKE_EXIT=1 steps compare && ko "compare accepted a plan that failed without a message" ||
  { grep -q "::error::tofu plan failed (exit 1)" "$W/stdout" && ok "compare fails a plan that failed without a message"; } ||
  ko "compare silent failure: $(cat "$W/stdout")"
FAKE_STREAM="$W/no-summary.jsonl" steps compare && ko "compare accepted a plan without a summary" ||
  { grep -q "::error::tofu plan gave no summary of its changes" "$W/stdout" && [[ ! -s "$W/job/output" ]] &&
    ok "compare fails a plan without a summary"; } || ko "no summary: $(cat "$W/stdout")"
step_env=("TF_VAR_team_emails=anna@example.com" "CLOUDFLARE_API_TOKEN=$TOKEN")
FAKE_STREAM="$W/clean.jsonl" steps compare && ko "compare accepted addresses that are not a JSON list" ||
  { grep -q "::error::TF_VAR_team_emails, from the secret CLOUDFLARE_TEAM_EMAILS, is not a JSON list" "$W/stdout" &&
    ! grep -q "^edge-tofu" "$W/log" && ! leaks "$W/stdout" &&
    ok "compare refuses addresses that are not a JSON list, without quoting them"; } || ko "bad list: $(cat "$W/stdout")"

step_env=(REPO=NF-Hotel/NFHotel SERVER=https://github.com RUN_ID=42 DIFFERS=true)
SUMMARY_TEXT="the summary" steps report &&
  grep -qF 'gh issue list --repo NF-Hotel/NFHotel --state open --search in:title "The public edge differs from its code"' "$W/log" &&
  grep -q "^gh issue create --repo NF-Hotel/NFHotel --title The public edge differs from its code --body-file results/summary.md$" "$W/log" &&
  grep -q "^Run: https://github.com/NF-Hotel/NFHotel/actions/runs/42$" "$W/job/results/summary.md" &&
  grep -q "::warning::the public edge differs from its code" "$W/stdout" &&
  ok "report of a difference without an open issue opens one, with the run" || ko "report create: $(cat "$W/stdout" "$W/log")"
SUMMARY_TEXT="the summary" FAKE_OPEN=7 steps report &&
  grep -q "^gh issue comment 7 --repo NF-Hotel/NFHotel --body-file results/summary.md$" "$W/log" &&
  ! grep -q "^gh issue create" "$W/log" &&
  ok "report of a difference with an open issue comments on it" || ko "report comment: $(cat "$W/log")"
step_env=(REPO=NF-Hotel/NFHotel SERVER=https://github.com RUN_ID=42 DIFFERS=false)
SUMMARY_TEXT="the summary" FAKE_OPEN=7 steps report &&
  grep -q "^gh issue close 7 --repo NF-Hotel/NFHotel --comment the summary" "$W/log" &&
  ! grep -q '::warning::' "$W/stdout" &&
  ok "report of a clean plan closes the open issue with the summary" || ko "report close: $(cat "$W/log")"
SUMMARY_TEXT="the summary" steps report && ! grep -q -e "^gh issue create" -e "^gh issue comment" -e "^gh issue close" "$W/log" &&
  ok "report of a clean plan without an open issue writes nothing" || ko "report quiet: $(cat "$W/log")"
step_env=(REPO=NF-Hotel/NFHotel SERVER=https://github.com RUN_ID=42 DIFFERS=)
SUMMARY_TEXT="the summary" steps report && ko "report accepted no result of compare" ||
  { grep -q "::error::DIFFERS is not true or false" "$W/stdout" && [[ ! -s "$W/log" ]] &&
    ok "report without the result of compare fails before gh"; } || ko "report differs: $(cat "$W/stdout")"
step_env=(REPO=NF-Hotel/NFHotel SERVER=https://github.com RUN_ID=42 DIFFERS=true)
steps report && ko "report ran without a summary" ||
  { grep -q "::error::no results/summary.md" "$W/stdout" && [[ ! -s "$W/log" ]] &&
    ok "report without a summary fails before gh"; } || ko "report no summary: $(cat "$W/stdout")"

echo "== the look at the public security.txt (MIL-084 task 5, ADR-0048)"
readonly SECURITY_TXT=https://nfhotel.devmatiker.com/.well-known/security.txt FRONT_PAGE=https://nfhotel.devmatiker.com/
readonly FILE="$OPS/deploy/nginx/well-known/security.txt" LOOK_TITLE="The public security.txt fails its weekly check"
tick=$'\x60'
# The fake check-security-txt.sh logs its arguments, prints FAKE_FOUND and exits with FAKE_EXIT.
cat >"$W/check" <<'EOF'
#!/bin/bash
echo "check $*" >>"$FAKE_LOG"
[[ -z "${FAKE_FOUND:-}" ]] || printf '%s\n' "$FAKE_FOUND"
exit "${FAKE_EXIT:-0}"
EOF
chmod +x "$W/check"
step_env=("NFHOTEL_CHECK_SECURITY_TXT=$W/check")
FAKE_FOUND="PASS  $SECURITY_TXT meets RFC 9116 and the rules of ADR-0048" steps look &&
  [[ "$(cat "$W/job/output")" == failed=false ]] &&
  [[ "$(grep '^check ' "$W/log")" == "check --url $SECURITY_TXT --canonical $SECURITY_TXT --same-as $FILE --links --hsts-like $FRONT_PAGE --expect-warnings unsigned,no-encryption,no-role-address" ]] &&
  cmp -s "$W/job/results/summary.md" "$W/job/step-summary" &&
  ok "look checks the public address with the file of the checkout, the addresses it names and the front page" ||
  ko "look arguments: $(cat "$W/log" "$W/stdout")"
FAKE_FOUND="error: --same-as names no readable file" FAKE_EXIT=2 steps look && ko "look accepted a check that could not run" ||
  { grep -q "^::error::check-security-txt.sh stopped with exit 2" "$W/stdout" &&
    grep -q "^error: --same-as names no readable file$" "$W/stdout" && [[ ! -s "$W/job/output" ]] &&
    ok "look fails when the check cannot run, and shows what the check said"; } || ko "look exit 2: $(cat "$W/stdout")"
FAKE_FOUND="$(for n in $(seq 60); do echo "FAIL  ADR-0048 rule: finding $tick$n$tick"; done)" FAKE_EXIT=1 steps look &&
  [[ "$(cat "$W/job/output")" == failed=true ]] &&
  grep -q "^FAIL  ADR-0048 rule: finding 50$" "$W/job/results/summary.md" && ! grep -q "finding 51" "$W/job/results/summary.md" &&
  grep -q "^10 more lines are in the log of the run.$" "$W/job/results/summary.md" &&
  grep -qF "finding ${tick}60$tick" "$W/stdout" && [[ "$(grep -c "^$tick$tick$tick" "$W/job/results/summary.md")" == 2 ]] &&
  ok "the issue quotes the first 50 lines of the check without backticks, and the log has them all" ||
  ko "look lines: $(cat "$W/job/results/summary.md")"

# The real check-security-txt.sh, through a fake curl. The fake answers an address from
# $FAKE_WEB/<the address with _ for each other character>/: status, headers, body, and with
# --location the final address in effective. It logs each address it is asked.
cat >"$W/curl" <<'EOF'
#!/bin/bash
output=/dev/null headers=/dev/null format="" address="" follow=0
while (($#)); do
  case "$1" in
    --output) output="$2" && shift ;;
    --dump-header) headers="$2" && shift ;;
    --write-out) format="$2" && shift ;;
    --location) follow=1 ;;
    --max-time | --max-filesize | --proto | --proto-redir | --max-redirs) shift ;;
    -*) ;;
    *) address="$1" ;;
  esac
  shift
done
echo "$address" >>"$FAKE_WEB/asked"
d="$FAKE_WEB/$(printf '%s' "$address" | tr -c 'A-Za-z0-9' '_')"
[[ -d "$d" ]] || { echo "curl: (6) Could not resolve host" >&2 && exit 6; }
final="$address"
((follow)) && [[ -f "$d/effective" ]] && final="$(cat "$d/effective")"
cat "$d/headers" >"$headers"
cat "$d/body" >"$output"
format="${format//'%{http_code}'/$(cat "$d/status")}"
printf '%s' "${format//'%{url_effective}'/$final}"
EOF
chmod +x "$W/curl"
# place <address>: the folder of the fake for the address.
place() { printf '%s/%s' "$W/web" "$(printf '%s' "$1" | tr -c 'A-Za-z0-9' '_')"; }
# serve <address> <status> <body file> [header...]: the fake answers the address with them.
serve() {
  local d
  d="$(place "$1")"
  mkdir -p "$d"
  echo "$2" >"$d/status"
  printf 'HTTP/1.1 %s\r\n' "$2" >"$d/headers"
  if (($# > 3)); then printf '%s\r\n' "${@:4}" >>"$d/headers"; fi
  cp "$3" "$d/body"
  rm -f "$d/effective"
}
# moved <address> <final address>: with --location, the address ends at the final address.
moved() { echo "$2" >"$(place "$1")/effective"; }
# The https addresses that the fields of the file name, and two of them.
mapfile -t NAMED < <(sed -n 's/^[A-Za-z-]*: \(https:[^[:space:]]*\)$/\1/p' "$FILE")
CONTACT="$(sed -n 's/^Contact: \(https:.*\)$/\1/p' "$FILE")"
POLICY_PAGE="$(sed -n 's/^Policy: //p' "$FILE")"
readonly TYPE='Content-Type: text/plain; charset=utf-8' HTML='Content-Type: text/html; charset=utf-8'
readonly HSTS='Strict-Transport-Security: max-age=2592000'
# site: the answers after the deploy: the file as the public listener serves it, the front page,
# each address that the file names, and the report form of GitHub, which sends a visitor without
# a session to the sign-in on its own host.
site() {
  local address
  rm -rf "$W/web" && mkdir -p "$W/web"
  for address in "${NAMED[@]}"; do serve "$address" 200 /dev/null "$HTML"; done
  serve "$SECURITY_TXT" 200 "$FILE" "$TYPE" "X-Content-Type-Options: nosniff" "$HSTS"
  serve "$FRONT_PAGE" 200 /dev/null "$HTML" "$HSTS"
  moved "$CONTACT" https://github.com/login
}
expires_s="$(date -u -d "$(sed -n 's/^Expires: //p' "$FILE")" +%s)"
# looks [days]: the steps look and look-report in one job folder, as the job runs them, each with
# the variables that the workflow gives it, and the clock of the check the days (default 100)
# before the Expires of the file; FAILED is what look wrote.
looks() {
  local now
  now="$(date -u -d "@$((expires_s - ${1:-100} * 86400))" +%Y-%m-%dT%H:%M:%SZ)"
  rm -rf "$W/job" && mkdir -p "$W/job" && : >"$W/log"
  (
    cd "$W/job" || exit 1
    env GITHUB_OUTPUT="$W/job/output" GITHUB_STEP_SUMMARY="$W/job/step-summary" NFHOTEL_NOW="$now" \
      NFHOTEL_CURL="$W/curl" FAKE_WEB="$W/web" bash "$OPS/scripts/edge-comparison-steps.sh" look || exit
    env REPO=NF-Hotel/NFHotel SERVER=https://github.com RUN_ID=42 FAILED="$(sed -n 's/^failed=//p' output)" \
      NFHOTEL_GH="$W/gh" FAKE_LOG="$W/log" bash "$OPS/scripts/edge-comparison-steps.sh" look-report
  ) >"$W/stdout" 2>&1
}

site
looks && [[ "$(cat "$W/job/output")" == failed=false ]] &&
  grep -qF "The weekly look at $SECURITY_TXT passed" "$W/job/results/summary.md" &&
  grep -qF "PASS  $SECURITY_TXT meets RFC 9116 and the rules of ADR-0048 (warnings: no-encryption,no-role-address,unsigned)" "$W/job/results/summary.md" &&
  grep -qF "gh issue list --repo NF-Hotel/NFHotel --state open --search in:title \"$LOOK_TITLE\"" "$W/log" &&
  ! grep -q -e "^gh issue create" -e "^gh issue comment" -e "^gh issue close" "$W/log" && ! grep -q '::warning::' "$W/stdout" &&
  ok "the file of the repository, served as the public listener serves it, passes the look, and no issue is written" ||
  ko "look passes: $(cat "$W/stdout" "$W/log")"
missing=""
for address in "$FRONT_PAGE" "${NAMED[@]}"; do grep -qxF "$address" "$W/web/asked" || missing+=" $address"; done
[[ -z "$missing" ]] && ok "the look asks the front page and each address that the file names" || ko "not asked:$missing"
[[ -s "$W/job/results/summary.md" ]] && ! grep -q '@' "$W/job/results/summary.md" "$W/stdout" &&
  ok "no mailbox address reaches the issue or the log" ||
  ko "a mailbox: $(cat "$W/job/results/summary.md")"
FAKE_OPEN=9 looks && grep -q "^gh issue close 9 --repo NF-Hotel/NFHotel --comment ## The public security.txt$" "$W/log" &&
  ok "a look that passes closes the open issue with its summary" || ko "look close: $(cat "$W/log")"

serve "$SECURITY_TXT" 404 /dev/null "$HTML"
looks && [[ "$(cat "$W/job/output")" == failed=true ]] &&
  grep -qF "FAIL  RFC 9116 3: $SECURITY_TXT answers 404, not 200" "$W/job/results/summary.md" &&
  grep -q "^gh issue create --repo NF-Hotel/NFHotel --title $LOOK_TITLE --body-file results/summary.md$" "$W/log" &&
  grep -q "^Run: https://github.com/NF-Hotel/NFHotel/actions/runs/42$" "$W/job/results/summary.md" &&
  grep -qF "::warning::the public security.txt fails its weekly check; see the issue \"$LOOK_TITLE\"" "$W/stdout" &&
  ok "a missing file opens the issue, with the run, and leaves the run green" || ko "look missing: $(cat "$W/stdout" "$W/log")"
site
sed '1s/.*/# A line that main does not have./' "$FILE" >"$W/changed"
serve "$SECURITY_TXT" 200 "$W/changed" "$TYPE" "$HSTS"
FAKE_OPEN=9 looks && [[ "$(cat "$W/job/output")" == failed=true ]] &&
  grep -qF "FAIL  ADR-0048 rule: the body differs from $FILE" "$W/job/results/summary.md" &&
  [[ "$(grep -c '^FAIL' "$W/job/results/summary.md")" == 1 ]] &&
  grep -q "^gh issue comment 9 --repo NF-Hotel/NFHotel --body-file results/summary.md$" "$W/log" &&
  ok "a file that differs from main comments on the open issue" || ko "look differs: $(cat "$W/stdout" "$W/log")"
site
looks 20 && grep -qF "FAIL  ADR-0048 rule: Expires is 20 days ahead, fewer than 30" "$W/job/results/summary.md" &&
  [[ "$(grep -c '^FAIL' "$W/job/results/summary.md")" == 1 ]] && grep -q "^gh issue create" "$W/log" &&
  ok "an Expires fewer than 30 days ahead opens the issue" || ko "look expires: $(cat "$W/stdout")"
serve "$POLICY_PAGE" 404 /dev/null "$HTML"
looks && grep -qF "(Policy) answers 404" "$W/job/results/summary.md" && grep -q "^gh issue create" "$W/log" &&
  ok "a named address that fails opens the issue" || ko "look dead address: $(cat "$W/stdout")"
site
moved "$CONTACT" https://login.example.net/
looks && grep -qF "(Contact) ends on another host: login.example.net" "$W/job/results/summary.md" &&
  grep -q "^gh issue create" "$W/log" && ok "a named address that ends on another host opens the issue" ||
  ko "look other host: $(cat "$W/stdout")"
site
serve "$FRONT_PAGE" 200 /dev/null "$HTML" "Strict-Transport-Security: max-age=31536000"
looks && grep -qF "Strict-Transport-Security is 'max-age=2592000', and $FRONT_PAGE sends 'max-age=31536000'" \
  "$W/job/results/summary.md" && grep -q "^gh issue create" "$W/log" &&
  ok "a Strict-Transport-Security other than the one of the front page opens the issue" || ko "look hsts: $(cat "$W/stdout")"

step_env=(REPO=NF-Hotel/NFHotel SERVER=https://github.com RUN_ID=42 FAILED=yes)
SUMMARY_TEXT="the summary" steps look-report && ko "look-report accepted no result of look" ||
  { grep -q "::error::FAILED is not true or false" "$W/stdout" && [[ ! -s "$W/log" ]] &&
    ok "look-report without the result of look fails before gh"; } || ko "look-report failed: $(cat "$W/stdout")"
step_env=(REPO=NF-Hotel/NFHotel SERVER=https://github.com RUN_ID=42 FAILED=true)
steps look-report && ko "look-report ran without a summary" ||
  { grep -q "::error::no results/summary.md: the step look did not write it" "$W/stdout" && [[ ! -s "$W/log" ]] &&
    ok "look-report without a summary fails before gh"; } || ko "look-report no summary: $(cat "$W/stdout")"

step_env=()
steps
[[ $? == 2 ]] && ok "the steps without a subcommand are a usage error" || ko "no subcommand: $(cat "$W/stdout")"
steps deploy
[[ $? == 2 ]] && ok "an unknown step is a usage error" || ko "unknown step: $(cat "$W/stdout")"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
