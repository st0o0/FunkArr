using FunkArr.Api;
using FunkArr.ArrApi;
using Servus.Application.Startup;

namespace FunkArr.Configuration;

public sealed class ArrApiSetupContainer : IServiceSetupContainer
{
    public void SetupServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddArrApiServices(configuration);

        services.AddHttpClient<ArrSetupClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddControllers()
            .AddApplicationPart(typeof(ArrApi.AssemblyMarker).Assembly);
    }
}
