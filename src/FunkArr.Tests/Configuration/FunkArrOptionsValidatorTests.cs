using FunkArr.Configuration;

namespace FunkArr.Tests.Configuration;

public class FunkArrOptionsValidatorTests
{
    private readonly FunkArrOptionsValidator _sut = new();

    private static FunkArrOptions ValidOptions() => new()
    {
        ApiKey = "test-key",
        DownloadPath = "/media/downloads",
        ConcurrentDownloads = 3,
        LogFormat = "text",
    };

    [Fact]
    public void Validate_DefaultSqlite_Succeeds()
    {
        var options = ValidOptions();

        var result = _sut.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_PostgresWithAllFields_Succeeds()
    {
        var options = ValidOptions();
        options.Postgres = new PostgresOptions
        {
            Host = "localhost",
            User = "funkarr",
            Password = "secret",
        };

        var result = _sut.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_PostgresWithoutUser_Fails()
    {
        var options = ValidOptions();
        options.Postgres = new PostgresOptions { Host = "localhost", Password = "secret" };

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("User", result.FailureMessage);
    }

    [Fact]
    public void Validate_PostgresWithoutPassword_Fails()
    {
        var options = ValidOptions();
        options.Postgres = new PostgresOptions { Host = "localhost", User = "funkarr" };

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Password", result.FailureMessage);
    }

    [Fact]
    public void Validate_PostgresHostEmpty_UsesSqlite_Succeeds()
    {
        var options = ValidOptions();
        options.Postgres = new PostgresOptions { Host = "", User = "", Password = "" };

        var result = _sut.Validate(null, options);

        Assert.True(result.Succeeded);
    }
}
