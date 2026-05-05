# Soft-Delete — AVCNDB

## Concept

Au lieu de supprimer physiquement une ligne (`DELETE`), on la marque comme supprimée
en renseignant la date de suppression (`deletedat`). La ligne reste en base mais devient
invisible pour l'application.

```
AVANT : DELETE FROM dci WHERE recordid = 5          → ligne perdue
APRÈS : UPDATE dci SET deletedat = NOW() WHERE recordid = 5  → ligne masquée, récupérable
```

---

## Tables concernées

| Table DB    | Entité C#    |
|-------------|-------------|
| `dci`       | `Dci`       |
| `family`    | `Families`  |
| `labos`     | `Labos`     |
| `formes`    | `Formes`    |
| `voie`      | `Voies`     |
| `specialites` | `Specialites` |
| `presents`  | `Presents`  |
| `medic`     | `Medic`     |

Toutes implémentent `ISoftDeletable` (`Models/ISoftDeletable.cs`).

---

## Comment ça fonctionne dans le code

### 1. Interface `ISoftDeletable`
```csharp
// Models/ISoftDeletable.cs
public interface ISoftDeletable
{
    DateTime? deletedat { get; set; }
}
```

### 2. Filtre global dans `AppDbContext`
```csharp
// DAL/AppDbContext.cs — OnModelCreating()
modelBuilder.Entity<Dci>().HasQueryFilter(e => e.deletedat == null);
modelBuilder.Entity<Families>().HasQueryFilter(e => e.deletedat == null);
// ... idem pour toutes les tables
```
EF Core ajoute automatiquement `WHERE deletedat IS NULL` à **toutes** les requêtes.
Aucun changement nécessaire dans les ViewModels ou Services existants.

### 3. `Repository.DeleteAsync` — suppression logique automatique
```csharp
// Services/Repository.cs
public virtual async Task DeleteAsync(T entity)
{
    if (entity is ISoftDeletable softDeletable)
    {
        softDeletable.deletedat = DateTime.Now;   // marquer supprimé
        _context.Entry(entity).State = EntityState.Modified;
    }
    else
    {
        _dbSet.Remove(entity);                    // hard-delete si non-ISoftDeletable
    }
    await _context.SaveChangesAsync();
}
```

---

## Migration SQL (V003)

Le script `database/migrations/V003__add_soft_delete_reference_tables.sql` a ajouté
la colonne `deletedat DATETIME NULL` sur chaque table concernée.

Exécution manuelle si nécessaire :
```sql
ALTER TABLE dci        ADD COLUMN deletedat DATETIME NULL DEFAULT NULL;
ALTER TABLE family     ADD COLUMN deletedat DATETIME NULL DEFAULT NULL;
ALTER TABLE labos      ADD COLUMN deletedat DATETIME NULL DEFAULT NULL;
ALTER TABLE formes     ADD COLUMN deletedat DATETIME NULL DEFAULT NULL;
ALTER TABLE voie       ADD COLUMN deletedat DATETIME NULL DEFAULT NULL;
ALTER TABLE specialites ADD COLUMN deletedat DATETIME NULL DEFAULT NULL;
ALTER TABLE presents   ADD COLUMN deletedat DATETIME NULL DEFAULT NULL;
```

Via Docker (conteneur `avcndb-db`) :
```powershell
docker exec avcndb-db mysql -umedwin -p0101 MEDICDB -e "SOURCE /path/V003__add_soft_delete_reference_tables.sql"
```

---

## Consulter les données supprimées (HeidiSQL)

Connexion : `127.0.0.1:3307` · user `medwin` · database `MEDICDB`

```sql
-- Toutes les lignes (y compris supprimées)
SELECT * FROM dci;

-- Uniquement les lignes supprimées
SELECT * FROM dci WHERE deletedat IS NOT NULL;

-- Restaurer une ligne supprimée
UPDATE dci SET deletedat = NULL WHERE recordid = 5;

-- Voir qui a été supprimé et quand, pour toutes les tables
SELECT 'dci' AS table_name, recordid, itemname, deletedat FROM dci        WHERE deletedat IS NOT NULL
UNION ALL
SELECT 'family',            recordid, itemname, deletedat FROM family     WHERE deletedat IS NOT NULL
UNION ALL
SELECT 'labos',             recordid, itemname, deletedat FROM labos      WHERE deletedat IS NOT NULL
UNION ALL
SELECT 'formes',            recordid, itemname, deletedat FROM formes     WHERE deletedat IS NOT NULL
UNION ALL
SELECT 'voie',              recordid, itemname, deletedat FROM voie       WHERE deletedat IS NOT NULL
UNION ALL
SELECT 'specialites',       recordid, itemname, deletedat FROM specialites WHERE deletedat IS NOT NULL
UNION ALL
SELECT 'presents',          recordid, itemname, deletedat FROM presents   WHERE deletedat IS NOT NULL;
```

---

## Ignorer le filtre depuis le code (cas avancé)

Si une requête doit inclure les lignes supprimées (ex. rapport d'audit) :
```csharp
var tousLesDcis = await context.Dcis
    .IgnoreQueryFilters()   // désactive le filtre deletedat
    .ToListAsync();
```
