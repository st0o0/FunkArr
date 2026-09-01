namespace FunkArr.Search;

public sealed record MediathekViewWebManagerState(int InFlight)
{
    public static readonly MediathekViewWebManagerState Empty = new(0);
}

public static class MediathekViewWebManagerStateExtensions
{
    public static MediathekViewWebManagerState Increment(this MediathekViewWebManagerState state) =>
        state with { InFlight = state.InFlight + 1 };

    public static MediathekViewWebManagerState Decrement(this MediathekViewWebManagerState state) =>
        state with { InFlight = state.InFlight - 1 };

    public static bool HasCapacity(this MediathekViewWebManagerState state, int maxConcurrent) =>
        state.InFlight < maxConcurrent;
}
