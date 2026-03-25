# Project State

## Current Position

- **Active phase**: 01-fichier-edition
- **Status**: Planning

## Decisions

- D-01: Feature is "Fichier d'édition" (Excel import + ML unknown-data detection), NOT stock movements
- D-02: ML approach = FuzzySharp token-sort-ratio (NuGet: FuzzySharp) — threshold ≥ 80 = known, < 80 = unknown. No Python runtime required.
- D-03: Unknown cells colored blue+bold via DataTrigger in XAML (not code-behind)
- D-04: Action flag "A ajouter" shown as colored badge in the Action DataGrid column
- D-05: Library management (DCI, Familles, Labos, Formes, Voies, Spécialités) accessed via dialog, not a separate page
- D-06: The Mouvements page (MovementsView.xaml) is repurposed as the Fichier d'édition page

## Pending

- Implement Phase 01 Fichier d'édition plans (01-01 through 01-04)
