## Partie 1

# Question 1

Utiliser create pourrait créer des doublons. L'utilisation de constraint permet de créer si un original n'existe pas.

# Question 2

Deux amis pour la première et un pour la deuxième

# Question 3

Au lieu de compter le nombre d'amis différents, là on a des amis qui ont le mm jeu qui sont comptés plusieurs fois

# Question 4

Il a pas d'amis qui possède le jeu même si il le possède

# Question 5

Un port pour l'interface et un port pour la connexion

# Question 6

C'est interpolée car la syntaxe ne permet pas à pseudo d'être un paramètre classique

# Question 7

Chaque opération utilise sa session donc await using. La règle c'est d'utiliser les objects déjà crées au lieu d'instancier des nouveaux

## Partie 2

# Question 8

PostgreSQL met tout en minuscules par défaut s'il n'y a pas de guillemets doubles. Comme EF Core a créé les tables et colonnes avec des majuscules ("Players", "Coins"), les guillemets sont indispensables pour respecter la casse exacte. Si on écrit `SELECT * FROM players;`, PostgreSQL cherche la table `players` en minuscules et renvoie une erreur : `relation "players" does not exist`.

# Question 9

La contrainte `UNIQUE ("PlayerId", "Titre")` garantit qu'un joueur ne peut pas acheter deux fois le même jeu (pas de doublon d'achat pour ce joueur).
La contrainte `coins_positifs` garantit que le solde de pièces d'un joueur ne sera jamais négatif.
Si on exécute `UPDATE "Players" SET "Coins" = -1 WHERE "Pseudo" = 'Ombre';`, PostgreSQL rejette l'opération avec une erreur de violation de contrainte de vérification (`check constraint "coins_positifs"`).

# Question 10

Les modifiedCount obtenus sont :
- 1ère commande : 10 (tous les jeux passent à 800)
- 2ème commande : 4 (les jeux avec note >= 4.7 passent à 1200)
- 3ème commande : 2 (les jeux du genre FPS passent à 0)

On a réécrit 0 document en entier : `$set` modifie ou ajoute uniquement le champ `prix` directement sans toucher au reste du document.
En SQL, il aurait fallu faire un `ALTER TABLE jeux ADD COLUMN prix INT;` suivi de plusieurs requêtes `UPDATE`.
Si on lançait la première commande en dernier, tous les jeux repasseraient à 800 pièces et ça écraserait les prix spécifiques des FPS et des jeux à bonne note.

# Question 11

Non, la valeur en cache ne contient pas les prix car elle a été mise en cache avant les modifications dans MongoDB.
L'API continuera de servir ce catalogue sans prix pendant 60 secondes maximum (la durée de vie du TTL).
Pour forcer la mise à jour tout de suite, on supprime la clé de cache dans Redis avec la commande :
`DEL cache:catalogue:tous`

# Question 12

En faisant `KEYS *`, on trouve notamment :
- `classement:global` : sorted set qui stocke les joueurs et leur score pour le leaderboard.
- `cache:catalogue:tous` : chaîne de caractères (JSON) qui garde en cache la liste complète des jeux pour éviter d'interroger MongoDB à chaque fois.
- `file:matchmaking` : liste Redis (FIFO) pour la file d'attente des joueurs.
- `ventes:jeux` : sorted set qui compte le nombre de ventes par jeu.

Sur un serveur de prod avec des millions de clés, `KEYS *` est dangereux car Redis est mono-threadé et va bloquer toutes les autres requêtes le temps de tout scanner.

# Question 13

Luna est présente dans Neo4j (elle a des amis et possède des jeux dans le graphe), mais elle n'existe pas dans la table `Players` de PostgreSQL.
Si Luna essaie d'acheter un jeu, la boutique renvoie une erreur 404 (`Joueur inconnu : Luna`) car l'API vérifie d'abord que le joueur existe dans PostgreSQL, qui est la référence absolue pour les comptes et la monnaie.

# Question 14

La requête SQL générée par `ExecuteUpdateAsync` ressemble à :
`UPDATE "Players" SET "Coins" = "Coins" - @prix WHERE "Id" = @id AND "Coins" >= @prix;`

C'est beaucoup plus sûr que de faire la lecture et la vérification en C# car l'opération est atomique et exécutée directement par le moteur PostgreSQL avec un verrou sur la ligne. Si on lit d'abord en C#, deux requêtes simultanées pourraient lire le même solde (par exemple 1200), voir que c'est suffisant toutes les deux, et appliquer deux débits alors qu'il n'y avait les fonds que pour un seul (problème de race condition).

# Question 15

Le test `AnyAsync` permet juste de renvoyer un message propre et rapide dans le cas normal, mais il ne garantit rien en cas de concurrence : deux requêtes simultanées peuvent faire le test en même temps, voir qu'il n'y a pas d'achat, et tenter d'insérer en même temps. C'est l'index UNIQUE posé en base qui est le vrai filet de sécurité garantissant qu'un seul des deux inserts réussira.
On ne fait pas ce test dans Neo4j car Neo4j n'est qu'une projection (mise à jour après coup et potentiellement désynchronisée), alors que PostgreSQL est la source de vérité avec des garanties ACID strictes.

# Question 16

L'achat passe (code 200).
C'est PostgreSQL qui a raison car c'est lui qui fait foi pour la facturation et les achats réels.
Cela montre que les données importées à la main dans Neo4j étaient des données initiales non validées par le système d'achat réel, ce qui crée une incohérence entre la base de facturation et le graphe social.

# Question 17

Un seul achat passe (code 200) et le deuxième est refusé pour solde insuffisant (code 409).
Le solde final est de 0 pièce.
Avec la version « lire, tester en C#, écrire », les deux requêtes auraient lu 800 pièces en même temps, auraient cru que le solde était suffisant, et les deux auraient débité le joueur, amenant son solde à -800 (ou créant un jeu offert).

# Question 18

La fiche joueur envoie 4 requêtes au total, réparties sur les 4 moteurs :
- 1 requête à PostgreSQL (pour le joueur, son solde et le nombre d'achats)
- 1 requête à Redis (pour obtenir son rang dans le classement)
- 1 requête à Neo4j (pour récupérer ses amis et la liste des titres de jeux possédés)
- 1 requête à MongoDB (pour récupérer les détails des fiches de ces jeux)

Si Nova possédait 200 jeux, `GetByTitlesAsync` ne fait toujours qu'une seule requête grâce au filtre `$in`, alors qu'une boucle aurait fait 200 requêtes consécutives vers MongoDB, ce qui ruinerait les performances.

# Question 19

C'est PostgreSQL qui a décidé que Luna n'existe pas, car la méthode commence par chercher le joueur dans la table des joueurs de PostgreSQL (`_db.Players`).
C'est le bon choix car PostgreSQL est le référentiel d'identité des utilisateurs. Si le compte n'existe pas officiellement dans la base principale, on ne doit pas afficher de profil, même si des traces subsistent dans les projections.

# Question 20

On obtient un code HTTP 500 (Internal Server Error).
Dans `psql`, on constate qu'Ombre A BIEN ÉTÉ DÉBITÉ de ses pièces et la ligne d'achat est bien présente dans la table `"Purchases"`.
Ombre croit que son achat a échoué puisqu'il a reçu une erreur 500, alors qu'en réalité il a bien payé !
Ça met 30 secondes car le driver Neo4j essaie de joindre le serveur Neo4j et attend la fin de son timeout de connexion avant de déclarer forfait et de lever une exception.

# Question 21

Ombre reçoit un code 409 Conflit (« Jeu déjà acheté »).
Il n'est pas débité une deuxième fois grâce au test d'existence et surtout à la contrainte d'index UNIQUE sur `(PlayerId, Titre)` dans PostgreSQL.

# Question 22

Non, la fiche d'Ombre n'est pas cohérente.
Son solde a bien diminué et son nombre d'achats indique 1 dans PostgreSQL, mais la liste de ses jeux ne contient pas Rocket League parce que l'écriture dans Neo4j a échoué.
C'est Neo4j qui est en retard et faux par rapport à la réalité.

# Question 23

On peut rejouer `/admin/resync` autant de fois qu'on veut car les opérations sont idempotentes :
- Dans Neo4j, on utilise `MERGE` au lieu de `CREATE`, donc si la relation ou le nœud existe déjà, rien n'est dupliqué.
- Dans Redis, on supprime la clé existante avec `KeyDeleteAsync` avant de réinsérer toutes les données.
Si `AddOwnershipAsync` avait utilisé `CREATE`, chaque appel à resync créerait une nouvelle relation `POSSEDE` en doublon entre le joueur et le jeu.

# Question 24

| Donnée | Système de référence | Projection(s) | Se reconstruit à partir de… | Retard acceptable ? |
|---|---|---|---|---|
| Solde de monnaie | PostgreSQL ("Players") | Aucune | Source de vérité (ne se reconstruit pas) | Non |
| « Ombre a acheté Rocket League » | PostgreSQL ("Purchases") | Neo4j (relation POSSEDE) | Table "Purchases" | Oui |
| Prix courant d'un jeu | MongoDB ("jeux") | Redis (cache:catalogue:tous) | Collection jeux de MongoDB | Oui |
| Prix payé par Ombre | PostgreSQL ("Purchases") | Aucune | Source de vérité (ne se reconstruit pas) | Non |
| Meilleures ventes | PostgreSQL ("Purchases") | Redis ("ventes:jeux") | Table "Purchases" (agrégation COUNT) | Oui |
| Catalogue complet | MongoDB ("jeux") | Redis (cache:catalogue:tous) | Collection jeux de MongoDB | Oui |

# Question 25

L'appel `/games` échoue parce que la classe `CachedGameCatalog` interroge Redis pour vérifier le cache sans intercepter l'erreur : quand Redis est arrêté, une exception est levée et interrompt la requête HTTP.
Le cache est devenu un SPOF (Single Point of Failure / point individuel de panne) : un composant censé être une simple optimisation de lecture bloque tout le service dès qu'il plante.

# Question 26

On peut rattraper la panne de Redis silencieusement car le cache et le classement ne sont que des optimisations ou des informations secondaires : si Redis ne répond pas, on peut aller chercher la vraie donnée dans MongoDB ou afficher le profil sans rang, l'application reste utilisable et intègre.
En revanche, pour PostgreSQL dans la boutique, il s'agit de transactions financières (débit de solde et facturation) : si PostgreSQL ne répond pas, on ne peut pas garantir que le joueur a les fonds ni tracer le paiement, donc il est impératif d'échouer plutôt que d'autoriser un achat dans le vide.
