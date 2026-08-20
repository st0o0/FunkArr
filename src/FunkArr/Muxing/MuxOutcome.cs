namespace FunkArr.Muxing;

public abstract record MuxOutcome(string NzoId)
{
    public sealed record Success(string NzoId, string OutputPath) : MuxOutcome(NzoId);
    public sealed record Failure(string NzoId, string Reason) : MuxOutcome(NzoId);
    public sealed record Skipped(string NzoId, string Reason) : MuxOutcome(NzoId);
}
