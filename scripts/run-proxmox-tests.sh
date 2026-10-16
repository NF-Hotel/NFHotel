#!/usr/bin/env bash
# Run the read-only Proxmox VE tests in a throwaway Docker container.
#
# The container is removed when it ends (docker run --rm) and gets no stdin,
# so the run never waits for a person. Settings come from pve.env (see
# pve.env.example); the secret is passed through --env-file, never on the
# command line. The tests only send GET requests.
#
# Usage: run-proxmox-tests.sh [--env-file <path>]
# Exit:  0 all checks passed; 1 a check or the setup failed; 2 usage error
# Needs: bash 4+, docker
# Task:  MIL-001 task 3 (docs/milestones/mil-001-proxmox-test-env.md)
# Env:   PVE_TIMEOUT_SECONDS is read from the env file (default 15)

set -euo pipefail

readonly IMAGE="nfhotel-proxmox-tests"
readonly CONTAINER="${IMAGE}-$$"

die() {
  echo "error: $*" >&2
  exit 1
}

main() {
  local root env_file="pve.env"
  root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

  while [[ $# -gt 0 ]]; do
    case "$1" in
      --env-file)
        env_file="${2:?usage: $0 [--env-file <path>]}"
        shift 2
        ;;
      *)
        echo "usage: $0 [--env-file <path>]" >&2
        exit 2
        ;;
    esac
  done

  command -v docker >/dev/null 2>&1 ||
    die "docker is not installed or not on PATH"
  docker info >/dev/null 2>&1 ||
    die "the docker daemon is not running; start Docker and retry"

  cd "$root"
  [[ -f "$env_file" ]] ||
    die "$env_file not found; copy pve.env.example to pve.env and fill it in"

  trap 'docker rm -f "$CONTAINER" >/dev/null 2>&1 || true' EXIT

  echo "Building test image ${IMAGE} (cached after the first run)..."
  docker build --quiet --tag "$IMAGE" \
    --file tests/NFHotel.Proxmox.Tests/Dockerfile \
    tests/NFHotel.Proxmox.Tests >/dev/null ||
    die "docker build failed"

  echo "Running Proxmox tests in a throwaway container..."
  docker run --rm --name "$CONTAINER" \
    --env-file "$env_file" \
    --env PVE_REQUIRE_LIVE=true \
    "$IMAGE"
}

main "$@"
