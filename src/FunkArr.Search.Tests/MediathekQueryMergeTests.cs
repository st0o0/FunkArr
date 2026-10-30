using FunkArr.Messages.Mediathek;

namespace FunkArr.Search.Tests;

public sealed class MediathekQueryMergeTests
{
    [Fact]
    public void Merge_deduplicates_items_by_video_url_and_keeps_first_occurrence()
    {
        var past = Item("Herz aus Eis", hd: "https://x.com/past-hd.mp4");
        var pastAgain = past with { Title = "Herz aus Eis (dupe)" };
        var future = Item("Borowski", video: "https://x.com/future.mp4");

        var merged = MediathekQueryMerge.Merge(
        [
            new QueryMediathekCompleted([past], 120, 1_761_800_000),
            new QueryMediathekCompleted([future, pastAgain], 140, 1_761_803_600)
        ]);

        var completed = Assert.IsType<QueryMediathekCompleted>(merged);
        Assert.Multiple(
            () => Assert.Equal(new[] { "Herz aus Eis", "Borowski" }, completed.Items.Select(i => i.Title).ToArray()),
            () => Assert.Equal(140, completed.Total),
            () => Assert.Equal(1_761_803_600, completed.ListTimestamp));
    }

    [Fact]
    public void Merge_falls_back_to_channel_title_timestamp_without_urls()
    {
        var a = Item("Folge 1");
        var b = Item("Folge 2");

        var merged = MediathekQueryMerge.Merge(
        [
            new QueryMediathekCompleted([a, b], 2),
            new QueryMediathekCompleted([a], 1)
        ]);

        var completed = Assert.IsType<QueryMediathekCompleted>(merged);
        Assert.Equal(new[] { "Folge 1", "Folge 2" }, completed.Items.Select(i => i.Title).ToArray());
    }

    [Fact]
    public void Queue_full_wins_over_error_and_success()
    {
        var merged = MediathekQueryMerge.Merge(
        [
            new QueryMediathekCompleted([Item("x", video: "https://x.com/x.mp4")], 1),
            new QueryMediathekError(new InvalidOperationException("boom")),
            new QueryMediathekQueueFull()
        ]);

        Assert.IsType<QueryMediathekQueueFull>(merged);
    }

    [Fact]
    public void Error_wins_over_success()
    {
        var merged = MediathekQueryMerge.Merge(
        [
            new QueryMediathekCompleted([Item("x", video: "https://x.com/x.mp4")], 1),
            new QueryMediathekError(new InvalidOperationException("boom"))
        ]);

        var error = Assert.IsType<QueryMediathekError>(merged);
        Assert.Equal("boom", error.Cause.Message);
    }

    [Fact]
    public void Single_response_is_returned_unchanged_in_content()
    {
        var item = Item("Solo", video: "https://x.com/solo.mp4");

        var merged = MediathekQueryMerge.Merge([new QueryMediathekCompleted([item], 7, 42)]);

        var completed = Assert.IsType<QueryMediathekCompleted>(merged);
        Assert.Multiple(
            () => Assert.Equal(item, Assert.Single(completed.Items)),
            () => Assert.Equal(7, completed.Total),
            () => Assert.Equal(42, completed.ListTimestamp));
    }

    private static MediathekItem Item(string title, string? video = null, string? hd = null) =>
        new("ARD", "Tatort", title, null, 1_761_800_000, 5400, 0, null, video, hd, null, null);
}
