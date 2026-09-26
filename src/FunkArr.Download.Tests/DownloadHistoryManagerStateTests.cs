using FunkArr.Messages;
using FunkArr.Messages.Download;
using FunkArr.Persistence;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download.Tests;

public sealed class DownloadHistoryManagerStateTests
{
    private static DownloadHistoryRecorded MakeRecorded(Guid id, string title = "Test", PersistedDownloadStatus status = PersistedDownloadStatus.Completed) =>
        new(id, title, PersistedMediaType.Show, 1_000_000, status, "/downloads/test.mkv", null, 120, 1234567890);

    [Fact]
    public void Apply_DownloadHistoryRecorded_adds_record()
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
            .Apply(new DownloadHistoryRecorded(id2, "Failed Video", PersistedMediaType.Show, 500_000,
                PersistedDownloadStatus.Failed, null, "Connection refused", 0, 1234567890));

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
            .Apply(new DownloadHistoryRecorded(id, "Broken", PersistedMediaType.Movie, 2_000_000,
                PersistedDownloadStatus.Failed, null, "Timeout", 0, 9999999999));

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
            .Apply(new DownloadHistoryRecorded(Guid.NewGuid(), "A", PersistedMediaType.Show, 1000, PersistedDownloadStatus.Completed, null, null, 100, 123))
            .Apply(new DownloadHistoryRecorded(Guid.NewGuid(), "B", PersistedMediaType.Movie, 1000, PersistedDownloadStatus.Completed, null, null, 100, 123))
            .Apply(new DownloadHistoryRecorded(Guid.NewGuid(), "C", PersistedMediaType.Show, 1000, PersistedDownloadStatus.Completed, null, null, 100, 123));

        var result = state.ToHistoryResult(new QueryHistory(Category: MediaType.Show));

        Assert.Equal(2, result.Items.Length);
        Assert.Equal(2, result.TotalItems);
    }

    [Fact]
    public void ToHistoryResult_category_filter_is_case_insensitive()
    {
        var state = DownloadHistoryManagerState.Empty
            .Apply(new DownloadHistoryRecorded(Guid.NewGuid(), "A", PersistedMediaType.Show, 1000, PersistedDownloadStatus.Completed, null, null, 100, 123));

        var result = state.ToHistoryResult(new QueryHistory(Category: MediaType.Show));

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
            .Apply(new DownloadHistoryRecorded(Guid.NewGuid(), "A", PersistedMediaType.Show, 1_000_000, PersistedDownloadStatus.Completed, "/a.mkv", null, 100, 123))
            .Apply(new DownloadHistoryRecorded(Guid.NewGuid(), "B", PersistedMediaType.Show, 2_000_000, PersistedDownloadStatus.Completed, "/b.mkv", null, 200, 456))
            .Apply(new DownloadHistoryRecorded(Guid.NewGuid(), "C", PersistedMediaType.Movie, 500_000, PersistedDownloadStatus.Failed, null, "Error", 0, 789));

        var result = state.ToHistoryStats();

        Assert.Equal(2, result.TotalCompleted);
        Assert.Equal(1, result.TotalFailed);
        Assert.Equal(3_000_000L, result.TotalBytes);
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
            .Apply(new DownloadHistoryRecorded(Guid.NewGuid(), "A", PersistedMediaType.Show, 1000, PersistedDownloadStatus.Completed, null, null, 100, 123))
            .Apply(new DownloadHistoryRecorded(Guid.NewGuid(), "B", PersistedMediaType.Movie, 1000, PersistedDownloadStatus.Completed, null, null, 100, 123))
            .Apply(new DownloadHistoryRecorded(Guid.NewGuid(), "C", PersistedMediaType.Show, 1000, PersistedDownloadStatus.Completed, null, null, 100, 123))
            .Apply(new DownloadHistoryRecorded(Guid.NewGuid(), "D", PersistedMediaType.Show, 1000, PersistedDownloadStatus.Completed, null, null, 100, 123));

        var result = state.ToHistoryCategories();

        Assert.Equal(2, result.Categories.Length);
        Assert.Equal("movie", result.Categories[0]);
        Assert.Equal("show", result.Categories[1]);
    }

    [Fact]
    public void TrimIfNeeded_removes_oldest_when_over_max()
    {
        var state = DownloadHistoryManagerState.Empty;
        for (var i = 0; i < 5; i++)
        {
            state = state.Apply(MakeRecorded(Guid.NewGuid(), $"Record {i}"));
        }

        var (trimmed, count) = state.TrimIfNeeded(3);

        Assert.Equal(2, count);
        Assert.Equal(3, trimmed.Records.Count);
        Assert.Equal("Record 2", trimmed.Records[0].Title);
    }

    [Fact]
    public void TrimIfNeeded_no_trim_when_under_max()
    {
        var state = DownloadHistoryManagerState.Empty
            .Apply(MakeRecorded(Guid.NewGuid(), "A"))
            .Apply(MakeRecorded(Guid.NewGuid(), "B"));

        var (trimmed, count) = state.TrimIfNeeded(5);

        Assert.Equal(0, count);
        Assert.Equal(2, trimmed.Records.Count);
    }

    [Fact]
    public void TrimIfNeeded_updates_stats()
    {
        var state = DownloadHistoryManagerState.Empty
            .Apply(new DownloadHistoryRecorded(Guid.NewGuid(), "A", PersistedMediaType.Show, 1_000, PersistedDownloadStatus.Completed, null, null, 100, 1))
            .Apply(new DownloadHistoryRecorded(Guid.NewGuid(), "B", PersistedMediaType.Show, 2_000, PersistedDownloadStatus.Completed, null, null, 200, 2))
            .Apply(new DownloadHistoryRecorded(Guid.NewGuid(), "C", PersistedMediaType.Show, 3_000, PersistedDownloadStatus.Completed, null, null, 300, 3));

        var (trimmed, _) = state.TrimIfNeeded(1);

        Assert.Equal(1, trimmed.Stats.TotalCompleted);
        Assert.Equal(3_000L, trimmed.Stats.TotalBytes);
    }

    [Fact]
    public void TrimIfNeeded_updates_index()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();
        var state = DownloadHistoryManagerState.Empty
            .Apply(MakeRecorded(id1, "A"))
            .Apply(MakeRecorded(id2, "B"))
            .Apply(MakeRecorded(id3, "C"));

        var (trimmed, _) = state.TrimIfNeeded(1);

        Assert.False(trimmed.Contains(id1));
        Assert.False(trimmed.Contains(id2));
        Assert.True(trimmed.Contains(id3));
    }

    [Fact]
    public void Stats_updated_on_remove()
    {
        var id = Guid.NewGuid();
        var state = DownloadHistoryManagerState.Empty
            .Apply(new DownloadHistoryRecorded(id, "A", PersistedMediaType.Show, 1_000, PersistedDownloadStatus.Completed, null, null, 100, 1));

        Assert.Equal(1, state.Stats.TotalCompleted);

        state = state.Apply(new HistoryRemoved(id));

        Assert.Equal(0, state.Stats.TotalCompleted);
        Assert.Equal(0L, state.Stats.TotalBytes);
    }

    [Fact]
    public void Roundtrip_persistence_preserves_state()
    {
        var state = DownloadHistoryManagerState.Empty
            .Apply(MakeRecorded(Guid.NewGuid(), "A"))
            .Apply(new DownloadHistoryRecorded(Guid.NewGuid(), "B", PersistedMediaType.Movie, 500, PersistedDownloadStatus.Failed, null, "Error", 0, 2));

        var persisted = state.GetPersistenceState();
        var restored = DownloadHistoryManagerStateExtensions.FromPersistence(persisted);

        Assert.Equal(state.Records.Count, restored.Records.Count);
        Assert.Equal(state.Stats.TotalCompleted, restored.Stats.TotalCompleted);
        Assert.Equal(state.Stats.TotalFailed, restored.Stats.TotalFailed);
        Assert.Equal(state.Stats.TotalBytes, restored.Stats.TotalBytes);
    }
}
