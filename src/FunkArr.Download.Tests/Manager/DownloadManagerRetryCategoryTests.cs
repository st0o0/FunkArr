using FunkArr.Download.Manager;
using FunkArr.Messages;
using FunkArr.Messages.Download;
using FunkArr.Persistence;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download.Tests.Manager;

public sealed class DownloadManagerRetryCategoryTests
{
    [Fact]
    public void Retry_with_movie_category_preserves_category()
    {
        var state = DownloadManagerState.Empty;
        state.Apply(new ConcurrencyLimit(3));
        state.Apply(new ScheduleChange(true, null));

        var id = Guid.NewGuid();
        state.Apply(new DownloadEnqueued(id, PersistedDownloadPriority.Normal, PersistedMediaType.Movie));
        state.Apply(new DownloadDispatched(id));
        state.Apply(new DownloadDequeued(id));

        state.Apply(new DownloadEnqueued(id, PersistedDownloadPriority.Normal, PersistedMediaType.Movie));

        var entry = state.Queued.First(e => e.Id == id);
        Assert.Equal(MediaType.Movie, entry.Category);
    }

    [Fact]
    public void Retry_with_show_category_preserves_category()
    {
        var state = DownloadManagerState.Empty;
        var id = Guid.NewGuid();

        state.Apply(new DownloadEnqueued(id, PersistedDownloadPriority.Normal, PersistedMediaType.Show));
        state.Apply(new DownloadDispatched(id));
        state.Apply(new DownloadDequeued(id));

        state.Apply(new DownloadEnqueued(id, PersistedDownloadPriority.Normal, PersistedMediaType.Show));

        var entry = state.Queued.First(e => e.Id == id);
        Assert.Equal(MediaType.Show, entry.Category);
    }

    [Fact]
    public void Retry_without_category_defaults_to_show()
    {
        var state = DownloadManagerState.Empty;
        var id = Guid.NewGuid();

        state.Apply(new DownloadEnqueued(id, PersistedDownloadPriority.Normal));

        var entry = state.Queued.First(e => e.Id == id);
        Assert.Equal(MediaType.Show, entry.Category);
    }
}
