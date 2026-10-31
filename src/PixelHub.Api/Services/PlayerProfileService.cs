using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using PixelHub.Api.Data;
using PixelHub.Api.Models;
using PixelHub.Api.Repositories;

namespace PixelHub.Api.Services;

public record ProfilJoueur(string Pseudo, int Solde, int NbAchats, long? Rang, List<string> Amis, List<Game> Jeux);

public class PlayerProfileService
{
    private readonly PixelHubContext _db;
    private readonly ILeaderboard _classement;
    private readonly ISocialGraph _graph;
    private readonly IGameCatalog _catalog;

    public PlayerProfileService(PixelHubContext db, ILeaderboard classement,
                                ISocialGraph graph, IGameCatalog catalog)
    {
        _db = db;
        _classement = classement;
        _graph = graph;
        _catalog = catalog;
    }

    public async Task<ProfilJoueur?> GetAsync(string pseudo)
    {
        var joueur = await _db.Players.SingleOrDefaultAsync(p => p.Pseudo == pseudo);
        if (joueur is null) return null;
        var nbAchats = await _db.Purchases.CountAsync(a => a.PlayerId == joueur.Id);

        long? rang = null;
        try
        {
            rang = await _classement.GetRankAsync(pseudo);
        }
        catch (Exception e) when (e is RedisException or RedisTimeoutException) { }

        var amis = await _graph.GetFriendsAsync(pseudo);
        var titres = await _graph.GetOwnedGamesAsync(pseudo);

        var jeux = await _catalog.GetByTitlesAsync(titres);

        return new ProfilJoueur(joueur.Pseudo, joueur.Coins, nbAchats, rang, amis, jeux);
    }
}
