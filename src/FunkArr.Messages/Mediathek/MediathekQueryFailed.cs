namespace FunkArr.Messages.Mediathek;

public sealed record MediathekQueryFailed : IMediathekResponse
{
    public string Reason { get; }
    public Exception? Cause { get; }

    public MediathekQueryFailed(string Reason)
    {
        this.Reason = Reason;
    }

    public MediathekQueryFailed(Exception Cause)
    {
        this.Cause = Cause;
        Reason = Cause.Message;
    }
}
