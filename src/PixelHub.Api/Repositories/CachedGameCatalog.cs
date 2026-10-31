using System.Text.Json;
using StackExchange.Redis;
using PixelHub.Api.Models;

namespace PixelHub.Api.Repositories;

public class CachedGameCatalog : IGameCatalog
{
    private const string KeyAll = "cache:catalogue:tous";
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    private readonly IGameCatalog _source;
    private readonly IDatabase _cache;

    public CachedGameCatalog(IGameCatalog source, IConnectionMultiplexer redis)
    {
        _source = source;
        _cache = redis.GetDatabase();
    }

    public async Task<List<Game>> GetAllAsync()
    {
        try
        {
            var cached = await _cache.StringGetAsync(KeyAll);
            if (cached.HasValue)                                   // cache hit
                return JsonSerializer.Deserialize<List<Game>>((string)cached!)!;
        }
        catch (Exception e) when (e is RedisException or RedisTimeoutException)
        {
            return await _source.GetAllAsync();
        }

        var jeux = await _source.GetAllAsync();                // cache miss
        try
        {
            await _cache.StringSetAsync(KeyAll, JsonSerializer.Serialize(jeux), Ttl);
        }
        catch (Exception e) when (e is RedisException or RedisTimeoutException) { }
        return jeux;
    }

    public Task<List<Game>> GetByGenreAsync(string genre) => _source.GetByGenreAsync(genre);
    public Task<List<Game>> GetTopRatedAsync(int count) => _source.GetTopRatedAsync(count);
    public Task<Game?> GetByTitleAsync(string titre) => _source.GetByTitleAsync(titre);
    public Task<List<Game>> GetByTitlesAsync(IEnumerable<string> titres) => _source.GetByTitlesAsync(titres);
    public Task<List<GenreStats>> GetStatsByGenreAsync() => _source.GetStatsByGenreAsync();
}
