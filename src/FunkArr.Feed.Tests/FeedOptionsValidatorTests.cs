using FunkArr.Core;

namespace FunkArr.Feed.Tests;

public sealed class FeedOptionsValidatorTests
{
    private readonly FeedOptionsValidator _validator = new();

    private static FeedOptions ValidOptions() => new()
    {
        Interval = TimeSpan.FromMinutes(15),
        PageSize = 1000,
        MaxPages = 20,
        MaxCatchUp = TimeSpan.FromDays(7),
        MaxConcurrency = 4,
        MinConfidence = 0.7,
        Retention = TimeSpan.FromDays(14),
        InitialLookback = TimeSpan.FromHours(48)
    };

    [Fact]
    public void Valid_config_passes()
    {
        var result = _validator.Validate(null, ValidOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Default_options_passes()
    {
        var result = _validator.Validate(null, new FeedOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Zero_interval_fails()
    {
        var options = ValidOptions();
        options.Interval = TimeSpan.Zero;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("FunkArr:Feed:Interval must be positive", result.FailureMessage);
    }

    [Fact]
    public void Zero_page_size_fails()
    {
        var options = ValidOptions();
        options.PageSize = 0;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("FunkArr:Feed:PageSize must be between 1 and 1000", result.FailureMessage);
    }

    [Fact]
    public void Page_size_over_1000_fails()
    {
        var options = ValidOptions();
        options.PageSize = 1001;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("FunkArr:Feed:PageSize must be between 1 and 1000", result.FailureMessage);
    }

    [Fact]
    public void Zero_max_pages_fails()
    {
        var options = ValidOptions();
        options.MaxPages = 0;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("FunkArr:Feed:MaxPages must be positive", result.FailureMessage);
    }

    [Fact]
    public void Zero_max_catch_up_fails()
    {
        var options = ValidOptions();
        options.MaxCatchUp = TimeSpan.Zero;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("FunkArr:Feed:MaxCatchUp must be positive", result.FailureMessage);
    }

    [Fact]
    public void Zero_max_concurrency_fails()
    {
        var options = ValidOptions();
        options.MaxConcurrency = 0;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("FunkArr:Feed:MaxConcurrency must be positive", result.FailureMessage);
    }

    [Fact]
    public void Negative_min_confidence_fails()
    {
        var options = ValidOptions();
        options.MinConfidence = -0.1;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("FunkArr:Feed:MinConfidence must be between 0 and 1", result.FailureMessage);
    }

    [Fact]
    public void Min_confidence_over_1_fails()
    {
        var options = ValidOptions();
        options.MinConfidence = 1.1;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("FunkArr:Feed:MinConfidence must be between 0 and 1", result.FailureMessage);
    }

    [Fact]
    public void Zero_retention_fails()
    {
        var options = ValidOptions();
        options.Retention = TimeSpan.Zero;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("FunkArr:Feed:Retention must be positive", result.FailureMessage);
    }

    [Fact]
    public void Zero_initial_lookback_fails()
    {
        var options = ValidOptions();
        options.InitialLookback = TimeSpan.Zero;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("FunkArr:Feed:InitialLookback must be positive", result.FailureMessage);
    }

    [Fact]
    public void Multiple_failures_reported_together()
    {
        var options = ValidOptions();
        options.Interval = TimeSpan.Zero;
        options.PageSize = 0;
        options.Retention = TimeSpan.Zero;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Interval must be positive", result.FailureMessage);
        Assert.Contains("PageSize must be between 1 and 1000", result.FailureMessage);
        Assert.Contains("Retention must be positive", result.FailureMessage);
    }
}
