namespace FunkArr.Messages.MetadataResolver;

public interface IMovieResolutionResponse;

public sealed record MoviesResolved(
    MovieResolved[] Movies) : IMovieResolutionResponse;

public sealed record MovieResolutionFailed : IMovieResolutionResponse
{
    public string Reason { get; }
    public Exception? Cause { get; }

    public MovieResolutionFailed(string Reason)
    {
        this.Reason = Reason;
    }

    public MovieResolutionFailed(Exception Cause)
    {
        this.Cause = Cause;
        Reason = Cause.Message;
    }
}
