namespace FunkArr.Messages.MetadataResolver;

public sealed record ResolvedEpisode(
    int Index,
    string Season,
    string Episode,
    string EpisodeName,
    float Confidence,
    string Strategy);
