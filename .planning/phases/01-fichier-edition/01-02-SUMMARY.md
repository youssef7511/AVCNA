# Plan 01-02 Summary: Services + Tests

## Status: COMPLETE

## What was built

### Services
- **`AVCNDB.WPF/Services/FuzzyDetectionService.cs`** — Implements `IUnknownDataDetectionService` using FuzzySharp 2.0.2 `Fuzz.TokenSortRatio`. Threshold >= 80 = known, < 80 = unknown (Decision D-02). Checks fields against library tables: Dci, Labos, Families, Formes, Voies, Specialites.

- **`AVCNDB.WPF/Services/EditionFileService.cs`** — Implements `IEditionFileService`:
  - `ImportExcelAsync(filePath, sourceType)`: Uses ClosedXML directly for flexible column mapping (not the generic `IExcelService.ImportAsync<T>`). Maps Excel columns to EditionRow fields.
  - `ValidateAgainstLibraryAsync(rows)`: Runs FuzzyDetectionService on all rows, returns count of unknowns. Sets `ActionFlag.AjouterNew` on rows with unknown fields.
  - `ApproveRowAsync(row)`: Adds unknown values to library tables, inserts Medic.
  - `RejectRowAsync(row)`: Sets `ActionFlag.Desaffecte`.
  - `ExportEditionFileAsync(rows, filePath)`: Exports to Excel via ClosedXML.
  - `SaveSessionAsync(session)`: Persists EditionFileSession to database.

### DI Registration
- Added in `App.xaml.cs`:
  - `IUnknownDataDetectionService` -> `FuzzyDetectionService` (transient)
  - `IEditionFileService` -> `EditionFileService` (transient)

### Tests (10 total, all pass)
- **`AVCNDB.WPF.Tests/Services/FuzzyDetectionServiceTests.cs`** — 6 tests:
  - Exact match returns empty unknown list
  - Close match (>= 80) returns empty
  - Unknown DCI returns "Dci" in unknown fields
  - Unknown Labo returns "Labo"
  - Multiple unknown fields detected
  - Empty field is not flagged as unknown

- **`AVCNDB.WPF.Tests/Services/EditionFileServiceTests.cs`** — 4 tests:
  - Import valid Excel returns rows
  - Validate against library returns unknown count
  - Approve row adds to library
  - Reject row sets Desaffecte flag

## Deviations from plan
- `IEditionFileService.ImportExcelAsync` takes 2 parameters (filePath + sourceType), returns `ImportResult` with `Success`, `Rows`, `ErrorMessage`.
- `ValidateAgainstLibraryAsync` returns `int` (count), not `List<EditionRow>`.
- Used `IDbContextFactory<AppDbContext>` for thread safety per project conventions.
