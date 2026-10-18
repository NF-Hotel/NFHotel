#!/usr/bin/env bash
# Deploy one NFHotel release on the VM: the commit and the image digests that the
# Release workflow recorded (ADR-0007, MIL-021 task 6). The CD workflow calls it
# over Tailscale SSH as the user deploy, through sudo; scripts/install-deploy-user.sh
# installs it as /usr/local/sbin/nfhotel-deploy, owned by root, so a change to this
# file in git takes effect only when S03 installs it again.
#
# deploy:
#   1. check every argument against a strict pattern (nothing else reaches a shell), and
#      refuse when /opt/nfhotel is not a git checkout of NF-Hotel/NFHotel (ADR-0010)
#   2. take a lock, so two deploys never overlap
#   3. run the database backup if it is installed (ADR-0008)
#   4. check out the commit in /opt/nfhotel (detached; the git-ignored settings stay)
#   5. run scripts/deploy-services.sh --no-build with the digests: images are pulled,
#      never built, and compose recreates only the containers whose image or
#      configuration changed; named volumes are never removed
#   6. smoke test: port 80 answers 301 and HTTPS answers 200 on the VM
#   7. on success, append the release to the release log; on failure, deploy the
#      previous release from the log again and exit 1
# rollback: deploy the release before the latest one in the log again.
# status:   print the release log.
# deploy and rollback are the explicit commands that change the VM; status and a
# refused argument change nothing. The die and usage helpers are kept in this file
# because it is installed alone (scripts/lib/common.sh is not beside it).
#
# Usage: deploy-release.sh deploy <sha> <web> <api> <analysis> <benchmark>
#        deploy-release.sh rollback
#        deploy-release.sh status
#   <sha>        the 40-character commit of the release
#   <web> ...    ghcr.io/nf-hotel/<image>@sha256:<64 hex>, in this order:
#                nfhotel-web, nfhotel-api, market-analysis, benchmark
# Env:   NFHOTEL_DIR (default /opt/nfhotel), NFHOTEL_USER (default nfhotel),
#        NFHOTEL_STATE (default /var/lib/nfhotel-deploy), NFHOTEL_REGISTRY
#        (default ghcr.io/nf-hotel)
# Exit:  0 deployed (or rolled back on request); 1 the deploy failed (a rollback was
#        tried); 2 usage error or an argument that does not match its pattern
# Needs: bash 4+, git, curl, flock, sudo, docker with the compose plugin

set -euo pipefail

app_dir="${NFHOTEL_DIR:-/opt/nfhotel}"
app_user="${NFHOTEL_USER:-nfhotel}"
state_dir="${NFHOTEL_STATE:-/var/lib/nfhotel-deploy}"
registry="${NFHOTEL_REGISTRY:-ghcr.io/nf-hotel}"
release_log="$state_dir/releases.log"
readonly BACKUP_COMMAND=/usr/local/sbin/nfhotel-backup
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
    sudo -u "$app_user" -H bash -c 'cd "$1" && shift && "$@"' _ "$app_dir" "$@"
  fi
}

# check_release <sha> <digest...>: refuse anything that is not a full commit and four
# digests of the expected images in the expected order.
check_release() {
  local sha="$1" i
  shift
  [[ "$sha" =~ ^[0-9a-f]{40}$ ]] || refuse "the commit must be 40 lower-case hex characters"
  (($# == ${#NAMES[@]})) || refuse "expected ${#NAMES[@]} image digests, got $#"
  for i in "${!NAMES[@]}"; do
    local ref="${*:i+1:1}"
    [[ "$ref" =~ ^${registry//./\\.}/${NAMES[$i]}@sha256:[0-9a-f]{64}$ ]] ||
      refuse "argument $((i + 2)) is not $registry/${NAMES[$i]}@sha256:<64 hex>"
  done
}

# nginx_port <variable> <default>: the published port from the nginx settings file.
nginx_port() {
  local value
  value="$(sed -n "s/^$1=\([0-9]\{1,5\}\)\$/\1/p" "$app_dir/deploy/nginx/nginx.env" 2>/dev/null | tail -n 1)"
  echo "${value:-$2}"
}

smoke_test() {
  local http https code
  http="$(nginx_port NGINX_HTTP_PORT 80)"
  https="$(nginx_port NGINX_HTTPS_PORT 443)"
  code="$(curl --silent --output /dev/null --write-out '%{http_code}' --max-time 10 "http://127.0.0.1:$http/")" || true
  [[ "$code" == "301" ]] || {
    echo "smoke test: port $http answered '$code', expected 301" >&2
    return 1
  }
  # The development certificate is self-signed, so the certificate is not checked here;
  # the TLS settings themselves are checked by the smoke test of MIL-017.
  code="$(curl --silent --insecure --output /dev/null --write-out '%{http_code}' --max-time 10 "https://127.0.0.1:$https/")" || true
  [[ "$code" == "200" ]] || {
    echo "smoke test: port $https answered '$code', expected 200" >&2
    return 1
  }
  echo "smoke test: $http answers 301, $https answers 200"
}

# apply <sha> <web> <api> <analysis> <benchmark>: check out and start one release.
apply() {
  local sha="$1"
  echo "==> checking out $sha in $app_dir"
  as_app git fetch --quiet --depth 1 origin "$sha" || return 1
  as_app git checkout --quiet --detach "$sha" || return 1
  [[ "$(as_app git rev-parse HEAD)" == "$sha" ]] || return 1

  echo "==> starting the services from the release images (no build)"
  as_app env NFHOTEL_WEB_IMAGE="$2" NFHOTEL_API_IMAGE="$3" MARKET_ANALYSIS_IMAGE="$4" BENCHMARK_IMAGE="$5" \
    bash scripts/deploy-services.sh --no-build || return 1
  smoke_test
}

# previous_release <n>: the n-th latest successful release line (1 = latest).
previous_release() {
  [[ -f "$release_log" ]] || return 1
  tail -n "$1" "$release_log" | head -n 1 | awk 'NF == 6 { print $2, $3, $4, $5, $6 }'
}

record() {
  echo "$(date -u +%Y-%m-%dT%H:%M:%SZ) $*" >>"$release_log"
}

# check_checkout: refuse a VM whose deployment is not this repository (ADR-0010). On a
# server that still runs a configuration outside git, a deploy would start a second
# stack next to the running one, so it stops before the backup and the checkout.
check_checkout() {
  local origin
  [[ -d "$app_dir/.git" ]] ||
    die "$app_dir is not a git checkout; reconcile the running deployment with the repository first (ADR-0010, MIL-021 task 10)"
  origin="$(as_app git config --get remote.origin.url)" || die "$app_dir has no origin remote"
  [[ "$origin" =~ ^(https://github\.com/|git@github\.com:)NF-Hotel/NFHotel(\.git)?$ ]] ||
    die "$app_dir is a checkout of '$origin', not of NF-Hotel/NFHotel"
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
    # shellcheck disable=SC2086 # the line holds the five checked fields
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

rollback() {
  local target
  target="$(previous_release 2 || true)"
  [[ -n "$target" ]] || die "the release log has no release before the latest one"
  check_checkout
  # shellcheck disable=SC2086 # the line holds the five checked fields
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
