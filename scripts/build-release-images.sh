#!/usr/bin/env bash
# Build the four NFHotel images of a release with their own Docker Compose files and,
# with --push, push them to the GitHub Container Registry and write the release
# manifest (ADR-0007, MIL-021 task 4). The Release workflow runs it with --push on
# main after green CI; the VM only pulls the images by digest and never builds them.
# Without --push it only builds and tags, which changes nothing outside this machine.
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
# Usage: build-release-images.sh [--push] [--registry <host/owner>] [--sha <commit>]
#                                [--output <file>] [--summary <file>]
#   --push       push the images and write the manifest (changes the registry)
#   --registry   image prefix (default ghcr.io/nf-hotel)
#   --sha        the commit of the release (default: git rev-parse HEAD)
#   --output     the release manifest to write with --push (default release.json)
#   --summary    a Markdown file to append the manifest to (the job summary in CI)
# The caller is logged in to the registry when pushing (docker login ghcr.io).
# Exit:  0 built (and pushed); 1 a build, push or digest step failed; 2 usage error
# Needs: bash 4+, docker with the compose plugin, git

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
output="release.json"
summary=""
push=0

while (($#)); do
  case "$1" in
    --push) push=1 ;;
    --registry)
      registry="${2:?--registry needs a value}"
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

[[ -n "$sha" ]] || sha="$(git -C "$ROOT" rev-parse HEAD)"
[[ "$sha" =~ ^[0-9a-f]{40}$ ]] || die "the commit '$sha' is not a full SHA"
[[ "$registry" =~ ^[a-z0-9./_-]+$ ]] || die "the registry '$registry' must be lower case"
docker compose version >/dev/null 2>&1 || die "the docker compose plugin is missing"

settings="$(mktemp -d)"
trap 'rm -rf "$settings"' EXIT

# Throwaway values for the variables that the compose files require (":?").
export POSTGRES_PASSWORD="release-build"
export API_TOKEN="release-build"

digests=()
for entry in "${IMAGES[@]}"; do
  read -r name folder image_var env_var service <<<"$entry"
  dir="$ROOT/deploy/$folder"
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

  if ((push)); then
    echo "==> pushing $tag"
    (cd "$dir" && docker compose push "$service") || die "pushing $name failed"
    digest="$(docker image inspect --format '{{range .RepoDigests}}{{println .}}{{end}}' "$tag" |
      grep -m1 "^$registry/$name@sha256:")" || die "no digest for $name after the push"
    [[ "$digest" =~ ^[a-z0-9./_-]+@sha256:[0-9a-f]{64}$ ]] || die "unexpected digest '$digest'"
    digests+=("\"$name\": \"$digest\"")
    echo "    $digest"
  fi
done

((push)) || {
  echo "==> built ${#IMAGES[@]} images; nothing pushed (give --push to publish)"
  exit 0
}

{
  echo "{"
  echo "  \"sha\": \"$sha\","
  echo "  \"images\": {"
  last=$((${#digests[@]} - 1))
  for i in "${!digests[@]}"; do
    sep=","
    ((i == last)) && sep=""
    echo "    ${digests[$i]}$sep"
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
