namespace FunkArr.Core;

public sealed class MetadataResolverOptions
{
    public string DefaultStrategy { get; set; } = "fuzzy";
    public float DefaultThreshold { get; set; } = 0.7f;
    public int DefaultAirdateTolerance { get; set; } = 7;
}
