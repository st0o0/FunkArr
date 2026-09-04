using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace FunkArr.Api.Tests;

public sealed class MediathekApiEndpointTests
{
    [Fact]
    public void MapMediathekApi_registers_search_endpoint()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        app.MapMediathekApi();

        var endpoints = app as IEndpointRouteBuilder;
        var dataSource = endpoints.DataSources;

        Assert.NotEmpty(dataSource);
    }
}
