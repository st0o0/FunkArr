namespace FunkArr.Download;

internal sealed record SubtitleTrack(string Format, string Language, IReadOnlyList<SubtitleCue> Cues);
