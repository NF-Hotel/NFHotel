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
# the daily scan of the running images does. Fakes stand in for docker, gh and edge-tofu.sh, so
# nothing reaches Cloudflare or GitHub. The identifiers and addresses below are made up.
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

step_env=()
steps
[[ $? == 2 ]] && ok "the steps without a subcommand are a usage error" || ko "no subcommand: $(cat "$W/stdout")"
steps deploy
[[ $? == 2 ]] && ok "an unknown step is a usage error" || ko "unknown step: $(cat "$W/stdout")"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
