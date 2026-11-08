using FunkArr.ArrApi.Newznab.Models;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Search;
using FunkArr.Messages.Shared;

namespace FunkArr.ArrApi.Newznab.Services;

public sealed class RssItemBuilder(NewznabAttributeBuilder attributeBuilder)
{
    public Rss ToRss(
        SearchResultItem[] items, int total, int offset,
        string baseUrl, string apiKey, NewznabCategory category) =>
        CreateRss([.. items.Select(item => ToItem(item, category, baseUrl, apiKey))], total, offset);

    public Rss ToFeedRss(FeedEntry[] entries, int total, int offset, string baseUrl, string apiKey) =>
        CreateRss(
        [
            .. entries.Select(entry => ToItem(
                new SearchResultItem(entry.Title, entry.Channel, entry.Topic, entry.Url,
                    entry.Duration, entry.Size, entry.Quality, entry.AiredAt, entry.Score,
                    entry.SubtitleUrl, new MatchMetadata(entry.Ids, entry.Season, entry.Episode, null, null)),
                entry.MediaType == MediaType.Movie ? NewznabCategory.Movie : NewznabCategory.Tv,
                baseUrl,
                apiKey,
                entry.StableId,
                entry.FirstSeenAt))
        ], total, offset);

    internal static Rss CreateRss(List<Item> items, int total, int offset) =>
        new()
        {
            Channel = new Channel
            {
                Response = new NewznabResponse { Offset = offset, Total = total },
                Items = items
            }
        };

    internal Item ToItem(
        SearchResultItem item, NewznabCategory category, string baseUrl, string apiKey,
        string? guid = null, DateTimeOffset? pubDate = null)
    {
        var nzbId = new NzbPayload(
            item.Title,
            item.Url,
            string.IsNullOrEmpty(item.SubtitleUrl) ? null : item.SubtitleUrl,
            item.Channel,
            item.Duration,
            item.Size,
            category == NewznabCategory.Movie ? MediaType.Movie : MediaType.Show).Encode();
        var getNzbUrl = $"{baseUrl}/index/api?t=get&id={Uri.EscapeDataString(nzbId)}&apikey={Uri.EscapeDataString(apiKey)}";
        return new Item
        {
            Title = item.Title,
            Guid = new ItemGuid { Value = guid ?? nzbId, IsPermaLink = false },
            Link = getNzbUrl,
            PubDate = (pubDate ?? item.AiredAt ?? DateTimeOffset.UtcNow).ToString("R"),
            Category = category.DisplayName(item.Quality),
            Description = $"{item.Channel} - {item.Topic}",
            Enclosure = new Enclosure { Url = getNzbUrl, Length = item.Size },
            Attributes = attributeBuilder.BuildAttributes(item, category, pubDate)
        };
    }
}
