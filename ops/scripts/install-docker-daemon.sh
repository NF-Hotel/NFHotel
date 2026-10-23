#!/usr/bin/env bash
# Install the Docker daemon settings of the NF Hotel VM (MIL-023 task 2, CIS Docker
# Benchmark section 2): deploy/host/docker/daemon.json as /etc/docker/daemon.json.
# The file turns on live-restore and no-new-privileges, sets the local log driver with a size
# limit (5 files of 10 MB per container), turns off the userland proxy and inter-container
# traffic on the default bridge. S01 or S03 runs it as root in the checkout /opt/nfhotel.
#
# It:
#   1. shows the difference between the installed file and the one in git
#   2. with --apply: keeps the installed file as daemon.json.<UTC time>.bak, installs the new
#      one (root, 0644) and checks it with `dockerd --validate`; an invalid file is rolled back
#   3. with --apply --restart: restarts Docker and checks that the settings are live
#
# The daemon reads the file only when it starts, so the settings take effect at the next
# restart. A restart stops every container once (live-restore is not on yet), and the
# containers come back by their restart policy. The log driver applies to containers created
# after the restart: recreate them (redeploy the release) to limit their logs.
#
# Usage: install-docker-daemon.sh [--apply [--restart]]
#   --apply    install the file; without it the script only shows what would change (dry run)
#   --restart  restart Docker after installing and check the settings (a short outage)
# Env:   NFHOTEL_DOCKER_DAEMON_JSON  the installed file (default /etc/docker/daemon.json; tests)
# Exit:  0 installed or nothing to do; 1 a step failed; 2 usage error
# Needs: bash 4+, root for --apply, diff, install, systemctl and docker for --restart

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=ops/scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"
readonly SOURCE="$ROOT/deploy/host/docker/daemon.json"
readonly TARGET="${NFHOTEL_DOCKER_DAEMON_JSON:-/etc/docker/daemon.json}"
apply=0
restart=0

while (($#)); do
  case "$1" in
    --apply) apply=1 ;;
    --restart) restart=1 ;;
    -h | --help)
      usage
      exit 0
      ;;
    *)
      usage >&2
      exit 2
      ;;
  esac
  shift
done
((apply || ! restart)) || die "--restart needs --apply"
((! apply)) || [[ "$(id -u)" == 0 ]] || die "run it as root (sudo)"

echo "==> 1. difference between $TARGET and deploy/host/docker/daemon.json"
if [[ -f "$TARGET" ]] && cmp -s "$SOURCE" "$TARGET"; then
  echo "    none; the installed file is already the one in git"
  changed=0
else
  old=/dev/null
  [[ -f "$TARGET" ]] && old="$TARGET"
  diff -u "$old" "$SOURCE" || true
  changed=1
fi

if ((! apply)); then
  ((changed)) && echo "==> dry run: give --apply to install it, and --restart to make it live"
  exit 0
fi

echo "==> 2. install"
if ((changed)); then
  backup=""
  if [[ -f "$TARGET" ]]; then
    backup="$TARGET.$(date -u +%Y%m%dT%H%M%SZ).bak"
    cp -p "$TARGET" "$backup"
    echo "    kept the old file as $backup"
  fi
  install -D -o root -g root -m 0644 "$SOURCE" "$TARGET"
  if command -v dockerd >/dev/null 2>&1; then
    if ! dockerd --validate --config-file "$TARGET" >/dev/null; then
      if [[ -n "$backup" ]]; then cp -p "$backup" "$TARGET"; else rm -f "$TARGET"; fi
      die "dockerd refused the new file; the old one is back in place"
    fi
    echo "    dockerd --validate accepts it"
  else
    echo "    dockerd is not installed here; the file was not validated"
  fi
else
  echo "    nothing to install"
fi

if ((! restart)); then
  echo "==> done; make it live in a maintenance window: $0 --apply --restart"
  exit 0
fi

echo "==> 3. restart Docker"
systemctl restart docker
live="$(docker info --format '{{.LiveRestoreEnabled}} {{.LoggingDriver}}')"
[[ "$live" == "true local" ]] || die "Docker runs with live-restore and log driver '$live', expected 'true local'"
# Not every Docker version lists the daemon-wide no-new-privileges; docker inspect of each
# container shows it too (CIS-001, row "no new privileges").
docker info --format '{{.SecurityOptions}}' | grep -q no-new-privileges ||
  echo "    note: docker info does not list no-new-privileges; check a container with docker inspect"
echo "==> done; Docker runs with the new settings. Recreate the containers to limit their logs."
