using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace Felix.Infrastructure.Persistence.Redis;

public class RedisContext : IRedisContext
{
    public ConversationStore Conversations { get; }

    public RedisContext(IConnectionMultiplexer redis, IConfiguration configuration)
    {
        Conversations = new ConversationStore(redis, configuration);
    }
}
