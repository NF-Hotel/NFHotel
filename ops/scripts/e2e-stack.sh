#!/usr/bin/env bash
# Start or remove the throwaway NF Hotel stack that the end-to-end tests and the DAST
# scan run against (MIL-027 tasks 1 and 2 of NFHotel). It runs nginx with a self-signed
# certificate, the web application and the API built from an NF-Hotel/NFHotel checkout,
# and an empty PostgreSQL 16, with the compose files of deploy/ in the deployed shape:
# TLS at nginx, the API and the database on internal networks with no host port.
#
# Every secret is generated for this run and written only to files under <out>, readable
# by the owner; nothing is read from a real settings file. As in production (ADR-0024), the
# containers read their secrets from /run/secrets, and `up` ends with check-secret-env.sh. The API applies its migrations
# at start and creates one staff account per role. The stack never reaches the VM, the
# tailnet or a public NF Hotel host: a client joins the Docker network <name>-proxy and
# opens https://nginx.
#
# Usage: e2e-stack.sh up --app-dir <NFHotel checkout> --out <folder> [--name <prefix>]
#        e2e-stack.sh down [--name <prefix>]
#   up       build web and api, start the stack, wait for the health checks, and write
#            <out>/stack.env: E2E_NETWORK, E2E_BASE_URL and the user name and password
#            of the Manager, Receptionist and Housekeeping test accounts; for the API
#            scan of the DAST run (MIL-077), E2E_API_NETWORK (the internal network of the
#            API), E2E_API_TOKEN and the user name and password of its own Manager account
#   down     remove the containers, volumes and networks of the stack
#   --name   prefix of the compose projects and networks (default nfhotel-e2e)
# Exit:  0 done; 1 a build, start or check failed; 2 usage error
# Needs: bash 4+, docker with the compose plugin, openssl

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=ops/scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"
# shellcheck source=ops/scripts/lib/stack.sh
. "$ROOT/scripts/lib/stack.sh"

readonly ROLES=(Manager Receptionist Housekeeping)

action="${1:-}"
[[ "$action" == "up" || "$action" == "down" ]] || {
  usage >&2
  exit 2
}
shift
name="nfhotel-e2e"
app_dir=""
out=""
while (($#)); do
  case "$1" in
    --app-dir)
      app_dir="${2:?--app-dir needs a value}"
      shift
      ;;
    --out)
      out="${2:?--out needs a value}"
      shift
      ;;
    --name)
      name="${2:?--name needs a value}"
      shift
      ;;
    *)
      usage >&2
      exit 2
      ;;
  esac
  shift
done
stack_init "$name"

if [[ "$action" == "down" ]]; then
  stack_down --volumes
  echo "==> removed the stack $name"
  exit 0
fi

[[ -n "$app_dir" ]] || die "give --app-dir, an NF-Hotel/NFHotel checkout with Dockerfile and Dockerfile.api"
[[ -n "$out" ]] || die "give --out, the folder for the settings and stack.env"
mkdir -p "$out"
chmod 700 "$out"
out="$(cd "$out" && pwd)"
settings="$out/settings"

stack_down --volumes
manager_password="$(stack_secret 16)"
stack_write_settings "$settings" e2e-manager "$manager_password" true
stack_start "$settings" "$app_dir" --file "$(stack_native "$ROOT/deploy/e2e/nginx-no-host-ports.yml")"

# The first Manager comes from the settings above; the other roles are made with the
# create-staff command of the API image, the password passed in its environment only.
declare -A users=([Manager]=e2e-manager)
declare -A passwords=([Manager]="$manager_password")
for role in "${ROLES[@]:1}"; do
  user="e2e-${role,,}"
  password="$(stack_secret 16)"
  echo "==> creating the $role account $user"
  stack_create_staff "$user" "$role" "$password" || die "creating the $role account failed"
  users[$role]="$user"
  passwords[$role]="$password"
done

# The API scan of the DAST run signs in at the API with a Manager account of its own, so the
# attacks of the web scan on the login form cannot lock out its account (MIL-077).
api_user="e2e-api-scan"
api_password="$(stack_secret 16)"
echo "==> creating the Manager account $api_user for the API scan"
stack_create_staff "$api_user" Manager "$api_password" || die "creating the account of the API scan failed"
api_token="$(sed -n 's/^Api__Token=//p' "$settings/nfhotel-api.env")"
[[ -n "$api_token" ]] || die "$settings/nfhotel-api.env holds no API token"

umask 077
{
  echo "E2E_NETWORK=$STACK_PROXY_NET"
  echo "E2E_BASE_URL=https://nginx"
  for role in "${ROLES[@]}"; do
    echo "E2E_${role^^}_USER=${users[$role]}"
    echo "E2E_${role^^}_PASSWORD=${passwords[$role]}"
  done
  echo "E2E_API_NETWORK=$STACK_API_NET"
  echo "E2E_API_TOKEN=$api_token"
  echo "E2E_API_USER=$api_user"
  echo "E2E_API_PASSWORD=$api_password"
} >"$out/stack.env"
echo "==> checking that no secret is in the environment of a container (MIL-035)"
stack_check_secret_env
echo "==> the stack $name is up: join $STACK_PROXY_NET and open https://nginx (settings in $out/stack.env)"
