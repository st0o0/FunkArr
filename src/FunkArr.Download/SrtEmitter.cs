using System.Text;

namespace FunkArr.Download;

internal static class SrtEmitter
{
    public static string Emit(IReadOnlyList<SubtitleCue> cues)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < cues.Count; i++)
        {
            var cue = cues[i];
            sb.Append(i + 1);
            sb.Append('\n');
            sb.Append(FormatTimestamp(cue.Start));
            sb.Append(" --> ");
            sb.Append(FormatTimestamp(cue.End));
            sb.Append('\n');
            sb.Append(cue.Text);
            sb.Append("\n\n");
        }

        return sb.ToString();
    }

    private static string FormatTimestamp(TimeSpan ts) =>
        $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2},{ts.Milliseconds:D3}";
}
