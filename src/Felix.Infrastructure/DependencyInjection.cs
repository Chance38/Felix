using Felix.Infrastructure.AI;
using Felix.Infrastructure.AI.Tools;
using Felix.Infrastructure.Clients.Weather;
using Felix.Infrastructure.Mcp;
using Felix.Infrastructure.Persistence.Redis;
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

        // MCP Client 管理
        services.AddSingleton<IMcpClientManager, McpClientManager>();

        // 外部 Client
        services.AddHttpClient<ITaiwanWeatherClient, TaiwanWeatherClient>();

        // 本地工具
        services.AddScoped<ILocalTool, TaiwanWeatherTool>();

        // Request Context
        services.AddScoped<IRequestContext, RequestContext>();

        // AI 服務
        services.AddSingleton<IKernelFactory, KernelFactory>();
        services.AddScoped<IFelix, Felix.Infrastructure.AI.Felix>();

        return services;
    }
}
