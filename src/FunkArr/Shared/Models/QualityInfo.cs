namespace FunkArr.Shared.Models;

public sealed record QualityInfo
{
    public required Resolution Resolution { get; init; }
    public QualityTier QualityTier => DeriveQualityTier(Resolution.Height);
    public string Codec { get; init; } = "h264";
    public int? BitrateKbps { get; init; }
    public required long FileSize { get; init; }
    public string Container { get; init; } = "mp4";
    public required ProbeSource ProbeSource { get; init; }

    public static QualityTier DeriveQualityTier(int height) => height switch
    {
        >= 1080 => QualityTier.HD1080,
        >= 720 => QualityTier.HD720,
        _ => QualityTier.SD,
    };

    public static QualityInfo Estimated(QualityTier tier, long fileSize) => new()
    {
        Resolution = tier switch
        {
            QualityTier.HD1080 => new Resolution(1920, 1080),
            QualityTier.HD720 => new Resolution(1280, 720),
            _ => new Resolution(640, 480),
        },
        FileSize = fileSize,
        ProbeSource = ProbeSource.Estimated,
    };
}

public readonly record struct Resolution(int Width, int Height);

public enum ProbeSource
{
    Estimated,
    UrlPattern,
    Head,
    ContainerHeader,
    HlsManifest,
}
