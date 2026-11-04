namespace FunkArr.Core;

public sealed class ScoringOptions
{
    public const string SectionName = "FunkArr:Scoring";

    public int PoolSize { get; set; } = 4;
}
