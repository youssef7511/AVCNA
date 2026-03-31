# Plan 01-04 Summary: Full UI (MovementsView.xaml)

## Status: COMPLETE

## What was built

### MovementsView.xaml — Full "Fichier d'edition" UI
Completely replaced the old placeholder with:

1. **Toolbar (Row 0)**: Source type ComboBox (6 source types), file path display with icon, Import/Export/Library Manager buttons using `TabToolbarPrimaryButtonStyle` and `TabToolbarSecondaryButtonStyle`. Error banner using `StringNotNullOrEmptyToVisibility` converter.

2. **Filter + Stats Bar (Row 1)**: 
   - 13 RadioButtons (`MaterialDesignTabRadioButtonBottom` style) in a horizontal ScrollViewer: Tous, Actifs, Inactifs, Affectes, Non-affectes, Med.[V.E.I], Avec A.P, Sans A.P, Rembourses, Non-rembourses, Selectionnes, Non-selectionnes, Changements de prix
   - Stats badges: Total (info), Inconnues (warning), Approuvees (success) — using `InfoLightBrush`/`WarningLightBrush`/`SuccessLightBrush`

3. **DataGrid (Row 2)** with `ModernDataGridStyle`:
   - Columns: Checkbox, #, Action badge, Code, Denomination, DCI, Association, Forme, Tab, VEIC, Labo, Famille, Specialite, Voie, Prix Ref, PPV, Remb, AP, Actions PopupBox
   - Blue (#1565C0) + bold DataTriggers on 8 detection-eligible columns (DCI, Association, Forme, Labo, Famille, Specialite, Voie) via `IsUnknown_*` bindings (D-03)
   - Action badge column with color-coded DataTriggers per ActionFlag value (D-04)
   - Price change highlighting (red + bold via `ErrorBrush` when `HasPriceChanged`)
   - Remb/AP columns using `IntToBoolConverter` with text indicators ("R"/"G")
   - Row actions via `materialDesign:PopupBox` with `BindingProxy`: Approve, Reject, Mark New, Mark Deletion, Reset, Similarity Search

4. **Status Bar (Row 3)**: Status message + row count display ("Affichage: X / Y lignes")

5. **Loading indicator**: `MaterialDesignCircularProgressBar` overlay on DataGrid, bound to `IsLoading`

### MovementsView.xaml.cs — Code-behind
- Constructor injection of `EditionFileViewModel` (DI resolved)
- `FilterRadioButton_Checked` event handler: sets `FilterType` from RadioButton `Tag`

### NavigationService.cs fix
- Removed dead `"MovementsView" => new MovementsView()` case from `NavigateToView()` switch, since navigation now uses ViewModel-based routing via `NavigateTo<EditionFileViewModel>()`.

## Blue cell highlighting approach (D-03)
Each detection-eligible column uses a `DataGridTemplateColumn` with a `TextBlock` inside. The TextBlock has a `Style` with `DataTrigger` binding to `IsUnknown_*` (e.g., `IsUnknown_Dci`). When `True`, sets `Foreground` to `{StaticResource UnknownFieldBrush}` (#1565C0) and `FontWeight` to `Bold`. The brush is defined as a `UserControl.Resource` for consistency.

## Action badge approach (D-04)
The Action column uses a `DataGridTemplateColumn` with a `Border` + `TextBlock`. Both have `DataTrigger`s on `ActionFlag` string values (`AjouterNew`, `Affecte`, `Desaffecte`, etc.) that set background/foreground colors. Badge is collapsed when `ActionFlag` is `None`.

## Build & Test results
- **WPF project build**: 0 errors, 0 warnings
- **Test suite**: 98 passed, 11 failed (all 11 failures are pre-existing entity tracking issues in RepositoryTests/StrictExcelSyncServiceTests — NOT related to Phase 01)
- **Phase 01 tests**: 16/16 passed (6 FuzzyDetection + 4 EditionFileService + 6 EditionFileViewModel)

## Human verification
Pending — user needs to run the app and verify the UI visually per the verification checklist in 01-04-PLAN.md.
