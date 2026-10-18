#!/usr/bin/env bash
# Restore drill for the NFHotel database (ADR-0008, MIL-021 task 7): prove that a
# backup can be read and restored. Run it monthly and before a major migration,
# on any machine with Docker and the private age key of S03, and record the
# result in an RC.
#
# Steps:
#   1. decrypt the backup with the age identity (the key file of S03)
#   2. start a throwaway PostgreSQL 16 container with no network
#   3. pg_restore the dump into it
#   4. report every table with its row count and the latest EF Core migration
#   5. remove the container and the decrypted dump
# The drill fails when the restore fails, when no table was restored, or when
# the migration history is missing.
#
# Usage: restore-drill.sh --identity <key file> <backup .dump.age>
#   --identity  the age identity file of S03 (never stored on the VM)
# Exit:  0 the restore worked; 1 the drill failed; 2 usage error
# Needs: bash 4+, docker, age

set -euo pipefail

identity=""
backup=""
image="postgres:16-alpine"
container="nfhotel-restore-drill-$$"

die() {
  echo "error: $*" >&2
  exit 1
}

usage() {
  sed -n '2,/^set -euo/p' "${BASH_SOURCE[0]}" | sed '$d' | sed 's/^# \{0,1\}//'
}

while (($#)); do
  case "$1" in
    --identity)
      identity="${2:?--identity needs a value}"
      shift
      ;;
    -h | --help)
      usage
      exit 0
      ;;
    -*)
      usage >&2
      exit 2
      ;;
    *) backup="$1" ;;
  esac
  shift
done

[[ -r "$identity" ]] || {
  usage >&2
  exit 2
}
[[ -r "$backup" ]] || {
  usage >&2
  exit 2
}
command -v age >/dev/null || die "age is not installed"

work="$(mktemp -d)"
cleanup() {
  docker rm -f "$container" >/dev/null 2>&1 || true
  rm -rf "$work"
}
trap cleanup EXIT

echo "==> 1. decrypting $(basename "$backup")"
age --decrypt --identity "$identity" --output "$work/nfhotel.dump" "$backup" || die "the backup cannot be decrypted"

echo "==> 2. starting a throwaway $image (no network)"
docker run --detach --name "$container" --network none \
  --env POSTGRES_PASSWORD=drill --env POSTGRES_DB=drill "$image" >/dev/null
for _ in $(seq 60); do
  docker exec "$container" pg_isready --username postgres --dbname drill >/dev/null 2>&1 && break
  sleep 1
done
docker exec "$container" pg_isready --username postgres --dbname drill >/dev/null || die "PostgreSQL did not start"

echo "==> 3. restoring"
docker cp "$work/nfhotel.dump" "$container:/tmp/nfhotel.dump" >/dev/null
docker exec "$container" pg_restore --username postgres --dbname drill --no-owner --exit-on-error /tmp/nfhotel.dump ||
  die "pg_restore failed"

echo "==> 4. what was restored"
sql() {
  docker exec "$container" psql --username postgres --dbname drill --tuples-only --no-align --command "$1"
}
tables="$(sql "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE'")"
((tables > 0)) || die "no table was restored"
sql "SELECT format('%I.%I', schemaname, relname) FROM pg_stat_user_tables ORDER BY 1" | while read -r table; do
  printf '    %-45s %s rows\n' "$table" "$(sql "SELECT count(*) FROM $table")"
done
migration="$(sql "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 1" 2>/dev/null)" ||
  die "the EF Core migration history is missing"
[[ -n "$migration" ]] || die "the EF Core migration history is empty"
echo "    latest migration: $migration"
echo "==> the restore drill passed ($tables tables); record the output in an RC"
