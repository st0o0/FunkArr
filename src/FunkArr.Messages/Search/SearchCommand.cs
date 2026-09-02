namespace FunkArr.Messages.Search;

public sealed record SearchCommand(
    string? Query,
    int? Cat,
    int? Limit,
    int? Offset,
    SearchCommand.ISearchParams? Params)
{
    public interface ISearchParams;

    public sealed record TvParams(int? Season, int? Episode, int? TvdbId, string? ImdbId) : ISearchParams;

    public sealed record MovieParams(string? ImdbId, int? TmdbId) : ISearchParams;
}
