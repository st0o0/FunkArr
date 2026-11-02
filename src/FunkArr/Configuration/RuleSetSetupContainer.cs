using FunkArr.Api;
using FunkArr.Core;
using FunkArr.RuleSet;
using Terp.Hosting;

namespace FunkArr.Configuration;

public sealed class RuleSetSetupContainer : ISetup<IServiceCollection>, ISetup<WebApplication>
{
    public void Setup(IServiceCollection services, SetupContext context)
    {
        services
            .AddOptions<RuleSetUpdaterOptions>()
            .Bind(context.Configuration.GetSection(RuleSetUpdaterOptions.SectionName))
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

    public void Setup(WebApplication app, SetupContext _) => app.MapRuleSetApi();
}
