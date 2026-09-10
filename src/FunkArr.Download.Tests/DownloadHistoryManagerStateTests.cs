using FunkArr.Download;
using FunkArr.Messages.Download;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download.Tests;

public sealed class DownloadHistoryManagerStateTests
{
    private static HistoryRecorded MakeRecorded(Guid id, string title = "Test", DownloadStatus status = DownloadStatus.Completed) =>
        new(id, title, "tv", 1_000_000, (int)status, "/downloads/test.mkv", null, 120, 1234567890);

    [Fact]
    public void Apply_HistoryRecorded_adds_record()
    {
        var id = Guid.NewGuid();
        var state = DownloadHistoryManagerState.Empty
            .Apply(MakeRecorded(id));

        Assert.Single(state.Records);
        Assert.Equal(id, state.Records[0].DownloadId);
        Assert.Equal("Test", state.Records[0].Title);
        Assert.Equal(DownloadStatus.Completed, state.Records[0].Status);
    }

    [Fact]
    public void Apply_HistoryRemoved_removes_record()
    {
        var id = Guid.NewGuid();
        var state = DownloadHistoryManagerState.Empty
            .Apply(MakeRecorded(id))
            .Apply(new HistoryRemoved(id));

        Assert.Empty(state.Records);
    }

    [Fact]
    public void Apply_HistoryRemoved_leaves_other_records()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var state = DownloadHistoryManagerState.Empty
            .Apply(MakeRecorded(id1, "First"))
            .Apply(MakeRecorded(id2, "Second"))
            .Apply(new HistoryRemoved(id1));

        Assert.Single(state.Records);
        Assert.Equal(id2, state.Records[0].DownloadId);
    }

    [Fact]
    public void Contains_returns_true_for_existing()
    {
        var id = Guid.NewGuid();
        var state = DownloadHistoryManagerState.Empty
            .Apply(MakeRecorded(id));

        Assert.True(state.Contains(id));
        Assert.False(state.Contains(Guid.NewGuid()));
    }

    [Fact]
    public void ToHistoryResult_maps_all_records()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var state = DownloadHistoryManagerState.Empty
            .Apply(MakeRecorded(id1, "Completed Video"))
            .Apply(new HistoryRecorded(id2, "Failed Video", "tv", 500_000,
                (int)DownloadStatus.Failed, null, "Connection refused", 0, 1234567890));

        var result = state.ToHistoryResult(new QueryHistory());

        Assert.Equal(2, result.Items.Length);
        Assert.Equal("Completed Video", result.Items[0].Title);
        Assert.Equal(DownloadStatus.Completed, result.Items[0].Status);
        Assert.Equal("Failed Video", result.Items[1].Title);
        Assert.Equal(DownloadStatus.Failed, result.Items[1].Status);
        Assert.Equal("Connection refused", result.Items[1].FailMessage);
    }

    [Fact]
    public void Failed_record_maps_correctly()
    {
        var id = Guid.NewGuid();
        var state = DownloadHistoryManagerState.Empty
            .Apply(new HistoryRecorded(id, "Broken", "movies", 2_000_000,
                (int)DownloadStatus.Failed, null, "Timeout", 0, 9999999999));

        var result = state.ToHistoryResult(new QueryHistory());

        Assert.Single(result.Items);
        Assert.Equal("", result.Items[0].RelativePath);
        Assert.Equal("Timeout", result.Items[0].FailMessage);
        Assert.Equal(9999999999L, result.Items[0].CompletedAt);
    }

    [Fact]
    public void ToHistoryResult_returns_all_when_limit_zero()
    {
        var state = DownloadHistoryManagerState.Empty
            .Apply(MakeRecorded(Guid.NewGuid(), "A"))
            .Apply(MakeRecorded(Guid.NewGuid(), "B"))
            .Apply(MakeRecorded(Guid.NewGuid(), "C"));

        var result = state.ToHistoryResult(new QueryHistory());

        Assert.Equal(3, result.Items.Length);
        Assert.Equal(3, result.TotalItems);
    }

    [Fact]
    public void ToHistoryResult_applies_start_and_limit()
    {
        var state = DownloadHistoryManagerState.Empty
            .Apply(MakeRecorded(Guid.NewGuid(), "A"))
            .Apply(MakeRecorded(Guid.NewGuid(), "B"))
            .Apply(MakeRecorded(Guid.NewGuid(), "C"))
            .Apply(MakeRecorded(Guid.NewGuid(), "D"));

        var result = state.ToHistoryResult(new QueryHistory(Start: 1, Limit: 2));

        Assert.Equal(2, result.Items.Length);
        Assert.Equal("B", result.Items[0].Title);
        Assert.Equal("C", result.Items[1].Title);
        Assert.Equal(4, result.TotalItems);
    }

    [Fact]
    public void ToHistoryResult_filters_by_category()
    {
        var state = DownloadHistoryManagerState.Empty
            .Apply(new HistoryRecorded(Guid.NewGuid(), "A", "tv", 1000, (int)DownloadStatus.Completed, null, null, 100, 123))
            .Apply(new HistoryRecorded(Guid.NewGuid(), "B", "movies", 1000, (int)DownloadStatus.Completed, null, null, 100, 123))
            .Apply(new HistoryRecorded(Guid.NewGuid(), "C", "tv", 1000, (int)DownloadStatus.Completed, null, null, 100, 123));

        var result = state.ToHistoryResult(new QueryHistory(Category: "tv"));

        Assert.Equal(2, result.Items.Length);
        Assert.Equal(2, result.TotalItems);
    }

    [Fact]
    public void ToHistoryResult_category_filter_is_case_insensitive()
    {
        var state = DownloadHistoryManagerState.Empty
            .Apply(new HistoryRecorded(Guid.NewGuid(), "A", "TV", 1000, (int)DownloadStatus.Completed, null, null, 100, 123));

        var result = state.ToHistoryResult(new QueryHistory(Category: "tv"));

        Assert.Single(result.Items);
    }

    [Fact]
    public void ToHistoryStats_empty_state_returns_zeros()
    {
        var result = DownloadHistoryManagerState.Empty.ToHistoryStats();

        Assert.Equal(0, result.TotalCompleted);
        Assert.Equal(0, result.TotalFailed);
        Assert.Equal(0L, result.TotalBytes);
        Assert.Equal(0, result.AverageDownloadTimeSeconds);
        Assert.Equal(0.0, result.SuccessRate);
    }

    [Fact]
    public void ToHistoryStats_computes_aggregates()
    {
        var state = DownloadHistoryManagerState.Empty
            .Apply(new HistoryRecorded(Guid.NewGuid(), "A", "tv", 1_000_000, (int)DownloadStatus.Completed, "/a.mkv", null, 100, 123))
            .Apply(new HistoryRecorded(Guid.NewGuid(), "B", "tv", 2_000_000, (int)DownloadStatus.Completed, "/b.mkv", null, 200, 456))
            .Apply(new HistoryRecorded(Guid.NewGuid(), "C", "movies", 500_000, (int)DownloadStatus.Failed, null, "Error", 0, 789));

        var result = state.ToHistoryStats();

        Assert.Equal(2, result.TotalCompleted);
        Assert.Equal(1, result.TotalFailed);
        Assert.Equal(3_500_000L, result.TotalBytes);
        Assert.Equal(150, result.AverageDownloadTimeSeconds);
        Assert.Equal(2.0 / 3.0, result.SuccessRate, 0.001);
    }

    [Fact]
    public void ToHistoryCategories_empty_state_returns_empty()
    {
        var result = DownloadHistoryManagerState.Empty.ToHistoryCategories();

        Assert.Empty(result.Categories);
    }

    [Fact]
    public void ToHistoryCategories_returns_distinct_sorted()
    {
        var state = DownloadHistoryManagerState.Empty
            .Apply(new HistoryRecorded(Guid.NewGuid(), "A", "tv", 1000, (int)DownloadStatus.Completed, null, null, 100, 123))
            .Apply(new HistoryRecorded(Guid.NewGuid(), "B", "movies", 1000, (int)DownloadStatus.Completed, null, null, 100, 123))
            .Apply(new HistoryRecorded(Guid.NewGuid(), "C", "TV", 1000, (int)DownloadStatus.Completed, null, null, 100, 123))
            .Apply(new HistoryRecorded(Guid.NewGuid(), "D", "tv", 1000, (int)DownloadStatus.Completed, null, null, 100, 123));

        var result = state.ToHistoryCategories();

        Assert.Equal(2, result.Categories.Length);
        Assert.Equal("movies", result.Categories[0]);
        Assert.Equal("tv", result.Categories[1]);
    }
}
