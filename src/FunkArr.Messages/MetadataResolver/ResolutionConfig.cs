namespace FunkArr.Messages.MetadataResolver;

public sealed record ResolutionConfig(
    string Strategy = "fuzzy",
    float Threshold = 0.7f,
    int AirdateTolerance = 7);
