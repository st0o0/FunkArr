using FunkArr.Messages;

namespace FunkArr.Core;

public sealed class VariantOptions
{
    public const string SectionName = "FunkArr:Variants";

    public VariantPolicy AudioDescription { get; set; } = VariantPolicy.Exclude;
    public VariantPolicy SignLanguage { get; set; } = VariantPolicy.Exclude;
    public VariantPolicy PlainLanguage { get; set; } = VariantPolicy.Exclude;
    public VariantPolicy OriginalVersion { get; set; } = VariantPolicy.Exclude;

    public VariantPolicy PolicyFor(MediathekVariant variant) => variant switch
    {
        MediathekVariant.AudioDescription => AudioDescription,
        MediathekVariant.SignLanguage => SignLanguage,
        MediathekVariant.PlainLanguage => PlainLanguage,
        MediathekVariant.OriginalVersion => OriginalVersion,
        _ => VariantPolicy.Allow
    };

    public bool IsExcluded(MediathekVariant variant) => PolicyFor(variant) == VariantPolicy.Exclude;
}
