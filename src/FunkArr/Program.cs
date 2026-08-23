using System.Reflection;
using FunkArr.Configuration;
using Serilog;
using Servus.Core.Application.Startup;

var builder = WebApplication.CreateBuilder(args);

var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0";

builder.Services.AddSerilog(config =>
{
    config
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .Enrich.FromLogContext()
        .Enrich.WithProperty("ApplicationVersion", version)
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
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
