using FunkArr.Core;
using FunkArr.Enrichment;
using Terp.Diagnostics;
using Terp.Hosting;

namespace FunkArr.Configuration;

public sealed class EnrichmentSetupContainer : ISetup<IServiceCollection>
{
    public void Setup(IServiceCollection services, SetupContext _)
    {
        services
            .AddOptions<TvdbOptions>()
            .BindConfiguration(TvdbOptions.SectionName);

        services
            .AddOptions<TmdbOptions>()
            .BindConfiguration(TmdbOptions.SectionName);

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
