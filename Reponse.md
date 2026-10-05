## Etape 1

# Question 1

Il ya :
- un conteneur postgresql qui contiendra les informations relatives aux joueurs
- un autre conteneur de bdd mais en nosql qui contiendra le catalogue des jeux
- un conteneur redis permettra de gérer le matchmaking et de mettre les utilisateurs dans une liste pour gérer le cache
- un conteneur Neoj qui permet de représenter les données sous forme de graphe

# Question 2

Ils ont des volumes puisqu'on souhaite à chaque arrêt du docker, avoir une bdd tjrs remplis des datas de la dernière session de travail.

# Question 3

Sur docker on utilise par defaut le port 15432 et sur le pc le port 5432

# Question 4

Non surtout en production.


## Etape 2

# Question 5

Cela pemet d'éxécuter une commande à l'intérieur du conteneur. Donc pour redis, cela se fait directement dans le conteneur redis

## Etape 3

# Question 6

Cela fonctionne puisque on est en local (locahost et le port de notre machine)

# Question 7

Non uniquement si les tables sont vides
