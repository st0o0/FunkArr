using System.Net;
using FunkArr.Core.Configuration;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace FunkArr.Download;

internal sealed class RouteProxyFilter(IOptionsMonitor<RoutingOptions> _options) : IHttpMessageHandlerBuilderFilter
{
    private const string _prefix = "route:";

    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
    {
        return builder =>
        {
            next(builder);

            if (builder.Name is null || !builder.Name.StartsWith(_prefix, StringComparison.Ordinal))
            {
                return;
            }

            var routeName = builder.Name[_prefix.Length..];
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
