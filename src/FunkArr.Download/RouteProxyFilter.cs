using System.Net;
using FunkArr.Core.Configuration;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace FunkArr.Download;

internal sealed class RouteProxyFilter(IOptionsMonitor<RoutingOptions> _options) : IHttpMessageHandlerBuilderFilter
{
    private const string Prefix = "route:";

    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
    {
        return builder =>
        {
            next(builder);

            if (builder.Name is null || !builder.Name.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return;
            }

            var routeName = builder.Name[Prefix.Length..];
            var definition = _options.CurrentValue.Definitions
                .Find(d => string.Equals(d.Name, routeName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(definition?.Proxy))
            {
                builder.PrimaryHandler = new HttpClientHandler
                {
                    Proxy = new WebProxy(definition.Proxy),
                    UseProxy = true
                };
            }
        };
    }
}
