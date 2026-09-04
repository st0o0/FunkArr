namespace FunkArr.Messages.MetadataResolver;

public interface IEpisodeResolutionResponse;

public sealed record EpisodesResolved(
    ResolvedEpisode[] Episodes) : IEpisodeResolutionResponse;

public sealed record EpisodeResolutionFailed(
    string Reason) : IEpisodeResolutionResponse;
