namespace FunkArr.Core;

public sealed class RoutingOptions
{
    public const string SectionName = "FunkArr:Routes";

    public List<RouteDefinition> Definitions { get; set; } = [new() { Name = "Direct" }];
    public List<ChannelRoute> ChannelRoutes { get; set; } = [];
    public string Default { get; set; } = "Direct";
}

public sealed class RouteDefinition
{
    public string Name { get; set; } = "";
    public string? Proxy { get; set; }
}

public sealed class ChannelRoute
{
    public string Pattern { get; set; } = "";
    public string Route { get; set; } = "";
}
