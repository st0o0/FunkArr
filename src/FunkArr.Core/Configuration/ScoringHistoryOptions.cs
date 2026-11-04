namespace FunkArr.Core;

public sealed class ScoringHistoryOptions
{
    public const string SectionName = "FunkArr:ScoringHistory";

    public int MaxSnapshots { get; set; } = 100;

    public int MaxAgeDays { get; set; } = 30;

    public int SnapshotInterval { get; set; } = 20;
}
