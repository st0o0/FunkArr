namespace FunkArr.Messages.Enrichment;

public sealed record EnrichEpisodes(
    int TvdbId,
    int? Season,
    EpisodeCandidate[] Candidates);
