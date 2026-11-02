using FunkArr.Api;
using FunkArr.ArrApi;
using Terp.Hosting;

namespace FunkArr.Configuration;

public sealed class ArrApiSetupContainer : ISetup<IServiceCollection>
{
    public void Setup(IServiceCollection services, SetupContext context)
    {
        services.AddArrApiServices(context.Configuration);

        services.AddHttpClient<ArrSetupClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddControllers()
            .AddApplicationPart(typeof(ArrApi.AssemblyMarker).Assembly);
    }
}
