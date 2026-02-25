# CHAPITRE I. CADRE GÉNÉRAL DU PROJET / PRÉSENTATION GÉNÉRALE

## Introduction
Ce chapitre présente le cadre général du projet **AVICENNA DB (AVCNDB)**, une application desktop de gestion pharmaceutique. L’objectif principal est de digitaliser la gestion des médicaments, des référentiels (DCI, familles, laboratoires, formes, voies) et des opérations associées (recherche, filtrage, import/export, suivi de stock), tout en assurant une interface moderne et une architecture logicielle maintenable.

## 1.2 Cadre de Projet

### 1.2.1 Contexte de Projet
Le secteur pharmaceutique repose sur des données volumineuses et sensibles : identification des médicaments, composition, dosage, classification thérapeutique, laboratoire, prix, et statut d’activité. Dans de nombreuses structures, la gestion de ces informations reste partiellement manuelle (Excel dispersés, saisies redondantes, absence de contrôle central), ce qui augmente le risque d’erreurs, ralentit la consultation et complique les mises à jour.

Le projet AVCNDB répond à ce besoin en proposant :
- une base de données centralisée,
- une interface utilisateur ergonomique,
- des mécanismes de recherche et de filtrage,
- des fonctionnalités d’import/export,
- une synchronisation des tables de référence,
- un cadre technique robuste (WPF, MVVM, EF Core, MySQL/MariaDB).

### 1.2.2 Analyse de l’existant

#### 1.2.2.1 Étude de l’existant
L’analyse initiale met en évidence un mode de travail basé sur :
- des fichiers hétérogènes (souvent Excel),
- une faible normalisation des données,
- des mises à jour non synchronisées,
- des recherches lentes et peu fiables,
- une difficulté à tracer les modifications.

Sur le plan technique, les solutions existantes ne couvrent pas de manière unifiée la gestion complète des médicaments et des référentiels dans une application bureau adaptée aux besoins métier.

#### 1.2.2.2 Critiques de l’existant
Les limites majeures constatées sont :
- **Redondance** : mêmes informations ressaisies dans plusieurs fichiers.
- **Incohérence** : divergences entre noms de familles/laboratoires selon les sources.
- **Faible traçabilité** : historique des modifications difficile à suivre.
- **Risque d’erreur** : absence de validation forte à la saisie.
- **Manque de performance opérationnelle** : recherche, filtrage, et extraction peu efficaces.
- **Maintenance difficile** : absence d’architecture claire et évolutive.

#### 1.2.2.3 Solution proposée
La solution proposée est une application desktop nommée **AVCNDB**, conçue autour des principes suivants :
- Architecture **MVVM** pour séparer interface, logique et données.
- Base relationnelle **MySQL/MariaDB** pour la persistance.
- Accès aux données via **Entity Framework Core**.
- Interface moderne avec **Material Design**.
- Gestion complète CRUD des entités métier.
- Recherche, pagination, filtres, et export ciblé.
- Synchronisation automatique entre médicaments et référentiels.

La solution vise à améliorer la qualité des données, réduire les erreurs métier, accélérer le traitement quotidien et faciliter l’évolution du système.

## 1.3 Méthodologies de travail

### 1.3.1 Les méthodologies agiles : la méthodologie SCRUM
Le projet s’inscrit dans une logique **Agile/Scrum** avec des itérations courtes. Cette approche permet d’intégrer progressivement les fonctionnalités critiques, de valider régulièrement les résultats et d’ajuster les priorités selon les retours.

Principes appliqués :
- Découpage du travail en sprints.
- Priorisation via backlog produit.
- Livraisons incrémentales.
- Réévaluation continue des besoins.

Bénéfices observés :
- meilleure visibilité sur l’avancement,
- adaptation rapide aux changements,
- réduction du risque de dérive fonctionnelle,
- amélioration continue de la qualité.

### 1.3.2 Langages de modélisation
La modélisation UML est utilisée pour clarifier les besoins et structurer la conception :
- **Diagramme de cas d’utilisation** pour exprimer les interactions acteur-système.
- **Diagrammes de classes** pour structurer les entités métier.
- **Diagrammes de séquence** (si nécessaire) pour décrire les flux clés.

Cette modélisation facilite la communication entre les parties prenantes et réduit les ambiguïtés techniques.

## Conclusion
Le cadre général du projet met en évidence la nécessité d’une plateforme unifiée, fiable et évolutive pour la gestion médicamenteuse. L’analyse de l’existant justifie le choix d’une solution desktop structurée autour de technologies modernes et d’une démarche agile. Ce socle prépare la phase suivante : la préparation détaillée du projet.

---

# CHAPITRE II. PRÉPARATION DU PROJET

## Introduction
Ce chapitre détaille la préparation du projet : capture du besoin, modélisation, organisation Scrum, environnement de travail et architecture technique adoptée pour réaliser AVCNDB.

## 2.1 Capture du Besoin

### 2.1.1 Spécifications des besoins

#### 2.1.1.1 Spécifications des besoins fonctionnels
Le système doit permettre de :
- Gérer les médicaments (ajout, modification, suppression, consultation).
- Gérer les tables de référence (DCI, Familles, Laboratoires, Formes, Voies).
- Rechercher et filtrer les données par critères métier.
- Paginer l’affichage pour améliorer la lisibilité.
- Importer et exporter les données (Excel/PDF).
- Consulter l’état du stock et les alertes associées.
- Assurer la synchronisation des valeurs de référence avec les médicaments.
- Fournir une interface paramétrable (thème, connexion base, seuils d’alerte).

#### 2.1.1.2 Spécification des besoins non fonctionnels
Le système doit respecter :
- **Performance** : temps de réponse acceptable pour recherche/filtrage.
- **Fiabilité** : cohérence des données et gestion des erreurs.
- **Sécurité** : accès base de données contrôlé.
- **Maintenabilité** : architecture modulaire et testable.
- **Ergonomie** : interface intuitive et moderne.
- **Évolutivité** : ajout de nouvelles fonctionnalités sans refonte majeure.

### 2.1.2 Modélisation des besoins

#### 2.1.2.1 Identification des acteurs
Acteurs principaux :
- **Utilisateur métier (pharmacien / gestionnaire)** : gère les médicaments et référentiels.
- **Administrateur applicatif** : configure la connexion base, paramètres techniques, supervision.

#### 2.1.2.2 Diagramme de cas d’utilisation global
Cas d’utilisation globaux :
- Authentifier/ouvrir l’application.
- Consulter les listes de données.
- Rechercher / filtrer / paginer.
- Ajouter / modifier / supprimer une entité.
- Importer / exporter des données.
- Gérer les paramètres et la connexion.
- Consulter alertes stock et interactions.

## 2.2 Pilotage du Projet avec Scrum

### 2.2.1 Équipe et rôle
Organisation type Scrum :
- **Product Owner** : définit les priorités métier.
- **Scrum Master** : facilite le processus et supprime les blocages.
- **Équipe de développement** : conçoit, implémente, teste et livre les incréments.

### 2.2.2 Le Backlog du produit
Le backlog contient les user stories priorisées, par exemple :
- En tant qu’utilisateur, je veux rechercher un médicament par nom/DCI/code-barres.
- En tant qu’utilisateur, je veux gérer plusieurs familles par médicament.
- En tant qu’utilisateur, je veux exporter uniquement les lignes sélectionnées.
- En tant qu’administrateur, je veux tester la connexion à la base depuis l’interface.

### 2.2.3 Planification de Release (ou Sprint)
Planification proposée :
- **Sprint 1** : fondation architecture + navigation + entités principales.
- **Sprint 2** : CRUD médicaments + référentiels.
- **Sprint 3** : recherche, filtres, pagination, synchronisation.
- **Sprint 4** : import/export, paramètres, optimisation, stabilisation.

## 2.3 Environnement de travail

### 2.3.1 Environnement matériel
Exemple d’environnement :
- PC de développement sous Windows.
- Mémoire suffisante pour IDE + base locale.
- Connexion réseau pour dépendances/outils.

### 2.3.2 Environnement logiciel

#### 2.3.2.1 Outils de développement et modélisation
- **Visual Studio / VS Code**
- **Git & GitHub**
- Outils UML pour la modélisation
- **XAMPP / MariaDB** pour la base locale

#### 2.3.2.2 Langages de programmation
- **C#** (logique métier et application)
- **XAML** (interfaces WPF)
- **SQL** (manipulation et administration des données)

#### 2.3.2.3 Framework utilisé
- **.NET 8 (WPF)**
- **CommunityToolkit.Mvvm** (pattern MVVM)
- **Entity Framework Core + Pomelo MySQL**
- **MaterialDesignInXAML**
- **Serilog**

## 2.4 Architecture
Architecture en couches :
- **Présentation** : vues XAML + styles Material Design.
- **ViewModels** : orchestration des cas d’usage et binding.
- **Services** : logique applicative transverse (export, sync, dialogue, navigation).
- **DAL/Repository** : accès aux données via EF Core.
- **Base de données** : MySQL/MariaDB pour la persistance métier.

Choix d’architecture retenus :
- séparation des responsabilités,
- testabilité améliorée,
- réutilisabilité des services,
- facilité de maintenance et d’évolution.

## Conclusion
La préparation du projet formalise les besoins, la méthodologie de pilotage et les choix techniques nécessaires à une implémentation réussie. Cette phase constitue une base solide pour passer à la réalisation détaillée et à la validation fonctionnelle du système.
