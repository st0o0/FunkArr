namespace FunkArr.Search;

public sealed record MediathekViewWebManagerState(int InFlight)
{
    public static readonly MediathekViewWebManagerState Empty = new(0);

    public sealed record RequestStarted;
    public sealed record RequestCompleted;
}

public static class MediathekViewWebManagerStateExtensions
{
    public static MediathekViewWebManagerState Apply(this MediathekViewWebManagerState state, MediathekViewWebManagerState.RequestStarted _) =>
        new(InFlight: state.InFlight + 1);

    public static MediathekViewWebManagerState Apply(this MediathekViewWebManagerState state, MediathekViewWebManagerState.RequestCompleted _) =>
        new(InFlight: state.InFlight - 1);

    public static bool HasCapacity(this MediathekViewWebManagerState state, int maxConcurrent) =>
        state.InFlight < maxConcurrent;
}
