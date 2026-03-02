using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

namespace Felix.Infrastructure.AI;

public interface IKernelFactory
{
    Kernel CreateKernel(string apiKey);
}

public class KernelFactory(IConfiguration configuration) : IKernelFactory
{
    private readonly string _model = configuration["Gemini:Model"] ?? "gemini-2.5-flash";

    public Kernel CreateKernel(string apiKey)
    {
        return Kernel.CreateBuilder()
            .AddGoogleAIGeminiChatCompletion(modelId: _model, apiKey: apiKey)
            .Build();
    }
}
