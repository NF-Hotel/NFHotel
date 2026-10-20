#!/usr/bin/env bash
# Prepare the inputs of tests/test-database.sh from an NF-Hotel/NFHotel checkout: the
# idempotent EF Core migration script and the API image (MIL-024 task 6). Used by the CI of
# this repository; it builds locally and pushes nothing.
#
# Usage: prepare-database-test.sh <NFHotel checkout> <output folder>
#   writes <output folder>/migrations.sql and builds the image nfhotel-api:test
# Exit:  0 prepared; 1 a step failed; 2 usage error
# Needs: bash 4+, the .NET SDK of the checkout's global.json, docker

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"

[[ $# == 2 && -f "$1/Dockerfile.api" ]] || {
  usage >&2
  exit 2
}
app="$(cd "$1" && pwd)"
mkdir -p "$2"
out="$(cd "$2" && pwd)"

bash "$ROOT/scripts/make-migration-script.sh" "$app" "$out/migrations.sql" || die "the migration script failed"

echo "==> the API image"
docker build --quiet -f "$app/Dockerfile.api" -t nfhotel-api:test "$app" >/dev/null || die "the API image did not build"
echo "==> $out/migrations.sql and nfhotel-api:test are ready"
