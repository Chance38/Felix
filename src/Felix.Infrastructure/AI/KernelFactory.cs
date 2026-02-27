using Felix.Infrastructure.AI.Plugins;
using Felix.Infrastructure.Clients.Geocoding;
using Felix.Infrastructure.Clients.Weather;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Felix.Infrastructure.AI;

public interface IKernelFactory
{
    Kernel CreateKernel(string apiKey);
}

public class KernelFactory(
    IOptions<GeminiOptions> options,
    IGeocodingClient geocodingClient,
    IWeatherClient weatherClient,
    IRequestContext requestContext) : IKernelFactory
{
    public Kernel CreateKernel(string apiKey)
    {
        var kernel = Kernel.CreateBuilder()
            .AddGoogleAIGeminiChatCompletion(
                modelId: options.Value.Model,
                apiKey: apiKey)
            .Build();

        // Plugins
        kernel.Plugins.AddFromObject(new GeocodingPlugin(geocodingClient), "Geocoding");
        kernel.Plugins.AddFromObject(new WeatherPlugin(weatherClient, geocodingClient, requestContext), "Weather");

        return kernel;
    }
}
