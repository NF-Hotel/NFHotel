#!/usr/bin/env bash
# Prepare the running NF Hotel PostgreSQL 16 container for the API (ADR-0011, MIL-024
# task 2): least-privilege roles, their secrets, the ownership of the existing objects
# and the internal network. S03 runs it on the VM as root in the checkout. Nothing is
# restarted, no volume or existing role is changed, and no second database is created.
#
# Steps (each needs its flag; without --apply the script only reports what it would do):
#   --apply               create the passwords in the secrets folder (once), run
#                         scripts/db/roles.sql, and write the settings files with the
#                         connection strings for the API and the migration step
#   --transfer-ownership  after a fresh backup, run scripts/db/transfer-ownership.sql
#                         (the existing tables, sequences and views move to nfhotel_owner;
#                         no row changes); needs --apply
#   --connect-network     create the internal network nfhotel-db if missing and join the
#                         running container to it with the alias postgres (no restart);
#                         needs --apply
#
# Passwords are made with openssl, kept in the secrets folder (root, 0600) and passed to
# psql on standard input only; they are never printed or put on a command line.
#
# Usage: prepare-database-roles.sh --container <name> [--secrets-dir <dir>] [--apply]
#                                  [--transfer-ownership] [--connect-network]
#   --container    the running PostgreSQL container (docker ps)
#   --secrets-dir  where the passwords and settings files go (default /etc/nfhotel/secrets)
# Env:   NFHOTEL_BACKUP_COMMAND  the backup run before --transfer-ownership
#        (default /usr/local/sbin/nfhotel-backup, called with --apply)
# Exit:  0 done (or reported); 1 a step failed; 2 usage error
# Needs: bash 4+, docker, openssl

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"
readonly NETWORK=nfhotel-db
readonly ALIAS=postgres
readonly LOGIN_ROLES=(app migrator backup)

container=""
secrets_dir=/etc/nfhotel/secrets
apply=0
transfer=0
connect=0
backup_command="${NFHOTEL_BACKUP_COMMAND:-/usr/local/sbin/nfhotel-backup}"

while (($#)); do
  case "$1" in
    --container)
      container="${2:?--container needs a value}"
      shift
      ;;
    --secrets-dir)
      secrets_dir="${2:?--secrets-dir needs a value}"
      shift
      ;;
    --apply) apply=1 ;;
    --transfer-ownership) transfer=1 ;;
    --connect-network) connect=1 ;;
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

[[ "$container" =~ ^[A-Za-z0-9][A-Za-z0-9_.-]*$ ]] || {
  usage >&2
  exit 2
}
((apply || ! (transfer || connect))) || die "--transfer-ownership and --connect-network need --apply"

# in_db: run psql as the superuser of the image in the application database, reading
# SQL from standard input.
in_db() {
  docker exec -i "$container" sh -c 'psql -X -q -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -f -'
}

# query <sql>: one value, as the superuser.
query() {
  docker exec -i "$container" sh -c 'psql -X -At --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -f -' <<<"$1"
}

echo "==> checking the container $container"
[[ "$(docker inspect --format '{{.State.Running}}' "$container" 2>/dev/null)" == "true" ]] ||
  die "$container is not a running container"
version="$(query 'SHOW server_version_num')" || die "cannot run psql in $container"
((version >= 160000)) || die "PostgreSQL 16 or later is required (found $version)"
db="$(docker exec "$container" printenv POSTGRES_DB)" || die "POSTGRES_DB is not set in $container"
[[ "$db" =~ ^[A-Za-z0-9_]+$ ]] || die "unexpected database name '$db'"
echo "    PostgreSQL $version, database $db"
echo "    current owners in public: $(query "SELECT string_agg(DISTINCT tableowner, ', ') FROM pg_tables WHERE schemaname = 'public'")"
echo "    roles present: $(query "SELECT coalesce(string_agg(rolname, ', ' ORDER BY rolname), 'none') FROM pg_roles WHERE rolname LIKE 'nfhotel\_%'")"

if ((! apply)); then
  echo "==> dry run: give --apply to create the roles nfhotel_owner, nfhotel_migrator, nfhotel_app, nfhotel_backup"
  echo "    and the settings files in $secrets_dir; add --transfer-ownership and --connect-network for those steps"
  exit 0
fi

echo "==> secrets in $secrets_dir"
install -d -m 0700 "$secrets_dir"
for role in "${LOGIN_ROLES[@]}"; do
  file="$secrets_dir/postgres-$role.password"
  if [[ ! -s "$file" ]]; then
    (umask 077 && openssl rand -hex 32 >"$file") || die "cannot write $file"
    echo "    created $(basename "$file")"
  fi
  [[ "$(cat "$file")" =~ ^[0-9a-f]{64}$ ]] || die "$file is not a 64-character hex password"
done

echo "==> roles and privileges (scripts/db/roles.sql)"
{
  echo "\\set dbname '$db'"
  for role in "${LOGIN_ROLES[@]}"; do
    echo "\\set ${role}_password '$(cat "$secrets_dir/postgres-$role.password")'"
  done
  cat "$ROOT/scripts/db/roles.sql"
} | in_db || die "roles.sql failed"

echo "==> settings files for the API and the migration step"
(
  umask 077
  echo "ConnectionStrings__HotelDatabase=Host=$ALIAS;Port=5432;Database=$db;Username=nfhotel_app;Password=$(cat "$secrets_dir/postgres-app.password")" \
    >"$secrets_dir/nfhotel-api-database.env"
  echo "NFHOTEL_MIGRATOR_URL=postgresql://nfhotel_migrator:$(cat "$secrets_dir/postgres-migrator.password")@$ALIAS:5432/$db?options=-c%20role%3Dnfhotel_owner" \
    >"$secrets_dir/nfhotel-migrator-database.env"
)
echo "    $secrets_dir/nfhotel-api-database.env, $secrets_dir/nfhotel-migrator-database.env (0600)"

if ((transfer)); then
  echo "==> backup before the ownership transfer"
  [[ -x "$backup_command" ]] || die "$backup_command is missing; install the backup first (scripts/install-backup.sh)"
  "$backup_command" --apply || die "the backup failed; the ownership was not changed"
  echo "==> ownership of the existing objects (scripts/db/transfer-ownership.sql)"
  before="$(query "SELECT count(*) FROM \"__EFMigrationsHistory\"" 2>/dev/null || echo none)"
  in_db <"$ROOT/scripts/db/transfer-ownership.sql" || die "transfer-ownership.sql failed and was rolled back"
  after="$(query "SELECT count(*) FROM \"__EFMigrationsHistory\"" 2>/dev/null || echo none)"
  [[ "$before" == "$after" ]] || die "the migration history changed ($before -> $after)"
  echo "    migration history rows unchanged ($after)"
fi

if ((connect)); then
  echo "==> internal network $NETWORK"
  if ! docker network inspect "$NETWORK" >/dev/null 2>&1; then
    docker network create --internal "$NETWORK" >/dev/null || die "cannot create $NETWORK"
    echo "    created (internal, no route out)"
  fi
  [[ "$(docker network inspect --format '{{.Internal}}' "$NETWORK")" == "true" ]] ||
    die "$NETWORK exists but is not internal; refusing to join the database to it"
  if docker inspect --format '{{range $name, $_ := .NetworkSettings.Networks}}{{println $name}}{{end}}' "$container" | grep -qx "$NETWORK"; then
    echo "    $container is already on $NETWORK"
  else
    docker network connect --alias "$ALIAS" "$NETWORK" "$container" || die "cannot join $container to $NETWORK"
    echo "    joined $container as $ALIAS (no restart)"
  fi
fi
echo "==> done; check with: bash scripts/verify-database.sh --container $container"
