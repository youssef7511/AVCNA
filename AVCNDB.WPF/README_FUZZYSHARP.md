# FuzzySharp — Détection de Données Inconnues

## Vue d'ensemble

Le projet utilise **FuzzySharp 2.0.2** (algorithme **Token Sort Ratio**) pour deux fonctionnalités distinctes :

1. **Détection de données inconnues** — Compare chaque champ importé (DCI, Labo, Forme, etc.) contre les tables de bibliothèque en base de données
2. **Correspondance floue des en-têtes Excel** — Reconnaît et corrige automatiquement les noms de colonnes mal orthographiés

---

## Architecture du Pipeline

```
┌──────────────┐     ┌──────────────────┐     ┌─────────────────────┐
│  Fichier     │     │ EditionFile      │     │ FuzzyDetection      │
│  Excel       │────▶│ Service          │────▶│ Service             │
│  (.xlsx)     │     │                  │     │ (TokenSortRatio)    │
└──────────────┘     └──────────────────┘     └─────────────────────┘
                            │                          │
                            │                          ▼
                            │                 ┌─────────────────────┐
                            │                 │ Tables Bibliothèque │
                            │                 │ (DCI, Labos, Formes │
                            │                 │  Voies, Familles,   │
                            │                 │  Spécialités)       │
                            │                 └─────────────────────┘
                            ▼
                     ┌──────────────────┐
                     │ EditionFile      │
                     │ ViewModel        │
                     │ (UI DataGrid)    │
                     └──────────────────┘
```

---

## 1. Détection de Données Inconnues (seuil = 80)

### Principe

Chaque valeur importée est comparée à toutes les valeurs de référence de la table correspondante. Le **meilleur score** détermine si la valeur est connue ou inconnue.

| Score | Statut | Exemple |
|-------|--------|---------|
| `≥ 80` | **Connu** ✅ | "PARACETAMOL" vs "Paracetamol" → score 100 |
| `< 80` | **Inconnu** ❌ | "Xylozantrin" vs aucune correspondance → score < 80 |
| Vide/null | **Connu** ✅ | Champs optionnels non remplis = pas de faux positif |

### Champs vérifiés (9 au total)

| Champ | Table de référence |
|-------|-------------------|
| `Dci1` (DCI principal) | `dcis` |
| `DciAssociation` | `dcis` |
| `Labo` | `labos` |
| `Fam1`, `Fam2`, `Fam3` | `families` |
| `Forme` | `formes` |
| `Voie` | `voies` |
| `Specialite` | `specialites` |

### Code — `FuzzyDetectionService.CheckValue()`

C'est la méthode centrale qui compare une valeur contre les références :

```csharp
public FieldDetectionResult CheckValue(string fieldName, string value, IReadOnlyList<string> knownValues)
{
    var result = new FieldDetectionResult
    {
        FieldName = fieldName,
        ImportedValue = value
    };

    // Valeur vide = toujours connue (pas de faux positif sur champs optionnels)
    if (string.IsNullOrWhiteSpace(value))
    {
        result.IsKnown = true;
        result.Score = 100;
        return result;
    }

    // Pas de référence = inconnu
    if (knownValues.Count == 0)
    {
        result.IsKnown = false;
        result.Score = 0;
        return result;
    }

    var upperValue = value.ToUpperInvariant();
    int bestScore = 0;
    string? bestMatch = null;

    foreach (var known in knownValues)
    {
        if (string.IsNullOrWhiteSpace(known)) continue;

        // FuzzySharp Token Sort Ratio : insensible à l'ordre des mots
        var score = Fuzz.TokenSortRatio(upperValue, known.ToUpperInvariant());
        if (score > bestScore)
        {
            bestScore = score;
            bestMatch = known;
        }

        // Short-circuit sur match exact
        if (score == 100) break;
    }

    result.Score = bestScore;
    result.BestMatch = bestMatch;
    result.IsKnown = bestScore >= KnownThreshold; // KnownThreshold = 80

    return result;
}
```

### Code — `BuildReport()` (vérification de tous les champs d'une ligne)

```csharp
private DetectionReport BuildReport(int rowIndex, EditionRow row, LibrarySnapshot library)
{
    var report = new DetectionReport { RowIndex = rowIndex };

    report.FieldResults.Add(CheckValue("Dci", row.Dci1, library.Dcis));
    report.FieldResults.Add(CheckValue("DciAssociation", row.DciAssociation, library.Dcis));
    report.FieldResults.Add(CheckValue("Labo", row.Labo, library.Labos));
    report.FieldResults.Add(CheckValue("Fam1", row.Fam1, library.Families));
    report.FieldResults.Add(CheckValue("Fam2", row.Fam2, library.Families));
    report.FieldResults.Add(CheckValue("Fam3", row.Fam3, library.Families));
    report.FieldResults.Add(CheckValue("Forme", row.Forme, library.Formes));
    report.FieldResults.Add(CheckValue("Voie", row.Voie, library.Voies));
    report.FieldResults.Add(CheckValue("Specialite", row.Specialite, library.Specialites));

    return report;
}
```

### Code — `ValidateAgainstLibraryAsync()` (orchestre la détection sur toutes les lignes)

```csharp
public async Task<int> ValidateAgainstLibraryAsync(List<EditionRow> rows)
{
    var reports = await _detectionService.DetectBatchAsync(rows);
    int unknownCount = 0;

    for (int i = 0; i < rows.Count; i++)
    {
        var row = rows[i];
        var report = reports[i];

        row.UnknownFields.Clear();
        row.UnknownFields.AddRange(report.UnknownFieldNames);
        row.NotifyUnknownFieldsChanged();

        if (!report.AllFieldsKnown)
        {
            if (row.ActionFlag == ActionFlag.None)
                row.ActionFlag = ActionFlag.AjouterNew;
            unknownCount++;
        }
    }

    return unknownCount;
}
```

### Exemples de scores réels (Token Sort Ratio)

| Valeur importée | Référence | Score | Résultat |
|----------------|-----------|-------|----------|
| `"Paracetamol"` | `"Paracetamol"` | 100 | ✅ Connu |
| `"PARACETAMOL"` | `"Paracetamol"` | 100 | ✅ Connu (insensible casse) |
| `"Parazetamol"` | `"Paracetamol"` | 82 | ✅ Connu (typo tolérée) |
| `"Pfiser"` | `"Pfizer"` | 83 | ✅ Connu (typo tolérée) |
| `"Gellule"` | `"Gélule"` | 77 | ❌ Inconnu (trop éloigné) |
| `"Xylozantrin"` | aucune | < 80 | ❌ Inconnu |
| `"BioNTech"` | aucune | < 80 | ❌ Inconnu |

---

## 2. Correspondance Floue des En-têtes Excel (seuil = 75)

### Principe

Lors de l'import, les noms de colonnes Excel sont comparés aux alias connus. Si un en-tête ne correspond pas exactement, FuzzySharp tente de le corriger automatiquement.

| Score | Action |
|-------|--------|
| **Exact match** | Mapping direct, pas d'avertissement |
| `≥ 75` | Auto-corrigé + avertissement "FuzzyCorrected" |
| `< 75` | Colonne ignorée + avertissement "Unrecognized" |

### Code — `BuildColumnMapFuzzy()`

```csharp
private static Dictionary<int, string> BuildColumnMapFuzzy(
    IXLRow headerRow,
    Dictionary<string, string> fieldAliases,
    List<HeaderWarning> warnings)
{
    const int FuzzyHeaderThreshold = 75;
    var map = new Dictionary<int, string>();
    var lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
    var aliasKeys = fieldAliases.Keys.ToList();

    for (int col = 1; col <= lastCol; col++)
    {
        var header = headerRow.Cell(col).GetString().Trim();
        if (string.IsNullOrWhiteSpace(header)) continue;

        // 1. Exact match (case-insensitive)
        if (fieldAliases.TryGetValue(header, out var fieldName))
        {
            map[col] = fieldName;
            continue;
        }

        // 2. Fuzzy match contre tous les alias connus
        int bestScore = 0;
        string? bestAlias = null;

        foreach (var alias in aliasKeys)
        {
            int score = Fuzz.TokenSortRatio(header, alias);
            if (score > bestScore)
            {
                bestScore = score;
                bestAlias = alias;
            }
        }

        if (bestScore >= FuzzyHeaderThreshold && bestAlias != null)
        {
            // Auto-correction de typo
            var mappedField = fieldAliases[bestAlias];
            map[col] = mappedField;
            warnings.Add(new HeaderWarning
            {
                Type = HeaderWarning.WarningType.FuzzyCorrected,
                OriginalHeader = header,
                CorrectedTo = bestAlias,
                MappedField = mappedField,
                FuzzyScore = bestScore
            });
        }
        else
        {
            // Colonne non reconnue — ignorée
            warnings.Add(new HeaderWarning
            {
                Type = HeaderWarning.WarningType.Unrecognized,
                OriginalHeader = header,
                FuzzyScore = bestScore
            });
        }
    }

    return map;
}
```

### Alias par type de source Excel

| Source | Colonnes reconnues |
|--------|--------------------|
| **ExcelSimple** | Tous les alias possibles (catch-all) |
| **ExcelCNAM** | Code PCT, Dénomination, DCI, Association, Forme, Tab, VEIC, Labo, Prix-Réf, Prix, Remb, A.P. |
| **ExcelPCT** | Code PCT, Dénomination, DCI, Forme, Voie, Labo, Prix |
| **ExcelPCTComplete** | Tous les champs PCT + Désignation, Spécialité, Famille, VEIC |
| **CatalogueLabo** | Code/Réf, Produit/Molécule, Forme galénique, Dosage, Fabricant, PU |
| **ListePharmacie** | Code/Code barre, Désignation, DCI, Forme, Fournisseur, PPA |

---

## 3. Détection Automatique d'Orientation (Horizontal vs Vertical)

```csharp
private static bool DetectOrientation(IXLWorksheet ws, Dictionary<string, string> fieldAliases)
{
    // Compare combien d'en-têtes connus en Ligne 1 (horizontal) vs Colonne A (vertical)
    // Fuzzy threshold = 70 pour la détection d'orientation
    
    // Vertical seulement si : verticalMatches > horizontalMatches 
    //                       && verticalMatches >= 3 
    //                       && horizontalMatches <= 1
    return verticalMatches > horizontalMatches && verticalMatches >= 3 && horizontalMatches <= 1;
}
```

Si vertical détecté → la feuille est **transposée automatiquement** (lignes ↔ colonnes) avant le parsing.

---

## 4. Workflow Approbation → Bibliothèque + Base de données

Quand l'utilisateur **approuve une ligne** (\`ApproveRowAsync\`) :

```csharp
public async Task ApproveRowAsync(EditionRow row)
{
    // 1. Ajouter les valeurs inconnues aux tables de bibliothèque
    foreach (var fieldName in row.UnknownFields)
    {
        var value = GetFieldValue(row, fieldName);
        if (!string.IsNullOrWhiteSpace(value))
            await AddToLibraryAsync(fieldName, value);
    }

    // 2. Insérer ou mettre à jour le Medic en base
    using var context = await _contextFactory.CreateDbContextAsync();
    
    if (row.OriginalMedicRecordId.HasValue)
    {
        // Mise à jour d'un médicament existant
        var medic = await context.Medics.FindAsync(row.OriginalMedicRecordId.Value);
        if (medic != null) { UpdateMedicFromRow(medic, row); context.Medics.Update(medic); }
    }
    else
    {
        // Insertion d'un nouveau médicament
        var newMedic = MapEditionRowToMedic(row);
        context.Medics.Add(newMedic);
    }
    
    await context.SaveChangesAsync();

    // 3. Nettoyer la ligne
    row.UnknownFields.Clear();
    row.ActionFlag = ActionFlag.Affecte;
    row.RowStatus = RowStatus.Modified;
}
```

### `AddToLibraryAsync()` — Insertion dans les tables de référence

```csharp
private async Task AddToLibraryAsync(string fieldName, string value)
{
    using var context = await _contextFactory.CreateDbContextAsync();

    switch (fieldName)
    {
        case "Dci":
        case "DciAssociation":
            if (!await context.Dcis.AnyAsync(d => d.itemname == value))
            {
                context.Dcis.Add(new Dci { itemname = value });
                await context.SaveChangesAsync();
            }
            break;

        case "Labo":
            if (!await context.Labos.AnyAsync(l => l.itemname == value))
                // ... même pattern pour Labos, Formes, Voies, Specialites, Families
            break;
    }
}
```

| Champ inconnu | Table cible |
|---------------|-------------|
| `Dci`, `DciAssociation` | `dcis` |
| `Labo` | `labos` |
| `Fam1`, `Fam2`, `Fam3` | `families` |
| `Forme` | `formes` |
| `Voie` | `voies` |
| `Specialite` | `specialites` |

---

## Modèles de données

### `FieldDetectionResult` — Résultat pour un champ

```csharp
public class FieldDetectionResult
{
    public string FieldName { get; set; }       // "Dci", "Labo", etc.
    public string ImportedValue { get; set; }    // Valeur importée
    public bool IsKnown { get; set; }            // true si score >= 80
    public int Score { get; set; }               // 0-100
    public string? BestMatch { get; set; }       // Meilleure correspondance
}
```

### `DetectionReport` — Rapport complet d'une ligne

```csharp
public class DetectionReport
{
    public int RowIndex { get; set; }
    public List<FieldDetectionResult> FieldResults { get; set; } = new();
    public bool AllFieldsKnown => FieldResults.All(f => f.IsKnown);
    public List<string> UnknownFieldNames => FieldResults
        .Where(f => !f.IsKnown).Select(f => f.FieldName).ToList();
}
```

### `HeaderWarning` — Avertissement sur un en-tête Excel

```csharp
public class HeaderWarning
{
    public enum WarningType { FuzzyCorrected, Unrecognized }
    public WarningType Type { get; set; }
    public string OriginalHeader { get; set; }  // "Dénominaion" (typo)
    public string? CorrectedTo { get; set; }     // "Dénomination" (corrigé)
    public string? MappedField { get; set; }     // "ItemName"
    public int FuzzyScore { get; set; }          // 91
}
```

---

## Fichiers clés

| Fichier | Rôle |
|---------|------|
| `Services/FuzzyDetectionService.cs` | Comparaison floue des données vs bibliothèque (seuil 80) |
| `Services/EditionFileService.cs` | Import Excel, fuzzy headers (seuil 75), orientation, approbation |
| `Contracts/Services/IUnknownDataDetectionService.cs` | Interface + modèles FieldDetectionResult, DetectionReport |
| `Contracts/Services/IEditionFileService.cs` | Interface + modèles ImportResult, HeaderWarning, EditionSourceType |
| `Models/EditionRow.cs` | Modèle de ligne avec UnknownFields |
| `ViewModels/EditionFileViewModel.cs` | Orchestration UI (import → détection → affichage warnings) |

---

## Résumé des seuils

| Fonctionnalité | Seuil | Algorithme |
|---------------|-------|------------|
| Détection données inconnues | **≥ 80** = connu | `Fuzz.TokenSortRatio` |
| Correction en-têtes Excel | **≥ 75** = corrigé | `Fuzz.TokenSortRatio` |
| Détection orientation | **≥ 70** = header reconnu | `Fuzz.TokenSortRatio` |
