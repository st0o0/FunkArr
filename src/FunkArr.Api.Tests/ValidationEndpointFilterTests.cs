using System.ComponentModel.DataAnnotations;
using FunkArr.Api.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FunkArr.Api.Tests;

public sealed class ValidationEndpointFilterTests
{
    private readonly ValidationEndpointFilter _filter = new();

    [Fact]
    public async Task Valid_model_passes_through()
    {
        var model = new ValidModel("test", 5);
        var context = CreateContext(model);
        var nextCalled = false;

        await _filter.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Invalid_model_returns_validation_problem()
    {
        var model = new ValidModel("", -1);
        var context = CreateContext(model);
        var nextCalled = false;

        var result = await _filter.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        Assert.False(nextCalled);
        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Null_arguments_are_skipped()
    {
        var context = CreateContext(null!);
        var nextCalled = false;

        await _filter.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Primitive_arguments_are_skipped()
    {
        var context = CreateContext(42);
        var nextCalled = false;

        await _filter.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        Assert.True(nextCalled);
    }

    private static EndpointFilterInvocationContext CreateContext(object? argument)
    {
        var httpContext = new DefaultHttpContext();
        return new DefaultEndpointFilterInvocationContext(httpContext, argument!);
    }

    private sealed record ValidModel(
        [property: Required] string Name,
        [property: Range(0, 100)] int Value);
}
