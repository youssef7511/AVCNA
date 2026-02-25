# Excel Import / Export — Architecture & Lifecycle

Documentation complète du système d'import/export Excel de l'application **AVICENNA DB**.

---

## Table des matières

1. [Vue d'ensemble](#vue-densemble)
2. [Architecture en couches](#architecture-en-couches)
3. [Interfaces](#interfaces)
4. [Couche 1 — ExcelService (ClosedXML)](#couche-1--excelservice-closedxml)
5. [Couche 2 — StrictExcelSyncService (Upsert)](#couche-2--strictexcelsyncservice-upsert)
6. [Helpers](#helpers)
7. [Modèles & Attributs](#modèles--attributs)
8. [Injection de dépendances](#injection-de-dépendances)
9. [Lifecycle — Export](#lifecycle--export)
10. [Lifecycle — Import Simple (Tools)](#lifecycle--import-simple-tools)
11. [Lifecycle — Import Strict (Bibliothèque)](#lifecycle--import-strict-bibliothèque)
12. [Lifecycle — Template Download](#lifecycle--template-download)
13. [Lifecycle — Selective Export (Checkbox)](#lifecycle--selective-export-checkbox)
14. [Diagrammes de séquence](#diagrammes-de-séquence)
15. [Méthodes — Référence complète](#méthodes--référence-complète)
16. [Formats de fichier](#formats-de-fichier)

---

## Vue d'ensemble

Le système gère deux flux principaux :

| Flux | Où | Service utilisé | Mode |
|------|----|-----------------|------|
| **Export simple** | Page Outils, listes DB | `IExcelService.ExportAsync` | Toutes les données ou sélection checkbox |
| **Import simple** | Page Outils | `IExcelService.ImportAsync` + `IRepository.AddAsync` | Ajout brut, pas de validation stricte |
| **Import strict** | Onglets Bibliothèque/DB (DCI, Familles, Labos, Médicaments) | `IStrictExcelSyncService.ImportAndSyncAsync` | Validation colonnes + upsert par `recordid` |
| **Template** | Onglets Bibliothèque/DB | `IStrictExcelSyncService.CreateTemplateAsync` | Fichier vide avec en-têtes corrects |

Package NuGet : **ClosedXML 0.102.3**

---

## Architecture en couches

```
┌─────────────────────────────────────────────────────────────────┐
│                        ViewModels                               │
│  ToolsViewModel  │  DciListVM  │  FamiliesListVM  │  LabosListVM │  MedicListVM
│  (import/export  │  (export,   │  (export,        │  (export,    │  (export,
│   global)        │   import    │   import strict,  │   import    │   import
│                  │   strict,   │   template)       │   strict,   │   strict,
│                  │   template) │                    │   template) │   template)
└──────┬───────────┴──────┬──────┴────────┬──────────┴──────┬─────┘
       │                  │               │                 │
       ▼                  ▼               ▼                 ▼
┌─────────────────┐  ┌──────────────────────────────┐
│  IExcelService  │  │  IStrictExcelSyncService<T>  │
│  (ExcelService) │◄─│  (StrictExcelSyncService<T>) │
└────────┬────────┘  └──────┬───────────────────────┘
         │                  │
         ▼                  ▼
    ClosedXML          IRepository<T>
    (XLWorkbook)       + ExcelTemplateHelper
                       + EntityCopyHelper
```

---

## Interfaces

### `IExcelService`

```csharp
public interface IExcelService
{
    // Import depuis un fichier .xlsx → liste d'objets T
    Task<IEnumerable<T>> ImportAsync<T>(string filePath, string? sheetName = null)
        where T : class, new();

    // Export vers un fichier .xlsx sur disque
    Task ExportAsync<T>(IEnumerable<T> data, string filePath, string sheetName = "Data")
        where T : class;

    // Export vers un byte[] en mémoire (pour téléchargement)
    Task<byte[]> ExportToBytesAsync<T>(IEnumerable<T> data, string sheetName = "Data")
        where T : class;

    // Validation des colonnes d'un fichier avant import
    Task<ExcelValidationResult> ValidateFileAsync(string filePath, IEnumerable<string> expectedColumns);
}
```

### `IStrictExcelSyncService<T>`

```csharp
public interface IStrictExcelSyncService<T> where T : class, ITrackable, new()
{
    // Liste des colonnes attendues (auto-générée par réflexion)
    IReadOnlyList<string> ExpectedColumns { get; }

    // Validation stricte : colonnes attendues, pas de doublons, pas de colonnes inconnues
    Task<ExcelStrictImportResult> ValidateStrictAsync(string filePath);

    // Crée un fichier Excel vide avec les bons en-têtes
    Task CreateTemplateAsync(string filePath, string sheetName);

    // Valide + importe + upsert (insert ou update par recordid)
    Task<ExcelStrictImportResult> ImportAndSyncAsync(string filePath, string sheetName);
}
```

### `IDialogService` (méthodes fichier)

```csharp
public interface IDialogService
{
    // Ouvre un dialogue "Ouvrir un fichier" — retourne le chemin ou null
    string? ShowOpenFileDialog(string filter, string title = "Ouvrir un fichier");

    // Ouvre un dialogue "Enregistrer sous" — retourne le chemin ou null
    string? ShowSaveFileDialog(string filter, string defaultFileName = "", string title = "Enregistrer sous");
}
```

---

## Couche 1 — ExcelService (ClosedXML)

Classe : `Services/ExcelService.cs`
Dépendance : ClosedXML

### `ImportAsync<T>(filePath, sheetName?)`

1. Ouvre le fichier avec `new XLWorkbook(filePath)`
2. Sélectionne la feuille par nom ou prend la première
3. Lit la ligne 1 comme en-têtes → mappe chaque colonne à un `PropertyInfo` de `T` (insensible à la casse)
4. Pour chaque ligne de données (à partir de la ligne 2) :
   - Crée une instance `new T()`
   - Pour chaque colonne mappée, convertit la cellule via `ConvertCellValue()` et appelle `property.SetValue()`
   - Si la cellule est vide et la propriété est un value type non-nullable → skip (garde la valeur par défaut)
5. Retourne `List<T>`

### `ExportAsync<T>(data, filePath, sheetName)`

1. Crée un `XLWorkbook` + une feuille avec le nom donné
2. Récupère les propriétés de `T` qui sont **lisibles** ET **pas `[NotMapped]`**
3. Écrit les en-têtes (ligne 1) avec style : fond bleu `#1976D2`, texte blanc, gras
4. Écrit les données (ligne 2+) via `property.GetValue(item)?.ToString()`
5. Auto-ajuste la largeur des colonnes
6. Applique des bordures fines sur toute la plage utilisée
7. Sauvegarde avec `workbook.SaveAs(filePath)`

### `ExportToBytesAsync<T>(data, sheetName)`

Identique à `ExportAsync` mais sauvegarde dans un `MemoryStream` et retourne `byte[]`.

### `ValidateFileAsync(filePath, expectedColumns)`

1. Ouvre le fichier, lit les en-têtes de la première feuille
2. Compare les colonnes trouvées avec les colonnes attendues (insensible à la casse)
3. Retourne un `ExcelValidationResult` avec :
   - `IsValid` : vrai si toutes les colonnes attendues sont présentes
   - `MissingColumns` : liste des colonnes absentes
   - `FoundColumns` : liste des colonnes trouvées
   - `RowCount` : nombre de lignes de données
   - `Errors` : messages d'erreur

### `ConvertCellValue(cell, targetType)` (privée)

Convertit une cellule ClosedXML vers le type .NET cible :

| Type cible | Méthode ClosedXML |
|-----------|-------------------|
| `string` | `cell.GetString()` (ou `string.Empty` si vide) |
| `int` | `cell.GetValue<int>()` |
| `decimal` | `cell.GetValue<decimal>()` |
| `double` | `cell.GetValue<double>()` |
| `DateTime` | `cell.GetDateTime()` |
| `bool` | `cell.GetBoolean()` |
| Autre | `Convert.ChangeType(cell.Value, type)` |
| Nullable | Dé-wrappe via `Nullable.GetUnderlyingType()` |

---

## Couche 2 — StrictExcelSyncService (Upsert)

Classe : `Services/StrictExcelSyncService<T>.cs`
Contrainte : `T : class, ITrackable, new()`
Dépendances : `IRepository<T>`, `IExcelService`, `ExcelTemplateHelper`, `EntityCopyHelper`

### Constructeur

```csharp
public StrictExcelSyncService(IRepository<T> repository, IExcelService excelService)
{
    _repository = repository;
    _excelService = excelService;
    ExpectedColumns = ExcelTemplateHelper.GetStrictColumns<T>();
}
```

Les colonnes attendues sont calculées au démarrage via réflexion sur `T`.

### `ValidateStrictAsync(filePath)`

1. Appelle `_excelService.ValidateFileAsync(filePath, ExpectedColumns)`
2. Vérifie les en-têtes dupliqués via `ExcelTemplateHelper.GetDuplicateColumns()`
3. Vérifie les colonnes non reconnues via `ExcelTemplateHelper.GetUnexpectedColumns()`
4. Retourne `ExcelStrictImportResult`

### `CreateTemplateAsync(filePath, sheetName)`

Appelle `_excelService.ExportAsync(Array.Empty<T>(), filePath, sheetName)` — crée un fichier avec uniquement les en-têtes (0 lignes de données).

### `ImportAndSyncAsync(filePath, sheetName)` — **méthode principale**

C'est le cœur du système d'import strict. Étapes :

```
1. ValidateStrictAsync(filePath)
   └─ Si invalide → retourne les erreurs immédiatement

2. _excelService.ImportAsync<T>(filePath, sheetName)
   └─ Désérialise toutes les lignes en objets T

3. Détection des recordid dupliqués dans le fichier
   └─ Si doublons → erreur, annulation

4. Récupération des entités existantes :
   └─ _repository.FindAsync(x => idsToUpdate.Contains(x.recordid))
   └─ Indexation par recordid dans un Dictionary<int, T>

5. Pour chaque ligne importée :
   ├─ Si itemname vide → skip (SkippedCount++)
   ├─ Si recordid > 0 ET existe en DB :
   │   ├─ EntityCopyHelper.CopyWritableProperties(source, dest, "recordid")
   │   ├─ Met à jour updatedat = DateTime.Now
   │   └─ _repository.UpdateAsync(current)  →  UpdatedCount++
   └─ Sinon (recordid == 0 ou non trouvé) :
       ├─ recordid = 0  (force auto-increment)
       ├─ addedat = DateTime.Now
       └─ Ajouté à la liste toInsert  →  InsertedCount++

6. _repository.AddRangeAsync(toInsert)
   └─ Insertion en batch de tous les nouveaux enregistrements

7. Retourne ExcelStrictImportResult
```

---

## Helpers

### `ExcelTemplateHelper` — `Helpers/ExcelTemplateHelper.cs`

| Méthode | Description |
|---------|-------------|
| `GetStrictColumns<T>()` | Retourne les noms de toutes les propriétés publiques inscriptibles de `T` (réflexion). Utilisé pour définir les colonnes attendues d'un template. |
| `GetUnexpectedColumns(found, expected)` | Retourne les colonnes du fichier qui ne correspondent à aucune colonne attendue (insensible à la casse). |
| `GetDuplicateColumns(found)` | Retourne les en-têtes qui apparaissent plus d'une fois dans le fichier. |

### `EntityCopyHelper` — `Helpers/EntityCopyHelper.cs`

| Méthode | Description |
|---------|-------------|
| `CopyWritableProperties<T>(source, dest, excludedNames)` | Copie toutes les propriétés publiques read/write de `source` vers `dest`, en excluant les propriétés nommées dans `excludedNames` (ex: `"recordid"`). Utilisé pendant l'upsert pour mettre à jour une entité existante. |

---

## Modèles & Attributs

### Interface `ITrackable`

```csharp
public interface ITrackable
{
    int recordid { get; set; }      // Clé primaire auto-incrémentée
    DateTime? addedat { get; set; } // Date d'ajout
    DateTime? updatedat { get; set; } // Date de dernière mise à jour
}
```

Tous les modèles importables/exportables implémentent `ITrackable`.

### Attribut `[NotMapped]`

La propriété `IsChecked` est décorée avec `[NotMapped]` pour :
1. **EF Core** : ne pas la persister en base
2. **ExcelService** : l'exclure automatiquement de l'export

```csharp
[NotMapped]
public bool IsChecked { get; set; }
```

Le filtrage dans `ExcelService` :

```csharp
var properties = typeof(T).GetProperties()
    .Where(p => p.CanRead && !Attribute.IsDefined(p, typeof(NotMappedAttribute)))
    .ToList();
```

### Modèles supportés

| Modèle | Table MySQL | Propriétés exportées | Particularités |
|--------|-------------|---------------------|----------------|
| `Medic` | `medic` | ~60 (barcode, itemname, dci1-4, fam1-3, labo, forme, voie, prix…) | Entité principale |
| `Dci` | `dci` | recordid, itemname, subvalue, iteminfo, addedat, updatedat | Substance active |
| `Families` | `family` | recordid, itemname, subvalue, addedat, updatedat | Famille thérapeutique |
| `Labos` | `labos` | recordid, itemname, subvalue, addedat, updatedat | Laboratoire |
| `Formes` | `formes` | recordid, itemname, subvalue, addedat, updatedat | Forme galénique |
| `Voies` | `voies` | recordid, itemname, subvalue, addedat, updatedat | Voie d'administration |

---

## Injection de dépendances

Enregistrement dans `App.xaml.cs` :

```csharp
// Service Excel générique
services.AddSingleton<IExcelService, ExcelService>();

// Service strict (open generic) — résolu automatiquement pour chaque T
services.AddTransient(typeof(IStrictExcelSyncService<>), typeof(StrictExcelSyncService<>));
```

Injection dans les ViewModels :

```csharp
public DciListViewModel(
    IRepository<Dci> repository,
    IDialogService dialogService,
    IExcelService excelService,                        // Couche 1
    IStrictExcelSyncService<Dci> strictExcelSyncService, // Couche 2
    MedicSyncService syncService)
```

---

## Lifecycle — Export

### Depuis un onglet (DCI, Familles, Labos, Médicaments)

```
Utilisateur clique "Exporter"
       │
       ▼
ViewModel.ExportAsync()
       │
       ├─ DialogService.ShowSaveFileDialog("Excel Files|*.xlsx", nom par défaut)
       │   └─ Retourne filePath ou null (annulé)
       │
       ├─ Vérifie les items cochés : collection.Where(x => x.IsChecked)
       │   ├─ Si > 0 cochés → exporte uniquement ceux-ci
       │   └─ Si 0 cochés → repository.GetAllAsync() → exporte tout
       │
       ├─ ExcelService.ExportAsync(dataToExport, filePath, sheetName)
       │   ├─ Réflexion sur T → propriétés (exclut [NotMapped])
       │   ├─ Crée XLWorkbook → feuille → en-têtes (bleu/blanc)
       │   ├─ Écrit les données ligne par ligne
       │   ├─ AdjustToContents() + bordures
       │   └─ SaveAs(filePath)
       │
       └─ DialogService.ShowSuccessAsync("Export réussi", info)
```

### Depuis la page Outils (export global)

```
Utilisateur clique "Exporter" (carte Outils)
       │
       ▼
ToolsViewModel.ExportExcelAsync()
       │
       ├─ DialogService.ShowSaveFileDialog(...)
       ├─ medicRepository.GetAllAsync()  →  tous les médicaments
       ├─ ExcelService.ExportAsync(medics, filePath, "Médicaments")
       └─ DialogService.ShowSuccessAsync(...)
```

---

## Lifecycle — Import Simple (Tools)

```
Utilisateur clique "Importer" (page Outils)
       │
       ▼
ToolsViewModel.ImportExcelAsync()
       │
       ├─ DialogService.ShowOpenFileDialog("Excel Files|*.xlsx;*.xls")
       │   └─ Retourne filePath ou null
       │
       ├─ DialogService.ShowConfirmAsync("Confirmer l'import", message)
       │   └─ Si non confirmé → annulé
       │
       ├─ ExcelService.ImportAsync<Medic>(filePath, "Médicaments")
       │   ├─ Ouvre XLWorkbook(filePath)
       │   ├─ Mappe en-têtes → PropertyInfo (insensible à la casse)
       │   ├─ Pour chaque ligne : new Medic() + SetValue par propriété
       │   └─ Retourne IEnumerable<Medic>
       │
       ├─ Pour chaque item importé :
       │   └─ medicRepository.AddAsync(item)
       │
       └─ DialogService.ShowSuccessAsync("Import terminé", count)
```

> **Note :** L'import simple n'effectue PAS de validation stricte des colonnes ni de détection de doublons. Il ajoute tous les enregistrements comme nouveaux.

---

## Lifecycle — Import Strict (Bibliothèque)

```
Utilisateur clique "Importer depuis Excel" (onglet DCI/Familles/Labos/Médicaments)
       │
       ▼
ViewModel.ImportFromExcelAsync()
       │
       ├─ DialogService.ShowOpenFileDialog("Excel Files|*.xlsx;*.xls")
       │   └─ Retourne filePath ou null
       │
       ▼
StrictExcelSyncService<T>.ImportAndSyncAsync(filePath, sheetName)
       │
       ├─── ÉTAPE 1 : Validation stricte ─────────────────────────
       │    ValidateStrictAsync(filePath)
       │    │
       │    ├─ ExcelService.ValidateFileAsync(filePath, ExpectedColumns)
       │    │   └─ Vérifie que toutes les colonnes attendues sont présentes
       │    │
       │    ├─ ExcelTemplateHelper.GetDuplicateColumns(foundColumns)
       │    │   └─ Erreur si en-têtes dupliqués
       │    │
       │    └─ ExcelTemplateHelper.GetUnexpectedColumns(found, expected)
       │        └─ Erreur si colonnes non reconnues
       │
       │    Si invalide → retourne ExcelStrictImportResult avec erreurs
       │
       ├─── ÉTAPE 2 : Désérialisation ────────────────────────────
       │    ExcelService.ImportAsync<T>(filePath, sheetName)
       │    └─ Retourne List<T> (toutes les lignes)
       │
       ├─── ÉTAPE 3 : Détection doublons dans le fichier ────────
       │    Groupe par recordid → erreur si doublons
       │
       ├─── ÉTAPE 4 : Chargement des existants ──────────────────
       │    repository.FindAsync(x => idsToUpdate.Contains(x.recordid))
       │    └─ Dictionary<int, T> existingById
       │
       ├─── ÉTAPE 5 : Boucle upsert ─────────────────────────────
       │    Pour chaque ligne importée :
       │    │
       │    ├─ itemname vide ?
       │    │   └─ OUI → skip (SkippedCount++)
       │    │
       │    ├─ recordid > 0 ET existe en DB ?
       │    │   └─ OUI → UPDATE
       │    │       ├─ EntityCopyHelper.CopyWritableProperties(row, current, "recordid")
       │    │       ├─ current.updatedat = DateTime.Now
       │    │       └─ repository.UpdateAsync(current)
       │    │       → UpdatedCount++
       │    │
       │    └─ NON → INSERT
       │        ├─ row.recordid = 0  (force auto-increment)
       │        ├─ row.addedat = DateTime.Now
       │        └─ Ajouté à toInsert[]
       │        → InsertedCount++
       │
       ├─── ÉTAPE 6 : Insertion batch ────────────────────────────
       │    repository.AddRangeAsync(toInsert)
       │
       └─── ÉTAPE 7 : Résultat ──────────────────────────────────
            Retourne ExcelStrictImportResult :
            - InsertedCount, UpdatedCount, SkippedCount
            - RowCount, Errors, Warnings
       │
       ▼
ViewModel :
       ├─ Si !result.IsValid → DialogService.ShowErrorAsync(erreurs)
       ├─ Sinon → LoadDataAsync() (recharge la liste)
       └─ DialogService.ShowSuccessAsync(résumé)
```

---

## Lifecycle — Template Download

```
Utilisateur clique "Télécharger le modèle Excel"
       │
       ▼
ViewModel.DownloadExcelTemplateAsync()
       │
       ├─ DialogService.ShowSaveFileDialog("Excel Files|*.xlsx", nom_template)
       │
       ▼
StrictExcelSyncService<T>.CreateTemplateAsync(filePath, sheetName)
       │
       └─ ExcelService.ExportAsync(Array.Empty<T>(), filePath, sheetName)
           ├─ Réflexion → propriétés de T (exclut [NotMapped])
           ├─ Écrit UNIQUEMENT les en-têtes (0 lignes de données)
           ├─ Style : fond bleu, texte blanc, gras
           └─ SaveAs(filePath)
       │
       ▼
DialogService.ShowSuccessAsync("Modèle généré", chemin)
```

Le fichier template résultant contient une seule ligne (les en-têtes) avec les colonnes exactes attendues par l'import strict.

---

## Lifecycle — Selective Export (Checkbox)

### Vue (XAML)

Chaque DataGrid possède une colonne de sélection :

```xml
<DataGridTemplateColumn Width="45" CanUserResize="False" CanUserSort="False">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <CheckBox IsChecked="{Binding IsChecked, UpdateSourceTrigger=PropertyChanged}"
                      HorizontalAlignment="Center" VerticalAlignment="Center"/>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

### ViewModel (logique)

```csharp
// 1. Vérifier les items cochés dans la collection affichée
var checkedItems = Dcis.Where(d => d.IsChecked).ToList();

// 2. Décider quoi exporter
IEnumerable<Dci> dataToExport;
if (checkedItems.Count > 0)
{
    dataToExport = checkedItems;           // Sélection uniquement
    exportInfo = $"{checkedItems.Count} élément(s) sélectionné(s)";
}
else
{
    dataToExport = await _repository.GetAllAsync();  // Tout
    exportInfo = $"Tous les éléments ({dataToExport.Count()})";
}

// 3. Exporter (IsChecked est [NotMapped] → pas inclus dans le fichier)
await _excelService.ExportAsync(dataToExport, filePath, "DCI");
```

---

## Diagrammes de séquence

### Export sélectif

```
User          ViewModel           DialogService       ExcelService        Repository
 │                │                    │                    │                  │
 │─ Click Export ─►│                   │                    │                  │
 │                │─ ShowSaveFileDialog ►│                  │                  │
 │                │◄─── filePath ───────│                   │                  │
 │                │                    │                    │                  │
 │                │─ Filter IsChecked ──►(ObservableCollection)               │
 │                │   count > 0 ?       │                    │                  │
 │                │   YES → use checked │                    │                  │
 │                │   NO ──────────────────────────────────────► GetAllAsync() │
 │                │◄────────────────────────────────────────────── data ───────│
 │                │                    │                    │                  │
 │                │─── ExportAsync(data, path, sheet) ──────►│                │
 │                │                    │                    │── Reflect props  │
 │                │                    │                    │── Write headers  │
 │                │                    │                    │── Write rows     │
 │                │                    │                    │── SaveAs()       │
 │                │◄─── done ──────────────────────────────│                  │
 │                │                    │                    │                  │
 │                │─ ShowSuccessAsync ──►│                  │                  │
 │◄── Dialog ─────│◄───────────────────│                   │                  │
```

### Import strict

```
User          ViewModel         StrictExcelSync      ExcelService      Repository
 │                │                    │                   │                │
 │─ Click Import ─►│                   │                   │                │
 │                │─ ShowOpenFileDialog ►                   │                │
 │                │◄─── filePath ───────                    │                │
 │                │                    │                   │                │
 │                │── ImportAndSyncAsync(filePath, sheet) ─►│               │
 │                │                    │                   │                │
 │                │                    │─ ValidateFileAsync ►│              │
 │                │                    │◄── validation ─────│               │
 │                │                    │                   │                │
 │                │                    │─ Check duplicates  │               │
 │                │                    │─ Check unexpected  │               │
 │                │                    │                   │                │
 │                │                    │─ ImportAsync<T> ───►│              │
 │                │                    │◄── List<T> ────────│               │
 │                │                    │                   │                │
 │                │                    │─ FindAsync(ids) ───────────────────►│
 │                │                    │◄── existing[] ─────────────────────│
 │                │                    │                   │                │
 │                │                    │─ Loop: upsert each row            │
 │                │                    │  ├─ UPDATE → CopyProps + UpdateAsync ►│
 │                │                    │  └─ INSERT → collect toInsert[]    │
 │                │                    │                   │                │
 │                │                    │─ AddRangeAsync(toInsert) ──────────►│
 │                │                    │◄── done ──────────────────────────│
 │                │                    │                   │                │
 │                │◄── result ─────────│                   │                │
 │                │                    │                   │                │
 │                │─ LoadDataAsync() ──────────────────────────────────────►│
 │                │─ ShowSuccessAsync ─►                   │                │
 │◄── Dialog ─────│                    │                   │                │
```

---

## Méthodes — Référence complète

### ExcelService

| Méthode | Signature | Description |
|---------|-----------|-------------|
| `ImportAsync<T>` | `(string filePath, string? sheetName) → Task<IEnumerable<T>>` | Désérialise un fichier Excel en objets .NET par réflexion |
| `ExportAsync<T>` | `(IEnumerable<T> data, string filePath, string sheetName) → Task` | Sérialise des objets vers un fichier .xlsx stylisé |
| `ExportToBytesAsync<T>` | `(IEnumerable<T> data, string sheetName) → Task<byte[]>` | Idem mais retourne un tableau d'octets |
| `ValidateFileAsync` | `(string filePath, IEnumerable<string> expectedColumns) → Task<ExcelValidationResult>` | Valide les colonnes d'un fichier Excel |
| `ConvertCellValue` | `(IXLCell cell, Type targetType) → object?` | Convertit une cellule vers un type .NET (privée) |

### StrictExcelSyncService\<T\>

| Méthode | Signature | Description |
|---------|-----------|-------------|
| `ValidateStrictAsync` | `(string filePath) → Task<ExcelStrictImportResult>` | Validation complète : colonnes attendues + doublons + inconnues |
| `CreateTemplateAsync` | `(string filePath, string sheetName) → Task` | Crée un fichier Excel template (en-têtes uniquement) |
| `ImportAndSyncAsync` | `(string filePath, string sheetName) → Task<ExcelStrictImportResult>` | Valide → importe → upsert (insert/update par recordid) |

### ExcelTemplateHelper

| Méthode | Signature | Description |
|---------|-----------|-------------|
| `GetStrictColumns<T>` | `() → IReadOnlyList<string>` | Noms des propriétés publiques inscriptibles de T |
| `GetUnexpectedColumns` | `(IEnumerable<string>, IEnumerable<string>) → List<string>` | Colonnes du fichier non reconnues |
| `GetDuplicateColumns` | `(IEnumerable<string>) → List<string>` | En-têtes dupliqués |

### EntityCopyHelper

| Méthode | Signature | Description |
|---------|-----------|-------------|
| `CopyWritableProperties<T>` | `(T source, T dest, params string[] excluded) → void` | Copie toutes les propriétés read/write sauf les exclues |

### Méthodes ViewModel (par table)

| ViewModel | Export | Import Strict | Template |
|-----------|--------|---------------|----------|
| `DciListViewModel` | `ExportAsync()` | `ImportFromExcelAsync()` | `DownloadExcelTemplateAsync()` |
| `FamiliesListViewModel` | `ExportAsync()` | `ImportFromExcelAsync()` | `DownloadExcelTemplateAsync()` |
| `LabosListViewModel` | `ExportAsync()` | `ImportFromExcelAsync()` | `DownloadExcelTemplateAsync()` |
| `MedicListViewModel` | `ExportToExcelAsync()` | `ImportFromExcelAsync()` | `DownloadExcelTemplateAsync()` |
| `ToolsViewModel` | `ExportExcelAsync()` | `ImportExcelAsync()` | — |

---

## Formats de fichier

### Fichier exporté (.xlsx)

```
┌────────────┬────────────┬────────────┬─────────┐
│ recordid   │ itemname   │ subvalue   │ ...     │  ← En-têtes (bleu #1976D2, blanc, gras)
├────────────┼────────────┼────────────┼─────────┤
│ 1          │ Paracétamol│ Antalgique │ ...     │  ← Données
│ 2          │ Ibuprofène │ AINS       │ ...     │
│ ...        │ ...        │ ...        │ ...     │
└────────────┴────────────┴────────────┴─────────┘
  Bordures fines · Colonnes auto-ajustées · Pas de IsChecked
```

### Fichier template (.xlsx)

```
┌────────────┬────────────┬────────────┬─────────┐
│ recordid   │ itemname   │ subvalue   │ ...     │  ← En-têtes uniquement
└────────────┴────────────┴────────────┴─────────┘
  0 ligne de données — prêt à remplir
```

### Règles d'import

| Règle | Import Simple (Tools) | Import Strict (Bibliothèque) |
|-------|-----------------------|------------------------------|
| Validation colonnes | Non | Oui (stricte) |
| Colonnes inconnues | Ignorées | Erreur |
| En-têtes dupliqués | Ignorés | Erreur |
| recordid dupliqués | Ajoutés comme nouveaux | Erreur |
| recordid existant en DB | Ajouté comme nouveau | Mis à jour (upsert) |
| recordid absent / 0 | Ajouté | Ajouté (auto-increment) |
| itemname vide | Ajouté | Ignoré (skip) |
| Propriétés non trouvées | Ignorées (valeur par défaut) | Ignorées (valeur par défaut) |

---

*Dernière mise à jour : Février 2026*
