## Partie 1

# Question 1

C'est Mongo qui l'a fabriqué. Pour Pixel, c'est postgres qui l'avait initialisé.

# Question 2

Select * from jeux where plateformes=="Switch";
Si plateformes est une table, il faut faire une jointure

# Question 3

Cela a été fait dans la base admin et le compte pixelhub était dans le docker

# Question 4

C'est plutôt un problème car dans le temps, surtout en équipe, les développeurs utiliseront chacun leurs mots clés et lors de commandes automatisés, cela va planter.

# Question 5

Cela n'est pas surprenant vu que la table Jeux n'existe pas, il n'y a pas de message d'erreur, juste null pour montrer qu'elle n'a rien à renvoyer.

# Question 6

MongoInvalidArgumentError: Update document requires atomic operators
db.jeux.updateOne({ titre: "Valorant" },{ $set: { note: 4.4 }})

# Question 7

ALTER TABLE jeux ADD COLUMN nb_votes INTEGER;

## Partie 2

# Question 8

Non car c'est mongoGameCatalog qui s'occupe d'aller chercher les données du stockage

# Question 9

J'ai un code 200 mais je reçois une liste vide.

