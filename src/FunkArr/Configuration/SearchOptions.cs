namespace FunkArr.Configuration;

public sealed class SearchOptions
{
    public const string SectionName = "FunkArr:Search";

    public int QualityProbeLimit { get; set; } = 30;
}
