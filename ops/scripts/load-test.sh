#!/usr/bin/env bash
# Run the load test of ops/load/nfhotel.js against the throwaway stack of scripts/e2e-stack.sh
# (MIL-036 task 3, MIL-089 task 4). k6 runs in a container at a pinned digest on two networks of
# the stack: the proxy network, where it opens https://nginx, and the internal network of the
# API, where it calls http://nfhotel-api:8080 as the web application does; never the VM, the
# tailnet or a public host. The token of the API and the Manager account of the stack reach k6
# in a file that only the calling user can read, mounted read-only and removed at the end, so no
# secret is on a command line or in the environment of a container. The thresholds of the script
# are the objectives of MIL-034 and the refusals of MIL-089; k6 exits non-zero when one is broken.
#
# Usage: load-test.sh --stack-env <stack.env> --results <folder> [--title <text>]
#   writes <folder>/summary.json (the k6 summary) and <folder>/summary.txt; with --title in a
#   workflow, it also adds summary.txt under that title to the page of the job ($GITHUB_STEP_SUMMARY)
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
  echo "usage: load-test.sh --stack-env <stack.env> --results <folder> [--title <text>]" >&2
  exit 2
}

# setting <name>: the value of a line NAME=value of the stack.env.
setting() { sed -n "s/^$1=//p" "$stack_env"; }

stack_env="" results="" title=""
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
    --title)
      (($# >= 2)) || usage
      title="$2"
      shift 2
      ;;
    *) usage ;;
  esac
done
[[ -n "$stack_env" && -n "$results" ]] || usage
[[ -r "$stack_env" ]] || die "cannot read $stack_env (the stack.env that e2e-stack.sh wrote)"
network="$(setting E2E_NETWORK)"
[[ "$network" =~ ^[A-Za-z0-9_.-]+$ ]] || die "$stack_env has no valid E2E_NETWORK"
api_network="$(setting E2E_API_NETWORK)"
[[ "$api_network" =~ ^[A-Za-z0-9_.-]+$ ]] || die "$stack_env has no valid E2E_API_NETWORK"
token="$(setting E2E_API_TOKEN)"
[[ "$token" =~ ^[0-9a-f]+$ ]] || die "$stack_env has no valid E2E_API_TOKEN"
user="$(setting E2E_API_USER)"
[[ "$user" =~ ^[A-Za-z0-9_.-]+$ ]] || die "$stack_env has no valid E2E_API_USER"
password="$(setting E2E_API_PASSWORD)"
[[ "$password" =~ ^[0-9a-f]+$ ]] || die "$stack_env has no valid E2E_API_PASSWORD"

mkdir -p "$results"
results="$(cd "$results" && pwd)"
secrets="$(umask 077 && mktemp -d)"
container="nfhotel-load-$$"
cleanup() {
  docker rm -f "$container" >/dev/null 2>&1 || true
  rm -rf "$secrets"
}
trap cleanup EXIT
# printf is a builtin of bash, so the values are on no command line; the file is the user's only.
(umask 077 && printf '{"apiToken":"%s","staffUser":"%s","staffPassword":"%s"}\n' \
  "$token" "$user" "$password" >"$secrets/load.json")

echo "==> load test against https://nginx on $network and the API on $api_network"
# k6 runs as the calling user, so it can read the secrets and write the summary into the
# mounted folder. A container joins a second network only before it starts.
docker create --name "$container" --network "$network" --user "$(id -u):$(id -g)" \
  -v "$OPS/load:/scripts:ro" -v "$results:/results" -v "$secrets:/secrets:ro" \
  -e TARGET=https://nginx -e API=http://nfhotel-api:8080 "$K6" run --quiet \
  --summary-export /results/summary.json /scripts/nfhotel.js >/dev/null
docker network connect "$api_network" "$container"
status=0
docker start --attach "$container" | tee "$results/summary.txt" || status=$?
# In a workflow the page of the job shows the summary under its title, also when a threshold broke.
if [[ -n "$title" && -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
  {
    echo "### $title"
    echo '```'
    printf '%s\n' "$(<"$results/summary.txt")"
    echo '```'
  } >>"$GITHUB_STEP_SUMMARY"
fi
# k6 exits 99 when a threshold is broken; this script says 1 for any failure.
((status == 0)) || exit 1
