#!/usr/bin/env bash
# Ties the client of the market analysis service to the service it is released with (MIL-026,
# RC-380 F3). The commit of NF-Hotel/notebook-market-analysis that the image is built from is
# written in the default of ops/deploy/market-analysis/docker-compose.yml, which the release
# build reads, and in market-analysis.env.example, which the VM copies: the two must agree. The
# contract copy docs/test-data/market-contract.yaml, from which the tests build their fake of the
# service, must be byte-identical to openapi/market-contract.yaml of the service at that commit.
# A pin moved without the copy, or a copy changed without the pin, fails here, so a release
# whose service refuses what the client sends (RC-380 F1) is caught before the merge.
#
# Run it from anywhere in the repository (it needs ops and docs, not only ops):
#   bash ops/tests/test-market-analysis-pin.sh
# Exit:  0 every check passed; 1 a check failed or the contract could not be fetched;
#        2 an argument was given (the script takes none)
# Needs: bash, cmp, curl, mktemp, sed, and HTTPS to raw.githubusercontent.com (the service is
#        a public repository, so no token is used)

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
# No -e: every check runs and is counted, and the exit code says whether one failed.
set -uo pipefail

if [[ $# -ne 0 ]]; then
  echo "error: this script takes no argument" >&2
  echo "usage: bash ops/tests/test-market-analysis-pin.sh" >&2
  exit 2
fi

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
DEPLOY="$ROOT/ops/deploy/market-analysis"
COPY="$ROOT/docs/test-data/market-contract.yaml"
SERVICE="https://raw.githubusercontent.com/NF-Hotel/notebook-market-analysis"
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

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

# The full commit in the default of the build argument and in the settings template.
mapfile -t in_compose < <(sed -n -E 's/.*[$][{]MARKET_ANALYSIS_REF:-([0-9a-f]{40})[}].*/\1/p' "$DEPLOY/docker-compose.yml")
mapfile -t in_template < <(sed -n -E 's/^MARKET_ANALYSIS_REF=([0-9a-f]{40})$/\1/p' "$DEPLOY/market-analysis.env.example")
[[ "${#in_compose[@]}" -eq 1 ]] && ok "docker-compose.yml pins one commit: ${in_compose[0]}" || ko "docker-compose.yml pins ${#in_compose[@]} full commits, expected 1"
[[ "${#in_template[@]}" -eq 1 ]] && ok "market-analysis.env.example pins one commit: ${in_template[0]}" || ko "market-analysis.env.example pins ${#in_template[@]} full commits, expected 1"
pin="${in_compose[0]:-}"
[[ -n "$pin" && "$pin" == "${in_template[0]:-}" ]] && ok "the compose file and the settings template pin the same commit" || ko "the compose file and the settings template pin different commits; change both together"

# The contract of the service at that commit against the copy of the tests.
if [[ -n "$pin" ]] && curl -fsSL --retry 3 --max-time 60 -o "$work/contract.yaml" "$SERVICE/$pin/openapi/market-contract.yaml"; then
  ok "fetched openapi/market-contract.yaml of the service at $pin"
  cmp -s "$work/contract.yaml" "$COPY" && ok "docs/test-data/market-contract.yaml is byte-identical to the contract at the pin" || ko "docs/test-data/market-contract.yaml differs from the contract at the pin; copy the file of the service, or move the pin"
else
  ko "could not fetch openapi/market-contract.yaml of the service at '$pin'"
fi

echo "passed $pass, failed $fail"
[[ "$fail" -eq 0 ]]
