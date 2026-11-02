using FunkArr.Configuration;
using Serilog;
using Terp.Hosting;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Debug()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Logging.ClearProviders();

    builder.Setups()
        .Add<LoggingSetupContainer>()
        .Add<TelemetrySetupContainer>()
        .Add<CoreSetupContainer>()
        .Add<AkkaSetupContainer>()
        .Add<SearchSetupContainer>()
        .Add<DownloadSetupContainer>()
        .Add<ScoringSetupContainer>()
        .Add<RuleSetSetupContainer>()
        .Add<EnrichmentSetupContainer>()
        .Add<FeedSetupContainer>()
        .Add<ArrApiSetupContainer>();

    var app = builder.Build();

    app.Setups()
        .Add<SearchSetupContainer>()
        .Add<DownloadSetupContainer>()
        .Add<RuleSetSetupContainer>()
        .Add<ApplicationSetupContainer>();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
