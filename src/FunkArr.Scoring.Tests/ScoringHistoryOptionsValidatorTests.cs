using FunkArr.Core;

namespace FunkArr.Scoring.Tests;

public sealed class ScoringHistoryOptionsValidatorTests
{
    private readonly ScoringHistoryOptionsValidator _validator = new();

    [Fact]
    public void Default_options_passes()
    {
        var result = _validator.Validate(null, new ScoringHistoryOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Zero_max_snapshots_fails()
    {
        var options = new ScoringHistoryOptions { MaxSnapshots = 0 };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("FunkArr:ScoringHistory:MaxSnapshots must be positive", result.FailureMessage);
    }

    [Fact]
    public void Zero_max_age_days_fails()
    {
        var options = new ScoringHistoryOptions { MaxAgeDays = 0 };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("FunkArr:ScoringHistory:MaxAgeDays must be positive", result.FailureMessage);
    }

    [Fact]
    public void Zero_snapshot_interval_fails()
    {
        var options = new ScoringHistoryOptions { SnapshotInterval = 0 };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("FunkArr:ScoringHistory:SnapshotInterval must be positive", result.FailureMessage);
    }

    [Fact]
    public void Multiple_failures_reported()
    {
        var options = new ScoringHistoryOptions { MaxSnapshots = 0, MaxAgeDays = 0, SnapshotInterval = 0 };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("MaxSnapshots", result.FailureMessage);
        Assert.Contains("MaxAgeDays", result.FailureMessage);
        Assert.Contains("SnapshotInterval", result.FailureMessage);
    }
}
