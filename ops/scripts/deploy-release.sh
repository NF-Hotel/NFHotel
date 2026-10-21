#!/usr/bin/env bash
# Deploy one NFHotel release on the VM: the NFHotel commit, the commit of the operations code
# and the image digests that the release action recorded (ADR-0007, MIL-021 task 6;
# ADR-0012 of NFHotel). The CD workflow calls it
# over Tailscale SSH as the user deploy, through sudo. scripts/install-deploy-user.sh
# installs it once as /usr/local/sbin/nfhotel-deploy, owned by root; after that, the
# update command of each CD run installs it from the release commit, only when that
# commit is on main (ADR-0018).
#
# deploy:
#   1. check every argument against a strict pattern (nothing else reaches a shell), and
#      refuse when /opt/nfhotel is not a git checkout of NF-Hotel/NFHotel, or of NF-Hotel/DevOps
#      until the VM is switched (ADR-0010, ADR-0016,
#      ADR-0012 of NFHotel)
#   2. take a lock, so two deploys never overlap
#   3. run the database backup if it is installed (ADR-0008)
#   4. check out the DevOps commit in /opt/nfhotel (detached; the git-ignored settings
#      stay): the compose files and scripts of the release. The application is in the
#      images, so no NFHotel checkout is on the VM
#   5. run scripts/deploy-services.sh of the operations folder (ops/ of an NFHotel checkout,
#      the root of a DevOps one) --no-build with the digests: images are pulled,
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
# update:   install the operations commands of a release commit (ADR-0018, MIL-032), before
#           migrate and deploy. It fetches main from origin and refuses a commit that is
#           not reachable from origin/main, the branch the ruleset protects: GitHub serves
#           commits of open pull requests by SHA, so this keeps unreviewed code from running
#           as root. From that commit (git show, never the working tree) it installs
#           ops/scripts/deploy-release.sh as nfhotel-deploy, ops/scripts/backup-database.sh
#           as nfhotel-backup, ops/scripts/renew-certificate.sh as nfhotel-cert-renew,
#           and the services and timers of the backup and of the certificate renewal
#           (enabled), each only when it differs.
#           The replaced copy is kept as <file>.previous, and each change is logged with
#           the old and new SHA-256. The sudo rule does not change.
# rollback: deploy the release before the latest one in the log again.
# status:   print the release log.
# update, deploy and rollback are the explicit commands that change the VM; status and a
# refused argument change nothing. The die and usage helpers are kept in this file
# because it is installed alone (scripts/lib/common.sh is not beside it).
#
# Usage: deploy-release.sh update <sha>
#        deploy-release.sh deploy <app-sha> <devops-sha> <web> <api> <analysis> <benchmark>
#        deploy-release.sh migrate <sha256 of the script> < migrations.sql
#        deploy-release.sh rollback
#        deploy-release.sh status
#   <app-sha>    the 40-character NF-Hotel/NFHotel commit of the release (recorded)
#   <devops-sha> the 40-character commit to check out: the commit of the operations code,
#                which since ADR-0016 is the NFHotel commit of the release
#   <web> ...    ghcr.io/nf-hotel/<image>@sha256:<64 hex>, in this order:
#                nfhotel-web, nfhotel-api, market-analysis, benchmark
# Env:   NFHOTEL_DIR (default /opt/nfhotel), NFHOTEL_USER (default nfhotel),
#        NFHOTEL_STATE (default /var/lib/nfhotel-deploy), NFHOTEL_REGISTRY
#        (default ghcr.io/nf-hotel), NFHOTEL_SECRETS (default /etc/nfhotel/secrets),
#        NFHOTEL_BACKUP_COMMAND (default /usr/local/sbin/nfhotel-backup; for the tests),
#        NFHOTEL_COMMAND_DIR (default /usr/local/sbin), NFHOTEL_SYSTEMD_DIR (default
#        /etc/systemd/system), NFHOTEL_SYSTEMCTL (default systemctl; for the tests)
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
readonly COMMAND_DIR="${NFHOTEL_COMMAND_DIR:-/usr/local/sbin}"
readonly SYSTEMD_DIR="${NFHOTEL_SYSTEMD_DIR:-/etc/systemd/system}"
readonly SYSTEMCTL="${NFHOTEL_SYSTEMCTL:-systemctl}"
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
  value="$(sed -n "s/^$1=\([0-9]\{1,5\}\)\$/\1/p" "$(ops_dir)/deploy/nginx/nginx.env" 2>/dev/null | tail -n 1)"
  echo "${value:-$2}"
}

# nginx_host: the address the proxy is published on (NGINX_BIND of the nginx settings file).
# The proxy is bound to one address, for example the tailnet address of the VM, so a test on
# 127.0.0.1 would not reach it. An empty value or 0.0.0.0 (all addresses) is tested on 127.0.0.1.
nginx_host() {
  local value
  value="$(sed -n 's/^NGINX_BIND=\([0-9.]\{7,15\}\)$/\1/p' "$(ops_dir)/deploy/nginx/nginx.env" 2>/dev/null | tail -n 1)"
  if [[ -z "$value" || "$value" == 0.0.0.0 ]]; then
    echo 127.0.0.1
  else
    echo "$value"
  fi
}

smoke_test() {
  local host http_port https_port code
  host="$(nginx_host)"
  http_port="$(nginx_port NGINX_HTTP_PORT 80)"
  https_port="$(nginx_port NGINX_HTTPS_PORT 443)"
  # Plain HTTP on purpose: port 80 must answer only with a redirect to HTTPS (MIL-017), and
  # this checks that redirect on the VM itself; nothing is sent but the request line.
  code="$(curl --silent --output /dev/null --write-out '%{http_code}' --max-time 10 "http://$host:$http_port/")" || true # NOSONAR
  [[ "$code" == "301" ]] || {
    echo "smoke test: $host:$http_port answered '$code', expected 301" >&2 # NOSONAR: a message, not a URL
    return 1
  }
  # The development certificate is self-signed, so the certificate is not checked here;
  # the TLS settings themselves are checked by the smoke test of MIL-017.
  code="$(curl --silent --insecure --output /dev/null --write-out '%{http_code}' --max-time 10 "https://$host:$https_port/")" || true
  [[ "$code" == "200" ]] || {
    echo "smoke test: $host:$https_port answered '$code', expected 200" >&2
    return 1
  }
  echo "smoke test: $host:$http_port answers 301, $host:$https_port answers 200"
}

# apply <app-sha> <devops-sha> <web> <api> <analysis> <benchmark>: check out the DevOps
# commit and start one release.
apply() {
  local sha="$2"
  as_app git fetch --quiet --depth 1 origin "$sha" || return 1
  if [[ "$(ops_rel)" == ops ]] && ! as_app git cat-file -e "$sha:ops/scripts/deploy-services.sh" 2>/dev/null; then
    # A release from before the switch of ADR-0016 names an operations commit of
    # NF-Hotel/DevOps, whose history NFHotel holds with the files at the root. That layout
    # has no ops/ and the settings now live in ops/, so a rollback to such a release keeps
    # the checkout and starts only the images of that release.
    echo "==> release of NFHotel $1: $sha is from before ADR-0016; keeping the operations of $(as_app git rev-parse --short HEAD) in $app_dir"
  else
    echo "==> release of NFHotel $1: checking out the operations commit $sha in $app_dir"
    as_app git checkout --quiet --detach "$sha" || return 1
    [[ "$(as_app git rev-parse HEAD)" == "$sha" ]] || return 1
  fi

  api_database_settings || return 1
  echo "==> starting the services from the release images (no build)"
  as_app env NFHOTEL_WEB_IMAGE="$3" NFHOTEL_API_IMAGE="$4" MARKET_ANALYSIS_IMAGE="$5" BENCHMARK_IMAGE="$6" \
    bash "$(ops_rel)/scripts/deploy-services.sh" --no-build || return 1
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

# previous_release <n>: the n-th latest successful release line (1 = latest). The log also
# holds the lines of update, which have six fields, so only release lines are counted.
previous_release() {
  [[ -f "$release_log" ]] || return 1
  awk 'NF == 7' "$release_log" | tail -n "$1" | head -n 1 | awk '{ print $2, $3, $4, $5, $6, $7 }'
}

record() {
  echo "$(date -u +%Y-%m-%dT%H:%M:%SZ) $*" >>"$release_log"
}

# ops_rel: the operations folder relative to the checkout: ops/ of an NFHotel checkout
# (ADR-0016), or the root of a NF-Hotel/DevOps checkout before the VM is switched.
ops_rel() {
  if [[ -f "$app_dir/ops/scripts/deploy-services.sh" ]]; then echo ops; else echo .; fi
}

# ops_dir: the same folder as an absolute path.
ops_dir() {
  echo "$app_dir/$(ops_rel)"
}

# check_checkout: refuse a VM whose deployment is not NF-Hotel/NFHotel, or NF-Hotel/DevOps
# until the switch of ADR-0016 (ADR-0010, ADR-0012). On a
# server that still runs a configuration outside git, a deploy would start a second
# stack next to the running one, so it stops before the backup and the checkout.
check_checkout() {
  local origin
  [[ -d "$app_dir/.git" ]] ||
    die "$app_dir is not a git checkout; reconcile the running deployment with the repository first (ADR-0010, MIL-021 task 10)"
  origin="$(as_app git config --get remote.origin.url)" || die "$app_dir has no origin remote"
  [[ "$origin" =~ ^(https://github\.com/|git@github\.com:)NF-Hotel/(NFHotel|DevOps)(\.git)?$ ]] ||
    die "$app_dir is a checkout of '$origin', not of NF-Hotel/NFHotel (or NF-Hotel/DevOps)"
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

# install_from <sha> <path in git> <target> <mode>: install one file of the commit when it
# differs from the target; keep the old target as <target>.previous and log both hashes.
# Returns 0 when it installed the file, 1 when the target was already equal. It is called
# in a condition, where set -e does not apply, so every step that can fail dies by itself.
# The new file is written in update_dir, which update removes on exit.
install_from() {
  local sha="$1" path="$2" target="$3" mode="$4" new="$update_dir/new" old=none
  as_app git show "$sha:$path" >"$new" || die "$path is not in $sha"
  if [[ -f "$target" ]] && cmp -s "$new" "$target"; then
    return 1
  fi
  if [[ -f "$target" ]]; then
    old="$(sha256sum "$target" | cut -d' ' -f1)" || die "cannot hash $target"
    cp -p "$target" "$target.previous" || die "cannot keep $target as $target.previous"
  fi
  install -o root -g root -m "$mode" "$new" "$target" || die "cannot install $target"
  record update "$sha" "$target" "$old" "$(sha256sum "$target" | cut -d' ' -f1)" || die "cannot log the update of $target"
  echo "    installed $target from $path"
}

update() {
  local sha="${1:-}" units=0 cert_units=0
  [[ "$sha" =~ ^[0-9a-f]{40}$ ]] || refuse "the commit must be 40 lower-case hex characters"
  update_dir="$(mktemp -d)"
  trap 'rm -rf "$update_dir"' EXIT
  check_checkout
  [[ "$(ops_rel)" == ops ]] || die "$app_dir has no ops/; update needs a checkout of NF-Hotel/NFHotel (ADR-0016)"
  echo "==> update: installing the operations commands of $sha (ADR-0018)"
  # The checkout is shallow. Fetching main brings the commits between its tip and what the
  # checkout has, which holds every release commit since; an older commit is refused.
  as_app git fetch --quiet --no-tags origin "+refs/heads/main:refs/remotes/origin/main" ||
    die "cannot fetch main from origin"
  as_app git cat-file -e "$sha^{commit}" 2>/dev/null ||
    as_app git fetch --quiet --no-tags --depth 1 origin "$sha" || die "cannot fetch $sha from origin"
  as_app git merge-base --is-ancestor "$sha" refs/remotes/origin/main ||
    refuse "$sha is not on main of origin; only a reviewed commit is installed (ADR-0018)"

  install_from "$sha" ops/scripts/deploy-release.sh "$COMMAND_DIR/nfhotel-deploy" 0755 || true
  install_from "$sha" ops/scripts/backup-database.sh "$COMMAND_DIR/nfhotel-backup" 0755 || true
  install_from "$sha" ops/scripts/renew-certificate.sh "$COMMAND_DIR/nfhotel-cert-renew" 0755 || true
  install_from "$sha" ops/deploy/systemd/nfhotel-backup.service "$SYSTEMD_DIR/nfhotel-backup.service" 0644 && units=1
  install_from "$sha" ops/deploy/systemd/nfhotel-backup.timer "$SYSTEMD_DIR/nfhotel-backup.timer" 0644 && units=1
  install_from "$sha" ops/deploy/systemd/nfhotel-cert-renew.service "$SYSTEMD_DIR/nfhotel-cert-renew.service" 0644 && cert_units=1
  install_from "$sha" ops/deploy/systemd/nfhotel-cert-renew.timer "$SYSTEMD_DIR/nfhotel-cert-renew.timer" 0644 && cert_units=1
  if ((units || cert_units)); then
    "$SYSTEMCTL" daemon-reload || die "systemctl daemon-reload failed"
  fi
  if ((units)); then
    "$SYSTEMCTL" try-restart nfhotel-backup.timer || die "the backup timer did not restart"
  fi
  # The certificate timer is enabled here, so a new VM gets it from its first CD (ADR-0009).
  if ((cert_units)); then
    "$SYSTEMCTL" enable --now nfhotel-cert-renew.timer || die "the certificate timer did not start"
  fi
  echo "==> update: the commands match $sha"
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
    update)
      shift
      update "$@"
      ;;
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
