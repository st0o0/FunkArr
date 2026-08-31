using FunkArr.Configuration;
using Serilog;
using Servus.Core.Application.Startup;
using ApplicationSetupContainer = FunkArr.Configuration.ApplicationSetupContainer;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Debug()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Logging.ClearProviders();

    var runner = AppBuilder.Create(builder, b => b.Build())
        .WithSetup<LoggingSetupContainer>()
        .WithSetup<ServiceSetupContainer>()
        .WithSetup<AkkaSetupContainer>()
        .WithSetup<ApplicationSetupContainer>()
        .Build();

    await runner.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
