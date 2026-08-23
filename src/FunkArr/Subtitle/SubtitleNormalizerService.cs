namespace FunkArr.Subtitle;

public sealed class SubtitleNormalizerService
{
    public Task<string?> NormalizeAsync(string subtitlePath, string outputPath) =>
        SubtitleNormalizer.NormalizeAsync(subtitlePath, outputPath);
}
