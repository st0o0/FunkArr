using FunkArr.Api;
using FunkArr.Core;
using FunkArr.RuleSet;
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

        services
            .AddOptions<ScoringOptions>()
            .Bind(configuration.GetSection(ScoringOptions.SectionName))
            .ValidateOnStart();

        services
            .AddOptions<MatchHistoryOptions>()
            .Bind(configuration.GetSection(MatchHistoryOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IRuleSetValidator, RuleSetValidator>();
        services.AddSingleton<IRuleSetExporter, RuleSetExporter>();

        var version = typeof(RuleSetSetupContainer).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        services.AddHttpClient(HttpClientNames.GitHub, client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
            client.DefaultRequestHeaders.Add("User-Agent", $"FunkArr/{version}");
        })
        .AddStandardResilienceHandler();
    }

    protected override void SetupApplication(WebApplication app)
    {
        app.MapRuleSetApi();
    }
}
