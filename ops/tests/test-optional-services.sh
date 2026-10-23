#!/usr/bin/env bash
# Test that the services outside the end-to-end stack still start healthy with the hardening
# of MIL-023 task 2 (CIS-001): ollama, the market analysis API and notebook, and the
# benchmark notebook, each from its compose file in deploy/. Every container must have the
# hardening (tests/lib/hardening.sh), be healthy, and be healthy again after a restart, which
# shows that the read-only root filesystem holds across one.
#
# It runs on a developer machine, not in CI: the images are large and the analysis and
# benchmark images come from the registry. No model is pulled. It starts the compose projects
# t-opt-* and the networks t-opt-proxy and t-opt-llm, and removes them at the end.
#
# Usage: test-optional-services.sh
# Env:   MARKET_ANALYSIS_IMAGE  the analysis image (default: the frozen digest of ADR-0500)
#        BENCHMARK_IMAGE        the benchmark image (required, e.g. ghcr.io/nf-hotel/benchmark:sha-...)
# Exit:  0 every check passed; 1 a check failed; 2 usage error
# Needs: bash 4+, docker with the compose plugin

set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=ops/tests/lib/hardening.sh
. "$ROOT/tests/lib/hardening.sh"
[[ -n "${BENCHMARK_IMAGE:-}" ]] || {
  echo "usage: BENCHMARK_IMAGE=<image> test-optional-services.sh" >&2
  exit 2
}
export BENCHMARK_IMAGE MARKET_ANALYSIS_IMAGE
MARKET_ANALYSIS_IMAGE="${MARKET_ANALYSIS_IMAGE:-$(cat "$ROOT/deploy/market-analysis/frozen-image")}"
export NFHOTEL_PROXY_NETWORK=t-opt-proxy NFHOTEL_LLM_NETWORK=t-opt-llm
pass=0
fail=0
check() {
  if [[ "$2" == "$3" ]]; then
    echo "PASS  $1"
    pass=$((pass + 1))
  else
    echo "FAIL  $1 (expected '$2', got '$3')"
    fail=$((fail + 1))
  fi
}

# A native path for the Docker daemon: Git Bash on Windows has cygpath, Linux does not.
native() {
  if command -v cygpath >/dev/null 2>&1; then cygpath -m "$1"; else echo "$1"; fi
}
settings="$(mktemp -d)"
compose() {
  local folder="$1"
  shift
  docker compose --project-name "t-opt-$folder" --file "$ROOT/deploy/$folder/docker-compose.yml" "$@"
}
cleanup() {
  local folder
  for folder in benchmark market-analysis ollama; do
    compose "$folder" down --volumes --remove-orphans >/dev/null 2>&1
  done
  docker network rm t-opt-proxy t-opt-llm >/dev/null 2>&1
  rm -rf "$settings"
}
trap cleanup EXIT
cleanup
mkdir -p "$settings"
docker network create t-opt-proxy >/dev/null
docker network create t-opt-llm >/dev/null
: >"$settings/ollama.env"
: >"$settings/market-analysis.env"
: >"$settings/benchmark.env"
cp "$ROOT/deploy/market-analysis/market-analysis.toml.example" "$settings/market-analysis.toml"
native_settings="$(native "$settings")"
export OLLAMA_ENV_FILE="$native_settings/ollama.env" MARKET_ANALYSIS_ENV_FILE="$native_settings/market-analysis.env"
export BENCHMARK_ENV_FILE="$native_settings/benchmark.env" MARKET_ANALYSIS_CONFIG="$native_settings/market-analysis.toml"

for folder in ollama market-analysis benchmark; do
  echo "== $folder"
  compose "$folder" up --detach --no-build --wait --wait-timeout 180 >/dev/null 2>&1
  check "$folder starts" 0 "$?"
  for id in $(compose "$folder" ps --quiet); do
    service="$(docker inspect --format '{{index .Config.Labels "com.docker.compose.service"}}' "$id")"
    check "$service has the hardening" "" "$(hardening_missing "$id")"
    check "$service is healthy" healthy "$(docker inspect --format '{{.State.Health.Status}}' "$id")"
    docker restart "$id" >/dev/null
  done
  compose "$folder" up --detach --no-build --wait --wait-timeout 180 >/dev/null 2>&1
  for id in $(compose "$folder" ps --quiet); do
    service="$(docker inspect --format '{{index .Config.Labels "com.docker.compose.service"}}' "$id")"
    check "$service is healthy after a restart" healthy "$(docker inspect --format '{{.State.Health.Status}}' "$id")"
  done
done
check "ollama answers on the network" 200 "$(MSYS_NO_PATHCONV=1 docker run --rm --network t-opt-llm curlimages/curl:8.10.1 \
  -s -o /dev/null -w '%{http_code}' http://ollama:11434/api/tags)"
check "the analysis API answers its health check" 200 "$(MSYS_NO_PATHCONV=1 docker run --rm --network t-opt-proxy \
  curlimages/curl:8.10.1 -s -o /dev/null -w '%{http_code}' http://market-analysis-api:8000/health)"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
