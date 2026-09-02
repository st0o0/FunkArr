namespace FunkArr.Download;

internal static class FfmpegArgumentBuilder
{
    public static string Build(string videoUrl, string? subtitleUrl, string outputPath)
    {
        var args = new List<string>
        {
            "-y",
            "-i",
            Quote(videoUrl)
        };

        if (subtitleUrl is not null)
        {
            args.Add("-i");
            args.Add(Quote(subtitleUrl));
            args.Add("-c:v");
            args.Add("copy");
            args.Add("-c:a");
            args.Add("copy");
            args.Add("-c:s");
            args.Add("srt");
            args.Add("-metadata:s:s:0");
            args.Add("language=deu");
        }
        else
        {
            args.Add("-c");
            args.Add("copy");
        }

        args.Add("-progress");
        args.Add("pipe:1");
        args.Add(Quote(outputPath));

        return string.Join(' ', args);
    }

    public static string BuildWithoutSubtitle(string videoUrl, string outputPath) =>
        Build(videoUrl, null, outputPath);

    private static string Quote(string value) => $"\"{value}\"";
}
