namespace Felix.Infrastructure.Persistence.Redis;

public class ConversationHistory
{
    public List<ConversationMessage> Messages { get; set; } = [];

    public void AddUserMessage(string content)
    {
        Messages.Add(new ConversationMessage("user", content));
    }

    public void AddAssistantMessage(string content)
    {
        Messages.Add(new ConversationMessage("assistant", content));
    }

    public void TrimToLimit(int maxMessages)
    {
        if (Messages.Count > maxMessages)
        {
            Messages = Messages.TakeLast(maxMessages).ToList();
        }
    }
}
