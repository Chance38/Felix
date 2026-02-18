using Microsoft.Extensions.DependencyInjection;

namespace Felix.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // TODO: Register infrastructure services
        // services.AddDbContext<FelixDbContext>();
        // services.AddHttpClient<IWeatherClient, WeatherClient>();

        return services;
    }
}
