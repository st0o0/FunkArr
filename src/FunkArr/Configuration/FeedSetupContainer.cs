using FunkArr.Core;
using Servus.Application.Startup;

namespace FunkArr.Configuration;

public sealed class FeedSetupContainer : IServiceSetupContainer
{
    public void SetupServices(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<FeedOptions>()
            .Bind(configuration.GetSection(FeedOptions.SectionName))
            .Validate(o => o.Interval > TimeSpan.Zero, "FunkArr:Feed:Interval must be positive")
            .Validate(o => o.PageSize is > 0 and <= 500, "FunkArr:Feed:PageSize must be between 1 and 500")
            .Validate(o => o.MaxPages > 0, "FunkArr:Feed:MaxPages must be positive")
            .Validate(o => o.MaxConcurrency > 0, "FunkArr:Feed:MaxConcurrency must be positive")
            .Validate(o => o.MinConfidence is >= 0 and <= 1, "FunkArr:Feed:MinConfidence must be between 0 and 1")
            .ValidateOnStart();
    }
}
