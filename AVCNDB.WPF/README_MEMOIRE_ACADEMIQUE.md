# CHAPITRE I — CADRE GÉNÉRAL DU PROJET

## Introduction

Dans le secteur de la santé, la donnée médicamenteuse joue un rôle critique dans la qualité des soins, la sécurité thérapeutique et l'efficacité opérationnelle. Lorsqu'elle est dispersée entre plusieurs fichiers ou gérée sans règles homogènes, le risque d'erreurs augmente rapidement : doublons, informations contradictoires, mises à jour partielles et difficulté de contrôle.

Le projet **AVICENNA DB (AVCNDB)** s'inscrit dans ce contexte. Il propose une application desktop orientée gestion de base de données médicamenteuses, avec une architecture technique modulaire, des workflows de saisie/édition robustes, et une stratégie claire d'évolution vers des fonctionnalités intelligentes (AI/ML) à moyen terme.

---

## 1.1 Présentation de l'organisme

**Expertises & Logiciels pour Particuliers & Entreprises (ESIB)**, fondée en 1996 et basée à Bizerte, Tunisie, est une entreprise innovante spécialisée dans le développement de solutions logicielles, avec une expertise particulière dans le domaine médical. Dirigée par M. Mounir Jerbi, ESIB s'est imposée comme un acteur clé grâce à **MEDWIN**, un logiciel de gestion de cabinet médical adopté par plus de 3 000 médecins tunisiens.

MEDWIN est conçu pour simplifier le quotidien des professionnels de santé en automatisant les tâches administratives et en organisant efficacement les informations des patients. Il offre des fonctionnalités avancées telles que l'optimisation de la gestion des rendez-vous, permettant une planification fluide et une gestion précise de l'agenda médical. Le logiciel facilite également la tenue des dossiers médicaux électroniques en garantissant un enregistrement structuré et un suivi détaillé des informations des patients.

En matière de facturation, MEDWIN automatise les transactions financières liées aux consultations, assurant ainsi une gestion rapide et transparente. Il simplifie également la coordination des ressources internes et la gestion des stocks, contribuant à une organisation optimale du cabinet médical.

En complément, ESIB a étendu les capacités de MEDWIN en proposant des services de publicité médicale et pharmaceutique via la même plateforme. Cette initiative permet aux entreprises partenaires de promouvoir leurs produits et services auprès de la communauté médicale, avec deux niveaux de partenariat : le niveau **PREMIUM**, qui inclut des espaces publicitaires privilégiés, et le niveau **BASIC**, axé sur la diffusion de messages promotionnels standards.

Cette approche innovante combine technologie et marketing médical, positionnant MEDWIN non seulement comme un outil de gestion, mais aussi comme une plateforme stratégique facilitant la collaboration entre les professionnels de la santé et leurs partenaires.

---

## 1.2 Cadre de Projet

### 1.2.1 Contexte de Projet

Le besoin initial vient d'un constat métier simple : la gestion quotidienne des médicaments (dénomination, DCI, labo, forme, voie, posologie, etc.) nécessite une base fiable, centralisée et facilement exploitable. Or, dans les pratiques classiques, les données sont souvent :

- stockées dans des tableaux bureautiques hétérogènes ;
- modifiées sans trace claire des opérations ;
- non synchronisées entre tables de référence et données médicaments ;
- difficiles à auditer après plusieurs cycles de mise à jour.

La conséquence directe est une perte de temps sur la vérification manuelle et un risque accru d'incohérence des informations médicales.

---

### 1.2.2 Analyse de l'existant

#### 1.2.2.1 Étude de l'existant

Le système actuel, connu sous le nom de **Medic 5X**, est une application de bureau développée en **Windows Forms (.NET Framework)**. Il a été conçu pour répondre aux besoins de gestion de la base de données médicamenteuse au sein de l'organisme. Son architecture repose sur les éléments suivants :

- **Base de données locale au format `.dbf`** (dBASE) : les données sont stockées dans des fichiers plats sur le poste de l'utilisateur, sans serveur centralisé.
- **Interface WinForms** : l'interface graphique utilise les composants standards de Windows Forms, avec des grilles de données, des formulaires de saisie et des menus classiques.
- **Export Excel au format `.xls`** : les données sont exportées vers des fichiers Excel pour transmission à la CNAM (Caisse Nationale d'Assurance Maladie), utilisés comme support officiel d'échange.
- **Mise à jour par FTP** : les nouvelles versions de la base de données sont distribuées sous forme de fichiers `.dbf` déposés sur un serveur FTP. L'utilisateur doit manuellement télécharger et remplacer les fichiers locaux.

Ce système assure les fonctions de base — consultation, recherche, édition et export des médicaments. Toutefois, son architecture monolithique et sa dépendance aux fichiers locaux limitent fortement ses capacités d'évolution.

#### 1.2.2.2 Critiques de l'existant

L'analyse du système **Medic 5X** met en évidence plusieurs limites structurelles :

| Axe | Limite identifiée |
|-----|-------------------|
| **Isolation des données** | Le format `.dbf` est archaïque et ne supporte ni les relations entre tables, ni les contraintes d'intégrité, ni les transactions. Chaque poste travaille sur une copie locale, sans synchronisation. |
| **Expérience utilisateur limitée** | Windows Forms offre une interface rigide, peu personnalisable et visuellement dépassée. L'ergonomie n'a pas évolué avec les standards modernes. |
| **Mise à jour lourde** | Le processus de mise à jour par FTP est entièrement manuel : téléchargement, décompression et remplacement des fichiers `.dbf`. Toute erreur de manipulation peut corrompre la base locale. |
| **Absence d'assistance intelligente** | Aucun mécanisme d'aide à la saisie, de détection d'incohérences ou d'analyse prédictive n'est intégré. Le contrôle qualité repose entièrement sur l'opérateur humain. |
| **Interopérabilité limitée** | L'export `.xls` est figé et ne permet pas d'adaptation dynamique aux exigences changeantes de la CNAM ou d'autres partenaires. |
| **Traçabilité insuffisante** | Aucun historique des modifications n'est conservé. Il est impossible de savoir qui a modifié quoi, ni quand. |

#### 1.2.2.3 Solution proposée

La solution retenue est le développement d'un nouveau système de gestion de la base de données, **Medic 6X (AVCNDB)**, qui remplace intégralement Medic 5X. Les axes de modernisation sont les suivants :

| Axe | Solution apportée par Medic 6X |
|-----|-------------------------------|
| **Base de données centralisée** | Migration vers **MySQL/MariaDB** déployé via **Docker**, avec un schéma relationnel complet (clés étrangères, contraintes, index). Accès aux données via **Entity Framework Core** et le pattern **Repository**. |
| **Interface moderne** | Réécriture complète en **WPF (.NET 8)** avec **Material Design In XAML**, offrant une expérience utilisateur fluide, moderne et personnalisable (thèmes, DataGrid interactif, filtres dynamiques, ComboBox filtrables). |
| **Mise à jour automatisée** | Remplacement du FTP par une synchronisation directe avec la base MySQL. Les mises à jour sont appliquées via des scripts SQL versionnés, sans intervention manuelle. |
| **Intelligence intégrée** | Préparation d'une couche **AI/ML** pour l'aide à l'import Excel, la détection d'interactions médicamenteuses et l'assistance thérapeutique — conçue comme module d'assistance, pas comme remplacement du contrôle métier. |
| **Interopérabilité Excel** | Import/Export structuré via **ClosedXML**, avec validation stricte des colonnes, mapping intelligent et gestion des erreurs explicite. |
| **Traçabilité complète** | Chaque enregistrement dispose de champs `addedat` et `updatedat` gérés automatiquement via l'interface `ITrackable`, assurant un suivi complet des modifications. |

Cette solution traite prioritairement la robustesse des flux de données avant l'ajout de fonctions avancées.

---

## 1.3 Méthodologies de travail

### 1.3.1 Méthodologie agile — SCRUM

Le projet est mené avec une logique itérative proche de Scrum :

- Découpage des objectifs en incréments fonctionnels.
- Priorisation continue selon les retours métier.
- Livraisons progressives (socle, CRUD, synchronisation, import/export, UX).
- Stabilisation rapide des anomalies par cycle court.

Cette approche a permis de maintenir un bon compromis entre vitesse d'exécution et qualité technique.

### 1.3.2 Langages de modélisation

La modélisation sert à clarifier :

- les besoins fonctionnels et non fonctionnels ;
- les acteurs et leurs interactions avec le système ;
- la frontière entre logique métier, logique technique et persistance.

L'objectif n'est pas uniquement documentaire : la modélisation guide les choix d'implémentation (couches, responsabilités, points de contrôle).

---

## Conclusion du Chapitre I

Le cadre général confirme que la valeur du projet AVCNDB repose d'abord sur la qualité de la base de données et la maîtrise des flux de mise à jour. Les choix de stack, d'architecture et de méthodologie établissent une fondation solide pour les phases de préparation et d'évolution vers des fonctions d'aide intelligente.

---
---

# CHAPITRE II — PRÉPARATION DU PROJET

## Introduction

Ce chapitre formalise la préparation du projet sous quatre angles : besoins, pilotage, environnement et architecture. Il intègre également l'orientation future AI/ML, en précisant comment ces fonctions seront ajoutées sans compromettre le socle applicatif actuel.

---

## 2.1 Capture du Besoin

### 2.1.1 Spécifications des besoins

#### 2.1.1.1 Spécifications des Besoins Fonctionnels

Le système doit couvrir les fonctions suivantes :

- Gestion CRUD des médicaments.
- Gestion CRUD des références (DCI, Familles, Labos, Formes, Voies, Spécialités, Présentations, Posologies).
- Recherche, filtrage et tri pour accélérer l'accès aux informations.
- Import/Export Excel avec règles strictes sur les colonnes.
- Synchronisation métier lors des renommages/suppressions des références.
- Mise à jour cohérente de l'UI après opérations critiques.
- Exploitation de la base dans un environnement Docker reproductible.

#### 2.1.1.2 Spécification des Besoins non Fonctionnels

Le système doit satisfaire :

- **Performance** : opérations courantes fluides sur un volume significatif.
- **Fiabilité** : cohérence des données et réduction des écarts entre tables.
- **Robustesse** : gestion explicite des erreurs et feedback utilisateur clair.
- **Maintenabilité** : architecture en couches et services injectables.
- **Évolutivité** : capacité d'intégrer des modules AI/ML de manière contrôlée.
- **Lisibilité UI** : cohérence visuelle et parcours de saisie clairs.

### 2.1.2 Modélisation des besoins

#### 2.1.2.1 Identification des acteurs

Acteurs principaux :

- **Utilisateur métier** : gère les références et les médicaments, importe/exporte les données, corrige les écarts.
- **Administrateur technique** : configure l'environnement (DB, Docker), supervise l'intégrité et accompagne les évolutions.

#### 2.1.2.2 Diagramme de cas d'utilisation global

Le cas d'utilisation global inclut :

- Consulter/rechercher des données médicamenteuses.
- Créer/modifier/supprimer des entités.
- Importer un fichier Excel et contrôler sa conformité.
- Exporter des données pour diffusion/analyse.
- Synchroniser les références avec les enregistrements médicaments.
- Préparer des extensions analytiques futures.

---

## 2.2 Pilotage du Projet avec Scrum

### 2.2.1 Équipe et rôles

Organisation type :

- **Rôle produit** : priorise les besoins métier.
- **Rôle coordination** : suit l'avancement et fluidifie les itérations.
- **Rôle développement** : implémente, teste et stabilise les modules.

### 2.2.2 Backlog du produit

Le backlog est structuré autour de thèmes :

- Qualité CRUD et synchronisation.
- Stabilité import/export Excel.
- Alignement UI / ViewModel / DB.
- Maintenance Docker/SQL.
- Préparation de la couche AI/ML.

### 2.2.3 Planification de Release

Plan de release progressif :

| Sprint | Objectif |
|--------|----------|
| **Sprint 1** | Socle technique, navigation, injection de dépendances, configuration. |
| **Sprint 2** | CRUD principal et références. |
| **Sprint 3** | Import/Export strict + synchronisation métier. |
| **Sprint 4** | Optimisation UI/UX, robustesse, documentation. |
| **Sprint 5** *(cible future)* | Pré-intégration AI/ML en mode assisté. |
| **Sprint 6**  | Authentification 

---

## 2.3 Environnement de travail

### 2.3.1 Environnement matériel

- Poste Windows de développement.
- Ressources suffisantes pour IDE + runtime .NET + Docker + MySQL.

### 2.3.2 Environnement Logiciel

#### 2.3.2.1 Outils de développement et modélisation

- Visual Studio / VS Code.
- Git + plateforme de versioning.
- Docker Desktop.
- Outils de documentation/planification.

#### 2.3.2.2 Langages de programmation

- **C#** — logique applicative et métier.
- **XAML** — présentation WPF.
- **SQL** — persistance et scripts de migration.

#### 2.3.2.3 Frameworks et bibliothèques

| Composant | Rôle |
|-----------|------|
| **.NET 8 WPF** | Framework applicatif desktop. |
| **CommunityToolkit.Mvvm** | Pattern MVVM (ObservableProperty, RelayCommand). |
| **EF Core + Pomelo MySQL** | ORM et connecteur MySQL/MariaDB. |
| **MaterialDesignInXAML** | Thème Material Design pour WPF. |
| **ClosedXML** | Lecture/écriture de fichiers Excel. |
| **Serilog** | Journalisation structurée. |

---

## 2.4 Architecture

L'architecture est organisée pour séparer clairement les responsabilités :

| Couche | Responsabilité |
|--------|---------------|
| **Présentation** | Vues WPF et expérience utilisateur. |
| **Présentation logique** | ViewModels (état, commandes, orchestration). |
| **Services** | Synchronisation, import Excel, navigation, dialogues. |
| **Accès données** | Repository + `AppDbContext` (EF Core). |
| **Persistance** | MySQL/MariaDB dans Docker. |

Cette architecture permet d'introduire des fonctions avancées sans fragiliser le noyau métier.

### 2.4.1 Aperçu AI/ML *(orientation future, non encore activée)*

L'AI/ML est prévue comme **couche d'assistance**, pas comme remplacement du contrôle métier. Le principe est :

- Conserver les validations strictes existantes.
- Ajouter une pré-analyse intelligente en amont.
- Exiger une validation humaine quand la confiance est insuffisante.
- Tracer toutes les propositions AI pour audit.

### 2.4.2 Notation AI/ML — Import intelligent d'Excel

**Objectif** : reconnaître automatiquement les colonnes et mapper correctement les enregistrements médicaments.

Fonctions cibles :

- Détection de variantes de noms de colonnes (alias, synonymes, fautes fréquentes).
- Normalisation des valeurs (noms, abréviations, unités, formats).
- Scoring de correspondance avec la base existante (match exact, match probable, conflit).
- Proposition d'action : insérer, mettre à jour, fusionner, ignorer.

**Sortie attendue** : intégration plus rapide de fichiers externes, moins d'erreurs de mapping, conservation de la gouvernance métier grâce à la validation finale.

### 2.4.3 Notation AI/ML — Modèle d'analyse des interactions (DCI × Formes)

**Objectif** : étendre l'analyse des interactions en croisant non seulement les DCI, mais aussi la forme galénique et le contexte de combinaison.

Approche cible :

- Extraction de caractéristiques (paires DCI, groupe de DCI, forme, sévérité historique).
- Modèle de classification du niveau de risque.
- Génération d'explications exploitables par l'utilisateur (raison, gravité, conduite à tenir).

**Valeur métier** : meilleure priorisation des alertes, interprétation plus rapide des associations sensibles, support plus cohérent pour les actions thérapeutiques.

### 2.4.4 Notation AI/ML — Aide à la décision thérapeutique

**Objectif** : fournir un assistant de recommandation basé sur les contraintes de traitement.

Principe :

- Agréger les signaux (interaction, contre-indication, forme, contexte patient lorsqu'il sera disponible).
- Proposer des options thérapeutiques ordonnées par pertinence.
- Expliciter pourquoi une option est recommandée ou déconseillée.

**Règle de gouvernance** : la décision finale reste humaine ; le module est un outil d'aide, non un moteur de prescription autonome.

### 2.4.5 Stratégie d'intégration progressive AI/ML

Plan prudent recommandé :

1. Stabiliser la qualité des données (schéma, contraintes, synchronisation, import strict).
2. Ajouter l'AI en mode lecture/assistance uniquement.
3. Mesurer précision et faux positifs sur un corpus contrôlé.
4. Activer progressivement par seuil de confiance.
5. Industrialiser monitoring, audit et rollback fonctionnel.

---

## Conclusion du Chapitre II

La préparation du projet consolide une base technique robuste et exploitable immédiatement. L'architecture actuelle répond aux besoins opérationnels de gestion de base médicamenteuse, tout en ouvrant un chemin réaliste vers des fonctionnalités AI/ML explicables, contrôlées et utiles en pratique.

