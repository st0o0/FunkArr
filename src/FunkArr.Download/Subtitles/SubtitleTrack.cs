namespace FunkArr.Download.Subtitles;

internal sealed record SubtitleTrack(string Format, string Language, IReadOnlyList<SubtitleCue> Cues);
