using System.Text.Json;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace Felix.Infrastructure.Persistence.Redis;

public class ConversationStore(IConnectionMultiplexer redis, IConfiguration configuration)
{
    private readonly IDatabase _db = redis.GetDatabase();
    private readonly int _maxMessages = configuration.GetValue("Conversation:MaxMessages", 100);
    private readonly TimeSpan _ttl = TimeSpan.FromDays(configuration.GetValue("Conversation:TtlDays", 1));

    private static string GetKey(string conversationId) => $"conversation:{conversationId}";

    public async Task<ConversationHistory?> GetAsync(string conversationId)
    {
        var key = GetKey(conversationId);
        var value = await _db.StringGetAsync(key);

        if (value.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<ConversationHistory>((string)value!);
    }

    public async Task SaveAsync(string conversationId, ConversationHistory history)
    {
        history.TrimToLimit(_maxMessages);

        var key = GetKey(conversationId);
        var json = JsonSerializer.Serialize(history);

        await _db.StringSetAsync(key, json, _ttl);
    }

    public async Task<bool> ExistsAsync(string conversationId)
    {
        var key = GetKey(conversationId);
        return await _db.KeyExistsAsync(key);
    }
}
