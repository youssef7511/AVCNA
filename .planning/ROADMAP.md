# AVICENNA DB — Roadmap

## Active Phase

### Phase 01: Fichier d'édition — Excel Import + ML Detection Workflow

**Goal:** Implement the "Fichier d'édition" page — a full edition-file workflow that imports an Excel file, displays it in a grid matching the `medic` table columns, uses ML (fuzzy string matching) to detect unknown/new data and highlights it in blue bold with an "A ajouter" action flag, lets the user validate or reject new data, provides a library manager dialog, row context menu actions, display filters, and similarity search.

**Requirements:** EDIT-01, EDIT-02, EDIT-03, EDIT-04, EDIT-05, EDIT-06

**Plans:** 4 plans

Plans:
- [ ] 01-01-PLAN.md — EditionRow model + EditionFileSession + SQL migration + service contracts (IEditionFileService, IUnknownDataDetectionService)
- [ ] 01-02-PLAN.md — ML detection engine: FuzzySharp-based IUnknownDataDetectionService implementation + unit tests
- [ ] 01-03-PLAN.md — EditionFileViewModel: import command, grid population, unknown-row flagging, library manager, row actions + DI wiring + ViewModel tests
- [ ] 01-04-PLAN.md — MovementsView.xaml full Fichier d'édition UI: toolbar, DataGrid with Action/blue-highlight columns, filter panel, library dialog, similarity popup + human verify

---

## Requirements Reference

| ID | Description |
|----|-------------|
| EDIT-01 | `EditionRow` model (in-memory/temp table): mirrors `Medic` columns + adds `ActionFlag` (None/AjouterNew/MarquerSuppression/Nouveau/Reinitialiser/Affecte/Desaffecte), `UnknownFields` list, `IsSelected` checkbox, `RowStatus` (Active/Inactive/New/Modified) |
| EDIT-02 | `IEditionFileService` contract + `EditionFileService` implementation: ImportExcelAsync (parse XLS → List<EditionRow>), ValidateAgainstLibrary (match each row field vs medic/dci/labo/famille library → detect unknowns), ApproveRow (adds new lib entry + inserts into medic), RejectRow, ExportEditionFile |
| EDIT-03 | `IUnknownDataDetectionService` contract + `FuzzyDetectionService` implementation: uses FuzzySharp token-sort-ratio, threshold ≥ 80 = known, < 80 = unknown; detects per-field (dci1-4, labo, fam1-3, forme, voie, specialite) |
| EDIT-04 | `EditionFileViewModel` with ObservableCollection<EditionRowViewModel>, ImportCommand, ApproveRowCommand, RejectRowCommand, LibraryManagerCommand, SimilaritySearchCommand, display filter (Tous/Actifs/Inactifs/Affectés/Non-affectés), row context menu commands |
| EDIT-05 | `MovementsView.xaml` full UI: title bar "Fichier d'édition", import toolbar, DataGrid with columns (☑ Lig. Id. Action Code-PCT Dénomination D.C.J-Association Forme Tab. VEiC Labo. Prix-Réf Prix Remb. A.P. Désignation), unknown cells colored blue+bold via DataTrigger, filter panel, library manager dialog, "A ajouter" badge in Action column |
| EDIT-06 | Unit tests: `FuzzyDetectionServiceTests` (known/unknown thresholds) + `EditionFileServiceTests` (import, validate, approve flow) |
