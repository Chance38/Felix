namespace Felix.Infrastructure.AI;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    public required string Model { get; set; }
    public required List<string> ApiKeys { get; set; }
}
