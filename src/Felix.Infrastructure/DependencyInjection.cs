using Felix.Domain.Weather;
using Felix.Infrastructure.AI;
using Felix.Infrastructure.AI.Tools;
using Felix.Infrastructure.ExternalClients.Weather;
using Felix.Infrastructure.McpClients;
using Felix.Infrastructure.Persistence.Redis;
using Felix.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Felix.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Redis
        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:4080";
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        services.AddSingleton<IRedisContext, RedisContext>();

        // Options
        services.Configure<TaiwanWeatherOptions>(
            configuration.GetSection(TaiwanWeatherOptions.SectionName));
        services.Configure<McpOptions>(
            configuration.GetSection(McpOptions.SectionName));

        // MCP Client 管理
        services.AddSingleton<IMcpClientManager, McpClientManager>();

        // 外部 Client
        services.AddHttpClient<ITaiwanWeatherClient, TaiwanWeatherClient>();

        // Domain Services
        services.AddScoped<IWeatherService, WeatherService>();

        // 本地工具
        services.AddScoped<ILocalTool, TaiwanWeatherTool>();

        // Request Context
        services.AddScoped<IRequestContext, RequestContext>();

        // AI 服務
        services.AddSingleton<IAiModelManager, AiModelManager>();
        services.AddScoped<IFelix, Felix.Infrastructure.AI.Felix>();

        return services;
    }
}
