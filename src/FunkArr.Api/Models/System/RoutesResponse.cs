namespace FunkArr.Api.Models;

public sealed record RoutesResponse(
    RouteDefinitionResponse[] Definitions,
    ChannelRouteResponse[] ChannelRoutes,
    string DefaultRoute);

public sealed record RouteDefinitionResponse(string Name, string? Proxy);

public sealed record ChannelRouteResponse(string Pattern, string Route);
