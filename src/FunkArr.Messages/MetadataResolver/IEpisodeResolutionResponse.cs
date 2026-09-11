namespace FunkArr.Messages.MetadataResolver;

public interface IEpisodeResolutionResponse;

public sealed record EpisodesResolved(
    ResolvedEpisode[] Episodes) : IEpisodeResolutionResponse;

public sealed record EpisodeResolutionFailed : IEpisodeResolutionResponse
{
    public string Reason { get; }
    public Exception? Cause { get; }

    public EpisodeResolutionFailed(string Reason)
    {
        this.Reason = Reason;
    }

    public EpisodeResolutionFailed(Exception Cause)
    {
        this.Cause = Cause;
        Reason = Cause.Message;
    }
}
