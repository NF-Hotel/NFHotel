#!/usr/bin/env bash
# Run the end-to-end tests of NFHotel (tests/NFHotel.E2E.Tests, MIL-027 task 3 of
# NFHotel) against a stack started by scripts/e2e-stack.sh. The tests are published here
# with the SDK of the NFHotel checkout (global.json), then run in the Playwright Docker
# image, which holds headless Chromium, on the stack's own Docker network. No browser
# opens on the host (framework/process/gui-testing.md).
#
# The results folder gets e2e-<mode>.trx and, per test, a Playwright trace; a failed test
# also gets a screenshot. The stack's passwords go to the container through an env file
# and are never printed.
#
# Usage: run-e2e-tests.sh --app-dir <NFHotel checkout> --stack-env <stack.env> --results <folder>
#          [--mode off|on]
#   --mode  off (default): every test but those with the trait Mode=Verification, against a stack
#           with the verification of a booking by email off; on: only those, after
#           e2e-stack.sh verification on (MIL-088, ADR-0052). A mode that runs no test fails.
# Exit:  0 every test passed; 1 a test failed, no test ran or the run could not start; 2 usage error
# Needs: bash 4+, docker, the .NET SDK of the checkout

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=ops/scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"

# Playwright 1.63.0, the version of Microsoft.Playwright in NFHotel.E2E.Tests.csproj; bump both together.
readonly PLAYWRIGHT="mcr.microsoft.com/playwright/dotnet:v1.64.0-noble@sha256:7b8bdf51f43ddf3d26ca1655b165e7fb1b50370c0a37e9eda9241b53edd6160c"
readonly PROJECT="tests/NFHotel.E2E.Tests/NFHotel.E2E.Tests.csproj"

app_dir="" stack_env="" results="" mode=off
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
    --mode)
      mode="${2:?--mode needs a value}"
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
[[ "$mode" == "off" || "$mode" == "on" ]] || die "--mode is off or on"
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

# The tests of the verification by email carry the trait Mode=Verification (xUnit v3 filters).
filter=(-trait- "Mode=Verification")
[[ "$mode" == "on" ]] && filter=(-trait "Mode=Verification")
trx="e2e-$mode.trx"
echo "==> running the end-to-end tests of mode $mode in $PLAYWRIGHT on $network"
status=0
MSYS_NO_PATHCONV=1 docker run --rm --init --ipc=host \
  --network "$network" \
  --env-file "$(native "$stack_env")" \
  -e E2E_REQUIRE=true -e E2E_RESULTS=/results \
  -v "$(native "$bin"):/tests:ro" -v "$(native "$results"):/results" \
  -w /tests "$PLAYWRIGHT" \
  dotnet NFHotel.E2E.Tests.dll "${filter[@]}" -result-trx "/results/$trx" || status=$?
((status == 0)) || die "the end-to-end tests failed (exit $status); the traces and screenshots are in $results"
# xUnit passes a filter that matches no test; a mode that ran none is a failure.
total="$(grep -o '<Counters [^>]*' "$results/$trx" 2>/dev/null | grep -o ' total="[0-9]*"' | tr -dc '0-9' || true)"
[[ "$total" =~ ^[1-9][0-9]*$ ]] || die "no end-to-end test of mode $mode ran (see $results/$trx)"
echo "==> the $total end-to-end tests of mode $mode passed; results in $results"
