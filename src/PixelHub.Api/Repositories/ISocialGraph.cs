namespace PixelHub.Api.Repositories;

public record GameRecommendation(string Titre, int NbAmis);
public record FriendSuggestion(string Pseudo, int AmisCommuns);

public interface ISocialGraph
{
    Task<List<string>> GetFriendsAsync(string pseudo);
    Task<List<GameRecommendation>> RecommendGamesAsync(string pseudo, int profondeur = 2);
    Task<List<FriendSuggestion>> SuggestFriendsAsync(string pseudo);
    Task AddOwnershipAsync(string pseudo, string titre, int annee);
    Task<List<string>> GetOwnedGamesAsync(string pseudo);
}
