namespace FunkArr.Download;

internal interface ISubtitleFormat
{
    string Name { get; }
    bool CanParse(string content);
    List<SubtitleCue> Parse(string content);
}
