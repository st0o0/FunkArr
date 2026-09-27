using FunkArr.Api;
using FunkArr.Core;
using FunkArr.RuleSet;
using Microsoft.Extensions.Caching.Memory;
using Servus.Core.Application.Startup;

namespace FunkArr.Configuration;

public sealed class RuleSetSetupContainer : ApplicationSetupContainer<WebApplication>, IServiceSetupContainer
{
    public void SetupServices(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<RuleSetUpdaterOptions>()
            .Bind(configuration.GetSection(RuleSetUpdaterOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IRuleSetValidator, RuleSetValidator>();
        services.AddSingleton<RuleSetStore>();

        var version = typeof(RuleSetSetupContainer).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        services.AddHttpClient(HttpClientNames.GitHub, client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
            client.DefaultRequestHeaders.Add("User-Agent", $"FunkArr/{version}");
        })
        .AddStandardResilienceHandler();

        services.AddOutputCache(options =>
        {
            options.AddPolicy("RuleSetList", builder =>
                builder.Expire(TimeSpan.FromSeconds(30)).Tag("rulesets"));
            options.AddPolicy("SystemVersion", builder =>
                builder.Expire(TimeSpan.FromSeconds(60)));
        });
    }

    protected override void SetupApplication(WebApplication app)
    {
        app.MapRuleSetApi();
    }
}
