# AVICENNA DB - WPF Application

Base de données pharmaceutique complète développée en WPF .NET 8.0 avec Material Design.

## 🏗️ Architecture

```
AVCNDB.WPF/
├── Controls/           # Contrôles personnalisés (CharTextBox, DecimalTextBox, etc.)
├── Converters/         # Convertisseurs XAML
├── Contracts/Services/ # Interfaces des services
├── DAL/               # Data Access Layer (Entity Framework Core)
├── Models/            # Entités de la base de données
├── Properties/        # Settings et configuration
├── Services/          # Implémentations des services
├── Styles/            # Dictionnaires de ressources XAML
├── ViewModels/        # ViewModels (MVVM)
└── Views/             # Vues XAML
```

## 🚀 Prérequis

- .NET 8.0 SDK ou supérieur
- Visual Studio 2022 (recommandé)
- MySQL/MariaDB 8.0+

## 📦 Packages NuGet

| Package | Version | Description |
|---------|---------|-------------|
| CommunityToolkit.Mvvm | 8.3.2 | MVVM Toolkit |
| MaterialDesignThemes | 5.0.0 | UI Material Design |
| MaterialDesignColors | 3.0.0 | Palette de couleurs |
| Pomelo.EntityFrameworkCore.MySql | 8.0.2 | Provider MySQL |
| ClosedXML | 0.102.3 | Export/Import Excel |
| QuestPDF | 2024.10.2 | Génération PDF |
| Serilog | 4.0.2 | Logging |

## 🔧 Configuration

### Base de données

Configurer la connexion dans `appsettings.json` :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=50000;Database=MEDICDB;User=root;Password=yourpassword;"
  }
}
```

### Thème

L'application supporte les thèmes clair et sombre. Configurable via Paramètres > Apparence.

## 🏃 Lancement

```bash
# Restaurer les packages
dotnet restore

# Compiler
dotnet build

# Exécuter
dotnet run --project AVCNDB.WPF
```

## 🧪 Tests

```bash
# Exécuter tous les tests
dotnet test

# Avec couverture de code
dotnet test --collect:"XPlat Code Coverage"
```

## 📋 Fonctionnalités

### Gestion des médicaments
- ✅ CRUD complet
- ✅ Recherche avancée
- ✅ Filtres par famille, laboratoire
- ✅ Pagination
- ✅ Fiches détaillées esthétiques

### Import/Export
- ✅ Import Excel avec validation
- ✅ Export Excel
- ✅ Export PDF (fiches, rapports)

### Gestion du stock
- ✅ Alertes stock bas
- ✅ Alertes péremption
- ✅ Tableau de bord

### Interactions médicamenteuses
- ✅ Analyse multi-DCI
- ✅ Niveaux de gravité
- ✅ Export rapport

## 📄 License

Ce projet est sous licence propriétaire. © 2024 AVCNA Team.
