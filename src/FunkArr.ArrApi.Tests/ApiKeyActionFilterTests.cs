using FunkArr.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace FunkArr.ArrApi.Tests;

public sealed class ApiKeyActionFilterTests
{
    private static readonly IOptions<FunkArrOptions> _options =
        Options.Create(new FunkArrOptions { ApiKey = "test-api-key" });

    [Fact]
    public async Task NewznabFilter_blocks_missing_api_key()
    {
        var filter = new NewznabApiKeyFilter(_options);
        var context = CreateContext(apiKey: null);
        var called = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            called = true;
            return Task.FromResult(new ActionExecutedContext(context, [], null!));
        });

        Assert.False(called);
        Assert.NotNull(context.Result);
        Assert.IsType<ContentResult>(context.Result);
        Assert.Equal(403, ((ContentResult)context.Result).StatusCode);
    }

    [Fact]
    public async Task NewznabFilter_blocks_wrong_api_key()
    {
        var filter = new NewznabApiKeyFilter(_options);
        var context = CreateContext(apiKey: "wrong-key");
        var called = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            called = true;
            return Task.FromResult(new ActionExecutedContext(context, [], null!));
        });

        Assert.False(called);
        Assert.NotNull(context.Result);
    }

    [Fact]
    public async Task NewznabFilter_passes_valid_api_key()
    {
        var filter = new NewznabApiKeyFilter(_options);
        var context = CreateContext(apiKey: "test-api-key");
        var called = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            called = true;
            return Task.FromResult(new ActionExecutedContext(context, [], null!));
        });

        Assert.True(called);
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task SabnzbdFilter_blocks_missing_api_key()
    {
        var filter = new SabnzbdApiKeyFilter(_options);
        var context = CreateContext(apiKey: null);
        var called = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            called = true;
            return Task.FromResult(new ActionExecutedContext(context, [], null!));
        });

        Assert.False(called);
        Assert.NotNull(context.Result);
        var jsonResult = Assert.IsType<JsonResult>(context.Result);
        Assert.Equal(403, jsonResult.StatusCode);
    }

    [Fact]
    public async Task SabnzbdFilter_passes_valid_api_key()
    {
        var filter = new SabnzbdApiKeyFilter(_options);
        var context = CreateContext(apiKey: "test-api-key");
        var called = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            called = true;
            return Task.FromResult(new ActionExecutedContext(context, [], null!));
        });

        Assert.True(called);
        Assert.Null(context.Result);
    }

    private static ActionExecutingContext CreateContext(string? apiKey)
    {
        var httpContext = new DefaultHttpContext();
        if (apiKey is not null)
            httpContext.Request.QueryString = new QueryString($"?apikey={apiKey}");

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(actionContext, [], new Dictionary<string, object?>(), null!);
    }
}
