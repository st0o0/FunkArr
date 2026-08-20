using FunkArr.Search;

namespace FunkArr.Tests.Search;

public class MediathekQueryTests
{
    [Fact]
    public void MediathekQuery_CanBeConstructed()
    {
        var query = new MediathekQuery
        {
            Queries =
            [
                new MediathekQueryItem { Fields = ["topic", "title"], Query = "Tatort" },
            ],
        };

        Assert.Single(query.Queries);
        Assert.Equal("Tatort", query.Queries[0].Query);
        Assert.Equal("desc", query.SortOrder);
        Assert.Equal(5000, query.Size);
    }

    [Fact]
    public void MediathekResultItem_DefaultsToEmpty()
    {
        var item = new MediathekResultItem();

        Assert.Equal(string.Empty, item.Channel);
        Assert.Equal(string.Empty, item.Topic);
        Assert.Equal(string.Empty, item.Title);
        Assert.Equal(0, item.Duration);
    }

    [Fact]
    public void TvdbShowInfo_DefaultsToEmpty()
    {
        var info = new TvdbShowInfo();

        Assert.Equal(string.Empty, info.SeriesName);
        Assert.Empty(info.Aliases);
    }
}
