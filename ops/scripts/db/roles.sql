-- Least-privilege roles of the NF Hotel database (ADR-0011, MIL-024 task 2).
-- Run by scripts/prepare-database-roles.sh as the superuser of the PostgreSQL image,
-- connected to the application database. Idempotent: a second run changes nothing.
--
-- psql variables (set by the caller on standard input, never on a command line):
--   app_password, migrator_password, backup_password   SCRAM passwords of the login roles
--   dbname                                              the application database
--
-- Roles:
--   nfhotel_owner     NOLOGIN; owns the schema objects of the application
--   nfhotel_migrator  LOGIN; may SET ROLE nfhotel_owner (no INHERIT); runs the migrations
--   nfhotel_app       LOGIN; reads and writes rows only (the API at run time)
--   nfhotel_backup    LOGIN; pg_read_all_data, for pg_dump only

\set ON_ERROR_STOP on
-- Keep the passwords out of the server log.
SET log_statement = 'none';
SET password_encryption = 'scram-sha-256';

SELECT 'CREATE ROLE nfhotel_owner NOLOGIN'
WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'nfhotel_owner') \gexec
SELECT 'CREATE ROLE nfhotel_migrator LOGIN'
WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'nfhotel_migrator') \gexec
SELECT 'CREATE ROLE nfhotel_app LOGIN'
WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'nfhotel_app') \gexec
SELECT 'CREATE ROLE nfhotel_backup LOGIN'
WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'nfhotel_backup') \gexec

ALTER ROLE nfhotel_owner WITH NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
ALTER ROLE nfhotel_migrator WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS
  CONNECTION LIMIT 2 PASSWORD :'migrator_password';
ALTER ROLE nfhotel_app WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS
  CONNECTION LIMIT 20 PASSWORD :'app_password';
ALTER ROLE nfhotel_backup WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS
  CONNECTION LIMIT 2 PASSWORD :'backup_password';

-- The migrator acts as the owner only after SET ROLE, so its own session cannot use the
-- owner's rights by accident; the objects it creates belong to nfhotel_owner.
GRANT nfhotel_owner TO nfhotel_migrator WITH INHERIT FALSE, SET TRUE;
GRANT pg_read_all_data TO nfhotel_backup;

-- Database: nobody but the owner may create schemas or temporary tables.
REVOKE CREATE, TEMPORARY ON DATABASE :"dbname" FROM PUBLIC;
GRANT CONNECT ON DATABASE :"dbname" TO nfhotel_migrator, nfhotel_app, nfhotel_backup;
-- CREATE on the database lets the owner create the trusted extension btree_gist.
GRANT CREATE ON DATABASE :"dbname" TO nfhotel_owner;

-- Other databases (postgres, template1): the new roles may not connect. Every existing
-- login role that is not ours keeps its access by an explicit grant first, so nothing
-- that connects today is cut off; then PUBLIC loses CONNECT there.
SELECT format('GRANT CONNECT ON DATABASE %I TO %I', d.datname, r.rolname)
FROM pg_database d CROSS JOIN pg_roles r
WHERE d.datallowconn AND d.datname <> current_database()
  AND r.rolcanlogin AND NOT r.rolsuper
  AND r.rolname NOT IN ('nfhotel_migrator', 'nfhotel_app', 'nfhotel_backup')
  AND has_database_privilege(r.oid, d.oid, 'CONNECT') \gexec
SELECT format('REVOKE CONNECT ON DATABASE %I FROM PUBLIC', datname)
FROM pg_database
WHERE datallowconn AND datname <> current_database() \gexec

-- Schema public: only the owner creates objects; the app may only use them.
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
GRANT USAGE, CREATE ON SCHEMA public TO nfhotel_owner;
GRANT USAGE ON SCHEMA public TO nfhotel_app, nfhotel_backup;

-- Existing objects, whoever owns them today.
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO nfhotel_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO nfhotel_app;

-- Objects that the migrations create later, as nfhotel_owner.
ALTER DEFAULT PRIVILEGES FOR ROLE nfhotel_owner IN SCHEMA public
  GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO nfhotel_app;
ALTER DEFAULT PRIVILEGES FOR ROLE nfhotel_owner IN SCHEMA public
  GRANT USAGE, SELECT ON SEQUENCES TO nfhotel_app;
