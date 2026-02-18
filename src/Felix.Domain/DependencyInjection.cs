using Microsoft.Extensions.DependencyInjection;

namespace Felix.Domain;

public static class DependencyInjection
{
    public static IServiceCollection AddDomain(this IServiceCollection services)
    {
        // TODO: Register domain services
        // services.AddScoped<INluService, NluService>();
        // services.AddScoped<ISkillService, SkillService>();
        // services.AddScoped<IConversationService, ConversationService>();

        return services;
    }
}
