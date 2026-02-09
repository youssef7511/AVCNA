# AVICENNA DB - Base de Données Médicamenteuse

Application de bureau complète pour la gestion médicamenteuse, développée en **WPF .NET 8.0** avec **Material Design**, **MVVM** (CommunityToolkit.Mvvm), **Entity Framework Core** (MySQL/MariaDB), et un système de logging **Serilog**.

---

## 🏗️ Architecture

```text
AVCNDB.WPF/
├── Activation/         # Handlers d'activation (notifications, démarrage)
├── Behaviors/          # Navigation header behaviors
├── Controls/           # Contrôles personnalisés (CharTextBox, DecimalControl, AutoSuggestTextBox, CRUDButtons, etc.)
├── Converters/         # Convertisseurs XAML (BoolToVisibility, IntToBool, etc.)
├── Contracts/Services/ # Interfaces des services (IRepository<T>, IDialogService, INavigationService, etc.)
├── DAL/                # Data Access Layer (AppDbContext, Repository<T>, DbContextFactory)
├── Dialogs/            # Boîtes de dialogue (Success, Error, YesNo, Info, CheckedYesNo)
├── Helpers/            # Utilitaires (ThemeHelper, TitleBarHelper, WindowHelper, etc.)
├── Models/             # 25 entités EF Core (Medic, Dci, Families, Labos, Formes, Voies, Stock, Interact, etc.)
├── Services/           # Implémentations (MedicSyncService, ExcelService, PdfService, StockService, ThemeService, etc.)
├── Strings/            # Ressources de localisation
├── Styles/             # Dictionnaires XAML (Colors, CardStyles, TextStyles, ButtonStyles, InputStyles, DataGridStyles)
├── Themes/             # Thèmes clair/sombre
├── ViewModels/         # ViewModels MVVM (MainViewModel, HomeViewModel, MedicEditViewModel, etc.)
└── Views/              # Vues XAML (MainWindow, HomeView, DatabaseView, LibraryView, etc.)
```

---

## 🚀 Prérequis

- **.NET 8.0 SDK** ou supérieur
- **Visual Studio 2022** (recommandé) ou **VS Code**
- **MySQL/MariaDB 8.0+**

---

## 📦 Packages NuGet

| Package | Version | Description |
| ------- | ------- | ----------- |
| CommunityToolkit.Mvvm | 8.3.2 | MVVM Toolkit (ObservableObject, RelayCommand, source generators) |
| MaterialDesignThemes | 5.0.0 | UI Material Design pour WPF |
| MaterialDesignColors | 3.0.0 | Palette de couleurs Material |
| Microsoft.EntityFrameworkCore | 8.0.2 | ORM Entity Framework Core |
| Pomelo.EntityFrameworkCore.MySql | 8.0.2 | Provider MySQL/MariaDB |
| Microsoft.Extensions.DependencyInjection | 8.0.1 | Injection de dépendances |
| Microsoft.Extensions.Hosting | 8.0.1 | Host générique .NET |
| Microsoft.Extensions.Configuration.Json | 8.0.1 | Configuration JSON |
| ClosedXML | 0.102.3 | Import/Export Excel |
| QuestPDF | 2024.10.2 | Génération de documents PDF |
| Serilog | 4.0.2 | Logging structuré |
| Serilog.Sinks.File | 6.0.0 | Logs vers fichier |
| Serilog.Sinks.Console | 6.0.0 | Logs vers console |

---

## 🔧 Configuration

### Base de données

Configurer la connexion dans `appsettings.json` :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=127.0.0.1;Database=MEDICDB;User Id=medwin;Password=0101;Port=3306;AllowZeroDateTime=True;ConvertZeroDateTime=True;",
    "RemoteConnection": "Server=remote-host;Database=MEDICDB;User Id=user;Password=password;Port=3306;AllowZeroDateTime=True;ConvertZeroDateTime=True;"
  },
  "AppSettings": {
    "UseRemoteDatabase": false
  }
}
```

> **Note :** `AllowZeroDateTime=True;ConvertZeroDateTime=True` sont requis pour éviter les erreurs MySQL sur les dates "0000-00-00".

L'application supporte la bascule entre base de données locale et distante via le paramètre `UseRemoteDatabase`.

### Thème

Thèmes clair et sombre avec bascule via la page **Paramètres** (ComboBox Clair/Sombre/Système).

### Paramètres

Page de configuration à deux colonnes :

- **Apparence** : sélection du thème (Clair, Sombre, Système)
- **Alertes** : seuil de stock bas (unités), alerte péremption (jours)
- **Base de données** : serveur, port, nom de base, utilisateur, mot de passe, bouton "Tester la connexion" (test réel via `MySqlConnection.OpenAsync()`), sauvegarde directe dans `appsettings.json`
- **À propos** : version, framework, copyright
- Bouton **Enregistrer** persistant en bas de page, **Réinitialiser** en haut
- Redémarrage requis après modification des paramètres de connexion

---

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
# Exécuter les 86 tests
dotnet test

# Avec couverture de code
dotnet test --collect:"XPlat Code Coverage"
```

---

## 🖥️ Interface utilisateur

### Navigation

Barre latérale rétractable avec **auto-hide** (survol pour déplier, 260px → 60px) avec les sections :

| Section | Icône | Description |
| ------- | ----- | ----------- |
| Accueil | Home | Tableau de bord avec statistiques |
| Bibliothèque | BookOpenPageVariant | Tables de référence (DCI, Familles, Labos, Formes, Voies) |
| Base de données | Database | Toutes les tables (Médicaments + références + Interactions) |
| Mouvements | SwapHorizontal | Gestion des mouvements de stock |
| Paramètres | Cog | Configuration de l'application |
| Outils | Tools | Utilitaires et diagnostics |

La barre de titre globale est **masquée sur toutes les pages** — chaque vue possède son propre en-tête intégré.

### Tableau de bord (Accueil)

- **4 cartes statistiques** : Médicaments, Substances actives (DCI), Laboratoires, Alertes de stock
- **Tuiles d'accès rapide** : navigation directe vers Médicaments, DCI, Laboratoires, Rafraîchir
- Design responsive avec `WrapPanel` adaptatif

### Base de données (7 onglets)

| Onglet | Contenu |
| ------ | ------- |
| Médicaments | Liste paginée avec CRUD complet, recherche, détail, édition |
| DCI | Substances actives avec import/export Excel |
| Familles | Familles thérapeutiques avec import/export Excel |
| Laboratoires | Laboratoires pharmaceutiques avec import/export Excel |
| Formes | Formes galéniques (comprimé, gélule, sirop, etc.) |
| Voies | Voies d'administration (orale, injectable, etc.) |
| Interactions | Analyse des interactions médicamenteuses multi-DCI |

### Bibliothèque (6 onglets)

| Onglet | Contenu |
| ------ | ------- |
| DCI | Consultation et gestion des substances actives |
| Familles | Familles thérapeutiques |
| Laboratoires | Laboratoires pharmaceutiques |
| Formes | Formes galéniques |
| Voies | Voies d'administration |
| Documentation | Espace réservé (fiches techniques, PDF — à venir) |

---

## 📋 Fonctionnalités

### Gestion des médicaments

- ✅ CRUD complet (création, lecture, modification, suppression)
- ✅ Recherche avancée avec filtres
- ✅ Pagination configurable
- ✅ Fiches détaillées esthétiques en Material Design
- ✅ Formulaire d'édition complet :
  - **Identification** : nom, code-barre EAN-13, N° AMM
  - **4 familles thérapeutiques** (fam1, fam2, fam3, family)
  - **4 DCI + dosages** (dci1–dci4 avec dose1–dose4)
  - **Laboratoire**, **Forme galénique**, **Voie d'administration**
  - **Tarification** : Prix Fab. HT, Hospitalier, Gros, Base Remb., PPV
  - **Options** : Pédiatrique, Générique, Remboursable, Tableau (A/B/C)
  - **Notes et indications** en texte libre

### Synchronisation bidirectionnelle (MedicSyncService)

- ✅ **Medic → Lookups** : quand un médicament est sauvegardé, les valeurs DCI, Familles, Labos, Formes et Voies sont automatiquement ajoutées aux tables de référence si absentes
- ✅ **Lookups → Medics** : quand une entrée de référence est renommée, tous les médicaments utilisant cette valeur sont mis à jour automatiquement
- ✅ **Protection à la suppression** : avant de supprimer une entrée de référence, l'application affiche le nombre de médicaments impactés et efface les références concernées
- ✅ Thread-safe via `IDbContextFactory<AppDbContext>`
- ✅ Logging complet via Serilog

| Table | Auto-ajout | Renommage propagé | Suppression protégée |
| ----- | ---------- | ------------------ | -------------------- |
| DCI (dci1–dci4) | ✅ | ✅ | ✅ |
| Familles (fam1–fam3, family) | ✅ | ✅ | ✅ |
| Laboratoires | ✅ | ✅ | ✅ |
| Formes | ✅ | ✅ | ✅ |
| Voies | ✅ | ✅ | ✅ |

### Import/Export

- ✅ Import Excel avec validation stricte (`IStrictExcelSyncService<T>`)
- ✅ Export Excel (toutes les tables de référence)
- ✅ **Export sélectif** : checkbox de sélection dans chaque DataGrid — exporte uniquement les lignes cochées, ou tout si aucune sélection
- ✅ Exclusion automatique des propriétés `[NotMapped]` à l'export (ex: `IsChecked`)
- ✅ Génération de modèles Excel téléchargeables
- ✅ Export PDF (fiches médicaments, rapports)

### Gestion du stock

- ✅ Alertes stock bas
- ✅ Alertes péremption
- ✅ Compteur d'alertes dans le tableau de bord et la barre de statut

### Interactions médicamenteuses

- ✅ Panneau de sélection multi-DCI (recherche + ajout/retrait par chips)
- ✅ Analyse croisée de toutes les paires de DCI sélectionnées
- ✅ Cartes de résultats avec niveaux de gravité colorés (Contre-indication, Association déconseillée, Précaution d'emploi)
- ✅ Affichage de la description, du mécanisme et de la conduite à tenir
- ✅ Export PDF du rapport d'interactions

### Page Outils (ToolsViewModel)

- ✅ **Export Excel global** : exporte tous les médicaments en un clic
- ✅ **Import Excel global** : import de données médicaments avec confirmation
- ✅ **Rapport PDF** : génération d'un rapport complet de la base
- ✅ **Sauvegarde SQL** : dump de la base via `mysqldump` (détection automatique du binaire)
- ✅ **Restauration SQL** : restauration depuis un fichier `.sql` avec confirmation de sécurité
- ✅ **Statistiques** : panneau overlay affichant les compteurs (médicaments, DCI, familles, labos, formes, voies)

### Thème sombre

- ✅ Tous les styles partagés (CardStyles, TextStyles, DataGridStyles) utilisent `DynamicResource` avec les clés Material Design (`MaterialDesignBody`, `MaterialDesignCardBackground`, `MaterialDesignDivider`, etc.)
- ✅ Adaptation automatique clair/sombre sans couleurs codées en dur
- ✅ Bascule instantanée via la page Paramètres (Clair / Sombre / Système)

---

## 🗄️ Modèle de données

Structure **flat/dénormalisée** — pas de clés étrangères, liaison par nom de chaîne. La synchronisation est assurée par `MedicSyncService`.

### Entités principales (25 modèles)

| Domaine | Modèles |
| ------- | ------- |
| Médicaments | `Medic`, `Dci`, `Families`, `Labos`, `Formes`, `Voies`, `Presents`, `Poso` |
| Spécialités | `Specialites`, `Specmedic`, `Cim10`, `Catveic` |
| Interactions | `Interact`, `Cilib`, `Cilist`, `Citypes` |
| Géographie | `Gouvern`, `Localites` |
| Professionnels | `Drugstores`, `Associates`, `Biologists`, `Dentists`, `Radiologues` |
| Stock | `Stock` |
| Suivi | `ITrackable` (interface : `addedat`, `updatedat`) |

---

## 🔒 Robustesse

- ✅ Gestionnaire d'exceptions global (DispatcherUnhandledException + TaskScheduler.UnobservedTaskException)
- ✅ Try/catch dans toutes les navigations
- ✅ Logging structuré Serilog (fichier + console)
- ✅ Retry automatique sur les connexions MySQL (`EnableRetryOnFailure`)
- ✅ `IDbContextFactory` pour la création thread-safe de DbContext
- ✅ 86 tests unitaires passants

---

## 📄 Licence

Ce projet est sous licence propriétaire. © 2024-2026 AVICENNA Team.
