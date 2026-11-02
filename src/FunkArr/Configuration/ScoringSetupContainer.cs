using FunkArr.Core;
using Terp.Hosting;

namespace FunkArr.Configuration;

public sealed class ScoringSetupContainer : ISetup<IServiceCollection>
{
    public void Setup(IServiceCollection services, SetupContext context)
    {
        services
            .AddOptions<ScoringOptions>()
            .Bind(context.Configuration.GetSection(ScoringOptions.SectionName))
            .ValidateOnStart();

        services
            .AddOptions<VariantOptions>()
            .Bind(context.Configuration.GetSection(VariantOptions.SectionName))
            .ValidateOnStart();

        services
            .AddOptions<ScoringHistoryOptions>()
            .Bind(context.Configuration.GetSection(ScoringHistoryOptions.SectionName))
            .ValidateOnStart();
    }
}
