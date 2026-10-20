#!/usr/bin/env bash
# Run the end-to-end tests of NFHotel (tests/NFHotel.E2E.Tests, MIL-027 task 3 of
# NFHotel) against a stack started by scripts/e2e-stack.sh. The tests are published here
# with the SDK of the NFHotel checkout (global.json), then run in the Playwright Docker
# image, which holds headless Chromium, on the stack's own Docker network. No browser
# opens on the host (framework/process/gui-testing.md).
#
# The results folder gets e2e.trx and, per test, a Playwright trace; a failed test
# also gets a screenshot. The stack's passwords go to the container through an env file
# and are never printed.
#
# Usage: run-e2e-tests.sh --app-dir <NFHotel checkout> --stack-env <stack.env> --results <folder>
# Exit:  0 every test passed; 1 a test failed or the run could not start; 2 usage error
# Needs: bash 4+, docker, the .NET SDK of the checkout

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"

# Playwright 1.62.0, the version of Microsoft.Playwright in NFHotel.E2E.Tests.csproj; bump both together.
readonly PLAYWRIGHT="mcr.microsoft.com/playwright/dotnet:v1.62.0-noble@sha256:25e558a847d4ae1ff7d14fa263de7fa2fffd357c1e50471278e522cec5ac1bb5"
readonly PROJECT="tests/NFHotel.E2E.Tests/NFHotel.E2E.Tests.csproj"

app_dir="" stack_env="" results=""
while (($#)); do
  case "$1" in
    --app-dir)
      app_dir="${2:?--app-dir needs a value}"
      shift
      ;;
    --stack-env)
      stack_env="${2:?--stack-env needs a value}"
      shift
      ;;
    --results)
      results="${2:?--results needs a value}"
      shift
      ;;
    *)
      usage >&2
      exit 2
      ;;
  esac
  shift
done
[[ -n "$app_dir" && -f "$app_dir/$PROJECT" ]] || die "give --app-dir, an NFHotel checkout with $PROJECT"
[[ -n "$stack_env" && -f "$stack_env" ]] || die "give --stack-env, the stack.env that e2e-stack.sh wrote"
[[ -n "$results" ]] || die "give --results, the folder for the test results"
network="$(sed -n 's/^E2E_NETWORK=//p' "$stack_env")"
[[ -n "$network" ]] || die "$stack_env has no E2E_NETWORK"

# A native path for the Docker daemon: Git Bash on Windows has cygpath, Linux does not.
native() {
  if command -v cygpath >/dev/null 2>&1; then cygpath -m "$1"; else echo "$1"; fi
}

mkdir -p "$results"
results="$(cd "$results" && pwd)"
bin="$(mktemp -d)"
trap 'rm -rf "$bin"' EXIT

echo "==> publishing the end-to-end tests"
# For linux-x64: the container runs Linux, and Playwright copies only the driver of the target runtime.
dotnet publish "$app_dir/$PROJECT" --configuration Release --runtime linux-x64 --self-contained false \
  --output "$bin" -nologo -v q >/dev/null ||
  die "publishing $PROJECT failed"

echo "==> running the end-to-end tests in $PLAYWRIGHT on $network"
status=0
MSYS_NO_PATHCONV=1 docker run --rm --init --ipc=host \
  --network "$network" \
  --env-file "$(native "$stack_env")" \
  -e E2E_REQUIRE=true -e E2E_RESULTS=/results \
  -v "$(native "$bin"):/tests:ro" -v "$(native "$results"):/results" \
  -w /tests "$PLAYWRIGHT" \
  dotnet NFHotel.E2E.Tests.dll -result-trx /results/e2e.trx || status=$?
((status == 0)) || die "the end-to-end tests failed (exit $status); the traces and screenshots are in $results"
echo "==> every end-to-end test passed; results in $results"
