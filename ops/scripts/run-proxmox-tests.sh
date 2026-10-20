#!/usr/bin/env bash
# Run the read-only Proxmox VE tests against a throwaway Proxmox container.
#
# Starts the Proxmox VE container (deploy/proxmox), waits until its API
# answers, sets the root password from pve.env, creates a per-run API token in
# it (scripts/pve-token.sh), runs the tests in a second throwaway container on
# the same network, and removes both containers and the volumes afterwards. It
# needs no input. pve.env holds only
# the root password; the token exists only in this script's environment and is
# passed to the test container by name, never written to a file.
#
# Usage: run-proxmox-tests.sh [--env-file <path>]
# Exit:  0 all checks passed; 1 a check or the setup failed; 2 usage error
# Needs: bash 4+, docker with the compose plugin, curl, /dev/kvm in Docker
# Env:   PVE_HOST_PORT host port for the Proxmox API check (default 18006)
# Task:  MIL-003 task 4 (docs/milestones/mil-003-throwaway-proxmox.md)

set -euo pipefail

readonly IMAGE="nfhotel-proxmox-tests"
readonly PROJECT="nfhotel-pve"
readonly PROXMOX_CONTAINER="${PROJECT}-proxmox-1"
readonly TEST_CONTAINER="${IMAGE}-$$"
readonly WAIT_TRIES=60

HOST_PORT="${PVE_HOST_PORT:-18006}"
ENV_FILE="pve.env"

die() {
  echo "error: $*" >&2
  exit 1
}

compose_proxmox() {
  PVE_HOST_PORT="$HOST_PORT" docker compose --project-name "$PROJECT" \
    --env-file "$ENV_FILE" --file deploy/proxmox/docker-compose.yml "$@"
}

cleanup() {
  docker rm -f "$TEST_CONTAINER" >/dev/null 2>&1 || true
  compose_proxmox down --volumes --remove-orphans >/dev/null 2>&1 || true
}

# read_password: the value of PVE_PASSWORD in the env file (no shell evaluation).
read_password() {
  sed -n 's/^PVE_PASSWORD=\(.*\)$/\1/p' "$ENV_FILE" | head -n 1
}

# set_root_password: set root's password from pve.env, through standard input so
# it never appears on a command line.
set_root_password() {
  local password
  password="$(read_password)"
  [[ -n "$password" ]] || die "PVE_PASSWORD is empty in $ENV_FILE"
  printf 'root:%s\n' "$password" |
    docker exec -i "$PROXMOX_CONTAINER" chpasswd ||
    die "could not set the root password in the Proxmox container"
}

has_kvm() {
  docker run --rm --privileged alpine sh -c 'test -e /dev/kvm' >/dev/null 2>&1
}

wait_for_api() {
  local tries="$WAIT_TRIES" status=""
  while ((tries > 0)); do
    status="$(curl --silent --insecure --output /dev/null \
      --write-out '%{http_code}' --max-time 5 \
      "https://127.0.0.1:${HOST_PORT}/api2/json/version" || true)"
    # 401 means Proxmox is up and asks for a token.
    [[ "$status" == "401" || "$status" == "200" ]] && return 0
    tries=$((tries - 1))
    sleep 5
  done
  return 1
}

main() {
  local root token_exports
  root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

  while [[ $# -gt 0 ]]; do
    case "$1" in
      --env-file)
        ENV_FILE="${2:?usage: $0 [--env-file <path>]}"
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
  command -v curl >/dev/null 2>&1 || die "curl is not installed or not on PATH"
  docker info >/dev/null 2>&1 ||
    die "the docker daemon is not running; start Docker and retry"

  cd "$root"
  [[ -f "$ENV_FILE" ]] ||
    die "$ENV_FILE not found; copy pve.env.example to pve.env and set PVE_PASSWORD"
  has_kvm ||
    die "/dev/kvm is not available to Docker; on WSL2 start a distribution that loads the KVM modules (for example: wsl -d Debian) and retry"

  trap cleanup EXIT

  echo "Starting the throwaway Proxmox VE container..."
  compose_proxmox up --detach >/dev/null || die "could not start Proxmox"
  echo "Waiting for the Proxmox API (up to $((WAIT_TRIES * 5)) seconds)..."
  wait_for_api || die "the Proxmox API did not answer in time"

  echo "Setting the root password..."
  set_root_password

  echo "Creating a per-run API token..."
  token_exports="$(bash scripts/pve-token.sh --container "$PROXMOX_CONTAINER")" ||
    die "could not create the API token"
  eval "$token_exports"

  echo "Building test image ${IMAGE} (cached after the first run)..."
  docker build --quiet --tag "$IMAGE" \
    --file tests/NFHotel.Proxmox.Tests/Dockerfile \
    tests/NFHotel.Proxmox.Tests >/dev/null || die "docker build failed"

  echo "Running Proxmox tests in a throwaway container..."
  docker run --rm --name "$TEST_CONTAINER" \
    --network "${PROJECT}_default" \
    --env PVE_HOST=proxmox --env PVE_PORT=8006 \
    --env PVE_VERIFY_TLS=false --env PVE_TIMEOUT_SECONDS=30 \
    --env PVE_REQUIRE_LIVE=true \
    --env PVE_NODE --env PVE_API_TOKEN_ID --env PVE_API_TOKEN_SECRET \
    "$IMAGE"
}

main "$@"
