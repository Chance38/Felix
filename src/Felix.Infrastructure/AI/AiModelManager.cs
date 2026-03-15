using Felix.Common;
using Felix.Infrastructure.Persistence.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

namespace Felix.Infrastructure.AI;

public interface IAiModelManager
{
    Task<Result<Kernel>> GetCurrentKernelAsync();
    Task<Result<Kernel>> AdvanceToNextProviderAsync();
    int GetProviderCount();
}

public class AiModelManager(IConfiguration configuration, IRedisContext redisContext) : IAiModelManager
{
    private readonly List<string> _providers = configuration
        .GetSection("AiModel")
        .GetChildren()
        .Select(s => s.Key)
        .ToList();

    public int GetProviderCount() => _providers.Count;

    public async Task<Result<Kernel>> GetCurrentKernelAsync()
    {
        if (_providers.Count == 0)
            return Result<Kernel>.ExternalError("未設定任何 AI Provider");

        var current = await redisContext.AiModel.GetCurrentProviderAsync() ?? _providers[0];
        return BuildKernel(current);
    }

    public async Task<Result<Kernel>> AdvanceToNextProviderAsync()
    {
        if (_providers.Count == 0)
            return Result<Kernel>.ExternalError("未設定任何 AI Provider");

        var current = await redisContext.AiModel.GetCurrentProviderAsync() ?? _providers[0];
        var currentIndex = _providers.IndexOf(current);
        if (currentIndex == -1) currentIndex = 0;

        var nextIndex = (currentIndex + 1) % _providers.Count;
        var next = _providers[nextIndex];

        await redisContext.AiModel.SetCurrentProviderAsync(next);
        return BuildKernel(next);
    }

    private Result<Kernel> BuildKernel(string provider)
    {
        var section = configuration.GetSection($"AiModel:{provider}");
        var model = section["Model"];
        var apiKey = section["ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
            return Result<Kernel>.ExternalError($"缺少 {provider} 的 ApiKey 設定");

        var kernel = provider.ToLowerInvariant() switch
        {
            "gemini" => Kernel.CreateBuilder()
                .AddGoogleAIGeminiChatCompletion(
                    modelId: model ?? "gemini-2.5-flash",
                    apiKey: apiKey)
                .Build(),
            "mistral" => Kernel.CreateBuilder()
                .AddOpenAIChatCompletion(
                    modelId: model ?? "mistral-small-latest",
                    apiKey: apiKey,
                    endpoint: new Uri("https://api.mistral.ai/v1"))
                .Build(),
            "groq" => Kernel.CreateBuilder()
                .AddOpenAIChatCompletion(
                    modelId: model ?? "llama-3.3-70b-versatile",
                    apiKey: apiKey,
                    endpoint: new Uri("https://api.groq.com/openai/v1"))
                .Build(),
            _ => null
        };

        return kernel is not null
            ? Result<Kernel>.Success(kernel)
            : Result<Kernel>.ExternalError($"未知的 AI Provider: {provider}");
    }
}
