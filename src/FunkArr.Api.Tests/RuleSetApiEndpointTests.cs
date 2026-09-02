using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace FunkArr.Api.Tests;

public sealed class RuleSetApiEndpointTests
{
    [Fact]
    public void MapRuleSetApi_registers_endpoints()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        app.MapRuleSetApi();

        var endpoints = app as IEndpointRouteBuilder;
        var dataSource = endpoints.DataSources;

        Assert.NotEmpty(dataSource);
    }
}
