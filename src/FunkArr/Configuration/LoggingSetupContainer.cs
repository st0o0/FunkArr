using Serilog;
using Servus.Core.Application.Startup;

namespace FunkArr.Configuration;

public sealed class LoggingSetupContainer : IServiceSetupContainer
{
    public void SetupServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilog(config =>
        {
            config
                .ReadFrom.Configuration(configuration)
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ApplicationVersion",
                    typeof(LoggingSetupContainer).Assembly.GetName().Version?.ToString() ?? "0.0.0")
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
        });
    }
}
