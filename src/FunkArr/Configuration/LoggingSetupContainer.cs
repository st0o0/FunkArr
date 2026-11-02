using FunkArr.Api;
using Serilog;
using Terp.Hosting;

namespace FunkArr.Configuration;

public sealed class LoggingSetupContainer : ISetup<IServiceCollection>
{
    public void Setup(IServiceCollection services, SetupContext context)
    {
        var ringBuffer = new RingBufferSink();
        services.AddSingleton(ringBuffer);

        services.AddSerilog(config =>
        {
            config
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ApplicationVersion",
                    typeof(LoggingSetupContainer).Assembly.GetName().Version?.ToString() ?? "0.0.0")
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Sink(ringBuffer);
        });
    }
}
