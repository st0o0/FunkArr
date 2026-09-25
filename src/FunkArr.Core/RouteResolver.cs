using System.IO.Enumeration;
using Microsoft.Extensions.Options;

namespace FunkArr.Core;

public sealed class RouteResolver(IOptionsMonitor<RoutingOptions> options) : IRouteResolver
{
    public ResolvedRoute Resolve(string channel)
    {
        var opts = options.CurrentValue;
        var routeName = opts.Default;

        foreach (var mapping in opts.ChannelRoutes)
        {
            if (FileSystemName.MatchesSimpleExpression(mapping.Pattern, channel))
            {
                routeName = mapping.Route;
                break;
            }
        }

        var definition = opts.Definitions.Find(d =>
            string.Equals(d.Name, routeName, StringComparison.OrdinalIgnoreCase));

        return new ResolvedRoute(
            definition?.Name ?? routeName,
            definition?.Proxy);
    }
}
