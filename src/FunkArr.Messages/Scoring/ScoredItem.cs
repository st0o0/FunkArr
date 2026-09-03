namespace FunkArr.Messages.Scoring;

public sealed record ScoredItem(
    int Index,
    double Score,
    bool Matched,
    MetadataSpec? Metadata = null);
