using System.Reflection;
using FunkArr.Configuration;
using Serilog;
using Serilog.Formatting.Compact;
using Servus.Core.Application.Startup;

var builder = WebApplication.CreateBuilder(args);

var dataPath = builder.Configuration
    .GetSection(FunkArrOptions.SectionName)
    .GetValue<string>("PersistencePath") ?? "data/funkarr.db";
var configJsonPath = Path.Combine(Path.GetDirectoryName(dataPath) ?? "data", "config.json");
builder.Configuration.AddJsonFile(configJsonPath, optional: true, reloadOnChange: true);

var logFormat = builder.Configuration
    .GetSection(FunkArrOptions.SectionName)
    .GetValue<string>("LogFormat") ?? "text";

var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0";

builder.Services.AddSerilog(config =>
{
    config
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .Enrich.FromLogContext()
        .Enrich.WithProperty("ApplicationVersion", version);

    if (logFormat == "json")
    {
        config.WriteTo.Console(new CompactJsonFormatter());
    }
    else
    {
        config.WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
    }
});
builder.Logging.ClearProviders();

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(6969);
    });
}

var runner = AppBuilder.Create(builder, b => b.Build())
    .WithSetup<FunkArrServiceSetup>()
    .WithSetup<FunkArrActorSystemSetup>()
    .WithSetup<FunkArrApplicationSetup>()
    .Build();

await runner.RunAsync();
