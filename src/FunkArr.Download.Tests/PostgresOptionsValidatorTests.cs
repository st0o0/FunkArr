using FunkArr.Core;

namespace FunkArr.Download.Tests;

public sealed class PostgresOptionsValidatorTests
{
    private readonly PostgresOptionsValidator _validator = new();

    [Fact]
    public void Default_options_passes()
    {
        var result = _validator.Validate(null, new PostgresOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Zero_port_fails()
    {
        var options = new PostgresOptions { Port = 0 };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("FunkArr:Postgres:Port must be between 1 and 65535", result.FailureMessage);
    }

    [Fact]
    public void Port_over_65535_fails()
    {
        var options = new PostgresOptions { Port = 70000 };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("FunkArr:Postgres:Port must be between 1 and 65535", result.FailureMessage);
    }

    [Fact]
    public void Empty_database_fails()
    {
        var options = new PostgresOptions { Database = "" };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("FunkArr:Postgres:Database must not be empty", result.FailureMessage);
    }
}
