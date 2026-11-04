using FunkArr.Core;
using Terp.Hosting;

namespace FunkArr.Configuration;

public sealed class ScoringSetupContainer : ISetup<IServiceCollection>
{
    public void Setup(IServiceCollection services, SetupContext context)
    {
        services
            .AddOptionsWithValidateOnStart<ScoringOptions, ScoringOptionsValidator>()
            .Bind(context.Configuration.GetSection(ScoringOptions.SectionName));

        services
            .AddOptionsWithValidateOnStart<VariantOptions>()
            .Bind(context.Configuration.GetSection(VariantOptions.SectionName));

        services
            .AddOptionsWithValidateOnStart<ScoringHistoryOptions, ScoringHistoryOptionsValidator>()
            .Bind(context.Configuration.GetSection(ScoringHistoryOptions.SectionName));
    }
}
