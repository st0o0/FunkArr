namespace FunkArr.Configuration;

public sealed class QualityOptions
{
    public const string SectionName = "FunkArr:Quality";

    public bool Probing { get; set; } = true;
    public int CacheTtlMinutes { get; set; } = 360;
    public int CacheCapacity { get; set; } = 50000;
}
