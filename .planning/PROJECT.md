# AVICENNA DB — Project Context

## What Is This?

**AVCNDB.WPF** is a WPF .NET 8.0 desktop application for pharmaceutical stock management. It serves as a comprehensive medication database manager used in a healthcare/pharmacy context.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| UI Framework | WPF .NET 8.0 + MaterialDesignThemes 5.0.0 |
| MVVM | CommunityToolkit.Mvvm 8.3.2 (source generators) |
| ORM | Entity Framework Core 8.0.2 (Pomelo MySQL provider) |
| DI | Microsoft.Extensions.DependencyInjection |
| Database | MySQL/MariaDB 8.0 (Docker) |
| Excel | ClosedXML 0.102.3 |
| PDF | QuestPDF 2024.10.2 |
| Logging | Serilog 4.0.2 |
| Testing | xUnit + Moq + FluentAssertions + EF InMemory |

## Architecture

- **Pattern**: MVVM (ViewModelBase → ObservableObject + INavigationAware)
- **DAL**: `AppDbContext` (DbContext) + `Repository<T>` (generic, thread-safe via IDbContextFactory)
- **Data model**: Flat/denormalized (no FK, linked by string name). `MedicSyncService` handles bidirectional sync.
- **Schema evolution**: Versioned SQL scripts in `database/migrations/` (V001, V002...) — NOT `dotnet ef migrations`
- **DI registration**: All services/VMs/Views registered in `App.xaml.cs`

## Navigation

Collapsible sidebar (260px ↔ 60px auto-hide):
- Accueil / Bibliothèque / Base de données / **Mouvements** / Paramètres / Outils

## Key Models

- `Medic` (25 fields: itemname, barcode, dci1-4, dose1-4, fam1-3, family, labo, forme, voie, prices, flags)
- `Stock` (medicid, medicname, quantity, minstock, maxstock, expirydate, batchno, location)
- **`StockMovement`** (to be created: medicid, medicname, movementtype [In/Out/Adjustment/Inventory], quantity, batchno, expirydate, reason, reference, operatedby, createdat)

## Style Conventions

- All colors use `DynamicResource` for theme support (light/dark)
- Brushes: `PrimaryBrush`, `ErrorBrush`, `WarningBrush`, `SuccessBrush`, `BorderBrush`
- Cards use `ElevatedCardStyle` / `CardStyles.xaml`
- DataGrids use `ModernDataGridStyle`
- `materialDesign:PopupBox` for row action menus
- `BindingProxy` helper for DataGrid command bindings

## Testing Conventions

- `TestDbContextFactory.CreateSeededContext()` for in-memory DB tests
- Mock pattern: `Mock<IRepository<T>>()` with `.Setup()` + `.ReturnsAsync()`
- Test classes: `IDisposable` with `_context.Dispose()`
- xUnit + FluentAssertions (`.Should()`)
