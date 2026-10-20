#!/usr/bin/env bash
# Build the four NFHotel images of a release with their own Docker Compose files and,
# with --push, push them to the GitHub Container Registry and write the release
# manifest (ADR-0007, MIL-021 task 4). The Release workflow runs it with --push on
# main after green CI; the VM only pulls the images by digest and never builds them.
# Without --push it only builds and tags, which changes nothing outside this machine.
#
# Every built image is scanned with Trivy before anything is pushed (MIL-022 task 1,
# scripts/scan-image.sh): a critical finding, or a high one with a fix, stops the
# release. The scan also writes the SBOM and the SARIF report of each image, and with
# --push the digest, into the scan folder, for the attestations of the release-images
# action (MIL-022 task 2).
#
# Images, each tagged sha-<commit> and identified by its digest after the push:
#   nfhotel-web       deploy/nfhotel-web      (root Dockerfile)
#   nfhotel-api       deploy/nfhotel-api      (Dockerfile.api)
#   market-analysis   deploy/market-analysis  (pinned external repository)
#   benchmark         deploy/benchmark        (pinned external repository)
#
# The compose files need their settings files to load. The build uses the tracked
# *.example files in a temporary folder and throwaway values for the variables that
# compose requires; no real setting or secret is read. The pinned refs of the Python
# services stay in their compose files, so they are written once.
#
# The application source is an NF-Hotel/NFHotel checkout (ADR-0012 of NFHotel); the compose
# files, Dockerfiles of the Python services and this script come from NF-Hotel/DevOps.
#
# Usage: build-release-images.sh --app-dir <NFHotel checkout> [--push] [--registry <host/owner>]
#                                [--sha <commit>] [--devops-sha <commit>]
#                                [--output <file>] [--summary <file>]
#                                [--scan-dir <folder>] [--no-scan] [--migrations <file>]
#   --push       push the images and write the manifest (changes the registry)
#   --registry   image prefix (default ghcr.io/nf-hotel)
#   --app-dir    the NFHotel checkout to build web and api from (its HEAD must be --sha)
#   --sha        the NFHotel commit of the release (default: HEAD of --app-dir)
#   --devops-sha the NF-Hotel/DevOps commit of this script, written to the manifest
#                (required with --push; default: HEAD of this repository)
#   --output     the release manifest to write with --push (default release.json)
#   --summary    a Markdown file to append the manifest to (the job summary in CI)
#   --scan-dir   where the scan writes <name>.trivy.json, .sarif, .spdx.json and, with
#                --push, <name>.digest (default release-scan)
#   --no-scan    skip the scan, for a quick local build only (refused with --push)
#   --migrations the idempotent migration script of the release (make-migration-script.sh);
#                its SHA-256 goes into the manifest as "migrations", and the CD applies
#                exactly that script before the deploy (MIL-022 task 6)
# The caller is logged in to the registry when pushing (docker login ghcr.io).
# Exit:  0 built, scanned (and pushed); 1 a build, scan, push or digest step failed;
#        2 usage error
# Needs: bash 4+, docker with the compose plugin, git, jq

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"

# <image name> <deploy folder> <image variable> <env file variable> <service to push>
readonly IMAGES=(
  "nfhotel-web nfhotel-web NFHOTEL_WEB_IMAGE NFHOTEL_WEB_ENV_FILE nfhotel-web"
  "nfhotel-api nfhotel-api NFHOTEL_API_IMAGE NFHOTEL_API_ENV_FILE nfhotel-api"
  "market-analysis market-analysis MARKET_ANALYSIS_IMAGE MARKET_ANALYSIS_ENV_FILE market-analysis-api"
  "benchmark benchmark BENCHMARK_IMAGE BENCHMARK_ENV_FILE benchmark"
)

registry="ghcr.io/nf-hotel"
sha=""
app_dir=""
devops_sha=""
output="release.json"
summary=""
scan_dir="release-scan"
migrations=""
scan=1
push=0

while (($#)); do
  case "$1" in
    --push) push=1 ;;
    --registry)
      registry="${2:?--registry needs a value}"
      shift
      ;;
    --app-dir)
      app_dir="${2:?--app-dir needs a value}"
      shift
      ;;
    --devops-sha)
      devops_sha="${2:?--devops-sha needs a value}"
      shift
      ;;
    --sha)
      sha="${2:?--sha needs a value}"
      shift
      ;;
    --output)
      output="${2:?--output needs a value}"
      shift
      ;;
    --summary)
      summary="${2:?--summary needs a value}"
      shift
      ;;
    --scan-dir)
      scan_dir="${2:?--scan-dir needs a value}"
      shift
      ;;
    --no-scan) scan=0 ;;
    --migrations)
      migrations="${2:?--migrations needs a value}"
      shift
      ;;
    -h | --help)
      usage
      exit 0
      ;;
    *)
      usage >&2
      exit 2
      ;;
  esac
  shift
done

[[ -n "$app_dir" && -d "$app_dir" ]] || die "give --app-dir, an NF-Hotel/NFHotel checkout"
app_dir="$(cd "$app_dir" && pwd)"
[[ -f "$app_dir/Dockerfile" && -f "$app_dir/Dockerfile.api" ]] || die "$app_dir has no Dockerfile and Dockerfile.api"
head="$(git -C "$app_dir" rev-parse HEAD)" || die "$app_dir is not a git checkout"
[[ -n "$sha" ]] || sha="$head"
[[ "$head" == "$sha" ]] || die "$app_dir is at $head, not at the release commit $sha"
[[ -n "$devops_sha" ]] || devops_sha="$(git -C "$ROOT" rev-parse HEAD)"
[[ "$devops_sha" =~ ^[0-9a-f]{40}$ ]] || die "the DevOps commit '$devops_sha' is not a full SHA"
# The compose files build web and api from here and label every image with the commit.
export NFHOTEL_APP_DIR="$app_dir" NFHOTEL_APP_SHA="$sha"
[[ "$sha" =~ ^[0-9a-f]{40}$ ]] || die "the commit '$sha' is not a full SHA"
[[ "$registry" =~ ^[a-z0-9./_-]+$ ]] || die "the registry '$registry' must be lower case"
docker compose version >/dev/null 2>&1 || die "the docker compose plugin is missing"
[[ -z "$migrations" || -s "$migrations" ]] || die "the migration script '$migrations' is missing or empty"
((scan || push == 0)) || die "--no-scan cannot be used with --push: every released image is scanned"
if ((scan)); then
  mkdir -p "$scan_dir"
  scan_dir="$(cd "$scan_dir" && pwd)"
fi

settings="$(mktemp -d)"
trap 'rm -rf "$settings"' EXIT

# Throwaway values for the variables that the compose files require (":?").
export POSTGRES_PASSWORD="release-build"
export API_TOKEN="release-build"

# Build and scan every image first, and push only when all of them passed, so a failed
# scan leaves nothing new in the registry. The manifest lists the images in IMAGES order.
declare -A digests=()
to_push=()
built=0
for entry in "${IMAGES[@]}"; do
  read -r name folder image_var env_var service <<<"$entry"
  dir="$ROOT/deploy/$folder"

  # A frozen image is not built: its source can no longer be built, so the release
  # carries the last good digest, written once in deploy/<folder>/frozen-image (ADR-0500).
  if [[ -f "$dir/frozen-image" ]]; then
    frozen="$(tr -d '[:space:]' <"$dir/frozen-image")"
    [[ "$frozen" =~ ^${registry//./\\.}/${name}@sha256:[0-9a-f]{64}$ ]] ||
      die "$dir/frozen-image must hold $registry/$name@sha256:<64 hex>, not '$frozen'"
    echo "==> $name is frozen at $frozen (not built)"
    if ((push)); then
      docker buildx imagetools inspect "$frozen" >/dev/null || die "the frozen image $frozen is not in the registry"
      digests[$name]="$frozen"
      # Its findings are reported but cannot be fixed by a rebuild (ADR-0500).
      docker pull -q "$frozen" >/dev/null || die "pulling the frozen image $frozen failed"
      bash "$ROOT/scripts/scan-image.sh" --report-only "$frozen" "$name" "$scan_dir"
    fi
    continue
  fi

  examples=("$dir"/*.env.example)
  [[ -f "${examples[0]}" ]] || die "no settings example in $dir"
  cp "${examples[0]}" "$settings/$folder.env"
  tag="$registry/$name:sha-$sha"
  export "$image_var=$tag" "$env_var=$settings/$folder.env"
  if [[ "$folder" == "market-analysis" ]]; then
    export MARKET_ANALYSIS_CONFIG="$dir/market-analysis.toml.example"
  fi

  echo "==> building $tag"
  (cd "$dir" && docker compose build --pull "$service") || die "building $name failed"
  docker image inspect "$tag" >/dev/null || die "no image $tag after the build"
  built=$((built + 1))
  if ((scan)); then
    bash "$ROOT/scripts/scan-image.sh" "$tag" "$name" "$scan_dir" || die "$name failed the container scan; nothing is pushed"
  fi
  to_push+=("$name $folder $service $tag")
done

if ((push)); then
  for entry in "${to_push[@]}"; do
    read -r name folder service tag <<<"$entry"
    echo "==> pushing $tag"
    (cd "$ROOT/deploy/$folder" && docker compose push "$service") || die "pushing $name failed"
    digest="$(docker image inspect --format '{{range .RepoDigests}}{{println .}}{{end}}' "$tag" |
      grep -m1 "^$registry/$name@sha256:")" || die "no digest for $name after the push"
    [[ "$digest" =~ ^[a-z0-9./_-]+@sha256:[0-9a-f]{64}$ ]] || die "unexpected digest '$digest'"
    digests[$name]="$digest"
    echo "${digest#*@}" >"$scan_dir/$name.digest"
    echo "    $digest"
  done
fi

((push)) || {
  note="(not scanned)"
  ((scan == 0)) || note="and scanned"
  echo "==> built $note $built of ${#IMAGES[@]} images (the others are frozen); nothing pushed (give --push to publish)"
  exit 0
}

{
  echo "{"
  echo "  \"sha\": \"$sha\","
  echo "  \"devops_sha\": \"$devops_sha\","
  if [[ -n "$migrations" ]]; then
    echo "  \"migrations\": \"$(sha256sum "$migrations" | cut -d' ' -f1)\","
  fi
  echo "  \"images\": {"
  i=0
  for entry in "${IMAGES[@]}"; do
    read -r name _ <<<"$entry"
    i=$((i + 1))
    sep=","
    ((i == ${#IMAGES[@]})) && sep=""
    echo "    \"$name\": \"${digests[$name]:?no digest for $name}\"$sep"
  done
  echo "  }"
  echo "}"
} >"$output"
echo "==> wrote $output"

if [[ -n "$summary" ]]; then
  {
    echo "## Release"
    echo
    echo '```json'
    cat "$output"
    echo '```'
  } >>"$summary"
fi
