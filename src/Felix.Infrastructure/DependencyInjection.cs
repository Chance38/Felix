using Felix.Infrastructure.AI;
using Felix.Infrastructure.Clients.Geocoding;
using Felix.Infrastructure.Clients.Weather;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Felix.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // HTTP Clients
        services.AddHttpClient<IGeocodingClient, GeocodingClient>();
        services.AddHttpClient<IWeatherClient, WeatherClient>();

        // Gemini 設定
        services.Configure<GeminiOptions>(
            configuration.GetSection(GeminiOptions.SectionName));

        // 驗證設定
        var geminiOptions = configuration
            .GetSection(GeminiOptions.SectionName)
            .Get<GeminiOptions>();

        if (geminiOptions?.ApiKeys == null || geminiOptions.ApiKeys.Count == 0)
        {
            throw new InvalidOperationException("At least one Gemini API key is required in configuration");
        }

        if (string.IsNullOrEmpty(geminiOptions.Model))
        {
            throw new InvalidOperationException("Gemini Model is required in configuration");
        }

        // Request Context (per request)
        services.AddScoped<IRequestContext, RequestContext>();

        // AI 服務
        services.AddSingleton<IGeminiKeyManager, GeminiKeyManager>();
        services.AddScoped<IKernelFactory, KernelFactory>();
        services.AddScoped<IAssistantClient, AssistantClient>();

        return services;
    }
}
