using FunkArr.Core;
using Terp.Hosting;

namespace FunkArr.Configuration;

public sealed class FeedSetupContainer : ISetup<IServiceCollection>
{
    public void Setup(IServiceCollection services, SetupContext context)
    {
        services
            .AddOptions<FeedOptions>()
            .Bind(context.Configuration.GetSection(FeedOptions.SectionName))
            .Validate(o => o.Interval > TimeSpan.Zero, "FunkArr:Feed:Interval must be positive")
            .Validate(o => o.PageSize is > 0 and <= 500, "FunkArr:Feed:PageSize must be between 1 and 500")
            .Validate(o => o.MaxPages > 0, "FunkArr:Feed:MaxPages must be positive")
            .Validate(o => o.MaxCatchUp > TimeSpan.Zero, "FunkArr:Feed:MaxCatchUp must be positive")
            .Validate(o => o.MaxConcurrency > 0, "FunkArr:Feed:MaxConcurrency must be positive")
            .Validate(o => o.MinConfidence is >= 0 and <= 1, "FunkArr:Feed:MinConfidence must be between 0 and 1")
            .Validate(o => o.Retention > TimeSpan.Zero, "FunkArr:Feed:Retention must be positive")
            .Validate(o => o.InitialLookback > TimeSpan.Zero, "FunkArr:Feed:InitialLookback must be positive")
            .ValidateOnStart();
    }
}
