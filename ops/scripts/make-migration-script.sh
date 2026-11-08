#!/usr/bin/env bash
# Make the idempotent EF Core migration script of an NF-Hotel/NFHotel checkout (ADR-0008,
# MIL-022 task 6). Each migration in it runs only when its id is not yet in
# __EFMigrationsHistory, so the script can be applied again without harm. The release
# records its SHA-256, and the CD applies exactly that script as nfhotel_migrator
# (scripts/deploy-release.sh migrate). The CI test of the database uses the same script.
# Nothing connects to a database here.
#
# Usage: make-migration-script.sh <NFHotel checkout> <output .sql>
# Exit:  0 written; 1 a step failed; 2 usage error
# Needs: bash 4+, the .NET SDK of the checkout's global.json

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=ops/scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"
# dotnet-ef runs at the EF Core version of the checkout, which its project file gives on the
# design package, so a bump of EF Core moves the tool with it (MIL-066 task 8).
readonly EF_DESIGN='Include="Microsoft.EntityFrameworkCore.Design"[[:space:]]+Version="([0-9]+[.][0-9]+[.][0-9]+)"'

[[ $# == 2 && -d "$1/src/NFHotel.Infrastructure" ]] || {
  usage >&2
  exit 2
}
app="$(cd "$1" && pwd)"
project="$app/src/NFHotel.Infrastructure/NFHotel.Infrastructure.csproj"
[[ -f "$project" && "$(<"$project")" =~ $EF_DESIGN ]] ||
  die "$project gives no version of Microsoft.EntityFrameworkCore.Design"
ef_version="${BASH_REMATCH[1]}"
mkdir -p "$(dirname "$2")"
out="$(cd "$(dirname "$2")" && pwd)/$(basename "$2")"

echo "==> dotnet-ef $ef_version"
dotnet tool update --global dotnet-ef --version "$ef_version" >/dev/null || die "cannot install dotnet-ef"
export PATH="$PATH:$HOME/.dotnet/tools"

echo "==> the idempotent migration script"
(cd "$app" && dotnet restore src/NFHotel.Infrastructure --locked-mode >/dev/null) || die "restore failed"
# The design-time factory needs a connection string; nothing connects to it.
(cd "$app" && NFHOTEL_CONNECTION="Host=localhost;Database=x;Username=x;Password=x" dotnet ef migrations script --idempotent \
  --project src/NFHotel.Infrastructure --startup-project src/NFHotel.Infrastructure \
  --msbuildprojectextensionspath g/obj/NFHotel.Infrastructure --output "$out") || die "the migration script failed"
[[ -s "$out" ]] || die "the migration script is empty"
echo "==> $out ($(sha256sum "$out" | cut -d' ' -f1))"
