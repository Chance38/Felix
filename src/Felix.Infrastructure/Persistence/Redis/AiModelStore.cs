using StackExchange.Redis;

namespace Felix.Infrastructure.Persistence.Redis;

public class AiModelStore(IConnectionMultiplexer redis)
{
    private readonly IDatabase _db = redis.GetDatabase();
    private const string Key = "aimodel:current_provider";

    public async Task<string?> GetCurrentProviderAsync()
    {
        var value = await _db.StringGetAsync(Key);
        return value.IsNullOrEmpty ? null : (string?)value;
    }

    public async Task SetCurrentProviderAsync(string provider)
    {
        await _db.StringSetAsync(Key, provider);
    }
}
