using FunkArr.Core.Configuration;
using Terp.Hosting;

namespace FunkArr.Configuration;

public sealed class FeedSetupContainer : ISetup<IServiceCollection>
{
    public void Setup(IServiceCollection services, SetupContext context)
    {
        services
            .AddOptionsWithValidateOnStart<FeedOptions, FeedOptionsValidator>()
            .Bind(context.Configuration.GetSection(FeedOptions.SectionName));
    }
}
