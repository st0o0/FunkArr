using FunkArr.Messages.Mediathek;
using ApiModels = FunkArr.Api.Models;

namespace FunkArr.Api.Extensions;

internal static class MediathekMappingExtensions
{
    internal static ApiModels.MediathekSearchResult ToApi(this MediathekItem item) =>
        new(item.Title,
            item.Topic,
            item.Channel,
            item.Duration,
            EstimateQuality(item),
            item.Description,
            item.Timestamp,
            item.Size,
            item.UrlSubtitle is not null,
            item.UrlVideoHd is not null,
            item.UrlWebsite);

    internal static int EstimateQuality(MediathekItem item)
    {
        if (item.UrlVideoHd is not null)
        {
            return 1080;
        }

        if (item.UrlVideo is not null)
        {
            return 720;
        }

        if (item.UrlVideoLow is not null)
        {
            return 480;
        }

        return 0;
    }
}
