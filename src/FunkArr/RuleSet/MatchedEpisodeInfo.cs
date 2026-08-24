using FunkArr.Search;
using FunkArr.Search.Resolvers;

namespace FunkArr.RuleSet;

public sealed record MatchedEpisodeInfo
{
    public required TvdbEpisodeInfo Episode { get; init; }
    public required string ShowName { get; init; }
    public required string MatchedTitle { get; init; }
}
