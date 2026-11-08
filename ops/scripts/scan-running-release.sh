#!/usr/bin/env bash
# Scan the images of the release that runs in production, again, with today's vulnerability
# database (MIL-036 task 1). The images were scanned when they were built (MIL-022), but a
# vulnerability that is published after a deploy would otherwise wait for the next release.
# With --third-party it also scans the third-party images that the compose files pin by digest
# (MIL-064 task 6, ADR-0040), which no build of the project scans: those of every stack in
# ops/deploy, the stacks of the VM and the monitoring stack, but not the development-only stacks
# of NOT_IN_PRODUCTION. The monitoring stack is deployed by hand from its own clone
# (deploy-monitoring.sh), so it runs these pins once that clone is at the operations commit.
# The workflow running-image-scan.yaml runs it daily on the release.json of the last release
# that CD deployed, with ops/deploy checked out at its operations commit (devops_sha), and
# opens or updates an issue when the gate fails.
#
# Each image is pulled by its digest and scanned by scripts/scan-image.sh, with the gate of
# scripts/scan-gate.sh: a critical finding, or a high one with a fix.
#
# Usage: scan-running-release.sh --manifest <release.json> --results <folder> [--third-party <folder>]
#   --third-party  ops/deploy at the operations commit of the manifest (devops_sha)
#   writes <folder>/summary.md (Markdown for the issue) and the reports of scan-image.sh, those
#   of the third-party images in <folder>/third-party
# Env:   NFHOTEL_SCAN_IMAGE (default scripts/scan-image.sh; for the tests),
#        NFHOTEL_DOCKER (default docker; for the tests)
# Exit:  0 every image passes; 1 an image fails the gate or cannot be scanned; 2 usage error
# Needs: bash 4+, docker, jq

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
scan_image="${NFHOTEL_SCAN_IMAGE:-$ROOT/scripts/scan-image.sh}"
docker="${NFHOTEL_DOCKER:-docker}"

# The stacks of ops/deploy that never run in production: the Proxmox VE lab of a development
# machine. Every other stack is scanned, so a new stack is covered without a change here.
readonly NOT_IN_PRODUCTION=(proxmox)
# A third-party image pinned by digest: [registry/]path[:tag]@sha256:<64 hex>.
readonly PINNED='^[a-z0-9][A-Za-z0-9._/:-]*@sha256:[0-9a-f]{64}$'
# GitHub refuses an issue body or a comment over 65536 characters, and the workflow adds a line.
readonly MAX_SUMMARY=60000

die() {
  echo "error: $*" >&2
  exit 1
}

usage() {
  echo "usage: scan-running-release.sh --manifest <release.json> --results <folder> [--third-party <folder>]" >&2
  exit 2
}

# in_list <word> <word...>: whether the first word is one of the others.
in_list() {
  local word="$1" item
  shift
  for item in "$@"; do
    [[ "$item" == "$word" ]] && return 0
  done
  return 1
}

# pinned_images <compose file>: the images of the file that are pinned by digest, one per line.
# An image key has its value on its line, or on the next one when the value is folded
# (image: >-), as in the compose file of cloudflared. A value with a variable or an alias is an
# image of the release or a local build, and is left out.
pinned_images() {
  awk '
    /^[[:space:]]*image:[[:space:]]*[>|][-+]?[[:space:]]*$/ { if ((getline line) > 0) print line; next }
    /^[[:space:]]*image:/ { sub(/^[[:space:]]*image:/, ""); print }
  ' "$1" | sed -E 's/[[:space:]]#.*$//' | tr -d "\"' \t\r" | { grep -E "$PINNED" || (($? == 1)); }
}

# check <reference> <name> <folder>: pull the image by its digest and scan it into the folder,
# with the output of the scan and the gate in <folder>/<name>.gate.txt.
# Returns 0 when it passes the gate, 1 when it fails it, 2 when the image cannot be pulled.
check() {
  "$docker" pull -q "$1" >/dev/null 2>&1 || return 2
  bash "$scan_image" "$1" "$2" "$3" >"$3/$2.gate.txt" 2>&1 || return 1
}

# details <title> <reference> <gate output>: the section of an image that fails, for the issue:
# the last lines of its scan, which name the findings of the gate.
details() {
  echo "### $1"
  echo
  echo "\`$2\`"
  echo
  echo '```'
  tail -n 40 "$3"
  echo '```'
  echo
}

manifest="" results="" third_party=""
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
    --third-party)
      (($# >= 2)) || usage
      third_party="$2"
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
for entry in "${entries[@]}"; do
  read -r name ref <<<"$entry"
  [[ "$name" =~ ^[a-z0-9-]+$ && "$ref" =~ ^[a-z0-9./-]+@sha256:[0-9a-f]{64}$ ]] ||
    die "$manifest has an image entry that is not <name> <registry/path@sha256:digest>: $entry"
done

# The third-party images, each once, with the stacks that pin it. They are read before any scan,
# so a wrong folder stops the run at once.
third_party_refs=()
declare -A stacks_of=()
if [[ -n "$third_party" ]]; then
  ops_sha="$(jq -r '.devops_sha // empty' "$manifest")"
  [[ "$ops_sha" =~ ^[0-9a-f]{40}$ ]] || die "$manifest has no operations commit (.devops_sha)"
  [[ -d "$third_party" ]] || die "no folder $third_party"
  files=0
  for file in "$third_party"/*/docker-compose.yml; do
    [[ -f "$file" ]] || continue
    stack="$(basename "$(dirname "$file")")"
    in_list "$stack" "${NOT_IN_PRODUCTION[@]}" && continue
    files=$((files + 1))
    found="$(pinned_images "$file")" || die "cannot read $file"
    while IFS= read -r ref; do
      [[ -n "$ref" ]] || continue
      [[ -n "${stacks_of[$ref]:-}" ]] || third_party_refs+=("$ref")
      [[ ", ${stacks_of[$ref]:-}, " == *", $stack, "* ]] || stacks_of[$ref]+="${stacks_of[$ref]:+, }$stack"
    done <<<"$found"
  done
  ((files)) || die "$third_party has no stack with a docker-compose.yml"
  ((${#third_party_refs[@]})) || die "the compose files in $third_party pin no third-party image by digest"
fi

mkdir -p "$results"
summary="$results/summary.md"
failed=0
: >"$summary.release"
: >"$summary.release-details"

for entry in "${entries[@]}"; do
  read -r name ref <<<"$entry"
  echo "==> $name: $ref"
  status=0
  check "$ref" "$name" "$results" || status=$?
  case "$status" in
    0)
      echo "    passes"
      echo "- **$name**: passes (\`$ref\`)" >>"$summary.release"
      ;;
    1)
      echo "    FAILS the gate"
      echo "- **$name**: fails the gate (\`$ref\`)" >>"$summary.release"
      details "$name" "$ref" "$results/$name.gate.txt" >>"$summary.release-details"
      ;;
    *)
      echo "    cannot be pulled"
      echo "- **$name**: the image could not be pulled (\`$ref\`)" >>"$summary.release"
      ;;
  esac
  ((status == 0)) || failed=1
done

: >"$summary.third-party"
: >"$summary.third-party-details"
if ((${#third_party_refs[@]})); then
  mkdir -p "$results/third-party"
  for ref in "${third_party_refs[@]}"; do
    # The name of the reports: the path of the image and the start of its digest, so two pins of
    # one image (two tags, or a pin that Dependabot is moving) keep their own reports.
    title="${ref%%@*}"
    digest="${ref##*@sha256:}"
    name="$(sed -E 's/:[^/:]*$//; s/[^a-z0-9]+/-/g; s/^-+|-+$//g' <<<"$title")-${digest:0:12}"
    echo "==> third-party image $title (${stacks_of[$ref]}): $ref"
    status=0
    check "$ref" "$name" "$results/third-party" || status=$?
    case "$status" in
      0)
        echo "    passes"
        echo "- **$title** (${stacks_of[$ref]}): passes (\`$ref\`)" >>"$summary.third-party"
        ;;
      1)
        echo "    FAILS the gate"
        echo "- **$title** (${stacks_of[$ref]}): fails the gate (\`$ref\`)" >>"$summary.third-party"
        details "$title" "$ref" "$results/third-party/$name.gate.txt" >>"$summary.third-party-details"
        ;;
      *)
        echo "    cannot be pulled"
        echo "- **$title** (${stacks_of[$ref]}): the image could not be pulled (\`$ref\`)" >>"$summary.third-party"
        ;;
    esac
    ((status == 0)) || failed=1
    # The runner has room for few of these images at once (ollama alone is 4.8 GB), and the
    # reports hold what the scan found. A removal that fails costs only disk space.
    "$docker" image rm "$ref" >/dev/null 2>&1 || echo "    note: $ref was not removed"
  done
fi

{
  if ((failed)); then
    echo "The daily scan of the release in production (\`${sha:0:7}\`) found vulnerabilities that the gate of DSA-001 section 8 does not accept: a critical one, or a high one with a fix."
  else
    echo "The daily scan of the release in production (\`${sha:0:7}\`) found nothing that the gate refuses."
  fi
  echo
  echo "## Release images"
  echo
  if [[ -s "$summary.release-details" ]]; then
    echo "Rebuild and release the image, or accept the finding in \`ops/security/trivyignore.yaml\` with a reason and an expiry (MIL-036)."
    echo
  fi
  cat "$summary.release"
  echo
  cat "$summary.release-details"
  if ((${#third_party_refs[@]})); then
    echo "## Third-party images"
    echo
    echo "The images pinned by digest in the compose files of ops/deploy at the operations commit \`${ops_sha:0:7}\`, every stack but ${NOT_IN_PRODUCTION[*]} (development only), with the stacks that run them."
    if [[ -s "$summary.third-party-details" ]]; then
      echo "An image that fails gets a remedy under ADR-0040: the upstream release or build that passes the gate, a patch layer from the release, or ignore entries of at most 30 days with a statement."
    fi
    echo
    cat "$summary.third-party"
    echo
    cat "$summary.third-party-details"
  fi
} >"$summary"
rm -f "$summary.release" "$summary.release-details" "$summary.third-party" "$summary.third-party-details"

# A summary that is too long for an issue is cut at a line, with its code block closed; every
# report stays in the results folder.
if (($(wc -c <"$summary") > MAX_SUMMARY)); then
  head -c "$MAX_SUMMARY" "$summary" | sed '$d' >"$summary.cut"
  if (($(grep -c '^```' "$summary.cut" || true) % 2)); then
    echo '```' >>"$summary.cut"
  fi
  printf '\n%s\n' "The summary is cut at $MAX_SUMMARY characters; the reports of the run hold every finding." >>"$summary.cut"
  mv "$summary.cut" "$summary"
fi
exit "$failed"
