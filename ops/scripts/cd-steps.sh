#!/usr/bin/env bash
# The steps of the deploy-release action (.github/actions/deploy-release, ADR-0007,
# MIL-021 task 6), called by cd.yaml of NF-Hotel/NFHotel (ADR-0012 of NFHotel), one
# subcommand per step (QC-YML-001 criterion 7). It runs on the GitHub runner of the
# NFHotel job, never on the VM.
#
# Subcommands:
#   check-run      the run in RUN_ID must be a successful "NF Hotel Release" run of main
#                  in this repository; writes id=<run> to GITHUB_OUTPUT
#   read-manifest  read release.json, check every field against a strict pattern and
#                  write sha and one digest per image to GITHUB_OUTPUT; when the manifest
#                  names a migration script, migrations.sql must be beside it with that
#                  SHA-256, which is written as migrations
#   verify         check the build provenance and the SBOM attestation of every image
#                  digest with gh attestation verify, signed by the release workflow of
#                  this repository (MIL-022 task 3); a frozen image (ADR-0500) is
#                  accepted only by its exact pinned digest. Any other digest without
#                  both attestations stops the deploy before the tailnet is joined
#   deploy         run the deploy command on the VM over Tailscale SSH; changes the VM,
#                  so it is only called after the approval and, for a manual run, only
#                  when APPLY is true. With MIGRATIONS set it first sends migrations.sql
#                  to the migrate command (backup, then the script as nfhotel_migrator),
#                  and deploys only when that succeeded (MIL-022 task 6)
#   summary        append the audit record of the run to GITHUB_STEP_SUMMARY
#
# Environment (set by the workflow through env:, never interpolated):
#   GITHUB_REPOSITORY, GITHUB_OUTPUT, GITHUB_STEP_SUMMARY, GITHUB_SERVER_URL,
#   GITHUB_ACTOR, GITHUB_EVENT_NAME   set by the runner
#   GH_TOKEN     the job token, for check-run and verify (attestations: read,
#                packages: read)
#   RUN_ID       the release run, for check-run and summary
#   VM_HOST      the tailnet name of the VM, for deploy
#   SHA DEVOPS_SHA WEB API ANALYSIS BENCHMARK   the checked manifest fields, for verify,
#                deploy and summary
#   MIGRATIONS   the checked SHA-256 of migrations.sql, for deploy (empty: none)
#   OUTCOME      the job status, for summary
#
# Usage: cd-steps.sh check-run|read-manifest|verify|deploy|summary
# Exit:  0 the step passed; 1 the step failed (with an ::error:: line); 2 usage error
# Needs: bash 4+, gh, jq, docker (for verify), tailscale (for deploy)

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"
readonly RELEASE_WORKFLOW="NF Hotel Release"
readonly IMAGE_NAMES=(nfhotel-web nfhotel-api market-analysis benchmark)
readonly MANIFEST=release.json
readonly MIGRATIONS_FILE=migrations.sql
readonly FENCE='```'
readonly OWNER=NF-Hotel
readonly SBOM_PREDICATE=https://spdx.dev/Document/v2.3

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
  local sha ops_sha name ref
  [[ -f "$MANIFEST" ]] || fail "$MANIFEST is missing"
  sha="$(jq -r '.sha' "$MANIFEST")"
  [[ "$sha" =~ ^[0-9a-f]{40}$ ]] || fail "bad commit in $MANIFEST"
  echo "sha=$sha" >>"$GITHUB_OUTPUT"
  ops_sha="$(jq -r '.devops_sha' "$MANIFEST")"
  [[ "$ops_sha" =~ ^[0-9a-f]{40}$ ]] || fail "bad DevOps commit in $MANIFEST"
  echo "devops_sha=$ops_sha" >>"$GITHUB_OUTPUT"
  local migrations
  migrations="$(jq -r '.migrations // ""' "$MANIFEST")"
  if [[ -n "$migrations" ]]; then
    [[ "$migrations" =~ ^[0-9a-f]{64}$ ]] || fail "bad migration script hash in $MANIFEST"
    [[ -f "$MIGRATIONS_FILE" ]] || fail "$MANIFEST names a migration script, but $MIGRATIONS_FILE is missing"
    [[ "$(sha256sum "$MIGRATIONS_FILE" | cut -d' ' -f1)" == "$migrations" ]] ||
      fail "$MIGRATIONS_FILE does not have the SHA-256 of $MANIFEST"
  fi
  echo "migrations=$migrations" >>"$GITHUB_OUTPUT"
  for name in "${IMAGE_NAMES[@]}"; do
    ref="$(jq -r --arg n "$name" '.images[$n]' "$MANIFEST")"
    [[ "$ref" =~ ^ghcr\.io/nf-hotel/${name}@sha256:[0-9a-f]{64}$ ]] || fail "bad digest for $name in $MANIFEST"
    echo "${name//-/_}=$ref" >>"$GITHUB_OUTPUT"
  done
}

verify() {
  local name var ref frozen logged_in=""
  [[ -n "${GITHUB_REPOSITORY:-}" ]] || fail "GITHUB_REPOSITORY is not set"
  local signer="$GITHUB_REPOSITORY/.github/workflows/release.yaml"
  for name in "${IMAGE_NAMES[@]}"; do
    case "$name" in
      nfhotel-web) var=WEB ;;
      nfhotel-api) var=API ;;
      market-analysis) var=ANALYSIS ;;
      benchmark) var=BENCHMARK ;;
    esac
    ref="${!var:-}"
    [[ "$ref" =~ ^ghcr\.io/nf-hotel/${name}@sha256:[0-9a-f]{64}$ ]] || fail "no checked digest for $name"
    frozen="$ROOT/deploy/$name/frozen-image"
    if [[ -f "$frozen" ]]; then
      [[ "$ref" == "$(tr -d '[:space:]' <"$frozen")" ]] ||
        fail "$name is frozen (ADR-0500), but the release names $ref, not the pinned digest"
      echo "==> $name is frozen at its pinned digest (ADR-0500): not built by a release, so no attestation"
      continue
    fi
    if [[ -z "${logged_in:-}" ]]; then
      # The packages are private: gh reads the digest through the Docker credentials.
      docker login ghcr.io --username "${GITHUB_ACTOR:-x}" --password-stdin <<<"${GH_TOKEN:?GH_TOKEN is not set}" >/dev/null ||
        fail "cannot log in to ghcr.io with the job token (packages: read)"
      logged_in=1
    fi
    echo "==> verifying $ref"
    gh attestation verify "oci://$ref" --owner "$OWNER" --signer-workflow "$signer" >/dev/null ||
      fail "$name has no valid build provenance from $signer: $ref"
    gh attestation verify "oci://$ref" --owner "$OWNER" --signer-workflow "$signer" \
      --predicate-type "$SBOM_PREDICATE" >/dev/null ||
      fail "$name has no valid SBOM attestation from $signer: $ref"
    echo "    build provenance and SBOM verified"
  done
}

deploy() {
  [[ "${VM_HOST:-}" =~ ^[a-z0-9.-]+$ ]] || fail "set the environment variable NFHOTEL_VM_HOST of production"
  local status=0
  if [[ -n "${MIGRATIONS:-}" ]]; then
    [[ "$MIGRATIONS" =~ ^[0-9a-f]{64}$ ]] || fail "bad migration script hash"
    tailscale ssh "deploy@$VM_HOST" sudo -n /usr/local/sbin/nfhotel-deploy migrate "$MIGRATIONS" \
      <"$MIGRATIONS_FILE" 2>&1 | tee migrate.log || status=$?
    ((status == 0)) || fail "the migrations on $VM_HOST failed (exit $status); nothing was deployed"
  fi
  tailscale ssh "deploy@$VM_HOST" \
    sudo -n /usr/local/sbin/nfhotel-deploy deploy "$SHA" "$DEVOPS_SHA" "$WEB" "$API" "$ANALYSIS" "$BENCHMARK" \
    2>&1 | tee deploy.log || status=$?
  ((status == 0)) || fail "the deploy on $VM_HOST failed (exit $status); see the deploy log"
}

summary() {
  {
    echo "## Deployment to production: ${OUTCOME:-unknown}"
    echo
    echo "- NFHotel commit: \`${SHA:-none}\`"
    echo "- DevOps commit: \`${DEVOPS_SHA:-none}\`"
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
  verify) verify ;;
  deploy) deploy ;;
  summary) summary ;;
  -h | --help) usage ;;
  *)
    usage >&2
    exit 2
    ;;
esac
