#!/usr/bin/env bash
# Scan the images of the release that runs in production, again, with today's vulnerability
# database (MIL-036 task 1). The images were scanned when they were built (MIL-022), but a
# vulnerability that is published after a deploy would otherwise wait for the next release.
# The workflow running-image-scan.yaml runs it daily on the release.json of the last release
# that CD deployed, and opens or updates an issue when the gate fails.
#
# Each image is pulled by its digest and scanned by scripts/scan-image.sh, with the gate of
# scripts/scan-gate.sh: a critical finding, or a high one with a fix.
#
# Usage: scan-running-release.sh --manifest <release.json> --results <folder>
#   writes <folder>/summary.md (Markdown for the issue) and the reports of scan-image.sh
# Env:   NFHOTEL_SCAN_IMAGE (default scripts/scan-image.sh; for the tests),
#        NFHOTEL_DOCKER (default docker; for the tests)
# Exit:  0 every image passes; 1 an image fails the gate or cannot be scanned; 2 usage error
# Needs: bash 4+, docker, jq

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
scan_image="${NFHOTEL_SCAN_IMAGE:-$ROOT/scripts/scan-image.sh}"
docker="${NFHOTEL_DOCKER:-docker}"

die() {
  echo "error: $*" >&2
  exit 1
}

usage() {
  echo "usage: scan-running-release.sh --manifest <release.json> --results <folder>" >&2
  exit 2
}

manifest="" results=""
while (($#)); do
  case "$1" in
    --manifest)
      (($# >= 2)) || usage
      manifest="$2"
      shift 2
      ;;
    --results)
      (($# >= 2)) || usage
      results="$2"
      shift 2
      ;;
    *) usage ;;
  esac
done
[[ -n "$manifest" && -n "$results" ]] || usage
[[ -r "$manifest" ]] || die "cannot read $manifest"
command -v jq >/dev/null || die "jq is missing"

sha="$(jq -r '.sha // empty' "$manifest")"
[[ "$sha" =~ ^[0-9a-f]{40}$ ]] || die "$manifest has no release commit (.sha)"
mapfile -t entries < <(jq -r '.images | to_entries[] | "\(.key) \(.value)"' "$manifest")
((${#entries[@]})) || die "$manifest names no image"

mkdir -p "$results"
summary="$results/summary.md"
failed=0
{
  echo "The daily scan of the release in production (\`${sha:0:7}\`) found vulnerabilities that the gate of DSA-001 section 8 does not accept: a critical one, or a high one with a fix. Rebuild and release the image, or accept the finding in \`ops/security/trivyignore.yaml\` with a reason and an expiry (MIL-036)."
  echo
} >"$summary.head"
: >"$summary.body"

for entry in "${entries[@]}"; do
  read -r name ref <<<"$entry"
  [[ "$name" =~ ^[a-z0-9-]+$ && "$ref" =~ ^[a-z0-9./-]+@sha256:[0-9a-f]{64}$ ]] ||
    die "$manifest has an image entry that is not <name> <registry/path@sha256:digest>: $entry"
  echo "==> $name: $ref"
  if ! "$docker" pull -q "$ref" >/dev/null 2>&1; then
    echo "- **$name**: the image could not be pulled (\`$ref\`)" >>"$summary.body"
    failed=1
    continue
  fi
  if bash "$scan_image" "$ref" "$name" "$results" >"$results/$name.gate.txt" 2>&1; then
    echo "    passes"
  else
    failed=1
    echo "    FAILS the gate"
    {
      echo "### $name"
      echo
      echo "\`$ref\`"
      echo
      echo '```'
      tail -n 40 "$results/$name.gate.txt"
      echo '```'
      echo
    } >>"$summary.body"
  fi
done

if ((failed)); then
  cat "$summary.head" "$summary.body" >"$summary"
else
  echo "The daily scan of the release in production (\`${sha:0:7}\`) found nothing that the gate refuses." >"$summary"
fi
rm -f "$summary.head" "$summary.body"
exit "$failed"
