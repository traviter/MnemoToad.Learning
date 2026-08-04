-- Manual, one-time bootstrap for the mnemotoad_learning database on the shared Azure Postgres
-- server. Run by hand while logged in as the server's admin/superuser (e.g. via psql) — this is
-- NOT a DbUp script and is never executed automatically (it lives outside Scripts/, which is the
-- only folder DbMigrator's WithScriptsFromFileSystem points at).
--
-- Replace 'changeit' with a real generated password for each role before running.

CREATE ROLE mnemotoad_learning_admin LOGIN PASSWORD 'changeit';
CREATE ROLE mnemotoad_learning_app LOGIN PASSWORD 'changeit';
CREATE DATABASE mnemotoad_learning OWNER mnemotoad_learning_admin;
GRANT CONNECT ON DATABASE mnemotoad_learning TO mnemotoad_learning_app;
