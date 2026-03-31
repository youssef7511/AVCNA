# Plan 01-01 Summary: Models + Contracts + SQL Migration

## Status: COMPLETE

## What was built

### Models
- **`AVCNDB.WPF/Models/EditionRow.cs`** — In-memory row model implementing `INotifyPropertyChanged`. Contains:
  - All Medic-mirrored fields: `ItemName`, `ShortName`, `Dci1`-`Dci4`, `DciAssociation`, `Forme`, `Voie`, `Tableau`, `Veic`, `Labo`, `Fam1`-`Fam3`, `Specialite`, `RefPrice`/`Price` (int), `IsAp`/`IsRemboursable` (int)
  - Edition fields: `ActionFlag` enum, `RowStatus` enum, `IsSelected`, `UnknownFields` list, `HasUnknownFields`, `ActionLabel`, `HasPriceChanged`
  - `ActionFlag` enum: None, AjouterNew, MarquerSuppression, Nouveau, Reinitialiser, Affecte, Desaffecte
  - `RowStatus` enum: Active, Inactive, New, Modified

- **`AVCNDB.WPF/Models/EditionFileSession.cs`** — EF Core entity for persisting import sessions. Uses lowercase property names (`filepath`, `sourcetype`, `totalrows`, `unknownrows`, `addedat`, `updatedat`) per `ITrackable` interface conventions.

### Contracts
- **`AVCNDB.WPF/Contracts/Services/IUnknownDataDetectionService.cs`** — ML detection contract with `DetectUnknownFieldsAsync(EditionRow, ...)` returning field names of unknowns.
- **`AVCNDB.WPF/Contracts/Services/IEditionFileService.cs`** — Orchestration contract with `ImportExcelAsync`, `ValidateAgainstLibraryAsync` (returns int), `ApproveRowAsync`, `RejectRowAsync`, `ExportEditionFileAsync`, `SaveSessionAsync`.

### Database
- **`database/migrations/V002__add_edition_file_session.sql`** — Creates `editionfilesessions` table with indexes.
- **`AVCNDB.WPF/DAL/AppDbContext.cs`** — Added `DbSet<EditionFileSession>` and EF model configuration.

## Deviations from plan
- Property names use actual Medic conventions (`ItemName` not `Denomination`, `int` not `decimal` for prices, `int` not `bool` for flags) — discovered during implementation and documented.
- `EditionSourceType` enum added to support multiple import source formats.
