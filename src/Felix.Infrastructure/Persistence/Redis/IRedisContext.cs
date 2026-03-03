namespace Felix.Infrastructure.Persistence.Redis;

public interface IRedisContext
{
    ConversationStore Conversations { get; }
}
