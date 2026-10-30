using FunkArr.Messages.Mediathek;

namespace FunkArr.Messages.Search;

public sealed record EvaluateMovies(
    Guid SearchId,
    SearchSource Source,
    MediathekItem[] Items) : IWithSearchId;
