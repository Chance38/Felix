using Felix.Infrastructure.Clients.Weather;
using Microsoft.Extensions.DependencyInjection;

namespace Felix.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddHttpClient<IWeatherClient, WeatherClient>();

        return services;
    }
}
