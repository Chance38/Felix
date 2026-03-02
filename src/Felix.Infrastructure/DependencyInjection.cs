using Felix.Infrastructure.AI;
using Felix.Infrastructure.AI.Tools;
using Felix.Infrastructure.Clients.Weather;
using Felix.Infrastructure.Mcp;
using Microsoft.Extensions.DependencyInjection;

namespace Felix.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // MCP Client 管理
        services.AddSingleton<IMcpClientManager, McpClientManager>();

        // 外部 Client
        services.AddHttpClient<ITaiwanWeatherClient, TaiwanWeatherClient>();

        // 本地工具
        services.AddScoped<ILocalTool, TaiwanWeatherTool>();

        // Request Context
        services.AddScoped<IRequestContext, RequestContext>();

        // AI 服務
        services.AddSingleton<IGeminiKeyManager, GeminiKeyManager>();
        services.AddSingleton<IKernelFactory, KernelFactory>();
        services.AddScoped<IFelix, Felix.Infrastructure.AI.Felix>();

        return services;
    }
}
