using FunkArr.ArrApi.Newznab.Models;
using FunkArr.Messages.Search;
using FunkArr.Messages.Shared;

namespace FunkArr.ArrApi.Newznab.Services;

public sealed class NewznabAttributeBuilder
{
    public List<NewznabAttribute> BuildAttributes(SearchResultItem item, NewznabCategory category)
    {
        var attrs = new List<NewznabAttribute>
        {
            new() { Name = "size", Value = item.Size.ToString() },
            new() { Name = "category", Value = category.CategoryId(item.Quality) }
        };

        if (item.Metadata?.Season is not null)
        {
            attrs.Add(new NewznabAttribute { Name = "season", Value = item.Metadata.Season });
        }

        if (item.Metadata?.Episode is not null)
        {
            attrs.Add(new NewznabAttribute { Name = "episode", Value = item.Metadata.Episode });
        }

        if (item.Metadata?.Ids?.TvdbId is not null)
        {
            attrs.Add(new NewznabAttribute { Name = "tvdbid", Value = item.Metadata.Ids.TvdbId.Value.ToString() });
        }

        if (item.Metadata?.Ids?.ImdbId is not null)
        {
            attrs.Add(new NewznabAttribute { Name = "imdb", Value = item.Metadata.Ids.ImdbId });
        }

        if (item.Metadata?.Ids?.TmdbId is not null)
        {
            attrs.Add(new NewznabAttribute { Name = "tmdbid", Value = item.Metadata.Ids.TmdbId.Value.ToString() });
        }

        return attrs;
    }
}
