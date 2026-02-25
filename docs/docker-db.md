# Docker database setup (MariaDB)

This project can run its database with Docker instead of XAMPP.

## 1) Prepare environment

From `WPF_APP/`:

```powershell
Copy-Item .env.example .env
```

You can keep defaults or edit `.env` values.

## 2) Start database container

```powershell
docker compose up -d db
```

Check status:

```powershell
docker compose ps
docker compose logs -f db
```

The container is exposed on host port `3307` by default.

## 3) App connection

`AVCNDB.WPF/appsettings.json` is configured to use:

- `Server=127.0.0.1`
- `Port=3307`
- `Database=MEDICDB`
- `User Id=medwin`

If you change `.env` values, keep `appsettings.json` in sync.

## 4) Import existing SQL backup

Option A (automatic on first start only):

1. Put `.sql` files in `docker/mysql/init/`
2. Start container with empty volume

Option B (manual restore anytime):

```powershell
docker exec -i avcndb-db mariadb -umedwin -p0101 MEDICDB < .\backup.sql
```

## 5) Stop / reset

Stop container:

```powershell
docker compose down
```

Full reset (deletes DB data):

```powershell
docker compose down -v
```

## 6) Backup/Restore from app

`ToolsViewModel` now tries:

1. Local `mysqldump`/`mysql`
2. Docker fallback (`docker exec ... mariadb-dump/mariadb`)

So backup/restore works even if MySQL CLI is not installed locally.

## 7) SQL migrations (recommended practice)

This project now supports versioned SQL migrations (without changing your DB design approach).

Files location:

- `database/migrations/V001__baseline_existing_schema.sql`
- Next changes: `V002__...`, `V003__...`

Apply migrations:

```powershell
.\scripts\apply-sql-migrations.ps1
```

Useful options:

```powershell
# Preview only
.\scripts\apply-sql-migrations.ps1 -DryRun

# Apply without creating backup file
.\scripts\apply-sql-migrations.ps1 -NoBackup
```

Execution history is stored in DB table:

- `schema_sql_migrations`

By default, the script creates a backup before applying pending migrations:

- `database/backups/MEDICDB-YYYYMMDD-HHMMSS-before-sql-migrations.sql`
