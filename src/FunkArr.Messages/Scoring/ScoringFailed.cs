namespace FunkArr.Messages.Scoring;

public sealed record ScoringFailed : IScoringResponse
{
    public string Reason { get; }
    public Exception? Cause { get; }

    public ScoringFailed(string Reason)
    {
        this.Reason = Reason;
    }

    public ScoringFailed(Exception Cause)
    {
        this.Cause = Cause;
        Reason = Cause.Message;
    }
}
