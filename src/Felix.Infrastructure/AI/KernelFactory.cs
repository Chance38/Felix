using Felix.Infrastructure.AI.Plugins;
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
    IWeatherClient weatherClient) : IKernelFactory
{
    public Kernel CreateKernel(string apiKey)
    {
        var kernel = Kernel.CreateBuilder()
            .AddGoogleAIGeminiChatCompletion(
                modelId: options.Value.Model,
                apiKey: apiKey)
            .Build();

        kernel.Plugins.AddFromObject(new WeatherPlugin(weatherClient), "Weather");

        return kernel;
    }
}
