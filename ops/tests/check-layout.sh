#!/usr/bin/env bash
# Keep one copy of the operations (ADR-0016): fail when a path that moved into ops/ is
# back at the root of the repository, or when a workflow calls the archived repository
# NF-Hotel/DevOps. Two copies of the deploy files drifted once and broke production (the
# market analysis network of MIL-026), so a pull request that brings an old path back
# fails here, and its change belongs in ops/ instead.
#
# Usage: check-layout.sh
# Exit:  0 the layout is as ADR-0016 says; 1 an old path or a DevOps call was found
# Needs: bash 4+, git

set -uo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO" || exit 1

# The root paths that live in ops/ since ADR-0016, and where they are now.
readonly MOVED=(
  "deploy/:ops/deploy/"
  "security/:ops/security/"
  "deploy.env.example:ops/deploy.env.example"
  "pve.env.example:ops/pve.env.example"
  "tests/NFHotel.Proxmox.Tests/:ops/tests/NFHotel.Proxmox.Tests/"
  "scripts/create-docker-vm.sh:ops/scripts/create-docker-vm.sh"
  "scripts/deploy-services.sh:ops/scripts/deploy-services.sh"
  "scripts/deploy.sh:ops/scripts/deploy.sh"
  "scripts/make-dev-certificate.sh:ops/scripts/make-dev-certificate.sh"
  "scripts/pve-token.sh:ops/scripts/pve-token.sh"
  "scripts/run-proxmox-tests.sh:ops/scripts/run-proxmox-tests.sh"
  "scripts/test-compose-proxy.sh:ops/scripts/test-compose-proxy.sh"
  "scripts/resolve-model-name.sh:ops/scripts/resolve-model-name.sh"
  "scripts/test-resolve-model-name.sh:ops/tests/test-resolve-model-name.sh"
)
failures=0

for entry in "${MOVED[@]}"; do
  old="${entry%%:*}" new="${entry#*:}"
  found="$(git ls-files -- "$old" | head -n 3)"
  if [[ -n "$found" ]]; then
    echo "FAIL  $old is back at the root; it lives in $new (ADR-0016):"
    while IFS= read -r line; do echo "      $line"; done <<<"$found"
    failures=$((failures + 1))
  fi
done

calls="$(git grep -n -E 'uses: *NF-Hotel/DevOps/' -- .github || true)"
if [[ -n "$calls" ]]; then
  echo "FAIL  a workflow calls the archived NF-Hotel/DevOps; use ./.github/actions/<name> (ADR-0016):"
  while IFS= read -r line; do echo "      $line"; done <<<"$calls"
  failures=$((failures + 1))
fi

((failures == 0)) || exit 1
echo "PASS  one copy of the operations, in ops/ (ADR-0016)"
