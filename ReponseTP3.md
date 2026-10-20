## Partie 1

# Question 1

Cela veut dire que chaque incrémentation est faite l'une après l'autre. Donc pour 1000 parties, chaque update se ferait sépaément des autres et de manière plus rapide qu'en sql avec un update parties

# Question 2

Il faudrait lire, désérialiser, rajouter 250, resérialiser, réécrire. Si on avait eu 2 requêtes, l'une aurait écraser l'autre.

# Question 3

La diff entre les 2 commandes, c'est que la première c'est par ordre croissant et l'autre décroissant. Pour un top 3 il faut ZREVRANGE

# Question 4

Ca renvoie la position dans la classement

# Question 5

RPOP renvoie le premier joueur de la liste qui est retiré, LPOP aurait pris le dernier

# Question 6

On obtient 0 puique le TTL reste 30sec. Quand on a EXIST, renvoie 1 si oui, 0 si non. Pour TTL, c'est la durée en sec qu'on a 

# Question 7

Il aura fallu mettre en place un trigger qui au bout de 30 sec vire nova d'une table cache

## Partie 2

# Question 8

Il gère les connexions entre l'app et Reddis. A chaque requête http, on aurait une instance de créé qui consomerait de l'espace tandis que là on est sur qu'il y en a qu'un. Il serait invisible car techniquement c'est pas une erreur.

# Question 9

Oui car le dernier du top 5 est renvoyé en premier. Il est dangereux puisque il n'y a pas de message d'erreur mais le top n'est pas cohérent.

# Question 12
Aucune ligne des endpoints n’a été modifiée, car le cache implémente la même interface IGameCatalog que MongoGameCatalog.
Le choix de la séance 2 est l’injection de dépendances via une interface, qui permet de remplacer l’implémentation sans modifier le code qui l’utilise.

# Question 13
Le premier appel est plus lent que les suivants. Le gain peut être faible avec peu de jeux et peu de requêtes. Un cache devient réellement décisif avec beaucoup de données, des accès fréquents et une base source lente ou fortement sollicitée.

# Question 14
 Les joueurs peuvent voir l’ancien titre pendant 60 secondes maximum, car le cache conserve les données jusqu’à expiration du TTL.

Deux stratégies pour réduire ce délai :

Invalider le cache après une modification : les données sont actualisées immédiatement, mais il faut gérer l’invalidation correctement.

Réduire le TTL : les données deviennent plus rapidement à jour, mais les accès à MongoDB augmentent.