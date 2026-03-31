# Plan 01-03 Summary: ViewModels + Tests

## Status: COMPLETE

## What was built

### ViewModels
- **`AVCNDB.WPF/ViewModels/EditionRowViewModel.cs`** — Observable wrapper around `EditionRow`:
  - Pass-through properties for all EditionRow fields (LineNumber, PctCode, ItemName, Dci1, Labo, etc.)
  - Per-field `IsUnknown_*` booleans for XAML DataTrigger binding (D-03): `IsUnknown_Dci`, `IsUnknown_DciAssociation`, `IsUnknown_Labo`, `IsUnknown_Forme`, `IsUnknown_Fam1`/`Fam2`/`Fam3`, `IsUnknown_Voie`, `IsUnknown_Specialite`
  - Two-way `IsSelected` property for checkbox column
  - Bubbles PropertyChanged events from underlying EditionRow

- **`AVCNDB.WPF/ViewModels/EditionFileViewModel.cs`** — Main page ViewModel:
  - 9 commands: Import, ApproveRow, RejectRow, MarkAsNew, MarkForDeletion, ResetRow, OpenLibraryManager, SimilaritySearch, Export
  - 13 display filter types via `FilterType` string property + `ApplyFilter()` method
  - Observable properties: `Rows` (filtered), `SelectedRow`, `CurrentFilePath`, `SelectedSourceType`, `TotalRowCount`, `UnknownRowCount`, `ApprovedRowCount`
  - Uses `ExecuteAsync` from ViewModelBase for loading state and error handling

### Navigation & DI Wiring
- `App.xaml.cs`: Registered `EditionFileViewModel` as transient in DI container
- `App.xaml`: Added `DataTemplate` mapping `EditionFileViewModel` -> `MovementsView`
- `MainViewModel.cs`: Changed `NavigateToMovements()` from `NavigateToView("MovementsView")` to `NavigateTo<EditionFileViewModel>()` with title "Fichier d'edition"

### Tests (6 total, all pass)
- **`AVCNDB.WPF.Tests/ViewModels/EditionFileViewModelTests.cs`**:
  - Constructor sets default filter to "Tous"
  - All 9 commands are not null
  - Filter "Affectes" shows only Affecte rows
  - Filter "Tous" shows all rows
  - Filter "Changements de prix" shows only price-changed rows
  - Filter "Avec A.P" shows only AP rows

## Deviations from plan
- Filter "Tous" default issue: Setting FilterType to "Tous" when already "Tous" doesn't trigger `OnFilterTypeChanged` — tests must change to another filter first then back, or call `ApplyFilter()` directly.
- CanExecute for row actions depends on `SelectedRow != null`, properly wired via `OnSelectedRowChanged`.
