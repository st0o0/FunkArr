using FunkArr.Core;

namespace FunkArr.Download.Tests;

public sealed class RoutingOptionsValidatorTests
{
    private readonly RoutingOptionsValidator _validator = new();

    [Fact]
    public void Default_options_passes()
    {
        var result = _validator.Validate(null, new RoutingOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Valid_full_config_passes()
    {
        var options = new RoutingOptions
        {
            Definitions =
            [
                new() { Name = "Direct" },
                new() { Name = "Austria", Proxy = "http://proxy-at:8888" },
                new() { Name = "Switzerland", Proxy = "https://proxy-ch:8888" }
            ],
            ChannelRoutes =
            [
                new() { Pattern = "ORF*", Route = "Austria" },
                new() { Pattern = "SRF*", Route = "Switzerland" }
            ],
            Default = "Direct"
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ChannelRoute_referencing_undefined_route_fails()
    {
        var options = new RoutingOptions
        {
            Definitions = [new() { Name = "Direct" }],
            ChannelRoutes = [new() { Pattern = "ORF*", Route = "NonExistent" }],
            Default = "Direct"
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("NonExistent", result.FailureMessage);
    }

    [Fact]
    public void Default_referencing_undefined_route_fails()
    {
        var options = new RoutingOptions
        {
            Definitions = [new() { Name = "Direct" }],
            Default = "Missing"
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Missing", result.FailureMessage);
    }

    [Fact]
    public void Duplicate_route_names_fails()
    {
        var options = new RoutingOptions
        {
            Definitions =
            [
                new() { Name = "Direct" },
                new() { Name = "Direct" }
            ],
            Default = "Direct"
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Duplicate", result.FailureMessage);
    }

    [Fact]
    public void Invalid_proxy_uri_fails()
    {
        var options = new RoutingOptions
        {
            Definitions = [new() { Name = "Bad", Proxy = "not-a-uri" }],
            Default = "Bad"
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("invalid Proxy URI", result.FailureMessage);
    }

    [Fact]
    public void Non_http_proxy_scheme_fails()
    {
        var options = new RoutingOptions
        {
            Definitions = [new() { Name = "Bad", Proxy = "ftp://proxy:8888" }],
            Default = "Bad"
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("http or https", result.FailureMessage);
    }

    [Fact]
    public void Empty_pattern_fails()
    {
        var options = new RoutingOptions
        {
            Definitions = [new() { Name = "Direct" }],
            ChannelRoutes = [new() { Pattern = "", Route = "Direct" }],
            Default = "Direct"
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("empty Pattern", result.FailureMessage);
    }

    [Fact]
    public void Empty_route_name_fails()
    {
        var options = new RoutingOptions
        {
            Definitions = [new() { Name = "" }],
            Default = ""
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("empty Name", result.FailureMessage);
    }
}
