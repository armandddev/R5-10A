# Fiche de Révision Complète — R5.A.10 : Nouveaux Paradigmes de Bases de Données
> **Module R5.A.10 (BUT Informatique 3e année)**  
> **Épreuve : 1h QCM / théorie (sans documents) + 2h pratique sur machine (avec aide-mémoire fourni)**

---

## Sommaire
1. [Modalités et Stratégie pour l'Évaluation](#1-modalités-et-stratégie-pour-lévaluation)
2. [Architecture Complète de PixelHub & Cartographie du Projet](#2-architecture-complète-de-pixelhub--cartographie-du-projet)
3. [Séance 1 : Panorama, Théorème CAP, ACID/BASE & PostgreSQL](#3-séance-1--panorama-théorème-cap-acidbase--postgresql)
4. [Séance 2 : MongoDB & Le Modèle Document](#4-séance-2--mongodb--le-modèle-document)
5. [Séance 3 : Redis, Structures en Mémoire & Stratégies de Cache](#5-séance-3--redis-structures-en-mémoire--stratégies-de-cache)
6. [Séance 4 & 4 bis : Neo4j, Graphe Social & Architecture à 4 Moteurs](#6-séance-4--4-bis--neo4j-graphe-social--architecture-à-4-moteurs)
7. [Boîte à Outils des 4 Consoles (Commandes & Syntaxes Types)](#7-boîte-à-outils-des-4-consoles-commandes--syntaxes-types)
8. [Les 10 Pièges Silencieux du Module](#8-les-10-pièges-silencieux-du-module)
9. [Banque de Questions / Réponses pour le QCM](#9-banque-de-questions--réponses-pour-le-qcm)

---

## 1. Modalités et Stratégie pour l'Évaluation

L'évaluation dure **3 heures** au total et se compose de deux parties :

1. **Épreuve écrite (1 h — notée /20)** :
   - QCM et questions courtes sur papier, **sans aucun document ni appareil électronique**.
   - Porte sur l'ensemble de la théorie : comparaison relationnel vs NoSQL, les 4 familles, théorème CAP et PACELC, garanties ACID vs modèle BASE, critères de choix de modélisation, persistance polyglotte.
2. **Épreuve sur machine (2 h — notée /20, coefficient double)** :
   - Sur un dépôt distribué au début de la séance avec vos propres données.
   - Requêtes à taper dans les 4 consoles avec résultats à reporter sur feuille.
   - Code C# ASP.NET Core à trous à compléter (méthodes de repositories ou services).
   - Recherche de pannes et bugs silencieux (ex: cache qui plante, relations manquantes, inversion de tri).
   - Maîtrise de l'architecture : déterminer quel moteur est la source de vérité, quel moteur est une projection, comment rétablir la cohérence.

---

## 2. Architecture Complète de PixelHub & Cartographie du Projet

### 2.1 Schéma de la Persistance Polyglotte

```
                           ┌───────────────────────────────┐
                           │      Client HTTP / Browser    │
                           └───────────────┬───────────────┘
                                           │ JSON (HTTP REST)
                                           ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                                 PixelHub API (ASP.NET Core)                            │
│                                                                                        │
│   Endpoints :                                                                          │
│   - /players, /shop/buy...   - /games, /games/genre...  - /leaderboard..., /online...  │
│   - /social/friends...       - /players/{pseudo}/profile (aggrégation 4 moteurs)       │
│                                                                                        │
│   Services Métier :                                                                    │
│   - ShopService (Orchestre achat : Mongo -> Postgres -> Neo4j + Redis)                 │
│   - PlayerProfileService (Composition : Postgres + Redis + Neo4j + Mongo)             │
│                                                                                        │
│   Couche d'Accès (Repositories / Contexte) :                                          │
│   - PixelHubContext (EF Core)        - CachedGameCatalog -> MongoGameCatalog           │
│   - RedisLeaderboard, RedisSales     - Neo4jSocialGraph                                │
└───────────────┬──────────────────────────┬─────────────────────────┬───────────────────┘
                │                          │                         │                   │
                ▼                          ▼                         ▼                   ▼
    ┌───────────────────────┐  ┌───────────────────────┐ ┌─────────────────────┐ ┌─────────────────────┐
    │      PostgreSQL       │  │        MongoDB        │ │        Redis        │ │        Neo4j        │
    │  (Port hôte: 15432)   │  │  (Port hôte: 27018)   │ │ (Port hôte: 16379)  │ │ (Port hôte: 17474)  │
    ├───────────────────────┤  ├───────────────────────┤ ├─────────────────────┤ ├─────────────────────┤
    │ Rôle :                │  │ Rôle :                │ │ Rôle :              │ │ Rôle :              │
    │ - Source de vérité    │  │ - Source de vérité    │ │ - Cache du catalogue│ │ - Graphe social     │
    │   comptes joueurs     │  │   catalogue de jeux   │ │ - Leaderboard score │ │ - Amis & guildes    │
    │ - Solde de monnaie    │  │ - Schéma flexible     │ │ - File matchmaking  │ │ - Recommandations   │
    │ - Traçabilité achats  │  │ - Métadonnées riches  │ │ - Meilleures ventes │ │ - Possession jeux   │
    │   (Purchases - ACID)  │  │   par genre           │ │   (projection ZSET) │ │   (projection)      │
    └───────────────────────┘  └───────────────────────┘ └─────────────────────┘ └─────────────────────┘
```

---

### 2.2 Rôle de chaque dossier et sous-dossier

| Dossier / Chemin | Rôle et But architectural |
|---|---|
| `data/` | Contient les scripts et données d'initialisation brutes : `games.json` (importé dans MongoDB) et `graph-import.cypher` (importé dans Neo4j). |
| `src/` | Racine du code source C# de la solution. |
| `src/PixelHub.Api/` | Projet principal ASP.NET Core Minimal API. |
| `src/PixelHub.Api/Data/` | Couche d'infrastructure relationnelle : contient `PixelHubContext.cs` (Entity Framework Core) pour PostgreSQL. |
| `src/PixelHub.Api/Models/` | Entités métier et DTO de l'application : `Player.cs` (joueur SQL), `Game.cs` (jeu Mongo), `Purchase.cs` (achat SQL), convertisseurs JSON. |
| `src/PixelHub.Api/Repositories/` | Abstraction de l'accès aux données : interfaces (`IGameCatalog`, `ISocialGraph`, `ILeaderboard`) et leurs implémentations concrètes (`MongoGameCatalog`, `CachedGameCatalog`, `Neo4jSocialGraph`, `RedisLeaderboard`, `RedisSales`, `RedisMatchmakingQueue`). |
| `src/PixelHub.Api/Services/` | Couche métier orchestrant plusieurs moteurs : `ShopService.cs` (gère le processus d'achat à travers Mongo, Postgres, Neo4j, Redis) et `PlayerProfileService.cs` (compose la fiche joueur complète). |
| `src/PixelHub.Api/Properties/` | Paramètres de démarrage locaux (`launchSettings.json` définissant les profils HTTP et le port 5199). |

---

### 2.3 Rôle précis de chaque fichier important

- `docker-compose.yml` : Déclare et configure les 4 conteneurs de bases de données (`postgres`, `mongo`, `redis`, `neo4j`), leurs images officielles, leurs variables d'environnement (identifiants), leurs volumes persistants et le mapping de ports avec la machine hôte.
- `.env` : Définit les variables d'environnement globales lues par docker-compose (ports hôtes comme `POSTGRES_PORT=15432`, `MONGO_PORT=27018`, `REDIS_PORT=16379`, `NEO4J_HTTP_PORT=17474`, `NEO4J_BOLT_PORT=17687`, et mots de passe).
- `Dockerfile` : Recette de conteneurisation de l'API .NET (utilisé si on lance l'application via Docker plutôt qu'en local via `dotnet run`).
- `PixelHub.sln` & `PixelHub.Api.csproj` : Fichiers de solution et de projet .NET, définissant le framework cible (.NET 10.0), les dépendances NuGet (`Npgsql.EntityFrameworkCore.PostgreSQL`, `MongoDB.Driver`, `StackExchange.Redis`, `Neo4j.Driver`) et `<UseAppHost>false</UseAppHost>`.
- `appsettings.json` : Fichier de configuration de l'API. Contient les chaînes de connexion vers les 4 bases via localhost et les ports redirigés par Docker.
- `Program.cs` : Point d'entrée de l'application web. Enregistre les services dans le conteneur d'injection de dépendances (IoC), configure le middleware, initialise les tables/données de base (`EnsureCreated`) et déclare tous les endpoints HTTP.
- `PixelHub.Api.http` : Carnet de requêtes HTTP intégré à l'IDE (remplaçant Swagger) permettant de tester unitairement et dans l'ordre chaque endpoint de l'API.
- `Data/PixelHubContext.cs` : DbContext EF Core reliant les classes C# aux tables PostgreSQL (`Players`, `Purchases`), avec les contraintes SQL (`coins_positifs`, `prix_paye_positif`) et l'index unique `(PlayerId, Titre)`.
- `Models/ObjectIdJsonConverter.cs` : Convertisseur personnalisé `System.Text.Json` pour traduire l'`ObjectId` de MongoDB en chaîne hexadécimale propre et éviter la réinitialisation de l'ID à zéro lors de la désérialisation depuis le cache Redis.
- `Repositories/CachedGameCatalog.cs` : Implémentation du pattern Décorateur. Intercepte les appels vers MongoDB pour servir les lectures depuis Redis si la clé existe, et gère la tolérance aux pannes (fallback vers MongoDB si Redis est éteint).

---

### 2.4 Injection de dépendances : Les cycles de vie (Scope) en C#

Le choix du cycle de vie est capital pour éviter les fuites mémoires ou les bugs de concurrence :

1. **Singleton (`AddSingleton`)** :
   - *Une seule instance partagée* par toute l'application pendant toute sa durée de vie.
   - Utilisé pour les clients lourds, thread-safe et gérant un pool de connexions interne : `IMongoClient`, `IConnectionMultiplexer` (Redis), `IDriver` (Neo4j), ainsi que pour les repositories sans état comme `MongoGameCatalog`, `CachedGameCatalog`, `RedisLeaderboard`, `Neo4jSocialGraph`.
   - **Piège** : Réinstancier un `ConnectionMultiplexer` ou un `IDriver` à chaque requête épuise les sockets réseau de la machine (socket starvation).
2. **Scoped (`AddScoped`)** :
   - *Une instance créée par requête HTTP*, partagée entre tous les composants appelés au cours de cette requête, puis détruite à la fin.
   - Utilisé obligatoirement pour le `PixelHubContext` (EF Core) et pour les services qui l'injectent (`ShopService`, `PlayerProfileService`).
   - **Règle absolue** : On ne peut **JAMAIS** injecter un service `Scoped` (comme `PixelHubContext`) dans un service `Singleton`.
3. **Transient (`AddTransient`)** :
   - Une nouvelle instance à chaque injection. Non utilisé ici car nos services sont sans état ou partagés.

---

## 3. Séance 1 : Panorama, Théorème CAP, ACID/BASE & PostgreSQL

### 3.1 Ce qui a été fait dans le TP 1
- Démarrage des 4 moteurs dans Docker via `docker compose up -d`.
- Vérification individuelle de chaque moteur avec `docker compose exec`.
- Création de la première entité C# `Player` et du contexte EF Core connecté à PostgreSQL.
- Compréhension des ports (hôte vs conteneur) et vérification de la persistance des données.

---

### 3.2 Cours théorique (Séance 1 & CM 1)

#### 1. Pourquoi le relationnel ne suffit pas toujours
Le relationnel (tables, colonnes typées, clés étrangères, SQL) est excellent pour les données structurées et les transactions strictes. Cependant, il rencontre des limites majeures sur 4 cas d'usage :
1. **Le schéma variable** : Exemple du catalogue de jeux. 3 champs communs (`titre`, `genre`, `note`), mais des dizaines d'attributs spécifiques selon le genre (cartes pour FPS, scénarios pour Gestion, etc.).
   - *Table unique* : 70% de cases vides (NULL), `ALTER TABLE` requis en production à chaque nouveau genre.
   - *Table par genre* : Requiert des `UNION` coûteuses et complexes pour faire un simple tri global par note.
   - *Entity-Attribute-Value (EAV)* : Stocker `(jeu_id, cle, valeur)` détruit le typage (`valeur` est un VARCHAR), empêche les comparaisons numériques et multiplie les jointures.
2. **Les jointures en profondeur** : Requêtes de type « amis d'amis » ou graphes de recommandations. Chaque degré de relation ajoute une jointure SQL (`JOIN`), ce qui devient illisible et très lent au-delà de 2 ou 3 sauts.
3. **La montée en charge horizontale (Scale-out)** :
   - *Scale-up (Vertical)* : Agrandir la machine (RAM, CPU). Simple mais plafond matériel et coût exponentiel.
   - *Scale-out (Horizontal)* : Multiplier les petites machines. Le relationnel a du mal à se répartir car les jointures inter-serveurs et les verrous distribués coûtent très cher.
4. **Les données éphémères à accès intensif** : Ex. 500 mises à jour par seconde pour un nombre de joueurs en ligne ou une file d'attente. Écrire sur disque via un `INSERT` / `UPDATE` SQL classique est un gâchis de ressources disque.

#### 2. Les 4 grandes familles NoSQL

| Famille | Unité stockée | Mode d'interrogation | Moteur d'exemple | Cas d'usage type |
|---|---|---|---|---|
| **Clé-valeur** | Valeur opaque (binaire, texte) | Uniquement par sa clé exacte | Redis, DynamoDB | Cache, sessions, compteurs, classements |
| **Document** | Document semi-structuré (JSON/BSON) | Par n'importe quel champ interne | MongoDB, CouchDB | Catalogues, profils utilisateurs, CMS |
| **Graphe** | Nœuds, Relations orientées, Propriétés | Motifs de chemins et voisinages | Neo4j, ArangoDB | Réseaux sociaux, recommandation, détection de fraude |
| **Colonnes larges** | Lignes à colonnes variables par partition | Clé de partition (+ clustering key) | Cassandra, ScyllaDB, HBase | Logs massifs, séries temporelles, télémétrie IoT |

#### 3. Le Théorème CAP (Brewer)
Dans tout système distribué (données réparties sur plusieurs machines), trois propriétés existent :
- **C (Consistency / Cohérence)** : Toute lecture renvoie l'écriture la plus récente. Tous les utilisateurs voient la même donnée en même temps.
- **A (Availability / Disponibilité)** : Toute requête reçoit une réponse sans erreur, même si certaines machines sont en panne.
- **P (Partition Tolerance / Tolérance au partitionnement)** : Le système continue de fonctionner même en cas de coupure de communication réseau entre les nœuds.

> [!CAUTION]
> **L'erreur classique à proscrire absolument à l'écrit** : « On peut choisir 2 propriétés parmi les 3 ».  
> **C'est faux !** Le réseau n'étant jamais fiable à 100 %, les coupures surviennent inévitablement. **P n'est pas une option, c'est une contrainte physique.**  
> Le vrai choix s'impose **uniquement en cas de coupure réseau** :
> - **Soit CP** : On préserve la cohérence en refusant de répondre (ou en attendant la réconciliation), quitte à perdre la disponibilité.
> - **Soit AP** : On répond immédiatement avec la copie locale (potentiellement périmée), quitte à perdre la cohérence.

#### 4. Le modèle PACELC
Extension du théorème CAP pour qualifier le comportement du système en régime normal :
- **Si Partition (P)** : Arbitrer entre **A**vailability et **C**onsistency.
- **Else (E - quand tout va bien)** : Arbitrer entre **L**atency et **C**onsistency (répondre ultra vite ou attendre la réplication synchrone sur tous les nœuds).

#### 5. Deux contrats opposés : ACID vs BASE

| Propriété ACID (Relationnel classique) | Propriété BASE (Systèmes distribués / NoSQL) |
|---|---|
| **A**tomicité : Tout ou rien (rollback intégral si échec). | **B**asically **A**vailable : Le système répond toujours, quitte à être en mode dégradé. |
| **C**ohérence : Respect absolu des contraintes de base (clés, CHECK). | **S**oft state : L'état du système peut fluctuer sans écriture directe, par propagation. |
| **I**solation : Les transactions concurrentes ne se parasitent pas. | **E**ventual consistency : En l'absence de nouvelles écritures, tous les nœuds finissent par converger vers la même valeur au bout d'un certain temps. |
| **D**urabilité : Une fois validée (`COMMIT`), la donnée ne peut être perdue. | |

> **Exemple fondamental** : Un retard de 2 secondes sur l'affichage d'un message dans un chat ou le score d'un classement est anodin (BASE / Eventual consistency suffit). En revanche, un retard de 2 secondes sur un solde bancaire ou un achat de monnaie permet à un joueur de cliquer 2 fois et d'acheter 2 objets avec le même argent (double dépense) : **ACID est obligatoire pour les finances**.

---

### 3.3 Analyse critique des Q/R du TP 1 (Corrections des erreurs)

- **Q1 (Pourquoi 4 services dans docker-compose dès le début ?)** : Pour préparer l'infrastructure complète du semestre. Chaque TP activera un moteur de plus sans avoir à modifier le fichier Docker ni risquer de casser l'existant.
- **Q2 (Volumes persistants — Attention piège !)** :
  - *Erreur commise* : Penser que les 4 services ont des volumes.
  - *Correction* : PostgreSQL, MongoDB et Neo4j ont des volumes déclarés (`postgres_data`, `mongo_data`, `neo4j_data`). **Redis n'a AUCUN volume !** Ses données vivent exclusivement en RAM dans le conteneur ; si on stoppe le conteneur Redis, ses clés non sauvegardées disparaissent.
- **Q3 (Mapping de ports `"15432:5432"` — Attention inversion !)** :
  - *Erreur fréquente* : Inverser les ports.
  - *Correction* : La syntaxe Docker est toujours `HÔTE:CONTENEUR`. Le port **15432** est le port d'écoute sur notre machine physique (hôte Windows), et **5432** est le port interne officiel par défaut de PostgreSQL dans le conteneur Linux.
- **Q4 (Mots de passe en clair)** : Acceptable en environnement de développement local (TP), totalement inacceptable en production (il faut utiliser des variables d'environnement secrètes ou des gestionnaires de secrets comme Docker Secrets / Vault).
- **Q5 (`docker compose exec`)** : Exécute un processus à l'intérieur d'un conteneur Linux en cours d'exécution. La commande `redis-cli` tourne donc dans l'espace isolé du conteneur Redis.
- **Q6 (`Host=localhost` dans la chaîne de connexion)** : Fonctionne car le port du conteneur est publié (forwardé) sur l'interface de boucle locale (`localhost`) de la machine hôte via le port 15432.
- **Q7 (Redémarrage de l'API & doublons)** : Les joueurs ne sont pas dupliqués car `Program.cs` effectue le test `if (!db.Players.Any())` avant d'insérer. L'insertion initiale n'a lieu que si la table est vide.

---

## 4. Séance 2 : MongoDB & Le Modèle Document

### 4.1 Ce qui a été fait dans le TP 2
- Import de données JSON dans MongoDB via `mongoimport`.
- Manipulation directe dans le shell `mongosh` (opérateurs de mise à jour, filtres).
- Découverte du *schema-on-read* et des conséquences d'un schéma non contrôlé par le SGBD.
- Intégration du driver C# `MongoDB.Driver`, création du modèle `Game` et du repository `MongoGameCatalog`.
- Écriture d'une agrégation par genre avec calcul de note moyenne.

---

### 4.2 Cours théorique (Séance 2)

#### 1. Le modèle orienté Document
- L'unité de stockage est le **Document**, sérialisé en **BSON** (Binary JSON), regroupé dans des **Collections**.
- Contrairement au JSON textuel, le BSON ajoute des types enrichis : `ObjectId`, `Date`, `int32`, `int64`, `double`, `Binary`.
- Pas de jointures obligatoires : un agrégat métier complet (ex: un jeu avec sa liste de plateformes et ses tags) réside dans un seul document.

#### 2. Imbriquer (Embed) vs Référencer (Reference)
- **Imbriquer** : Placer les sous-documents ou tableaux directement dans le document parent.
  - *Quand l'utiliser* : Relation 1-to-few, données toujours lues ensemble, cycle de vie lié (ex: les tags d'un jeu, les adresses d'un utilisateur).
  - *Avantage* : Lecture atomique en une seule requête, aucune jointure.
- **Référencer** : Stocker l'ID de l'autre document (façon clé étrangère).
  - *Quand l'utiliser* : Relation 1-to-many avec croissance infinie (ex: commentaires d'une vidéo), données accédées séparément, entités partagées.
  - *Attention* : Pas d'intégrité référentielle automatique garantie par le moteur.

#### 3. *Schema-on-write* vs *Schema-on-read*
- **Relationnel = Schema-on-write** : La structure des données est validée par la base de données au moment de l'écriture (`INSERT`/`UPDATE`). Impossible d'écrire un champ inconnu.
- **MongoDB = Schema-on-read** : La base accepte n'importe quelle structure JSON. **Le schéma ne disparaît pas, il est déplacé dans le code de l'application.**
- **Le danger** : Deux développeurs peuvent nommer un champ différemment (`nbVotes` vs `nombre_votes`), ou insérer des types incompatibles. La rigueur repose entièrement sur l'application.

---

### 4.3 Analyse critique des Q/R du TP 2 (Corrections des erreurs)

- **Q1 (`_id` et `ObjectId`)** :
  - *Fabrication* : L'`ObjectId` est fabriqué **côté client** par l'outil (`mongoimport` ou le driver C#) avant l'envoi de la requête au serveur.
  - *Comparaison SQL* : Dans PostgreSQL, la clé primaire auto-incrémentée (`SERIAL` / `IDENTITY`) est calculée côté serveur lors de l'insertion et n'est connue du client qu'après l'exécution.
  - *Anatomie d'un ObjectId (12 octets)* : Contient un horodatage (timestamp sur 4 octets), un identifiant de machine/processus (5 octets) et un compteur incrémental (3 octets).
- **Q2 (Recherche dans un tableau `{ plateformes: "Switch" }`)** :
  - En MongoDB, interroger un tableau avec une valeur scalaire cherche automatiquement si la valeur est présente dans la liste.
  - En SQL normalisé, il aurait fallu une table intermédiaire `Jeu_Plateforme(jeu_id, plateforme_id)` et une jointure explicite (`JOIN Plateforme ON ... WHERE nom = 'Switch'`).
- **Q3 (`--authenticationDatabase admin`)** :
  - L'utilisateur `pixelhub` a été provisionné dans la base système `admin` lors de l'initialisation de Docker. Même si on travaille sur la base `pixelhub`, l'authentification doit cibler la base où les identifiants sont stockés.
- **Q4 (Souplesse de schéma : bonne nouvelle ou danger ?)** :
  - C'est une arme à double tranchant. Idéal pour prototyper et pour accueillir des données hétérogènes. Mais en équipe, sans gouvernance, cela génère de la « dette de données » avec des documents aux schémas hétéroclites impossibles à désérialiser de manière cohérente.
- **Q5 (`db.Jeux.find()` avec majuscule — Piège silencieux !)** :
  - *Erreur fréquente* : Croire que MongoDB renvoie une erreur disant que la table n'existe pas.
  - *Réalité* : MongoDB est **sensible à la casse**. `db.Jeux` cible la collection `Jeux` (avec majuscule). En lecture, une collection inconnue n'est pas créée et renvoie simplement **un curseur vide (0 document), sans AUCUNE erreur !**
- **Q6 (`updateOne` sans opérateur `$set`)** :
  - *Erreur* : `MongoInvalidArgumentError: Update document requires atomic operators`.
  - MongoDB refuse car une syntaxe comme `updateOne({ titre: "Valorant" }, { note: 4.4 })` écraserait la totalité du document pour ne laisser que le champ `note` !
  - Pour modifier un champ : utiliser les opérateurs atomiques (`$set`, `$inc`, `$push`, `$unset`). Pour remplacer tout le document : utiliser explicitement `db.jeux.replaceOne(...)`.
- **Q7 (`ALTER TABLE` en SQL)** :
  - `ALTER TABLE jeux ADD COLUMN nb_votes INTEGER;` ajoute la colonne à **toutes les lignes existantes** (avec la valeur `NULL` ou une valeur par défaut). En MongoDB, seul le document ciblé par l'`updateOne` acquiert le nouveau champ.
- **Q8 (Découplage de `IGameCatalog`)** :
  - L'interface ne mentionne ni Mongo ni Bson. Cela respecte le principe d'inversion des dépendances (SOLID) : le reste de l'application dépend d'une abstraction et ignore si le stockage est en MongoDB, en mémoire ou dans des fichiers.
- **Q9 (Crash au démarrage sur `/games` — Grosse erreur de l'étudiant !)** :
  - *Ce que l'étudiant avait répondu* : « J'ai un code 200 mais je reçois une liste vide. » (**FAUX !**)
  - *La réalité technique* : L'API lève une exception de désérialisation : `FormatException: Element 'joueursParEquipe' does not match any field or property of class PixelHub.Api.Models.Game`.
  - *Cause* : Le driver C# est par défaut strict. Quand il lit les documents de `games.json` contenant des champs non déclarés dans la classe C# `Game` (`joueursParEquipe`, `cartes`...), il ne sait pas où les ranger et lève une exception.
- **Q10 / Q11 (Gestion du schéma variable en C#)** :
  - `[BsonIgnoreExtraElements]` : Ignore silencieusement tous les champs inconnus. Avantage : rapide et robuste. Inconvénient : perte d'informations lors des lectures/écritures.
  - `[BsonExtraElements]` avec un dictionnaire `BsonDocument AutresChamps` : Capture les champs non modélisés. Permet de ne rien perdre, mais complexifie la sérialisation JSON de l'API.
  - *Hiérarchie de classes* (`FpsGame : Game`, `GestionGame : Game`) : Typage fort complet, mais suppose de connaître tous les genres à l'avance et d'enregistrer des discriminateurs de types polymorphiques (`[BsonDiscriminator]`).

---

## 5. Séance 3 : Redis, Structures en Mémoire & Stratégies de Cache

### 5.1 Ce qui a été fait dans le TP 3
- Manipulation dans `redis-cli` des 5 structures de base.
- Mise en place d'un classement compétitif (Leaderboard) via un `Sorted Set` (`ZSET`).
- Implémentation d'une file de matchmaking FIFO via une `List`.
- Mise en cache du catalogue MongoDB dans Redis avec TTL de 60 secondes (Pattern Décorateur `CachedGameCatalog`).
- Mesure des gains de performance et analyse de la tolérance aux pannes.

---

### 5.2 Cours théorique (Séance 3 & PPTX 3)

#### 1. L'architecture Redis : Pourquoi est-ce si rapide ?
- **Tout en RAM** : Pas d'I/O disque bloquant pour répondre à une commande.
- **Modèle mono-threadé événementiel (Event Loop)** :
  - Toutes les opérations s'exécutent les unes après les autres sur un seul thread.
  - **Conséquence positive** : Aucune concurrence interne, aucun verrou de ligne/table à gérer, **chaque commande primitive est intrinsèquement atomique** (ex: `INCR`, `LPUSH`).
  - **Conséquence critique** : Une commande lourde qui met 500 ms à s'exécuter bloque l'intégralité du serveur Redis pour tous les autres clients pendant 500 ms ! C'est pourquoi **`KEYS *` est strictement interdit en production** (on utilise `SCAN`).

#### 2. Les 5 Structures de Données de Redis

| Structure | Description & Commandes Clés | Cas d'Usage Type |
|---|---|---|
| **String** | Valeur binaire ou texte (max 512 Mo). `SET`, `GET`, `INCR`, `INCRBY`. | Cache HTML/JSON, jetons de session, compteurs atomiques. |
| **Hash** | Mini-dictionnaire clé-valeur dans une clé. `HSET`, `HGET`, `HGETALL`, `HINCRBY`. | Fiche utilisateur, objet avec plusieurs champs modifiables unitairement sans tout resérialiser. |
| **List** | Liste doublement chaînée ordonnée par insertion. `LPUSH`, `RPUSH`, `LPOP`, `RPOP`, `LRANGE`. | Files de messages, file d'attente FIFO (Push à gauche, Pop à droite), logs récents. |
| **Set** | Ensemble de chaînes uniques non ordonnées. `SADD`, `SREM`, `SMEMBERS`, `SISMEMBER`. | Tags uniques, amis en commun (opérations d'intersection `SINTER`). |
| **Sorted Set (ZSET)** | Ensemble d'éléments uniques, chacun associé à un score numérique (flottant). `ZADD`, `ZRANGE`, `ZREVRANGE`, `ZREVRANK`, `ZINCRBY`. | Classements (Leaderboards), index temporels glissants. |

#### 3. Gestion de l'expiration : Le TTL (Time To Live)
- Associé à une clé avec `EXPIRE key seconds` ou `SET key val EX seconds`.
- Permet la suppression automatique par Redis sans intervention applicative.
- Valeurs de retour de la commande `TTL key` :
  - Nombre positif : Temps restant en secondes avant suppression.
  - `-1` : La clé existe mais n'a **aucune date d'expiration** (vit indéfiniment).
  - `-2` : La clé **n'existe pas** (ou a déjà expiré).

#### 4. Stratégie de Cache : Cache-Aside (Lazy Loading)
1. L'application reçoit une demande de lecture.
2. Elle interroge Redis (`GET cache:catalogue:tous`).
3. **Cache Hit** : La donnée est présente dans Redis -> Désérialisation et renvoi immédiat.
4. **Cache Miss** : La donnée est absente -> Lecture dans la base primaire (MongoDB), écriture dans Redis avec un TTL (ex: 60s), puis renvoi au client.

#### 5. Le Cache comme Single Point of Failure (SPOF)
- Si l'implémentation du cache n'intercepte pas les exceptions de connexion à Redis, la panne de Redis fait planter l'ensemble de l'API.
- **Principe de dégradation gracieuse (Graceful Degradation)** : Le cache n'est qu'une optimisation de performance. Si Redis tombe, l'API doit basculer de façon transparente sur la base primaire (MongoDB) sans lever d'erreur HTTP 500 pour l'utilisateur.

---

### 5.3 Analyse critique des Q/R du TP 3 (Corrections des erreurs)

- **Q1 (`INCR` atomique vs SQL)** :
  - `INCR` s'exécute en mémoire sur l'event loop mono-threadée de Redis. Même avec 1000 requêtes concurrentes, chaque incrément s'applique l'un après l'autre sans conflit.
  - En SQL, `UPDATE parties SET nb = nb + 1` requiert des verrous de ligne et des écritures dans le journal de transactions (WAL) sur disque, ce qui génère de la contention et des temps de latence bien supérieurs.
- **Q2 (Hash vs String JSON pour un joueur)** :
  - Avec une String JSON : Pour ajouter 250 XP, il faut lire la chaîne, la désérialiser en C#, faire `xp += 250`, la resérialiser et la réécrire avec un `SET`. Si deux requêtes le font en parallèle, la seconde écrase la mise à jour de la première (Lost Update).
  - Avec un Hash : `HINCRBY joueur:7 xp 250` effectue l'opération directement en base de manière atomique.
- **Q3 (`ZREVRANGE` vs `ZRANGE`)** :
  - `ZRANGE` trie par score croissant (du plus petit au plus grand).
  - `ZREVRANGE` trie par score décroissant (du plus grand au plus petit). Pour un top 3 compétitif où le score élevé l'emporte, il faut impérativement `ZREVRANGE`.
- **Q4 (`ZREVRANK`)** :
  - `ZREVRANK` renvoie un rang avec un indice commençant à 0 (le 1er joueur obtient 0). Il faut impérativement faire `rang + 1` avant de l'afficher à l'utilisateur.
- **Q5 (`LPUSH` + `RPOP`)** :
  - Avec `LPUSH`, les nouveaux éléments arrivent en tête (à gauche). `RPOP` retire l'élément à l'opposé (à droite), c'est-à-dire le plus ancien entré : c'est le principe d'une file d'attente FIFO (First In, First Out). `LPOP` aurait dépilé le dernier arrivé (LIFO / Pile).
- **Q6 (Valeurs de retour de `TTL` et `EXISTS`)** :
  - Clé active avec TTL : `TTL` renvoie les secondes restantes, `EXISTS` renvoie `1`.
  - Clé sans TTL : `TTL` renvoie `-1`, `EXISTS` renvoie `1`.
  - Clé expirée / inexistante : `TTL` renvoie `-2`, `EXISTS` renvoie `0`.
- **Q7 (Équivalent du TTL en relationnel)** :
  - En SQL, il faut stocker une colonne `expire_at` et programmer un démon d'arrière-plan (CRON / Worker Service) qui exécute périodiquement `DELETE FROM sessions WHERE expire_at < NOW();`. Risques : charge disque périodique, verrous de tables, sessions mortes visibles entre deux nettoyages.
- **Q8 (`ConnectionMultiplexer` en Singleton)** :
  - Le `ConnectionMultiplexer` gère et multiplexe l'ensemble des connexions vers Redis de façon thread-safe. Si on en créait un par requête HTTP, on saturerait rapidement les ports TCP de l'hôte (Socket Starvation). En TP avec 1 utilisateur, le bug est invisible car le volume de requêtes est très faible.
- **Q9 (Oubli de `Order.Descending` — Piège silencieux !)** :
  - L'API renvoie 200 sans aucune erreur, mais le top affiche les pires scores en premier. C'est un bug très dangereux car aucune exception n'est levée et les tests automatisés basiques (vérifiant juste le status code HTTP) restent au vert.
- **Q10 / Q11 (Joueurs en ligne : Set vs Sorted Set)** :
  - Avec un `Set` : Impossible d'expirer un seul membre d'un ensemble (Redis ne sait expirer qu'une clé entière).
  - Solution `Sorted Set` : Clé `enligne:joueurs`. Chaque joueur est un membre, et le **score est le timestamp Unix** de son dernier signe de vie.
    - *Touch* : `ZADD enligne:joueurs <timestamp_actuel> <pseudo>`
    - *Comptage des joueurs actifs (< 60s)* : `ZCOUNT enligne:joueurs (maintenant - 60) +inf`
    - *Nettoyage des inactifs* : `ZREMRANGEBYSCORE enligne:joueurs -inf (maintenant - 60)`
- **Q12 (Impact architectural du Décorateur)** :
  - **Zéro ligne** modifiée dans les endpoints de `Program.cs`. L'injection de dépendances et le respect de l'interface commune `IGameCatalog` permettent de substituer l'implémentation sans impacter les contrôleurs/endpoints consommateurs.
- **Q14 (Délai de visibilité et invalidation de cache)** :
  - Si un titre est corrigé dans MongoDB, les utilisateurs verront l'ancien titre jusqu'à expiration du TTL (60 s).
  - Solutions :
    1. Réduire le TTL : Données plus fraîches, mais plus de pression sur MongoDB.
    2. Invalidation explicite (`DEL cache:catalogue:tous`) lors des écritures : Données à jour immédiatement, mais complexité de couplage entre l'écriture et le cache.

---

## 6. Séance 4 & 4 bis : Neo4j, Graphe Social & Architecture à 4 Moteurs

### 6.1 Ce qui a été fait dans le TP 4 & 4 bis
- Import du graphe dans Neo4j (joueurs, jeux, guildes, relations `AMI_DE`, `POSSEDE`, `MEMBRE_DE`).
- Écriture de requêtes Cypher de navigation (amis, recommandations d'amis et de jeux par saut de graphe).
- Intégration du driver .NET `Neo4j.Driver` dans l'API.
- Implémentation de la boutique PixelHub touchant les 4 moteurs simultanément :
  1. Lecture du prix dans MongoDB.
  2. Transaction financière ACID dans PostgreSQL (vérification du solde, débit atomique, création de la ligne `Purchases`).
  3. Mise à jour de la possession dans Neo4j (`AddOwnershipAsync`).
  4. Incrémentation des meilleures ventes dans Redis (`RecordSaleAsync`).
- Création de la fiche joueur complète agrégeant les 4 bases en un seul appel REST.
- Simulation de pannes (arrêt de Neo4j, arrêt de Redis), constat des incohérences et réparation via la route de resynchronisation `/admin/resync`.

---

### 6.2 Cours théorique (Séance 4 & PPTX 4)

#### 1. Le modèle Graphe orienté Propriétés
- Composé de **Nœuds** (entités portant des étiquettes / *labels* et des propriétés clé-valeur) et de **Relations** (liens typés, orientés et portant des propriétés).
- **L'adjacence sans index (Index-Free Adjacency)** :
  - Dans un moteur relationnel, une jointure utilise un index (coût logarithmique `O(log N)` par rapport au volume total de la table).
  - En Neo4j, chaque nœud détient des pointeurs mémoire directs vers ses voisins. **Le coût de parcours dépend uniquement du voisinage local du nœud (son degré), et non de la taille globale de la base de données.** Parcourir les amis de Nova prend le même temps qu'il y ait 100 nœuds ou 100 millions de nœuds en base.

#### 2. Règle de modélisation : Nœud ou Propriété ?
- **La règle d'or** : *« Est-ce que je veux traverser cette information pour naviguer vers d'autres entités ? »*
  - Si **OUI** -> C'est un **Nœud** (ex: le `Genre`, si l'on veut relier des joueurs qui partagent les mêmes genres).
  - Si **NON** -> C'est une **Propriété** (ex: le `pseudo`, la `note` d'un jeu, ou l'année d'achat).
- Une relation exprime toujours un lien entre deux nœuds et peut avoir des attributs propres (ex: `-[r:POSSEDE {depuis: 2024}]->`).

#### 3. Le langage Cypher (ASCII-art)
- Nœud : `(variable:Label {propriete: 'valeur'})`
- Relation orientée : `-[variable:TYPE_RELATION]->`
- Relation non-orientée en lecture : `-[:AMI_DE]-`
- Longueur de chemin variable : `-[:AMI_DE*1..3]-` (de 1 à 3 sauts).

#### 4. `CREATE` vs `MERGE`
- `CREATE` : Crée systématiquement un nouveau nœud ou une nouvelle relation, même s'il existe déjà un élément identique.
- `MERGE` : Recherche si le motif existe déjà. S'il existe, il le réutilise ; sinon, il le crée.
- `MERGE` garantit **l'idempotence** des scripts d'importation et des projections.

#### 5. Orientations et conventions en Cypher
- **Au stockage** : En Neo4j, une relation a **TOUJOURS** une direction obligatoire lors de sa création (`(a)-[:AMI_DE]->(b)`).
- **À l'interrogation** : Pour une relation conceptuellement symétrique (l'amitié), on interroge **SANS flèche** (`-[:AMI_DE]-`) pour parcourir le lien dans les deux sens.
- Conventions : `PascalCase` pour les labels de nœuds (`:Joueur`, `:Jeu`), `MAJUSCULES_AVEC_UNDERSCORE` pour les types de relations (`:AMI_DE`, `:POSSEDE`).

---

### 6.3 Théorie de la Cohabitation & Architecture à 4 Moteurs

#### 1. Source de Vérité vs Projections
Dans un système polyglotte, chaque donnée doit avoir **un et un seul propriétaire officiel** (le système d'enregistrement / *System of Record*) :
- **Source de Vérité** : La base qui fait foi sur le plan métier et contractuel.
  - PostgreSQL est la source de vérité des identités, des soldes de monnaie et des achats légaux (`Purchases`).
  - MongoDB est la source de vérité des fiches et des prix catalogue des jeux.
- **Projections** : Données dérivées, optimisées pour des interrogations spécifiques.
  - La relation `:POSSEDE` dans Neo4j n'est qu'une projection de la table SQL `Purchases`, optimisée pour les calculs de recommandation.
  - Le sorted set `ventes:jeux` dans Redis n'est qu'une projection du comptage groupé des lignes de `Purchases`.
  - La clé `cache:catalogue:tous` dans Redis n'est qu'une projection temporaire du catalogue MongoDB.

#### 2. Principe de Reconstructibilité et Idempotence
Toute projection doit être **intégralement reconstructible** à partir de la source de vérité.
- Si Neo4j ou Redis se désynchronise suite à un crash, un batch comme `/admin/resync` relit la table `Purchases` de PostgreSQL pour réécrire les relations Neo4j et recalculer les classements Redis.
- Pour que cette opération puisse être rejouée sans danger, elle doit être **idempotente** (utiliser `MERGE` dans Neo4j et écraser/purger la clé dans Redis avant réinsertion).

#### 3. Gestion des Défaillances partielles lors d'un Achat
Que faire si la base transactionnelle valide l'achat mais que la mise à jour des projections échoue ?
- **Interdit** : Annuler la transaction PostgreSQL ou renvoyer une erreur HTTP 500. Le joueur a été débité et la facture existe : sur le plan métier, l'achat a eu lieu.
- **Bonne pratique** : Valider la transaction PostgreSQL, intercepter l'échec des projections (try/catch), logger l'incident, et renvoyer une réponse HTTP 200 en indiquant `projectionsAJour: false`. La cohérence sera rétablie en tâche de fond (Eventual Consistency via `/admin/resync`).

---

### 6.4 Analyse critique des Q/R du TP 4 & 4 bis (Corrections des erreurs)

- **Q1 (`CREATE` vs `MERGE` et `CREATE CONSTRAINT`)** :
  - Lancer deux fois un script avec `CREATE` dupliquerait tous les nœuds et fausserait tous les comptages. `MERGE` évite les doublons.
  - Les contraintes d'unicité (`CREATE CONSTRAINT ... REQUIRE j.pseudo IS UNIQUE`) servent de verrou absolu contre les doublons et créent automatiquement un index sous-jacent qui accélère les recherches du `MERGE`.
- **Q2 (Direction de la relation d'amitié — Piège silencieux !)** :
  - `MATCH (:Joueur {pseudo:'Krayz'})-[:AMI_DE]-(a)` renvoie **2 amis** (Nova et Pixel).
  - `MATCH (:Joueur {pseudo:'Krayz'})-[:AMI_DE]->(a)` renvoie **1 seul ami** (Pixel).
  - *Pourquoi aucune erreur ?* Parce que syntaxiquement la requête est valide. Mais comme l'amitié Krayz-Nova a été stockée dans le sens Nova -> Krayz, la requête avec flèche sortante ignore Nova. Pour une relation symétrique, il faut impérativement omettre la flèche.
- **Q3 (`count(DISTINCT ami)` vs `count(*)`)** :
  - `count(*)` compte les chemins : si deux amis intermédiaires possèdent le même jeu, ce jeu est compté deux fois.
  - `count(DISTINCT ami)` compte le nombre d'amis uniques qui possèdent le jeu.
  - Passer de `*1..2` à `*1..4` ne change qu'un seul chiffre en Cypher. En SQL, cela aurait exigé d'ajouter deux jointures récursives ou CTE supplémentaires.
- **Q4 (*Stardew Valley* n'apparaît pas pour Nova)** :
  - Parce que Nova le possède déjà (`WHERE NOT (moi)-[:POSSEDE]->(j)`).
- **Q5 (Deux ports pour Neo4j : 17474 et 17687)** :
  - 17474 est le port HTTP pour l'interface d'administration web (Neo4j Browser).
  - 17687 est le port binaire protocole **Bolt** utilisé par les drivers applicatifs C#.
- **Q6 (Paramètre `$pseudo` vs Interpolation `{profondeur}`)** :
  - La grammaire Cypher n'autorise pas les paramètres sur les plages de longueurs de relations (`*1..$p` est interdit par le parseur). On doit donc interpoler en chaîne C#.
  - *Précaution vitale* : Valider strictement que `profondeur` est comprise entre 1 et 4 pour éviter toute attaque par injection Cypher ou surcharge exponentielle du moteur.
- **Q7 (`IDriver` en singleton vs Session scoped)** :
  - `IDriver` gère le pool de connexions (lourd, thread-safe -> Singleton).
  - La session (`IAsyncSession`) représente une unité logique de travail éphémère (légère, non thread-safe -> instanciée avec `await using` et détruite après la transaction). Règle identique à `MongoClient` et `ConnectionMultiplexer`.
- **Q8 (Guillemets doubles dans PostgreSQL)** :
  - Sans guillemets, PostgreSQL convertit automatiquement tous les identifiants en minuscules. Comme EF Core crée les tables et colonnes en PascalCase (`"Players"`, `"Coins"`), les guillemets doubles sont obligatoires pour respecter la casse. `SELECT * FROM players;` échoue car la table `players` en minuscules n'existe pas.
- **Q9 (`UNIQUE` et `CHECK coins_positifs`)** :
  - `UNIQUE ("PlayerId", "Titre")` empêche qu'un joueur n'achète deux fois le même jeu.
  - `CHECK ("Coins" >= 0)` garantit que le solde ne passera jamais en négatif.
  - Tenter d'affecter `-1` provoque une violation de contrainte CHECK bloquée par PostgreSQL.
- **Q10 (Mise à jour des prix dans MongoDB)** :
  - Aucun document n'a été réécrit en entier : `$set` opère une modification ciblée in-place.
  - Si la 1ère commande (prix = 800) était lancée en dernier, elle écraserait toutes les règles spécifiques précédentes.
- **Q11 (Cache désynchronisé après mise à jour)** :
  - La clé Redis contient l'ancien JSON. L'API servira le catalogue sans prix pendant 60 s maximum (durée du TTL). On peut purger immédiatement via `DEL cache:catalogue:tous`.
- **Q13 (Incohérence Luna)** :
  - Luna existe dans Neo4j mais pas dans PostgreSQL. Son achat renvoie 404 car PostgreSQL est le référentiel des comptes utilisateurs.
- **Q14 (`ExecuteUpdateAsync` vs lecture/test en C# — Piège de concurrence)** :
  - Traduit en SQL : `UPDATE "Players" SET "Coins" = "Coins" - @prix WHERE "Id" = @id AND "Coins" >= @prix;`
  - Tout est exécuté de manière atomique dans PostgreSQL avec verrouillage de la ligne. La méthode naïve en C# (lire, tester `if`, puis soustraire) crée une **race condition** : deux requêtes parallèles lisent le même solde, valident le test et appliquent deux débits alors que les fonds étaient insuffisants pour les deux.
- **Q15 (`AnyAsync` + rattrapage de l'index unique)** :
  - Deux requêtes à la même milliseconde passent toutes deux le test `AnyAsync`. Seul l'index unique au niveau SQL garantit physiquement le rejet de la seconde écriture.
  - On ne teste pas dans Neo4j car Neo4j n'est qu'une projection sans garantie ACID.
- **Q16 (Krayz achète *Valorant*)** :
  - L'achat réussit (200) car PostgreSQL n'avait aucune trace de cet achat dans `Purchases`. PostgreSQL fait foi ; les données injectées manuellement dans le graphe n'étaient pas synchronisées avec les achats réels.
- **Q18 (Nombre de requêtes pour la fiche joueur)** :
  - 4 requêtes au total (1 vers chaque moteur).
  - Pour 200 jeux, `GetByTitlesAsync` ne fait toujours qu'**une seule requête** grâce à l'opérateur MongoDB `$in`, évitant 200 allers-retours réseau.
- **Q20 / Q21 (Crash de Neo4j pendant l'achat — Piège du timeout)** :
  - L'API répond 500 après un délai de **30 secondes** (délai d'attente du driver Neo4j avant expiration de la tentative de connexion).
  - Dans PostgreSQL, le débit a eu lieu et la ligne `Purchases` est créée ! Le joueur croit à tort que l'achat a échoué.
  - S'il réessaie, il reçoit 409 (Jeu déjà acheté) et n'est pas débité une seconde fois grâce à l'index unique.
- **Q23 (Idempotence de `/admin/resync`)** :
  - Rejouable à l'infini sans doublon grâce à `MERGE` dans Neo4j et `ReplaceAllAsync` (qui purge la clé avant réinsertion) dans Redis.
- **Q25 (Panne de Redis et catalogue inaccessible)** :
  - Sans bloc try/catch, l'indisponibilité de Redis bloque la route `/games` alors que MongoDB fonctionne. Le cache est devenu un **SPOF**.
- **Q26 (Tolérance aux pannes : Redis vs PostgreSQL)** :
  - Redis n'héberge que du cache et des statistiques dérivées : on peut masquer son indisponibilité et se replier sur MongoDB.
  - PostgreSQL gère la facturation et les soldes financiers réels : on ne peut pas masquer sa panne, il faut refuser l'opération pour préserver l'intégrité financière.

---

## 7. Boîte à Outils des 4 Consoles (Commandes & Syntaxes Types)

À garder en tête pour l'épreuve machine :

### 1. PostgreSQL (`docker compose exec postgres psql -U pixelhub -d pixelhub`)
```sql
-- Consulter les joueurs (guillemets obligatoires)
SELECT "Pseudo", "Coins" FROM "Players" ORDER BY "Coins" DESC;

-- Consulter les achats
SELECT * FROM "Purchases";

-- Modifier un solde
UPDATE "Players" SET "Coins" = 1000 WHERE "Pseudo" = 'Ombre';

-- Vérifier les contraintes
\d "Players"
\d "Purchases"
```

### 2. MongoDB (`docker compose exec mongo mongosh -u pixelhub -p pixelhub_dev --authenticationDatabase admin pixelhub`)
```javascript
// Nombre de documents
db.jeux.countDocuments()

// Rechercher avec filtre et projection (1 = afficher, 0 = cacher)
db.jeux.find({ genre: "FPS" }, { titre: 1, note: 1, _id: 0 })

// Recherche dans un tableau
db.jeux.find({ plateformes: "Switch" })

// Mettre à jour un champ sans écraser le document ($set obligatoire)
db.jeux.updateOne({ titre: "Valorant" }, { $set: { note: 4.4 } })

// Ajouter un champ à plusieurs documents
db.jeux.updateMany({ genre: "FPS" }, { $set: { prix: 0 } })

// Agrégation (moyenne par genre)
db.jeux.aggregate([
  { $group: { _id: "$genre", moyenne: { $avg: "$note" }, total: { $sum: 1 } } },
  { $sort: { moyenne: -1 } }
])
```

### 3. Redis (`docker compose exec redis redis-cli`)
```bash
# Vérifier toutes les clés (interdit en prod, toléré en TP)
KEYS *

# Chaînes & Cache
GET cache:catalogue:tous
TTL cache:catalogue:tous
DEL cache:catalogue:tous

# Sorted Set (Leaderboard)
ZREVRANGE classement:global 0 2 WITHSCORES    # Top 3 décroissant
ZRANGE    classement:global 0 2 WITHSCORES    # Flop 3 croissant
ZREVRANK  classement:global "Krayz"           # Rang basé sur 0
ZINCRBY   classement:global 200 "Nova"        # Ajouter des points

# Liste (Queue)
LPUSH file:matchmaking "Nova"
RPOP  file:matchmaking
LLEN  file:matchmaking
```

### 4. Neo4j (Browser sur `http://localhost:17474` avec port Bolt `17687`)
```cypher
// Compter tous les nœuds
MATCH (n) RETURN count(n)

// Trouver les amis d'un joueur (SANS FLÈCHE pour symétrie)
MATCH (:Joueur {pseudo:'Krayz'})-[:AMI_DE]-(a:Joueur)
RETURN a.pseudo

// Les jeux possédés par Nova
MATCH (:Joueur {pseudo:'Nova'})-[:POSSEDE]->(j:Jeu)
RETURN j.titre

// Recommandation d'amis (amis d'amis non encore amis)
MATCH (moi:Joueur {pseudo:'Nova'})-[:AMI_DE]-(c:Joueur)-[:AMI_DE]-(s:Joueur)
WHERE s <> moi AND NOT (moi)-[:AMI_DE]-(s)
RETURN s.pseudo, count(DISTINCT c) AS communs
ORDER BY communs DESC

// Recommandation de jeux (jeux des amis non possédés)
MATCH (moi:Joueur {pseudo:'Nova'})-[:AMI_DE*1..2]-(ami:Joueur)-[:POSSEDE]->(j:Jeu)
WHERE ami <> moi AND NOT (moi)-[:POSSEDE]->(j)
RETURN j.titre, count(DISTINCT ami) AS nbAmis
ORDER BY nbAmis DESC

// Création idempotente d'une possession
MERGE (j:Joueur {pseudo: 'Nova'})
MERGE (g:Jeu {titre: 'Terraria'})
MERGE (j)-[r:POSSEDE]->(g)
ON CREATE SET r.depuis = 2026
```

---

## 8. Les 10 Pièges Silencieux du Module

| # | Piège | Symptôme | Cause & Solution |
|---|---|---|---|
| **1** | Casse des tables PostgreSQL | `relation "players" does not exist` | Postgres met en minuscules sans guillemets. Écrire `"Players"` et `"Coins"`. |
| **2** | Casse dans MongoDB | `db.Jeux.find()` renvoie 0 document | Mongo est sensible à la casse (`jeux` ≠ `Jeux`) et n'affiche aucune erreur en lecture. |
| **3** | Oubli de `$set` dans `updateOne` | `MongoInvalidArgumentError` | MongoDB exige des opérateurs atomiques pour ne pas écraser tout le document. |
| **4** | Champs inconnus en C# (.NET) | `FormatException` sur `/games` | Ajouter `[BsonIgnoreExtraElements]` sur le modèle `Game`. |
| **5** | Désérialisation `ObjectId` depuis Redis | L'ID des jeux passe à zéro (`00000000...`) | Ajouter `[JsonConverter(typeof(ObjectIdJsonConverter))]` sur l'ID de `Game`. |
| **6** | Tri Leaderboard Redis | Top 5 inversé (les plus faibles en premier) | Utiliser impérativement `Order.Descending` dans `SortedSetRangeByRankWithScoresAsync`. |
| **7** | Direction de relation Neo4j | La moitié des amis disparaît | Utiliser `-[:AMI_DE]-` (sans flèche) pour interroger une relation symétrique. |
| **8** | Casse des labels Neo4j | `MATCH (j:joueur)` renvoie 0 résultat | Sensible à la casse : écrire `:Joueur` et non `:joueur`. |
| **9** | Transaction sans `CommitAsync` | Achat renvoie 200 mais solde inchangé | Sans `await tx.CommitAsync();`, EF Core annule la transaction à la sortie du bloc `using`. |
| **10** | Port de connexion Neo4j Browser | Erreur « mot de passe incorrect » | Le Browser propose `7687` par défaut : corriger impérativement en `17687` (port Bolt externe). |

---

## 9. Banque de Questions / Réponses pour le QCM

**Q1. Dans le théorème CAP, pourquoi est-il faux d'affirmer qu'un système peut être « CA » ?**  
*R :* Parce que dans un réseau physique distribué, les pannes et coupures réseau sont inévitables. La tolérance au partitionnement (P) n'est pas une option qu'on choisit, c'est une réalité subie. Le seul choix possible intervient **au moment où le partitionnement a lieu** : choisir entre C (cohérence) et A (disponibilité).

**Q2. Quelle est la différence fondamentale entre ACID et BASE ?**  
*R :* ACID privilégie la cohérence immédiate et stricte à chaque transaction (tout ou rien, intégrité garantie). BASE privilégie la haute disponibilité en acceptant un état transitoire souple (soft state) et une cohérence à terme (eventual consistency), où les nœuds convergent après propagation.

**Q3. Qu'appelle-t-on l'adjacence sans index dans une base orientée graphe ?**  
*R :* C'est le fait que chaque nœud stocke des pointeurs directs en mémoire vers ses nœuds et relations voisins. Le coût de traversée dépend uniquement du degré du nœud local et reste constant quelle que soit la taille totale du graphe, contrairement au SQL qui doit parcourir des index.

**Q4. Pourquoi une file d'attente de matchmaking s'implémente-t-elle avec une `List` Redis plutôt qu'un `Set` ?**  
*R :* Une `List` conserve l'ordre chronologique d'insertion (permettant une politique FIFO avec `LPUSH` et `RPOP`), tandis qu'un `Set` ne garantit aucun ordre.

**Q5. Dans quelle situation un cache peut-il devenir un SPOF (Single Point of Failure) ?**  
*R :* Lorsque le code applicatif tente d'interroger le cache sans intercepter les erreurs de connexion. En cas de panne du cache, l'API plante alors même que la base de données source est parfaitement saine.

**Q6. Pourquoi utilise-t-on `MERGE` plutôt que `CREATE` dans les scripts de synchronisation Neo4j ?**  
*R :* Pour rendre les écritures idempotentes. `MERGE` vérifie l'existence préalable du motif et ne crée le nœud ou la relation que s'il est absent, évitant les doublons lors des réexécutions multiples.

**Q7. Que signifie l'acronyme PACELC ?**  
*R :* If **P**artition : choose between **A**vailability and **C**onsistency ; **E**lse : choose between **L**atency and **C**onsistency. Il comble le vide de CAP en définissant l'arbitrage en régime de fonctionnement normal (vitesse de réponse vs réplication synchrone).

**Q8. Pourquoi MongoDB nécessite-t-il l'attribut `[BsonIgnoreExtraElements]` en C# alors que JavaScript ne pose aucun problème ?**  
*R :* JavaScript est un langage dynamique tolérant aux propriétés arbitraires. C# est fortement typé : le désérialiseur du driver MongoDB mappe chaque clé BSON sur une propriété C# et lève une exception s'il rencontre une clé inconnue non déclarée.
