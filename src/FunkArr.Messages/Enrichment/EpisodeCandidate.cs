namespace FunkArr.Messages.Enrichment;

public sealed record EpisodeCandidate(
    int Index,
    string Title,
    string? ConstructedTitle,
    DateTimeOffset? AiredAt,
    int Duration,
    string? ExistingSeason,
    string? ExistingEpisode);
