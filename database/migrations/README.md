SQL migration files for MEDICDB.

Naming convention:
- `V001__baseline_existing_schema.sql`
- `V002__add_some_change.sql`

Rules:
- Keep files immutable once applied.
- Use idempotent SQL when possible.
- Use one migration per logical change.
- Keep statements compatible with MariaDB 10.11.

Apply migrations:
- From `WPF_APP/` run:
  - `.\scripts\apply-sql-migrations.ps1`

The runner stores execution history in table:
- `schema_sql_migrations`

