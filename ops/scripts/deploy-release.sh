#!/usr/bin/env bash
# Deploy one NFHotel release on the VM: the NFHotel commit, the NF-Hotel/DevOps commit
# and the image digests that the release action recorded (ADR-0007, MIL-021 task 6;
# ADR-0012 of NFHotel). The CD workflow calls it
# over Tailscale SSH as the user deploy, through sudo; scripts/install-deploy-user.sh
# installs it as /usr/local/sbin/nfhotel-deploy, owned by root, so a change to this
# file in git takes effect only when S03 installs it again.
#
# deploy:
#   1. check every argument against a strict pattern (nothing else reaches a shell), and
#      refuse when /opt/nfhotel is not a git checkout of NF-Hotel/DevOps (ADR-0010,
#      ADR-0012 of NFHotel)
#   2. take a lock, so two deploys never overlap
#   3. run the database backup if it is installed (ADR-0008)
#   4. check out the DevOps commit in /opt/nfhotel (detached; the git-ignored settings
#      stay): the compose files and scripts of the release. The application is in the
#      images, so no NFHotel checkout is on the VM
#   5. run scripts/deploy-services.sh --no-build with the digests: images are pulled,
#      never built, and compose recreates only the containers whose image or
#      configuration changed; named volumes are never removed
#   6. smoke test: port 80 answers 301 and HTTPS answers 200 on the VM
#   7. on success, append the release to the release log; on failure, deploy the
#      previous release from the log again and exit 1
# migrate:  apply the idempotent migration script of a release to the database, before
#           its deploy (MIL-022 task 6, ADR-0008, ADR-0011). The script comes on standard
#           input and must have the SHA-256 given as the argument, the one recorded in
#           the release manifest. A fresh backup runs first, then psql applies the script
#           in the PostgreSQL container as nfhotel_migrator, which sets the role
#           nfhotel_owner, with the URL of /etc/nfhotel/secrets/nfhotel-migrator-database.env.
#           Each migration of the script commits on its own and is skipped when its id is
#           already in the history, so a rerun changes nothing.
# rollback: deploy the release before the latest one in the log again.
# status:   print the release log.
# deploy and rollback are the explicit commands that change the VM; status and a
# refused argument change nothing. The die and usage helpers are kept in this file
# because it is installed alone (scripts/lib/common.sh is not beside it).
#
# Usage: deploy-release.sh deploy <app-sha> <devops-sha> <web> <api> <analysis> <benchmark>
#        deploy-release.sh migrate <sha256 of the script> < migrations.sql
#        deploy-release.sh rollback
#        deploy-release.sh status
#   <app-sha>    the 40-character NF-Hotel/NFHotel commit of the release (recorded)
#   <devops-sha> the 40-character NF-Hotel/DevOps commit to check out
#   <web> ...    ghcr.io/nf-hotel/<image>@sha256:<64 hex>, in this order:
#                nfhotel-web, nfhotel-api, market-analysis, benchmark
# Env:   NFHOTEL_DIR (default /opt/nfhotel), NFHOTEL_USER (default nfhotel),
#        NFHOTEL_STATE (default /var/lib/nfhotel-deploy), NFHOTEL_REGISTRY
#        (default ghcr.io/nf-hotel), NFHOTEL_SECRETS (default /etc/nfhotel/secrets),
#        NFHOTEL_BACKUP_COMMAND (default /usr/local/sbin/nfhotel-backup; for the tests)
# Exit:  0 deployed (or rolled back on request); 1 the deploy failed (a rollback was
#        tried); 2 usage error or an argument that does not match its pattern
# Needs: bash 4+, git, curl, flock, sudo, docker with the compose plugin

set -euo pipefail

app_dir="${NFHOTEL_DIR:-/opt/nfhotel}"
app_user="${NFHOTEL_USER:-nfhotel}"
state_dir="${NFHOTEL_STATE:-/var/lib/nfhotel-deploy}"
registry="${NFHOTEL_REGISTRY:-ghcr.io/nf-hotel}"
release_log="$state_dir/releases.log"
secrets_dir="${NFHOTEL_SECRETS:-/etc/nfhotel/secrets}"
# Overridable for the tests only: sudo resets the environment of the deploy user.
readonly BACKUP_COMMAND="${NFHOTEL_BACKUP_COMMAND:-/usr/local/sbin/nfhotel-backup}"
readonly NAMES=(nfhotel-web nfhotel-api market-analysis benchmark)

die() {
  echo "error: $*" >&2
  exit 1
}

refuse() {
  echo "refused: $*" >&2
  exit 2
}

usage() {
  sed -n '2,/^set -euo/p' "${BASH_SOURCE[0]}" | sed '$d' | sed 's/^# \{0,1\}//'
}

# as_app <command...>: run a command as the application user in the checkout.
as_app() {
  if [[ "$(id -un)" == "$app_user" ]]; then
    (cd "$app_dir" && "$@")
  else
    # The database settings of the API pass in the environment, never as an argument.
    sudo -u "$app_user" -H --preserve-env=NFHOTEL_API_CONNECTION_STRING,NFHOTEL_API_MIGRATE_ON_START \
      bash -c 'cd "$1" && shift && "$@"' _ "$app_dir" "$@"
  fi
}

# check_release <app-sha> <devops-sha> <digest...>: refuse anything that is not two full
# commits and four digests of the expected images in the expected order.
check_release() {
  local i
  [[ "${1:-}" =~ ^[0-9a-f]{40}$ ]] || refuse "the NFHotel commit must be 40 lower-case hex characters"
  [[ "${2:-}" =~ ^[0-9a-f]{40}$ ]] || refuse "the DevOps commit must be 40 lower-case hex characters"
  shift 2
  (($# == ${#NAMES[@]})) || refuse "expected ${#NAMES[@]} image digests, got $#"
  for i in "${!NAMES[@]}"; do
    local ref="${*:i+1:1}"
    [[ "$ref" =~ ^${registry//./\\.}/${NAMES[$i]}@sha256:[0-9a-f]{64}$ ]] ||
      refuse "argument $((i + 3)) is not $registry/${NAMES[$i]}@sha256:<64 hex>"
  done
}

# nginx_port <variable> <default>: the published port from the nginx settings file.
nginx_port() {
  local value
  value="$(sed -n "s/^$1=\([0-9]\{1,5\}\)\$/\1/p" "$app_dir/deploy/nginx/nginx.env" 2>/dev/null | tail -n 1)"
  echo "${value:-$2}"
}

# nginx_host: the address the proxy is published on (NGINX_BIND of the nginx settings file).
# The proxy is bound to one address, for example the tailnet address of the VM, so a test on
# 127.0.0.1 would not reach it. An empty value or 0.0.0.0 (all addresses) is tested on 127.0.0.1.
nginx_host() {
  local value
  value="$(sed -n 's/^NGINX_BIND=\([0-9.]\{7,15\}\)$/\1/p' "$app_dir/deploy/nginx/nginx.env" 2>/dev/null | tail -n 1)"
  if [[ -z "$value" || "$value" == 0.0.0.0 ]]; then
    echo 127.0.0.1
  else
    echo "$value"
  fi
}

smoke_test() {
  local host http https code
  host="$(nginx_host)"
  http="$(nginx_port NGINX_HTTP_PORT 80)"
  https="$(nginx_port NGINX_HTTPS_PORT 443)"
  code="$(curl --silent --output /dev/null --write-out '%{http_code}' --max-time 10 "http://$host:$http/")" || true
  [[ "$code" == "301" ]] || {
    echo "smoke test: $host:$http answered '$code', expected 301" >&2
    return 1
  }
  # The development certificate is self-signed, so the certificate is not checked here;
  # the TLS settings themselves are checked by the smoke test of MIL-017.
  code="$(curl --silent --insecure --output /dev/null --write-out '%{http_code}' --max-time 10 "https://$host:$https/")" || true
  [[ "$code" == "200" ]] || {
    echo "smoke test: $host:$https answered '$code', expected 200" >&2
    return 1
  }
  echo "smoke test: $host:$http answers 301, $host:$https answers 200"
}

# apply <app-sha> <devops-sha> <web> <api> <analysis> <benchmark>: check out the DevOps
# commit and start one release.
apply() {
  local sha="$2"
  echo "==> release of NFHotel $1: checking out DevOps $sha in $app_dir"
  as_app git fetch --quiet --depth 1 origin "$sha" || return 1
  as_app git checkout --quiet --detach "$sha" || return 1
  [[ "$(as_app git rev-parse HEAD)" == "$sha" ]] || return 1

  api_database_settings || return 1
  echo "==> starting the services from the release images (no build)"
  as_app env NFHOTEL_WEB_IMAGE="$3" NFHOTEL_API_IMAGE="$4" MARKET_ANALYSIS_IMAGE="$5" BENCHMARK_IMAGE="$6" \
    bash scripts/deploy-services.sh --no-build || return 1
  smoke_test
}

# api_database_settings: once the roles of ADR-0011 exist, the API connects as nfhotel_app
# with the login in the secrets folder (root only, 0600), and it no longer migrates at
# start, because the migrate command applies the migrations (MIL-022 task 6). Before that,
# the API keeps the superuser of postgres.env and its migrations at start.
api_database_settings() {
  local file="$secrets_dir/nfhotel-api-database.env" value
  unset NFHOTEL_API_CONNECTION_STRING NFHOTEL_API_MIGRATE_ON_START
  [[ -f "$file" ]] || return 0
  value="$(sed -n 's/^ConnectionStrings__HotelDatabase=//p' "$file" | tail -n 1)"
  [[ "$value" == *"Username=nfhotel_app;"* ]] || {
    echo "error: $file holds no connection string for nfhotel_app" >&2
    return 1
  }
  export NFHOTEL_API_CONNECTION_STRING="$value" NFHOTEL_API_MIGRATE_ON_START=false
  echo "==> the API connects as nfhotel_app and does not migrate at start (ADR-0011)"
}

# previous_release <n>: the n-th latest successful release line (1 = latest).
previous_release() {
  [[ -f "$release_log" ]] || return 1
  tail -n "$1" "$release_log" | head -n 1 | awk 'NF == 7 { print $2, $3, $4, $5, $6, $7 }'
}

record() {
  echo "$(date -u +%Y-%m-%dT%H:%M:%SZ) $*" >>"$release_log"
}

# check_checkout: refuse a VM whose deployment is not NF-Hotel/DevOps (ADR-0010, ADR-0012). On a
# server that still runs a configuration outside git, a deploy would start a second
# stack next to the running one, so it stops before the backup and the checkout.
check_checkout() {
  local origin
  [[ -d "$app_dir/.git" ]] ||
    die "$app_dir is not a git checkout; reconcile the running deployment with the repository first (ADR-0010, MIL-021 task 10)"
  origin="$(as_app git config --get remote.origin.url)" || die "$app_dir has no origin remote"
  [[ "$origin" =~ ^(https://github\.com/|git@github\.com:)NF-Hotel/DevOps(\.git)?$ ]] ||
    die "$app_dir is a checkout of '$origin', not of NF-Hotel/DevOps"
}

deploy() {
  check_release "$@"
  check_checkout
  local last=""
  last="$(previous_release 1 || true)"

  if [[ -x "$BACKUP_COMMAND" ]]; then
    echo "==> backing up the database before the deploy"
    "$BACKUP_COMMAND" --apply || die "the backup failed; nothing was deployed"
  else
    echo "warning: $BACKUP_COMMAND is not installed; deploying without a fresh backup" >&2
  fi

  if apply "$@"; then
    record "$@"
    echo "==> deployed $1"
    return 0
  fi

  echo "==> the deploy of $1 failed" >&2
  if [[ -n "$last" ]]; then
    echo "==> rolling back to the previous release" >&2
    # shellcheck disable=SC2086 # the line holds the six checked fields
    if apply $last; then
      echo "==> rolled back to ${last%% *}" >&2
    else
      echo "error: the rollback failed too; S03 must restore by hand" >&2
    fi
  else
    echo "error: no previous release in $release_log to roll back to" >&2
  fi
  exit 1
}

# migrate <sha256>: see the header. Nothing is applied unless the script on standard
# input has exactly that hash and the backup has run.
migrate() {
  local want="${1:-}" got pg
  [[ "$want" =~ ^[0-9a-f]{64}$ ]] || refuse "the migration script hash must be 64 lower-case hex characters"
  [[ -f "$secrets_dir/nfhotel-migrator-database.env" ]] ||
    die "$secrets_dir/nfhotel-migrator-database.env is missing; run scripts/prepare-database-roles.sh (ADR-0011)"
  script="$(mktemp)"
  trap 'rm -f "$script"' EXIT
  cat >"$script"
  got="$(sha256sum "$script" | cut -d' ' -f1)"
  [[ "$got" == "$want" ]] || refuse "the migration script has SHA-256 $got, not $want"
  [[ -s "$script" ]] || refuse "the migration script is empty"
  pg="$(docker ps --quiet --filter label=com.docker.compose.project=nfhotel-postgres \
    --filter label=com.docker.compose.service=postgres | head -n 1)"
  [[ -n "$pg" ]] || die "the PostgreSQL container of the project nfhotel-postgres is not running"

  if [[ -x "$BACKUP_COMMAND" ]]; then
    echo "==> backing up the database before the migrations"
    "$BACKUP_COMMAND" --apply || die "the backup failed; no migration was applied"
  else
    die "$BACKUP_COMMAND is not installed; migrations run only after a fresh backup (ADR-0008)"
  fi

  echo "==> applying the migration script $want as nfhotel_migrator"
  # shellcheck disable=SC2016 # expanded by the shell inside the container
  docker exec -i --env-file "$secrets_dir/nfhotel-migrator-database.env" "$pg" \
    sh -c 'psql "$NFHOTEL_MIGRATOR_URL" --quiet --no-psqlrc -v ON_ERROR_STOP=1' <"$script" ||
    die "the migration script failed; the backup above holds the state before it"
  echo "==> migrations applied"
}

rollback() {
  local target
  target="$(previous_release 2 || true)"
  [[ -n "$target" ]] || die "the release log has no release before the latest one"
  check_checkout
  # shellcheck disable=SC2086 # the line holds the six checked fields
  check_release $target
  # shellcheck disable=SC2086
  apply $target || die "the rollback to ${target%% *} failed"
  # shellcheck disable=SC2086
  record $target
  echo "==> rolled back to ${target%% *}"
}

main() {
  [[ $# -ge 1 ]] || {
    usage >&2
    exit 2
  }
  mkdir -p "$state_dir"
  exec 9>"$state_dir/lock"
  flock --nonblock 9 || die "another deploy is running"

  case "$1" in
    deploy)
      shift
      deploy "$@"
      ;;
    migrate)
      shift
      migrate "$@"
      ;;
    rollback) rollback ;;
    status) cat "$release_log" 2>/dev/null || echo "no release deployed yet" ;;
    -h | --help) usage ;;
    *)
      usage >&2
      exit 2
      ;;
  esac
}

main "$@"
