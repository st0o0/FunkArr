namespace FunkArr.Core;

public sealed class MatchHistoryOptions
{
    public const string SectionName = "FunkArr:MatchHistory";

    public int MaxSnapshots { get; set; } = 100;

    public int MaxAgeDays { get; set; } = 30;

    public int SnapshotInterval { get; set; } = 20;
}
