#!/usr/bin/env bash
# The steps of the CD workflow (.github/workflows/cd.yaml, ADR-0007, MIL-021 task 6),
# one subcommand per step, so the workflow keeps each run block to one command
# (QC-YML-001 criterion 7). It runs on the GitHub runner, never on the VM.
#
# Subcommands:
#   check-run      the run in RUN_ID must be a successful "NF Hotel Release" run of main
#                  in this repository; writes id=<run> to GITHUB_OUTPUT
#   read-manifest  read release.json, check every field against a strict pattern and
#                  write sha and one digest per image to GITHUB_OUTPUT
#   deploy         run the deploy command on the VM over Tailscale SSH; changes the VM,
#                  so it is only called after the approval and, for a manual run, only
#                  when APPLY is true
#   summary        append the audit record of the run to GITHUB_STEP_SUMMARY
#
# Environment (set by the workflow through env:, never interpolated):
#   GITHUB_REPOSITORY, GITHUB_OUTPUT, GITHUB_STEP_SUMMARY, GITHUB_SERVER_URL,
#   GITHUB_ACTOR, GITHUB_EVENT_NAME   set by the runner
#   GH_TOKEN     the job token, for check-run
#   RUN_ID       the release run, for check-run and summary
#   VM_HOST      the tailnet name of the VM, for deploy
#   SHA WEB API ANALYSIS BENCHMARK   the checked manifest fields, for deploy and summary
#   OUTCOME      the job status, for summary
#
# Usage: cd-steps.sh check-run|read-manifest|deploy|summary
# Exit:  0 the step passed; 1 the step failed (with an ::error:: line); 2 usage error
# Needs: bash 4+, gh, jq, tailscale (for deploy)

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"
readonly RELEASE_WORKFLOW="NF Hotel Release"
readonly IMAGE_NAMES=(nfhotel-web nfhotel-api market-analysis benchmark)
readonly MANIFEST=release.json
readonly FENCE='```'

fail() {
  echo "::error::$*"
  exit 1
}

check_run() {
  local run
  [[ "${RUN_ID:-}" =~ ^[0-9]+$ ]] || fail "the release run ID must be a number"
  run="$(gh api "repos/$GITHUB_REPOSITORY/actions/runs/$RUN_ID")" || fail "cannot read run $RUN_ID"
  jq -e --arg workflow "$RELEASE_WORKFLOW" --arg repo "$GITHUB_REPOSITORY" \
    '.name == $workflow and .head_branch == "main" and .conclusion == "success" and .repository.full_name == $repo' \
    <<<"$run" >/dev/null ||
    fail "run $RUN_ID is not a successful $RELEASE_WORKFLOW run of main in $GITHUB_REPOSITORY"
  echo "id=$RUN_ID" >>"$GITHUB_OUTPUT"
}

read_manifest() {
  local sha name ref
  [[ -f "$MANIFEST" ]] || fail "$MANIFEST is missing"
  sha="$(jq -r '.sha' "$MANIFEST")"
  [[ "$sha" =~ ^[0-9a-f]{40}$ ]] || fail "bad commit in $MANIFEST"
  echo "sha=$sha" >>"$GITHUB_OUTPUT"
  for name in "${IMAGE_NAMES[@]}"; do
    ref="$(jq -r --arg n "$name" '.images[$n]' "$MANIFEST")"
    [[ "$ref" =~ ^ghcr\.io/nf-hotel/${name}@sha256:[0-9a-f]{64}$ ]] || fail "bad digest for $name in $MANIFEST"
    echo "${name//-/_}=$ref" >>"$GITHUB_OUTPUT"
  done
}

deploy() {
  [[ "${VM_HOST:-}" =~ ^[a-z0-9.-]+$ ]] || fail "set the environment variable NFHOTEL_VM_HOST of production"
  local status=0
  tailscale ssh "deploy@$VM_HOST" \
    sudo -n /usr/local/sbin/nfhotel-deploy deploy "$SHA" "$WEB" "$API" "$ANALYSIS" "$BENCHMARK" \
    2>&1 | tee deploy.log || status=$?
  ((status == 0)) || fail "the deploy on $VM_HOST failed (exit $status); see the deploy log"
}

summary() {
  {
    echo "## Deployment to production: ${OUTCOME:-unknown}"
    echo
    echo "- Commit: \`${SHA:-none}\`"
    echo "- Release run: $GITHUB_SERVER_URL/$GITHUB_REPOSITORY/actions/runs/${RUN_ID:-none}"
    echo "- Started by: $GITHUB_ACTOR ($GITHUB_EVENT_NAME); the approver is in the environment history"
    if [[ -f "$MANIFEST" ]]; then
      printf '\n%sjson\n%s\n%s\n' "$FENCE" "$(cat "$MANIFEST")" "$FENCE"
    fi
    if [[ -f deploy.log ]]; then
      printf '\n<details><summary>Deploy log</summary>\n\n%s\n%s\n%s\n</details>\n' "$FENCE" "$(tail -n 60 deploy.log)" "$FENCE"
    fi
  } >>"$GITHUB_STEP_SUMMARY"
}

case "${1:-}" in
  check-run) check_run ;;
  read-manifest) read_manifest ;;
  deploy) deploy ;;
  summary) summary ;;
  -h | --help) usage ;;
  *)
    usage >&2
    exit 2
    ;;
esac
