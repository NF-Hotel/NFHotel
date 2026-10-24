#!/usr/bin/env bash
# Tests of the settings archive of scripts/backup-database.sh (MIL-037 task 3, ADR-0025): the
# backup also writes nfhotel-settings-<kind>-<time>.tar.age, encrypted to the age key of S03;
# it holds the settings files, the proxy certificate, /etc/nfhotel and the ssh key of the copy,
# and leaves out the secret files that the deploy writes (ADR-0024); a restore unpacks it in
# place, and the old archives are pruned like the dumps. A fake stands in for docker, and a
# throwaway age key for the key of S03.
#
# Run it in Debian 12 with the repository at /src:
#   docker run --rm -v "$PWD:/src:ro" debian:12-slim bash /src/tests/test-backup-settings.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 12 (installs age and tar)

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

OPS="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
apt-get update -qq >/dev/null && apt-get install -y -qq --no-install-recommends age >/dev/null ||
  {
    echo "FAIL  cannot install age"
    exit 1
  }
W="$(mktemp -d)"
trap 'rm -rf "$W"' EXIT
pass=0
fail=0
ok() {
  echo "PASS  $1"
  pass=$((pass + 1))
}
ko() {
  echo "FAIL  $1"
  fail=$((fail + 1))
}

# A VM in miniature: the checkout, /etc/nfhotel, the ssh key of the copy and a fake docker.
app="$W/opt/nfhotel" etc="$W/etc/nfhotel"
mkdir -p "$app/ops/deploy/postgres/secrets" "$app/ops/deploy/nfhotel-api" "$app/ops/deploy/nginx/tls" \
  "$app/ops/deploy/market-analysis" "$etc/secrets" "$W/root/.ssh" "$W/bin" "$W/backups"
echo "POSTGRES_PASSWORD=pg" >"$app/ops/deploy/postgres/postgres.env"
echo "pg" >"$app/ops/deploy/postgres/secrets/POSTGRES_PASSWORD"
echo "Api__Token=tok" >"$app/ops/deploy/nfhotel-api/nfhotel-api.env"
echo "[llm]" >"$app/ops/deploy/market-analysis/market-analysis.toml"
echo cert >"$app/ops/deploy/nginx/tls/fullchain.pem"
echo "ConnectionStrings__HotelDatabase=app" >"$etc/secrets/nfhotel-api-database.env"
echo "private" >"$W/root/.ssh/nfhotel-backup"
age-keygen -o "$W/identity" 2>/dev/null
recipient="$(age-keygen -y "$W/identity")"
printf 'AGE_RECIPIENT=%s\nBACKUP_DIR=%s\nBACKUP_CONTAINER=nfhotel-postgres-postgres-1\nBACKUP_SSH_KEY=%s\n' \
  "$recipient" "$W/backups" "$W/root/.ssh/nfhotel-backup" >"$etc/backup.env"
printf '#!/bin/bash\necho dump\n' >"$W/bin/docker"
chmod +x "$W/bin/docker"
backup() {
  PATH="$W/bin:$PATH" NFHOTEL_BACKUP_SETTINGS="$etc/backup.env" NFHOTEL_DIR="$app" NFHOTEL_ETC="$etc" \
    NFHOTEL_METRICS_DIR="$W/metrics" bash "$OPS/scripts/backup-database.sh" --apply >"$W/out" 2>&1
}

backup && ok "the backup runs" || ko "the backup: $(tail -3 "$W/out")"
archive="$(find "$W/backups" -name 'nfhotel-settings-*.tar.age' | head -1)"
[[ -n "$archive" && "$(stat -c %a "$archive")" == 600 ]] && ok "the settings archive is written, 0600" ||
  ko "no settings archive: $(ls "$W/backups")"
head -c 21 "$archive" | grep -q "age-encryption.org" && ! grep -q "Api__Token" "$archive" &&
  ok "the archive is encrypted with age" || ko "the archive is not encrypted"

age --decrypt --identity "$W/identity" "$archive" | tar --list --file - >"$W/list" 2>/dev/null
for wanted in postgres/postgres.env nfhotel-api/nfhotel-api.env market-analysis/market-analysis.toml \
  nginx/tls/fullchain.pem etc/nfhotel/secrets/nfhotel-api-database.env etc/nfhotel/backup.env root/.ssh/nfhotel-backup; do
  grep -q "$wanted\$" "$W/list" || ko "the archive lacks $wanted"
done
grep -q "secrets/POSTGRES_PASSWORD" "$W/list" && ko "the archive holds the secret files the deploy writes" ||
  ok "the archive holds the settings, the certificate, /etc/nfhotel and the key, not the derived secret files"

mkdir -p "$W/restore"
age --decrypt --identity "$W/identity" "$archive" | tar --extract --file - --directory "$W/restore" &&
  [[ "$(cat "$W/restore${app}/ops/deploy/nfhotel-api/nfhotel-api.env")" == "Api__Token=tok" ]] &&
  ok "a restore unpacks the settings in place" || ko "the restore: $(find "$W/restore" -type f | head -3)"

for day in 01 02 03 04 05 06 07 08 09 10 11 12 13 14 15 16; do
  : >"$W/backups/nfhotel-settings-daily-202601${day}T010000Z.tar.age"
done
backup
[[ "$(find "$W/backups" -name 'nfhotel-settings-daily-*' | wc -l)" -le 14 ]] && ok "old settings archives are pruned" ||
  ko "pruning: $(find "$W/backups" -name 'nfhotel-settings-daily-*' | wc -l) left"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
