-- Move the ownership of the existing application objects to nfhotel_owner (ADR-0011,
-- MIL-024 task 2). Run by scripts/prepare-database-roles.sh --transfer-ownership as the
-- superuser, after a verified backup and after roles.sql. ALTER ... OWNER TO changes
-- catalog entries only: no row, and no row of __EFMigrationsHistory, is read or written.
-- One transaction: either every object moves, or none. Idempotent.
--
-- Not moved: extensions (btree_gist stays with the superuser that created it) and
-- objects outside the schema public.

\set ON_ERROR_STOP on
BEGIN;

ALTER SCHEMA public OWNER TO nfhotel_owner;

-- Tables, with the sequences they own (identity and serial columns move with them).
SELECT format('ALTER TABLE %I.%I OWNER TO nfhotel_owner', schemaname, tablename)
FROM pg_tables
WHERE schemaname = 'public' AND tableowner <> 'nfhotel_owner'
ORDER BY tablename \gexec

-- Sequences that no table owns.
SELECT format('ALTER SEQUENCE %I.%I OWNER TO nfhotel_owner', s.schemaname, s.sequencename)
FROM pg_sequences s
JOIN pg_class c ON c.relname = s.sequencename
JOIN pg_namespace n ON n.oid = c.relnamespace AND n.nspname = s.schemaname
WHERE s.schemaname = 'public' AND s.sequenceowner <> 'nfhotel_owner'
  AND NOT EXISTS (SELECT FROM pg_depend d WHERE d.objid = c.oid AND d.deptype IN ('a', 'i'))
ORDER BY s.sequencename \gexec

-- Views and materialized views.
SELECT format('ALTER VIEW %I.%I OWNER TO nfhotel_owner', schemaname, viewname)
FROM pg_views
WHERE schemaname = 'public' AND viewowner <> 'nfhotel_owner' \gexec
SELECT format('ALTER MATERIALIZED VIEW %I.%I OWNER TO nfhotel_owner', schemaname, matviewname)
FROM pg_matviews
WHERE schemaname = 'public' AND matviewowner <> 'nfhotel_owner' \gexec

COMMIT;
