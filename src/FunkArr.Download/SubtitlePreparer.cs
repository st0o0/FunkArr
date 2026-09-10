namespace FunkArr.Download;

internal sealed class SubtitlePreparer(HttpClient http) : ISubtitlePreparer
{
    public async Task<string?> PrepareAsync(string url, string outputDirectory, CancellationToken ct)
    {
        string content;
        try
        {
            var response = await http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            content = await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(content))
            return null;

        var trimmed = content.TrimStart();

        if (trimmed.StartsWith("WEBVTT", StringComparison.Ordinal))
            return WriteFile(outputDirectory, ".vtt", content);

        if (trimmed.StartsWith("<?xml", StringComparison.Ordinal) || trimmed.StartsWith("<tt", StringComparison.Ordinal))
        {
            var srt = TtmlToSrtConverter.Convert(content);
            return string.IsNullOrWhiteSpace(srt) ? null : WriteFile(outputDirectory, ".srt", srt);
        }

        if (IsSrt(trimmed))
            return WriteFile(outputDirectory, ".srt", content);

        return null;
    }

    private static bool IsSrt(string content) =>
        content.Length > 5 && char.IsDigit(content[0]) && content.Contains("-->");

    private static string WriteFile(string directory, string extension, string content)
    {
        var path = Path.Combine(directory, $"subtitle{extension}");
        File.WriteAllText(path, content);
        return path;
    }
}
