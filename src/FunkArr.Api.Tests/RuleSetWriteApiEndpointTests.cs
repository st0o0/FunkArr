using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace FunkArr.Api.Tests;

public sealed class RuleSetWriteApiEndpointTests
{
    [Fact]
    public void MapRuleSetWriteApi_registers_endpoints()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        app.MapRuleSetWriteApi();

        var endpoints = app as IEndpointRouteBuilder;
        var dataSource = endpoints.DataSources;

        Assert.NotEmpty(dataSource);
    }
}
