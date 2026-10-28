-- The group role of the team (MIL-048, ADR-0034). Run by scripts/manage-team-db-users.sh as
-- the superuser of the PostgreSQL image, connected to the application database, before each
-- change of a personal role. Idempotent: a second run changes nothing.
--
-- psql variables (set by the caller on standard input):
--   dbname   the application database
--
-- nfhotel_team  NOLOGIN; reads and writes rows, like nfhotel_app; never creates, truncates
--               or drops anything. Each person logs in with their own role dev_<name>,
--               a member of this one.

\set ON_ERROR_STOP on

SELECT 'CREATE ROLE nfhotel_team NOLOGIN'
WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'nfhotel_team') \gexec
ALTER ROLE nfhotel_team WITH NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;

GRANT CONNECT ON DATABASE :"dbname" TO nfhotel_team;
GRANT USAGE ON SCHEMA public TO nfhotel_team;

-- Existing objects, whoever owns them today.
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO nfhotel_team;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO nfhotel_team;

-- Objects that the migrations create later, as nfhotel_owner (ADR-0011).
ALTER DEFAULT PRIVILEGES FOR ROLE nfhotel_owner IN SCHEMA public
  GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO nfhotel_team;
ALTER DEFAULT PRIVILEGES FOR ROLE nfhotel_owner IN SCHEMA public
  GRANT USAGE, SELECT ON SEQUENCES TO nfhotel_team;

-- The migration history belongs to the migrations: the team may read it, not change it.
SELECT 'REVOKE INSERT, UPDATE, DELETE ON TABLE public."__EFMigrationsHistory" FROM nfhotel_team'
WHERE to_regclass('public."__EFMigrationsHistory"') IS NOT NULL \gexec

-- Every log line names the role and the database, so a change logged by log_statement = 'mod'
-- of a personal role says who made it. Kept in postgresql.auto.conf in the data volume.
ALTER SYSTEM SET log_line_prefix = '%m [%p] %q%u@%d ';
SELECT pg_reload_conf() \gset
