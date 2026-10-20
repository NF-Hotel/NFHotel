#!/usr/bin/env bash
# The build step of the release-images action (.github/actions/release-images,
# ADR-0012 of NFHotel): runs build-release-images.sh with the action's inputs, and with
# --push only when PUSH is 'true', so the action keeps its run block to one command
# (QC-YML-001 criterion 7). After a push it writes the digest of every built image as
# the step output <name>-digest, for the attestation steps (MIL-022 task 2); a frozen
# image gets none, because it was not built here. With a push it also makes the idempotent
# migration script migrations.sql of the release, whose SHA-256 goes into the manifest
# (MIL-022 task 6).
#
# Env:   APP_PATH, APP_SHA, REGISTRY, PUSH, DEVOPS_SHA   the action inputs
#        SCAN_DIR                                        the scan folder (see the action)
#        GITHUB_STEP_SUMMARY, GITHUB_OUTPUT              set by the runner
# Usage: release-steps.sh
# Exit:  the exit code of build-release-images.sh
# Needs: bash 4+

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
args=(--app-dir "${APP_PATH:?}" --sha "${APP_SHA:?}" --devops-sha "${DEVOPS_SHA:?}"
  --registry "${REGISTRY:?}" --scan-dir "${SCAN_DIR:?}")
if [[ "${PUSH:-false}" == "true" ]]; then
  bash "$ROOT/scripts/make-migration-script.sh" "$APP_PATH" migrations.sql
  args+=(--push --output release.json --summary "${GITHUB_STEP_SUMMARY:-/dev/null}" --migrations migrations.sql)
fi
bash "$ROOT/scripts/build-release-images.sh" "${args[@]}"

for file in "$SCAN_DIR"/*.digest; do
  [[ -f "$file" ]] || continue
  echo "$(basename "$file" .digest)-digest=$(<"$file")" >>"${GITHUB_OUTPUT:-/dev/null}"
done
