#!/usr/bin/env bash
# The steps of the workflow running-image-scan.yaml (MIL-036, MIL-064 task 6), one subcommand per
# step, so that the workflow keeps one line of shell per step (QC-YML-001 criterion 7), as
# cd-steps.sh does for NF Hotel CD.
#
# Usage: running-image-scan-steps.sh find|scan
#   find  write release/release.json. With TEST_IMAGE, a manifest of that one image, which proves
#         the issue path with an image that has known vulnerabilities. Otherwise the release.json
#         of the release in production: the head of the last successful run of NF Hotel CD on
#         main, and the artifact release-manifest of the NF Hotel Release run of that commit. For a
#         release it writes devops_sha, its operations commit (40 hex), to GITHUB_OUTPUT, for the
#         checkout of ops/deploy at that commit, and names the release in the step summary.
#   scan  run scan-running-release.sh on release/release.json into results/, with --third-party
#         running/ops/deploy when DEVOPS_SHA is set (the workflow checks ops/deploy out there, at
#         that commit); write failed=true or failed=false to GITHUB_OUTPUT and the summary of the
#         scan to the step summary. A failed gate does not fail the step: the next step of the
#         workflow reports it in the issue.
# Both run in the folder of the checkout, which holds release/, results/ and running/.
#
# Env:   REPO, GITHUB_SHA, GITHUB_OUTPUT, GITHUB_STEP_SUMMARY and GH_TOKEN, as the workflow sets
#        them; TEST_IMAGE (find); DEVOPS_SHA (scan); NFHOTEL_GH (default gh) and
#        NFHOTEL_SCAN_RELEASE (default scripts/scan-running-release.sh), for the tests
# Exit:  0 done; 1 no release in production, or a manifest without an operations commit;
#        2 usage error
# Needs: bash 4+, gh, jq

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
gh="${NFHOTEL_GH:-gh}"
scan_release="${NFHOTEL_SCAN_RELEASE:-$ROOT/scripts/scan-running-release.sh}"
readonly MANIFEST=release/release.json

fail() {
  echo "::error::$*" >&2
  exit 1
}

usage() {
  echo "usage: running-image-scan-steps.sh find|scan" >&2
  exit 2
}

find_release() {
  local sha run ops
  mkdir -p release
  if [[ -n "${TEST_IMAGE:-}" ]]; then
    jq -n --arg sha "${GITHUB_SHA:?GITHUB_SHA is not set}" --arg image "$TEST_IMAGE" \
      '{sha: $sha, images: {test: $image}}' >"$MANIFEST"
    return 0
  fi
  : "${REPO:?REPO is not set}" "${GITHUB_OUTPUT:?GITHUB_OUTPUT is not set}"
  : "${GITHUB_STEP_SUMMARY:?GITHUB_STEP_SUMMARY is not set}"
  sha="$("$gh" run list --repo "$REPO" --workflow cd.yaml --branch main --status success \
    --limit 1 --json headSha --jq '.[0].headSha')"
  [[ "$sha" =~ ^[0-9a-f]{40}$ ]] || fail "no successful CD run found"
  run="$("$gh" run list --repo "$REPO" --workflow release.yaml --commit "$sha" \
    --status success --limit 1 --json databaseId --jq '.[0].databaseId')"
  [[ "$run" =~ ^[0-9]+$ ]] || fail "no release run for $sha"
  "$gh" run download "$run" --repo "$REPO" --name release-manifest --dir release
  ops="$(jq -r '.devops_sha // empty' "$MANIFEST")"
  [[ "$ops" =~ ^[0-9a-f]{40}$ ]] || fail "the release.json of $sha has no operations commit"
  echo "devops_sha=$ops" >>"$GITHUB_OUTPUT"
  echo "Release in production: $sha (NF Hotel Release run $run), operations commit $ops" \
    >>"$GITHUB_STEP_SUMMARY"
}

scan() {
  local args=(--manifest "$MANIFEST" --results results)
  : "${GITHUB_OUTPUT:?GITHUB_OUTPUT is not set}" "${GITHUB_STEP_SUMMARY:?GITHUB_STEP_SUMMARY is not set}"
  [[ -z "${DEVOPS_SHA:-}" ]] || args+=(--third-party running/ops/deploy)
  if bash "$scan_release" "${args[@]}"; then
    echo "failed=false" >>"$GITHUB_OUTPUT"
  else
    echo "failed=true" >>"$GITHUB_OUTPUT"
  fi
  cat results/summary.md >>"$GITHUB_STEP_SUMMARY"
}

(($# == 1)) || usage
case "$1" in
  find) find_release ;;
  scan) scan ;;
  *) usage ;;
esac
