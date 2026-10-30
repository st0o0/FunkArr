using FunkArr.Messages.Mediathek;

namespace FunkArr.Messages.Search;

public sealed record EvaluateSeries(
    Guid SearchId,
    SearchSource Source,
    MediathekItem[] Items) : IWithSearchId;
