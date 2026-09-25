using FunkArr.Core;
using FunkArr.Tests.Shared;

namespace FunkArr.Download.Tests;

public sealed class RouteResolverTests
{
    [Fact]
    public void No_config_returns_direct_with_no_proxy()
    {
        var resolver = CreateResolver(new RoutingOptions());

        var result = resolver.Resolve("ARD");

        Assert.Equal("Direct", result.Name);
        Assert.Null(result.ProxyUrl);
    }

    [Fact]
    public void Channel_matching_pattern_returns_mapped_route()
    {
        var options = new RoutingOptions
        {
            Definitions =
            [
                new() { Name = "Direct" },
                new() { Name = "Austria", Proxy = "http://proxy-at:8888" }
            ],
            ChannelRoutes = [new() { Pattern = "ORF*", Route = "Austria" }],
            Default = "Direct"
        };

        var result = CreateResolver(options).Resolve("ORF");

        Assert.Equal("Austria", result.Name);
        Assert.Equal("http://proxy-at:8888", result.ProxyUrl);
    }

    [Fact]
    public void Unmatched_channel_returns_default()
    {
        var options = new RoutingOptions
        {
            Definitions =
            [
                new() { Name = "Direct" },
                new() { Name = "Austria", Proxy = "http://proxy-at:8888" }
            ],
            ChannelRoutes = [new() { Pattern = "ORF*", Route = "Austria" }],
            Default = "Direct"
        };

        var result = CreateResolver(options).Resolve("ZDF");

        Assert.Equal("Direct", result.Name);
        Assert.Null(result.ProxyUrl);
    }

    [Fact]
    public void First_matching_pattern_wins()
    {
        var options = new RoutingOptions
        {
            Definitions =
            [
                new() { Name = "Direct" },
                new() { Name = "Austria", Proxy = "http://proxy-at:8888" },
                new() { Name = "Switzerland", Proxy = "http://proxy-ch:8888" }
            ],
            ChannelRoutes =
            [
                new() { Pattern = "ORF*", Route = "Austria" },
                new() { Pattern = "OR*", Route = "Switzerland" }
            ],
            Default = "Direct"
        };

        var result = CreateResolver(options).Resolve("ORF");

        Assert.Equal("Austria", result.Name);
    }

    [Fact]
    public void Wildcard_pattern_matches_subchannel()
    {
        var options = new RoutingOptions
        {
            Definitions =
            [
                new() { Name = "Direct" },
                new() { Name = "Switzerland", Proxy = "http://proxy-ch:8888" }
            ],
            ChannelRoutes = [new() { Pattern = "SRF*", Route = "Switzerland" }],
            Default = "Direct"
        };

        var result = CreateResolver(options).Resolve("SRF zwei");

        Assert.Equal("Switzerland", result.Name);
        Assert.Equal("http://proxy-ch:8888", result.ProxyUrl);
    }

    [Fact]
    public void Direct_route_has_null_proxy()
    {
        var options = new RoutingOptions
        {
            Definitions = [new() { Name = "Direct" }],
            ChannelRoutes = [],
            Default = "Direct"
        };

        var result = CreateResolver(options).Resolve("ARD");

        Assert.Null(result.ProxyUrl);
    }

    private static RouteResolver CreateResolver(RoutingOptions options) =>
        new(new TestOptionsMonitor<RoutingOptions>(options));
}
