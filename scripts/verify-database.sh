#!/usr/bin/env bash
# Read-only checks of the NF Hotel database preparation (ADR-0011, MIL-024 task 3). S03
# runs it on the VM before and after each step of prepare-database-roles.sh; it changes
# nothing in the database, the containers or the networks.
#
# Checks (each prints PASS or FAIL):
#   - the four roles exist with the expected attributes (no superuser, login as planned)
#   - nfhotel_app may read and write every table, and may not create, truncate or own
#   - PUBLIC may not create in the database or the schema public
#   - with --expect-transfer: every table in public is owned by nfhotel_owner
#   - the migration history exists; its rows and the row count of every table are listed
#   - with --expect-network: the container is on the internal network nfhotel-db as postgres
#
# Usage: verify-database.sh --container <name> [--expect-transfer] [--expect-network]
# Exit:  0 every check passed; 1 a check failed; 2 usage error
# Needs: bash 4+, docker

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"
readonly NETWORK=nfhotel-db
readonly ALIAS=postgres

container=""
expect_transfer=0
expect_network=0
failures=0

while (($#)); do
  case "$1" in
    --container)
      container="${2:?--container needs a value}"
      shift
      ;;
    --expect-transfer) expect_transfer=1 ;;
    --expect-network) expect_network=1 ;;
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

# query <sql>: the result as the superuser of the image, in a read-only transaction.
query() {
  docker exec -i "$container" sh -c \
    'PGOPTIONS="-c default_transaction_read_only=on" psql -X -At -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -f -' <<<"$1"
}

# check <description> <expected> <sql>
check() {
  local got
  got="$(query "$3" 2>&1)" || got="error: $got"
  if [[ "$got" == "$2" ]]; then
    echo "PASS  $1"
  else
    echo "FAIL  $1 (expected '$2', got '$got')"
    failures=$((failures + 1))
  fi
}

check "role nfhotel_owner: NOLOGIN, not superuser" "f|f" \
  "SELECT rolcanlogin, rolsuper FROM pg_roles WHERE rolname = 'nfhotel_owner'"
for role in nfhotel_migrator nfhotel_app nfhotel_backup; do
  check "role $role: LOGIN, not superuser, cannot create roles or databases" "t|f|f|f" \
    "SELECT rolcanlogin, rolsuper, rolcreaterole, rolcreatedb FROM pg_roles WHERE rolname = '$role'"
done
check "nfhotel_migrator may SET ROLE nfhotel_owner without inheriting it" "t|f" \
  "SELECT m.set_option, m.inherit_option FROM pg_auth_members m JOIN pg_roles r ON r.oid = m.roleid JOIN pg_roles u ON u.oid = m.member WHERE r.rolname = 'nfhotel_owner' AND u.rolname = 'nfhotel_migrator'"
check "nfhotel_backup is a member of pg_read_all_data" "t" \
  "SELECT pg_has_role('nfhotel_backup', 'pg_read_all_data', 'USAGE')"
check "nfhotel_app may read and write every table in public" "0" \
  "SELECT count(*) FROM pg_tables WHERE schemaname = 'public' AND NOT (has_table_privilege('nfhotel_app', format('%I.%I', schemaname, tablename), 'SELECT,INSERT,UPDATE,DELETE'))"
check "nfhotel_app may not truncate any table" "0" \
  "SELECT count(*) FROM pg_tables WHERE schemaname = 'public' AND has_table_privilege('nfhotel_app', format('%I.%I', schemaname, tablename), 'TRUNCATE')"
check "nfhotel_app may not create in the schema public or the database" "f|f|f" \
  "SELECT has_schema_privilege('nfhotel_app', 'public', 'CREATE'), has_database_privilege('nfhotel_app', current_database(), 'CREATE'), has_database_privilege('nfhotel_app', current_database(), 'TEMPORARY')"
check "PUBLIC may not create in the schema public" "f" \
  "SELECT coalesce(bool_or(a.privilege_type = 'CREATE'), false) FROM pg_namespace n, aclexplode(coalesce(n.nspacl, acldefault('n', n.nspowner))) a WHERE n.nspname = 'public' AND a.grantee = 0"
check "nfhotel_app owns nothing" "0" \
  "SELECT count(*) FROM pg_class c JOIN pg_roles r ON r.oid = c.relowner WHERE r.rolname = 'nfhotel_app'"
if ((expect_transfer)); then
  check "every table in public is owned by nfhotel_owner" "0" \
    "SELECT count(*) FROM pg_tables WHERE schemaname = 'public' AND tableowner <> 'nfhotel_owner'"
fi
check "the migration history table exists" "t" \
  "SELECT to_regclass('public.\"__EFMigrationsHistory\"') IS NOT NULL"

echo "INFO  migrations: $(query "SELECT string_agg(migration_id, ', ' ORDER BY migration_id) FROM \"__EFMigrationsHistory\"" 2>/dev/null || echo none)"
echo "INFO  rows: $(query "SELECT string_agg(format('%s=%s', relname, n_live_tup), ', ' ORDER BY relname) FROM pg_stat_user_tables" 2>/dev/null || echo none) (estimates)"

if ((expect_network)); then
  aliases="$(docker inspect --format "{{with index .NetworkSettings.Networks \"$NETWORK\"}}{{join .Aliases \" \"}}{{end}}" "$container" 2>/dev/null || true)"
  if [[ " $aliases " == *" $ALIAS "* ]] && [[ "$(docker network inspect --format '{{.Internal}}' "$NETWORK" 2>/dev/null)" == "true" ]]; then
    echo "PASS  $container is on the internal network $NETWORK as $ALIAS"
  else
    echo "FAIL  $container is not on the internal network $NETWORK as $ALIAS (aliases: '${aliases:-none}')"
    failures=$((failures + 1))
  fi
fi

((failures == 0)) || die "$failures check(s) failed"
echo "==> all checks passed"
