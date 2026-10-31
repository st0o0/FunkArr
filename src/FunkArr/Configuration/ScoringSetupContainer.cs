using FunkArr.Core;
using Servus.Application.Startup;

namespace FunkArr.Configuration;

public sealed class ScoringSetupContainer : IServiceSetupContainer
{
    public void SetupServices(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<ScoringOptions>()
            .Bind(configuration.GetSection(ScoringOptions.SectionName))
            .ValidateOnStart();

        services
            .AddOptions<VariantOptions>()
            .Bind(configuration.GetSection(VariantOptions.SectionName))
            .ValidateOnStart();

        services
            .AddOptions<ScoringHistoryOptions>()
            .Bind(configuration.GetSection(ScoringHistoryOptions.SectionName))
            .ValidateOnStart();
    }
}
