using FunkArr.Core.Configuration;
using FunkArr.Enrichment.Tmdb;
using FunkArr.Enrichment.Tvdb;
using Terp.Diagnostics;
using Terp.Hosting;

namespace FunkArr.Configuration;

public sealed class EnrichmentSetupContainer : ISetup<IServiceCollection>
{
    public void Setup(IServiceCollection services, SetupContext context)
    {
        services
            .AddOptionsWithValidateOnStart<TvdbOptions, TvdbOptionsValidator>()
            .Bind(context.Configuration.GetSection(TvdbOptions.SectionName));

        services
            .AddOptionsWithValidateOnStart<TmdbOptions, TmdbOptionsValidator>()
            .Bind(context.Configuration.GetSection(TmdbOptions.SectionName));

        services.AddHttpClient<TvdbClient>(client =>
        {
            client.BaseAddress = new Uri("https://api4.thetvdb.com/v4/");
        })
        .AddHttpMessageHandler(() => new TerpHttpMetricsHandler("tvdb"))
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.DelayGenerator = RetryAfterDefaults.DelayGenerator;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.FailureRatio = 0.25;
        });

        services.AddHttpClient<TmdbClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
        })
        .AddHttpMessageHandler(() => new TerpHttpMetricsHandler("tmdb"))
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.DelayGenerator = RetryAfterDefaults.DelayGenerator;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.FailureRatio = 0.25;
        });
    }
}
