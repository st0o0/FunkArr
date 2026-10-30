using FunkArr.ArrApi.Newznab.Models;

namespace FunkArr.ArrApi.Newznab;

internal static class ValidationRss
{
    internal const string TvTitle = "FunkArr.Validation.S01E01.GERMAN.1080p.WEB.h264-FunkArr";
    internal const string MovieTitle = "FunkArr.Validation.2024.GERMAN.1080p.WEB.h264-FunkArr";
    internal const string TvGuid = "funkarr-validation-tv";
    internal const string MovieGuid = "funkarr-validation-movie";
    internal const long Size = 1_000_000_000;

    private const int _hdQuality = 1080;

    internal static Rss Create(NewznabCategory category, int offset, DateTimeOffset now, string baseUrl, string apiKey)
    {
        var safeOffset = Math.Max(0, offset);
        var item = CreateItem(category, now, baseUrl, apiKey);

        return new Rss
        {
            Channel = new Channel
            {
                Response = new NewznabResponse { Offset = safeOffset, Total = 1 },
                Items = safeOffset == 0 ? [item] : []
            }
        };
    }

    private static Item CreateItem(NewznabCategory category, DateTimeOffset now, string baseUrl, string apiKey)
    {
        var isMovie = category == NewznabCategory.Movie;
        var guid = isMovie ? MovieGuid : TvGuid;
        var link = $"{baseUrl}/index/api?t=get&id={guid}&apikey={Uri.EscapeDataString(apiKey)}";

        return new Item
        {
            Title = isMovie ? MovieTitle : TvTitle,
            Guid = new ItemGuid { Value = guid, IsPermaLink = false },
            Link = link,
            PubDate = now.ToString("R"),
            Category = category.DisplayName(_hdQuality),
            Description = "FunkArr validation entry for indexer tests and RSS sync",
            Enclosure = new Enclosure { Url = link, Length = Size },
            Attributes =
            [
                new NewznabAttribute { Name = "size", Value = Size.ToString() },
                new NewznabAttribute { Name = "category", Value = category.CategoryId(_hdQuality) }
            ]
        };
    }
}
