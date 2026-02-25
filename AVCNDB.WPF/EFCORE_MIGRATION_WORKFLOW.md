# EF Core + SQL Migration Workflow

Ce document explique le workflow recommande pour gerer l'evolution de la base `MEDICDB` dans ce projet, sans casser la structure existante.

## 1) Etat actuel du projet

- L'application utilise EF Core pour le mapping modele <-> tables (`[Table(...)]`, `DbSet<>`, repository).
- Connexion configuree dans `appsettings.json` (MySQL/MariaDB Docker sur `127.0.0.1:3307`).
- Le schema existe deja en base (table `__efmigrationshistory` presente).
- Les migrations ne sont pas pilotees au runtime via `db.Database.Migrate()`.

Conclusion: EF Core est utilise pour les operations CRUD et le mapping, et les evolutions de schema sont gerees par scripts SQL versionnes.

## 2) Strategie retenue (bonne pratique pour ce projet)

On garde une approche **SQL versionnee**:

- Scripts dans `WPF_APP/database/migrations/`
- Runner: `WPF_APP/scripts/apply-sql-migrations.ps1`
- Historique d'execution: table `schema_sql_migrations`
- Backup automatique avant application (sauf option `-NoBackup`)

Cette approche est stable pour une base deja en production et evite les surprises d'un basculement brutal.

## 3) Arborescence utile

- `WPF_APP/database/migrations/V001__baseline_existing_schema.sql`
- `WPF_APP/database/migrations/README.md`
- `WPF_APP/scripts/apply-sql-migrations.ps1`
- `WPF_APP/database/backups/`
- `WPF_APP/docs/docker-db.md`

## 4) Workflow standard pour une nouvelle evolution DB

### Etape A - Modifier le code

1. Mettre a jour le(s) modele(s) `Models/*.cs` si necessaire.
2. Mettre a jour `DAL/AppDbContext.cs` (DbSet/index/defaults) si necessaire.
3. Mettre a jour ViewModel/View si la logique metier change.

### Etape B - Creer une migration SQL

Depuis `WPF_APP/database/migrations/`, ajouter un fichier:

- `V002__add_xxx.sql`
- `V003__update_yyy.sql`

Convention:

- Prefixe `VNNN__`
- Nom explicite
- Script idempotent si possible
- Ne pas modifier un script deja applique

### Etape C - Verifier avant application

Depuis `WPF_APP/`:

```powershell
.\scripts\apply-sql-migrations.ps1 -DryRun
```

### Etape D - Appliquer

```powershell
.\scripts\apply-sql-migrations.ps1
```

Options utiles:

```powershell
# Sans backup auto
.\scripts\apply-sql-migrations.ps1 -NoBackup

# Cible custom
.\scripts\apply-sql-migrations.ps1 -ContainerName avcndb-db -Database MEDICDB -User medwin -Password 0101
```

### Etape E - Valider

1. Build:

```powershell
dotnet build .\AVCNDB.WPF\AVCNDB.WPF.csproj
```

2. Verifier que la migration est tracee:

```sql
SELECT version, name, applied_at
FROM schema_sql_migrations
ORDER BY version;
```

3. Tester les ecrans CRUD concernes dans l'application.

## 5) Rollback / securite

Le runner cree un backup SQL dans `database/backups/` avant application.

Si rollback necessaire:

1. Stopper l'app.
2. Restaurer le backup SQL.
3. Rejouer proprement avec un script corrige.

Important: en pratique, preferer une migration corrective (`V00X__fix...sql`) plutot que modifier un fichier deja applique.

## 6) Regles d'equipe

- Ne jamais editer une migration deja appliquee.
- Une migration = un objectif clair.
- Ajouter les checks d'integrite SQL quand pertinent.
- Toujours lancer `-DryRun` avant application.
- Garder DB, modeles C#, et UI synchronises dans le meme changement.

## 7) EF Core migrations (`dotnet ef`) dans ce projet

Tu peux les utiliser si tu veux plus tard, mais ce n'est pas le pipeline principal actuel.

Pipeline principal actuel:

- **Schema change**: scripts SQL versionnes
- **Data access**: EF Core (Repository + DbContext)

Cette separation est volontaire pour garder la base stable et facilement controlable en Docker.

