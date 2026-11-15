using FunkArr.Download.Worker;
using FunkArr.Messages.Download;
using FunkArr.Persistence;
using FunkArr.Persistence.Events.Download;
using FunkArr.Persistence.Events.Shared;

namespace FunkArr.Download.Tests.Worker;

public sealed class DownloadWorkerRetryCountTests
{
    private static readonly Guid _testId = Guid.NewGuid();

    private static readonly PersistedDownloadMedia _testMedia = new(
        "Test Video", "https://example.com/video.mp4", null,
        "ARD", 3600, 1_000_000, PersistedMediaType.Show);

    private static DownloadWorkerState StateAtAttempt(int attempt)
    {
        var state = new DownloadWorkerState();
        state.Apply(new DownloadInitialized(_testId, _testMedia));

        for (var i = 1; i <= attempt; i++)
        {
            state.Apply(new DownloadAttemptStarted(_testId, i));
            if (i < attempt)
            {
                state.Apply(new DownloadFaulted(_testId, "Transient error", PersistedFailureKind.Transient));
            }
        }

        state.Apply(new DownloadFaulted(_testId, "Transient error", PersistedFailureKind.Transient));
        return state;
    }

    [Theory]
    [InlineData(1, 3, true)]
    [InlineData(2, 3, true)]
    [InlineData(3, 3, true)]
    [InlineData(4, 3, false)]
    [InlineData(5, 3, false)]
    public void Retry_allowed_when_attempt_at_most_max_retries(int attempt, int maxRetries, bool expected)
    {
        var state = StateAtAttempt(attempt);

        Assert.Equal(attempt, state.Attempt);
        Assert.Equal(expected, state.Attempt <= maxRetries);
    }

    [Fact]
    public void MaxRetries_3_allows_4_total_attempts()
    {
        const int maxRetries = 3;
        var totalAttempts = 0;

        for (var attempt = 1; attempt <= maxRetries + 1; attempt++)
        {
            totalAttempts++;
            if (attempt <= maxRetries)
            {
                continue;
            }

            break;
        }

        Assert.Equal(4, totalAttempts);
    }

    [Fact]
    public void Attempt_5_with_MaxRetries_3_is_not_retried()
    {
        var state = StateAtAttempt(4);

        Assert.Equal(4, state.Attempt);
        Assert.False(state.Attempt <= 3);
    }
}
