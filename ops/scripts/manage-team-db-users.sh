#!/usr/bin/env bash
# Manage the personal database roles of the team (MIL-048, ADR-0034): dev_<name>, a member of
# nfhotel_team, which reads and writes rows and changes nothing else. S01 runs it on the VM as
# root in the checkout. Without --apply it only reports what it would do.
#
# Each password is made with openssl, kept in <secrets-dir>/team/dev_<name>.password (root,
# 0600) and passed to psql on standard input only; it is never printed or put on a command
# line. S01 reads the file and gives the password to the person in private.
#
# Usage: manage-team-db-users.sh [--container <name>] [--secrets-dir <dir>] [--apply]
#                                (--add <name> | --reset <name> | --remove <name> | --list)
#   --add          create dev_<name> with a new password; refused when it exists
#   --reset        give dev_<name> a new password and end its sessions
#   --remove       end the sessions of dev_<name>, drop it and delete its password file
#   --list         show the roles of the team (needs no --apply)
#   <name>         2 to 20 small letters and digits, starting with a letter (jakob, william2)
#   --container    the running PostgreSQL container (default nfhotel-postgres-postgres-1)
#   --secrets-dir  where the team/ folder of passwords goes (default /etc/nfhotel/secrets)
# Exit:  0 done (or reported); 1 a step failed; 2 usage error
# Needs: bash 4+, docker, openssl

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=ops/scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"

container=nfhotel-postgres-postgres-1
secrets_dir=/etc/nfhotel/secrets
apply=0
action=""
name=""

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
    --add | --reset | --remove)
      [[ -z "$action" ]] || die "give one of --add, --reset, --remove and --list"
      action="${1#--}"
      name="${2:-}"
      shift
      ;;
    --list)
      [[ -z "$action" ]] || die "give one of --add, --reset, --remove and --list"
      action=list
      ;;
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

[[ -n "$action" && "$container" =~ ^[A-Za-z0-9][A-Za-z0-9_.-]*$ ]] || {
  usage >&2
  exit 2
}
if [[ "$action" != list && ! "$name" =~ ^[a-z][a-z0-9]{1,19}$ ]]; then
  echo "error: the name must be 2 to 20 small letters and digits, starting with a letter" >&2
  exit 2
fi
readonly role="dev_$name" file="$secrets_dir/team/dev_$name.password"

# in_db: run psql as the superuser of the image in the application database, reading SQL
# from standard input.
in_db() {
  docker exec -i "$container" sh -c 'psql -X -q -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -f -'
}

# query <sql>: the rows, unaligned, as the superuser.
query() {
  docker exec -i "$container" sh -c 'psql -X -At -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -f -' <<<"$1"
}

[[ "$(docker inspect --format '{{.State.Running}}' "$container" 2>/dev/null)" == "true" ]] ||
  die "$container is not a running container"
db="$(docker exec "$container" printenv POSTGRES_DB)" || die "POSTGRES_DB is not set in $container"
[[ "$db" =~ ^[A-Za-z0-9_]+$ ]] || die "unexpected database name '$db'"

if [[ "$action" == list ]]; then
  query "SELECT r.rolname || CASE WHEN pg_has_role(r.oid, 'nfhotel_team', 'MEMBER') THEN '' ELSE ' (not in nfhotel_team)' END
         FROM pg_roles r WHERE r.rolname LIKE 'dev\_%' ORDER BY 1" || die "cannot list the roles"
  exit 0
fi

exists="$(query "SELECT count(*) FROM pg_roles WHERE rolname = '$role'")" || die "cannot run psql in $container"
case "$action" in
  add) [[ "$exists" == 0 ]] || die "$role exists; use --reset to give it a new password" ;;
  reset | remove) [[ "$exists" == 1 ]] || die "$role does not exist" ;;
esac

if ((! apply)); then
  echo "dry run: would $action $role in $db (password file $file); give --apply to do it"
  exit 0
fi

# new_password: write a new password to the file of the role, readable by root only.
new_password() {
  install -d -m 0700 "$secrets_dir/team"
  (umask 077 && openssl rand -hex 24 >"$file") || die "cannot write $file"
}

# The passwords are kept out of the server log: this session logs no statement, and the
# psql variables arrive on standard input.
case "$action" in
  add)
    new_password
    {
      echo "\\set dbname '$db'"
      cat "$ROOT/scripts/db/team-roles.sql"
      echo "\\set role '$role'"
      echo "\\set password '$(cat "$file")'"
      cat <<'SQL'
SET log_statement = 'none';
SET password_encryption = 'scram-sha-256';
CREATE ROLE :"role" LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS
  CONNECTION LIMIT 3 PASSWORD :'password' IN ROLE nfhotel_team;
-- Every change by the person is logged with the role's name (ADR-0034); an open transaction
-- in DBeaver cannot hold up the API for long.
ALTER ROLE :"role" SET log_statement = 'mod';
ALTER ROLE :"role" SET idle_in_transaction_session_timeout = '10min';
ALTER ROLE :"role" SET lock_timeout = '5s';
SQL
    } | in_db || {
      rm -f "$file"
      die "creating $role failed; nothing was kept"
    }
    echo "created $role; give the password to the person in private: sudo cat $file"
    ;;
  reset)
    new_password
    {
      echo "\\set role '$role'"
      echo "\\set password '$(cat "$file")'"
      cat <<'SQL'
SET log_statement = 'none';
SET password_encryption = 'scram-sha-256';
ALTER ROLE :"role" PASSWORD :'password';
SELECT count(pg_terminate_backend(pid)) AS ended FROM pg_stat_activity WHERE usename = :'role' \gset
SQL
    } | in_db || die "the new password of $role failed"
    echo "new password for $role, its sessions ended; give it in private: sudo cat $file"
    ;;
  remove)
    {
      echo "\\set role '$role'"
      cat <<'SQL'
SELECT count(pg_terminate_backend(pid)) AS ended FROM pg_stat_activity WHERE usename = :'role' \gset
DROP ROLE :"role";
SQL
    } | in_db || die "dropping $role failed"
    rm -f "$file"
    echo "removed $role and its password file"
    ;;
esac
