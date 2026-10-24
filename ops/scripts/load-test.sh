#!/usr/bin/env bash
# Run the load test of ops/load/nfhotel.js against the throwaway stack of scripts/e2e-stack.sh
# (MIL-036 task 3). k6 runs in a container at a pinned digest on the stack's own Docker network
# and opens https://nginx only, never the VM, the tailnet or a public host. The thresholds of the
# script are the objectives of MIL-034; k6 exits non-zero when one is broken.
#
# Usage: load-test.sh --stack-env <stack.env> --results <folder>
#   writes <folder>/summary.json (the k6 summary) and <folder>/summary.txt
# Exit:  0 every threshold holds; 1 a threshold is broken or k6 failed; 2 usage error
# Needs: bash 4+, docker

set -euo pipefail

OPS="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# k6 1.4.0; Dependabot does not see this line, so bump it by hand with the digest.
readonly K6=grafana/k6:1.4.0@sha256:6a3ee54ac0e9ff5527923f6295257453dd88012f32f40dadf0eb1b638cbb21c7

die() {
  echo "error: $*" >&2
  exit 1
}

usage() {
  echo "usage: load-test.sh --stack-env <stack.env> --results <folder>" >&2
  exit 2
}

stack_env="" results=""
while (($#)); do
  case "$1" in
    --stack-env)
      (($# >= 2)) || usage
      stack_env="$2"
      shift 2
      ;;
    --results)
      (($# >= 2)) || usage
      results="$2"
      shift 2
      ;;
    *) usage ;;
  esac
done
[[ -n "$stack_env" && -n "$results" ]] || usage
[[ -r "$stack_env" ]] || die "cannot read $stack_env (the stack.env that e2e-stack.sh wrote)"
network="$(sed -n 's/^E2E_NETWORK=//p' "$stack_env")"
[[ "$network" =~ ^[A-Za-z0-9_.-]+$ ]] || die "$stack_env has no valid E2E_NETWORK"

mkdir -p "$results"
results="$(cd "$results" && pwd)"
echo "==> load test against https://nginx on $network"
# k6 runs as the calling user, so it can write the summary into the mounted folder.
docker run --rm --network "$network" --user "$(id -u):$(id -g)" \
  -v "$OPS/load:/scripts:ro" -v "$results:/results" \
  -e TARGET=https://nginx "$K6" run --quiet \
  --summary-export /results/summary.json /scripts/nfhotel.js | tee "$results/summary.txt"
