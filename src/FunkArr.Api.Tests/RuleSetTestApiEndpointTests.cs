using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace FunkArr.Api.Tests;

public sealed class RuleSetTestApiEndpointTests
{
    [Fact]
    public void MapRuleSetApi_registers_test_endpoint()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        app.MapRuleSetApi();

        var endpoints = app as IEndpointRouteBuilder;
        var dataSource = endpoints.DataSources;

        Assert.NotEmpty(dataSource);
    }
}
