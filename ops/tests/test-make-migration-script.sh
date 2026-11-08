#!/usr/bin/env bash
# Tests of scripts/make-migration-script.sh (MIL-066 task 8): dotnet-ef is installed at the
# version of Microsoft.EntityFrameworkCore.Design in the project file of the checkout, so a
# bump of EF Core moves the tool with it, and a project file without a fixed version of three
# numbers stops the script before dotnet runs. A fake stands in for dotnet, so nothing is
# downloaded; the CI job "Ops PostgreSQL preparation" runs the script with the real tool.
#
# Run it in Debian 12 with the ops folder at /src:
#   docker run --rm -v "$PWD/ops:/src:ro" debian:12-slim bash /src/tests/test-make-migration-script.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: bash 4+, coreutils, grep

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

OPS="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
W="$(mktemp -d)"
trap 'rm -rf "$W"' EXIT
pass=0
fail=0
ok() {
  echo "PASS  $1"
  pass=$((pass + 1))
}
ko() {
  echo "FAIL  $1"
  fail=$((fail + 1))
}

# The fake dotnet logs its arguments, and writes a script where ef is told to (--output).
mkdir -p "$W/bin"
cat >"$W/bin/dotnet" <<'EOF'
#!/usr/bin/env bash
echo "$*" >>"$FAKE_LOG"
if [[ "$1" == ef ]]; then
  while (($#)); do
    [[ "$1" == --output ]] && echo "-- the migration script" >"$2"
    shift
  done
fi
exit 0
EOF
chmod +x "$W/bin/dotnet"
export PATH="$W/bin:$PATH"

# checkout <name> <project file>: a checkout with only the project file of NFHotel.Infrastructure
checkout() {
  mkdir -p "$W/$1/src/NFHotel.Infrastructure"
  printf '%s\n' "$2" >"$W/$1/src/NFHotel.Infrastructure/NFHotel.Infrastructure.csproj"
}
# run <name>: the script on the checkout <name>, with a fresh log of the fake; output in $W/out
run() {
  export FAKE_LOG="$W/log.$1"
  : >"$FAKE_LOG"
  bash "$OPS/scripts/make-migration-script.sh" "$W/$1" "$W/sql/$1.sql" >"$W/out" 2>&1
}
# installs <name> <version> <check>: the script on <name> installs dotnet-ef at <version>
installs() {
  run "$1" && grep -qx "tool update --global dotnet-ef --version $2" "$W/log.$1" &&
    grep -qx "==> dotnet-ef $2" "$W/out" && [[ -s "$W/sql/$1.sql" ]] && ok "$3" ||
    ko "$3: $(cat "$W/out" "$W/log.$1")"
}
# refused <name> <check>: the script on <name> stops with exit 1 and the message, before dotnet runs
refused() {
  run "$1"
  local code=$?
  ((code == 1)) && grep -q "gives no version of Microsoft.EntityFrameworkCore.Design" "$W/out" &&
    [[ ! -s "$W/log.$1" ]] && ! grep -qi "no such file" "$W/out" && ok "$2" ||
    ko "$2 (exit $code): $(cat "$W/out" "$W/log.$1")"
}
# project <version>: a project file like the one of NFHotel.Infrastructure, with another EF
# Core package first, so only the design package may give the version
project() {
  printf '%s\n' '<Project Sdk="Microsoft.NET.Sdk">' '  <ItemGroup>' \
    '    <PackageReference Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.20" />' \
    "    <PackageReference Include=\"Microsoft.EntityFrameworkCore.Design\" Version=\"$1\">" \
    '      <PrivateAssets>all</PrivateAssets>' '    </PackageReference>' '  </ItemGroup>' '</Project>'
}

echo "== the version comes from the design package"
checkout current "$(project 10.0.14)"
installs current 10.0.14 "dotnet-ef 10.0.14 for the design package 10.0.14, not the 10.0.20 of Relational"
checkout bumped "$(project 10.0.15)"
installs bumped 10.0.15 "a bump of the design package to 10.0.15 moves dotnet-ef with it"

echo "== a project file without a fixed version of three numbers is refused"
checkout none '<Project><ItemGroup><PackageReference Include="Npgsql" Version="10.0.1" /></ItemGroup></Project>'
refused none "a project file without the design package"
checkout two "$(project 10.0)"
refused two "a version of two numbers (10.0)"
checkout four "$(project 10.0.14.1)"
refused four "a version of four numbers (10.0.14.1)"
checkout floating "$(project '10.0.*')"
refused floating "a floating version (10.0.*)"
checkout preview "$(project 11.0.0-preview.1)"
refused preview "a prerelease version (11.0.0-preview.1)"
mkdir -p "$W/missing/src/NFHotel.Infrastructure"
refused missing "a checkout without the project file, with no shell error"

echo "== usage"
export FAKE_LOG="$W/log.usage"
: >"$FAKE_LOG"
bash "$OPS/scripts/make-migration-script.sh" "$W/current" >"$W/out" 2>&1
code=$?
((code == 2)) && [[ ! -s "$FAKE_LOG" ]] && ok "one argument is a usage error (exit 2) and dotnet does not run" ||
  ko "usage: exit $code, $(cat "$W/out" "$FAKE_LOG")"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
