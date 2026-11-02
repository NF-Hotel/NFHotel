#!/usr/bin/env bash
# Start NF Hotel on this machine in the light development mode (ADR-0035, MIL-056): PostgreSQL, the API, the web
# application and nginx run straight in the local Docker, from the compose files of ops/deploy/ in the deployed shape,
# with no Proxmox VE and no VM. It needs no /dev/kvm and about 3 GiB of memory, so it runs on macOS (Intel and Apple
# Silicon) as well as on Linux and Windows. Run it as: bash scripts/dev-up.sh --lite
#
#   this machine -> Docker -> postgres, api, web, nginx  ->  https://localhost:8443/
#
# The site is https://localhost:8443/ (a development certificate), and the mobile app reaches the API at
# https://localhost:8443/mobile-api/ (10.0.2.2:8443 from the Android emulator). Ollama, the market analysis and the
# benchmark service are not started; use the Proxmox mode (scripts/dev-up.sh) for them. The settings, the secrets and
# the development accounts are made once in ops/.dev-lite/ (git-ignored, readable by the owner only) and kept, so the
# database survives a restart. Each run rebuilds the API and the web application from the working tree.
#
# Usage: dev-lite.sh [--seed] [--open] [--check | --down | --destroy]
#   --seed     load the sample rooms, guests and bookings of db/seed_dev.sql
#   --open     open the site in the browser when it is ready
#   --check    only test that this machine can run it, and say how to fix each problem
#   --down     stop the stack; the database and the settings stay
#   --destroy  remove the stack, the database and the settings in ops/.dev-lite/
# Env:   DEV_SITE_PORT port of the site on this machine (default 8443)
#        DEV_LITE_NAME prefix of the containers and networks (default nfhotel-dev); DEV_LITE_DIR folder of the settings
#        (default ops/.dev-lite). A test sets both, so it never touches the developer's own stack.
# Exit:  0 done; 1 a check, build or start failed; 2 usage error
# Needs: bash 4+, docker with the compose plugin 2.24.4 or later, openssl, curl, git

set -euo pipefail
# Before anything of bash 4 runs: macOS comes with bash 3.2.
if ((BASH_VERSINFO[0] < 4)); then
  echo "error: bash ${BASH_VERSION} is too old; install bash 4 or later (on macOS: brew install bash, then open a new terminal)" >&2
  exit 1
fi

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ops="$root/ops"
# shellcheck source=ops/scripts/lib/common.sh
. "$ops/scripts/lib/common.sh"
# shellcheck source=ops/scripts/lib/stack.sh
. "$ops/scripts/lib/stack.sh"
export MSYS_NO_PATHCONV=1

export DEV_SITE_PORT="${DEV_SITE_PORT:-8443}"
readonly DIR="${DEV_LITE_DIR:-$ops/.dev-lite}"
readonly SETTINGS="$DIR/settings"
readonly ACCOUNTS="$DIR/dev-staff.env"
readonly MIN_COMPOSE="2.24.4"
seed=0
open_site=0
action=up

for arg in "$@"; do
  case "$arg" in
    --seed) seed=1 ;;
    --open) open_site=1 ;;
    --check) action=check ;;
    --down) action=down ;;
    --destroy) action=destroy ;;
    -h | --help)
      usage
      exit 0
      ;;
    *)
      echo "error: unknown argument: $arg" >&2
      usage >&2
      exit 2
      ;;
  esac
done
[[ "$DEV_SITE_PORT" =~ ^[0-9]+$ ]] || die "DEV_SITE_PORT must be a port number"
stack_init "${DEV_LITE_NAME:-nfhotel-dev}"

# The value of KEY in a settings file (no shell evaluation).
value_of() {
  sed -n "s/^$2=//p" "$1" | tr -d '\r' | head -n 1
}

# version_at_least <version> <minimum>: true when the dotted version is the minimum or later.
version_at_least() {
  local IFS=.
  local -a have want
  local i
  read -r -a have <<<"${1%%[!0-9.]*}"
  read -r -a want <<<"$2"
  for i in 0 1 2; do
    ((${have[i]:-0} > ${want[i]:-0})) && return 0
    ((${have[i]:-0} < ${want[i]:-0})) && return 1
  done
  return 0
}

# Tests that this machine can run the light mode, and says how to fix each problem. Every line starts with ok, warning
# or PROBLEM; one PROBLEM fails the check. Nothing is started or changed.
preflight() {
  local problems=0 tool compose_version kib mem_mib

  report() { # status text
    printf '  %-8s %s\n' "$1" "$2"
    [[ "$1" == "PROBLEM" ]] && problems=$((problems + 1))
    return 0
  }

  echo "Checking this machine for the light mode of dev-up.sh"
  report ok "bash ${BASH_VERSION%%(*}"
  for tool in openssl curl git; do
    if command -v "$tool" >/dev/null 2>&1; then
      report ok "$tool"
    else
      report PROBLEM "$tool is not on PATH (on Windows: Git for Windows; on macOS: xcode-select --install)"
    fi
  done

  if ! command -v docker >/dev/null 2>&1; then
    report PROBLEM "docker is not on PATH; install Docker Desktop (Windows, macOS) or Docker Engine (Linux)"
  elif ! docker info >/dev/null 2>&1; then
    report PROBLEM "the Docker daemon does not answer; start Docker Desktop, wait until it says running, and try again"
  else
    report ok "docker $(docker version --format '{{.Server.Version}}' 2>/dev/null)"
    compose_version="$(docker compose version --short 2>/dev/null || true)"
    if [[ -z "$compose_version" ]]; then
      report PROBLEM "the docker compose plugin is missing; update Docker Desktop, or install docker-compose-plugin"
    elif version_at_least "$compose_version" "$MIN_COMPOSE"; then
      report ok "docker compose $compose_version"
    else
      report PROBLEM "docker compose $compose_version is older than $MIN_COMPOSE; update Docker Desktop or the compose plugin"
    fi
    kib="$(docker info --format '{{.MemTotal}}' 2>/dev/null || echo 0)"
    mem_mib=$((kib / 1024 / 1024))
    if ((mem_mib >= 3072)); then
      report ok "Docker has ${mem_mib} MiB of memory (the light mode uses about 3 GiB)"
    else
      report warning "Docker has ${mem_mib} MiB of memory and the light mode uses about 3 GiB; raise it in the settings of Docker Desktop"
    fi
  fi

  # The port is free, unless nginx of an earlier run owns it.
  if [[ "$(docker inspect -f '{{.State.Running}}' "$STACK_NAME-nginx-nginx-1" 2>/dev/null || true)" == "true" ]]; then
    report ok "port ${DEV_SITE_PORT} belongs to the running stack"
  elif (exec 3<>"/dev/tcp/127.0.0.1/${DEV_SITE_PORT}") 2>/dev/null; then
    report PROBLEM "port ${DEV_SITE_PORT} on this machine is in use (perhaps by the Proxmox mode: bash scripts/dev-up.sh --down); stop what uses it, or set DEV_SITE_PORT"
  else
    report ok "port ${DEV_SITE_PORT} is free"
  fi

  if ((problems > 0)); then
    echo "$problems problem(s). Fix them and run: bash scripts/dev-up.sh --lite --check"
    return 1
  fi
  echo "This machine can run the light mode."
}

case "$action" in
  check)
    preflight
    exit
    ;;
  down)
    stack_down
    echo "Stopped the light stack; the database and the settings in $DIR stay."
    exit 0
    ;;
  destroy)
    stack_down --volumes
    rm -rf "$DIR"
    echo "Removed the light stack, its database and the settings."
    exit 0
    ;;
esac

preflight || exit 1

# 1. Settings, made once and kept: the database was created with this password.
mkdir -p "$DIR"
chmod 700 "$DIR"
if [[ ! -f "$SETTINGS/nfhotel-api.env" ]]; then
  echo "==> making the settings and secrets in $SETTINGS"
  (
    umask 077
    printf 'manager=%s\nreceptionist=%s\nhousekeeping=%s\n' "$(stack_secret 9)" "$(stack_secret 9)" "$(stack_secret 9)" >"$ACCOUNTS"
  )
  stack_write_settings "$SETTINGS" manager "$(value_of "$ACCOUNTS" manager)" false
fi

# 2. The stack, with the routes of the mobile app (ADR-0015) and the site on this machine's loopback only.
stack_start "$SETTINGS" "$root" --file "$(stack_native "$ops/deploy/nginx/docker-compose.dev.yml")" \
  --file "$(stack_native "$ops/deploy/dev-lite/nginx-port.yml")"

# 3. The Receptionist and Housekeeping accounts, once; the Manager is made by the API from its settings.
for entry in receptionist:Receptionist housekeeping:Housekeeping; do
  user="${entry%%:*}"
  grep -q "^created-${user}$" "$ACCOUNTS" && continue
  echo "==> creating the ${entry##*:} account $user"
  stack_create_staff "$user" "${entry##*:}" "$(value_of "$ACCOUNTS" "$user")" || die "creating the account $user failed"
  echo "created-${user}" >>"$ACCOUNTS"
done

if ((seed)); then
  echo "==> loading db/seed_dev.sql"
  stack_compose postgres exec -T postgres psql -v ON_ERROR_STOP=1 -U nfhotel -d nfhotel <"$root/db/seed_dev.sql" >/dev/null ||
    die "the seed script failed (it may already have been loaded)"
fi

echo "==> checking that no secret is in the environment of a container (MIL-035)"
stack_check_secret_env

url="https://localhost:${DEV_SITE_PORT}/"
echo "==> waiting for $url"
ready=0
for _ in $(seq 1 30); do
  if [[ "$(curl -sk -o /dev/null -w '%{http_code}' "$url" || true)" == "200" ]]; then
    ready=1
    break
  fi
  sleep 2
done
((ready)) || die "the site did not answer 200 at $url; look at: docker logs $STACK_NAME-nfhotel-web-nfhotel-web-1"

echo
echo "Ready: $url (development certificate: accept the browser warning)"
echo "Mobile app API: ${url}mobile-api/ (from the Android emulator: https://10.0.2.2:${DEV_SITE_PORT}/mobile-api/)"
echo
echo "Back office accounts (development only), sign in at ${url}admin/login:"
printf '  %-14s %-13s %s\n' "user name" "role" "password"
printf '  %-14s %-13s %s\n' manager Manager "$(value_of "$ACCOUNTS" manager)"
printf '  %-14s %-13s %s\n' receptionist Receptionist "$(value_of "$ACCOUNTS" receptionist)"
printf '  %-14s %-13s %s\n' housekeeping Housekeeping "$(value_of "$ACCOUNTS" housekeeping)"

if ((open_site)); then
  if command -v open >/dev/null 2>&1; then
    open "$url" || true
  elif command -v xdg-open >/dev/null 2>&1; then
    xdg-open "$url" >/dev/null 2>&1 || true
  elif command -v cmd.exe >/dev/null 2>&1; then
    cmd.exe /c start "" "$url" >/dev/null 2>&1 || true
  fi
fi
