namespace FunkArr.Messages.Enrichment;

public abstract record EnrichEpisodesResponse;

public sealed record EnrichEpisodesCompleted(
    EnrichedEpisode[] Episodes) : EnrichEpisodesResponse;

public sealed record EnrichEpisodesFailed(Exception Cause) : EnrichEpisodesResponse;
