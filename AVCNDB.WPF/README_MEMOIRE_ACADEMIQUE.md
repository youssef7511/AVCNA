# CHAPITRE I. CADRE GENERAL DU PROJET / PRESENTATION GENERALE

## Introduction
Dans le secteur de la sante, la donnee medicamenteuse joue un role critique dans la qualite des soins, la securite therapeutique et l'efficacite operationnelle. Lorsqu'elle est dispersee entre plusieurs fichiers ou geree sans regles homogenes, le risque d'erreurs augmente rapidement: doublons, informations contradictoires, mises a jour partielles, et difficulte de controle.

Le projet AVICENNA DB (AVCNDB) s'inscrit dans ce contexte. Il propose une application desktop orientee gestion de base de donnees medicamenteuses, avec une architecture technique modulaire, des workflows de saisie/edition robustes, et une strategie claire d'evolution vers des fonctionnalites intelligentes (AI/ML) a moyen terme.

## 1.2 Cadre de Projet

### 1.2.1 Contexte de Projet
Le besoin initial vient d'un constat metier simple: la gestion quotidienne des medicaments (denomination, DCI, labo, forme, voie, posologie, etc.) necessite une base fiable, centralisee et facilement exploitable. Or, dans les pratiques classiques, les donnees sont souvent:
- stockees dans des tableaux bureautiques heterogenes;
- modifiees sans trace claire des operations;
- non synchronisees entre tables de reference et donnees medicaments;
- difficiles a auditer apres plusieurs cycles de mise a jour.

La consequence directe est une perte de temps sur la verification manuelle et un risque accru d'incoherence des informations medicales.

### 1.2.2 Analyse de l'existant

#### 1.2.2.1 Etude de l'existant
L'etude preliminaire a mis en evidence les points suivants:
- Multiplicite des sources: plusieurs fichiers pour des domaines proches.
- Qualite variable des colonnes: noms non standardises et formats differents.
- Saisie non controlee: peu de validation stricte avant integration.
- Maintenance difficile: corrections repetitives et peu industrialisees.
- Faible lisibilite globale: absence d'un point de verite unique.

#### 1.2.2.2 Critiques de l'existant
Les limites majeures identifiees sont:
- **Redondance**: une meme valeur peut exister sous plusieurs variantes.
- **Incoherence**: valeurs non synchronisees entre references et medicaments.
- **Traçabilite insuffisante**: difficile de reconstruire l'historique des changements.
- **Risque metier**: informations inexactes pouvant impacter l'interpretation therapeutique.
- **Scalabilite faible**: le systeme devient instable lorsque le volume augmente.

#### 1.2.2.3 Solution proposee
La solution retenue repose sur une application WPF .NET 8, structuree en couches, avec:
- une interface de consultation/edition ergonomique;
- un modele de donnees centralise sur MySQL/MariaDB (Docker);
- un acces donnees via EF Core + Repository;
- des services metier de synchronisation entre tables;
- un import Excel strict avec validation de structure;
- une base technique evolutive pour futures extensions AI/ML.

Cette solution traite prioritairement la robustesse des flux de donnees avant l'ajout de fonctions avancees.

## 1.3 Methodologies de travail

### 1.3.1 Les Methodologies agile : La methodologie SCRUM
Le projet est mene avec une logique iterative proche de Scrum:
- decoupage des objectifs en increments fonctionnels;
- priorisation continue selon les retours metier;
- livraisons progressives (socle, CRUD, sync, import/export, UX);
- stabilisation rapide des anomalies par cycle court.

Cette approche a permis de maintenir un bon compromis entre vitesse d'execution et qualite technique.

### 1.3.2 Langages de modelisation
La modelisation sert a clarifier:
- les besoins fonctionnels et non fonctionnels;
- les acteurs et leurs interactions avec le systeme;
- la frontiere entre logique metier, logique technique et persistance.

L'objectif n'est pas uniquement documentaire: la modelisation guide les choix d'implementation (couches, responsabilites, points de controle).

## Conclusion
Le cadre general confirme que la valeur du projet AVCNDB repose d'abord sur la qualite de la base de donnees et la maitrise des flux de mise a jour. Les choix de stack, d'architecture et de methodologie etablissent une fondation solide pour les phases de preparation et d'evolution vers des fonctions d'aide intelligente.

---

# CHAPITRE 2. PREPARATION DE PROJET

## Introduction
Ce chapitre formalise la preparation du projet sous quatre angles: besoins, pilotage, environnement et architecture. Il integre egalement l'orientation future AI/ML, en precisant comment ces fonctions seront ajoutees sans casser le socle applicatif actuel.

## 2.1 Capture du Besoin

### 2.1.1 Specifications des besoins

#### 2.1.1.1 Specifications des Besoins Fonctionnels
Le systeme doit couvrir les fonctions suivantes:
- Gestion CRUD des medicaments.
- Gestion CRUD des references (DCI, Familles, Labos, Formes, Voies, Specialites, Presents, Poso).
- Recherche, filtrage et tri pour accelerer l'acces aux informations.
- Import/Export Excel avec regles strictes sur les colonnes.
- Synchronisation metier lors des renommages/suppressions des references.
- Mise a jour coherente de l'UI apres operations critiques.
- Exploitation de la base dans un environnement Docker reproductible.

#### 2.1.1.2 Specification des Besoins non fonctionnels
Le systeme doit satisfaire:
- **Performance**: operations courantes fluides sur un volume significatif.
- **Fiabilite**: coherence des donnees et reduction des ecarts entre tables.
- **Robustesse**: gestion explicite des erreurs et feedback utilisateur clair.
- **Maintenabilite**: architecture en couches et services injectables.
- **Evolutivite**: capacite d'integrer des modules AI/ML de maniere controlee.
- **Lisibilite UI**: coherence visuelle et parcours de saisie clairs.

### 2.1.2 Modelisation des besoins

#### 2.1.2.1 Identification des acteurs
Acteurs principaux:
- **Utilisateur metier**: gere les references et les medicaments, importe/exporte les donnees, corrige les ecarts.
- **Administrateur technique**: configure l'environnement (DB, Docker), supervise l'integrite, et accompagne les evolutions.

#### 2.1.2.2 Diagramme de cas d'utilisation global
Le cas d'utilisation global (decrit textuellement) inclut:
- consulter/rechercher des donnees medicamenteuses;
- creer/modifier/supprimer des entites;
- importer un fichier Excel et controler sa conformite;
- exporter des donnees pour diffusion/analyse;
- synchroniser les references avec les enregistrements medicaments;
- preparer des extensions analytiques futures.

## 2.2 Pilotage du Projet avec Scrum

### 2.2.1 Equipe et role
Organisation type:
- **Role produit**: priorise les besoins metier.
- **Role coordination**: suit l'avancement et fluidifie les iterations.
- **Role developpement**: implemente, teste et stabilise les modules.

### 2.2.2 Le Backlog du produit
Le backlog est structure autour de themes:
- Qualite CRUD et synchronisation.
- Stabilite import/export Excel.
- Alignement UI/VM/DB.
- Maintenance Docker/SQL.
- Preparation de la couche AI/ML.

### 2.2.3 Planification de Release [ou Sprint ]
Plan de release progressif:
- **Sprint 1**: socle technique, navigation, DI, configuration.
- **Sprint 2**: CRUD principal et references.
- **Sprint 3**: import/export strict + synchronisation metier.
- **Sprint 4**: optimisation UI/UX, robustesse, documentation.
- **Sprint 5 (cible future)**: pre-integration AI/ML en mode assiste.

## 2.3 Environnement de travail

### 2.3.1 Environnement materiel
- Poste Windows de developpement.
- Ressources suffisantes pour IDE + runtime .NET + Docker + MySQL.

### 2.3.2 Environnement Logiciel

#### 2.3.2.1 Outils de developpement et modelisation :
- Visual Studio / VS Code.
- Git + plateforme de versioning.
- Docker Desktop.
- Outils de documentation/planification.

#### 2.3.2.2 Langages de programmation
- C# (logique applicative et metier).
- XAML (presentation WPF).
- SQL (persistence et scripts de migration).

#### 2.3.2.3 Framework utilise
- .NET 8 WPF.
- CommunityToolkit.Mvvm.
- EF Core + Pomelo MySQL.
- MaterialDesignInXAML.
- ClosedXML.
- Serilog.

## 2.4 Architecture
L'architecture est organisee pour separer clairement les responsabilites:
- **Presentation**: vues WPF et experience utilisateur.
- **Presentation logique**: ViewModels (etat, commandes, orchestration).
- **Services**: synchronisation, import Excel, navigation, dialogues.
- **Acces donnees**: Repository + AppDbContext EF Core.
- **Persistance**: MySQL/MariaDB dans Docker.

Cette architecture permet d'introduire des fonctions avancees sans fragiliser le noyau metier.

### 2.4.1 Apercu AI/ML (orientation future, non encore activee)
L'AI/ML est prevue comme **couche d'assistance**, pas comme remplacement du controle metier. Le principe est:
- conserver les validations strictes existantes;
- ajouter une pre-analyse intelligente en amont;
- exiger une validation humaine quand la confiance est insuffisante;
- tracer toutes les propositions AI pour audit.

### 2.4.2 Notation AI/ML - Import intelligent d'Excel
Objectif: reconnaitre automatiquement les colonnes et mapper correctement les enregistrements medicaments.

Fonctions cibles:
- detection de variantes de noms de colonnes (alias, synonymes, fautes frequentes);
- normalisation des valeurs (noms, abreviations, unites, formats);
- scoring de correspondance avec la base existante (match exact, match probable, conflit);
- proposition d'action: inserer, mettre a jour, fusionner, ignorer.

Sortie attendue:
- une integration plus rapide de fichiers externes,
- moins d'erreurs de mapping,
- conservation de la gouvernance metier grace a la validation finale.

### 2.4.3 Notation AI/ML - Modele d'analyse des interactions (DCI x Formes)
Objectif: etendre l'analyse des interactions en croisant non seulement les DCI, mais aussi la forme galenique et le contexte de combinaison.

Approche cible:
- extraction de caracteristiques (paires DCI, groupe de DCI, forme, severite historique);
- modele de classification du niveau de risque;
- generation d'explications exploitables par l'utilisateur (raison, gravite, conduite a tenir).

Valeur metier:
- meilleure priorisation des alertes,
- interpretation plus rapide des associations sensibles,
- support plus coherent pour les actions therapeutiques.

### 2.4.4 Notation AI/ML - Aide a la decision therapeutique
Objectif: fournir un assistant de recommandation base sur les contraintes de traitement.

Principe:
- agreger les signaux (interaction, contre-indication, forme, contexte patient lorsqu'il sera disponible);
- proposer des options therapeutiques ordonnees par pertinence;
- expliciter pourquoi une option est recommandee ou deconseillee.

Regle de gouvernance:
- la decision finale reste humaine;
- le module est un outil d'aide, non un moteur de prescription autonome.

### 2.4.5 Strategie d'integration progressive AI/ML
Plan prudent recommande:
1. Stabiliser la qualite des donnees (schema, contraintes, sync, import strict).
2. Ajouter l'AI en mode lecture/assistance uniquement.
3. Mesurer precision et faux positifs sur un corpus controle.
4. Activer progressivement par seuil de confiance.
5. Industrialiser monitoring, audit, et rollback fonctionnel.

## Conclusion
La preparation du projet consolide une base technique robuste et exploitable immediatement. L'architecture actuelle repond aux besoins operationnels de gestion de base medicamenteuse, tout en ouvrant un chemin realiste vers des fonctionnalites AI/ML explicables, controlees et utiles en pratique.
