using MongoDB.Driver;
using PixelHub.Api.Models;

namespace PixelHub.Api.Repositories;

public class MongoGameCatalog : IGameCatalog
{
    private readonly IMongoCollection<Game> _jeux;

    public MongoGameCatalog(IMongoDatabase database)
    {
        _jeux = database.GetCollection<Game>("jeux");
    }

    public async Task<List<Game>> GetAllAsync() =>
        await _jeux.Find(_ => true).ToListAsync();

    public async Task<List<Game>> GetByGenreAsync(string genre) =>
        await _jeux.Find(g => g.Genre == genre).ToListAsync();

    public async Task<List<Game>> GetTopRatedAsync(int count) =>
        await _jeux.Find(_ => true)
                   .SortByDescending(g => g.Note)
                   .Limit(count)
                   .ToListAsync();
}