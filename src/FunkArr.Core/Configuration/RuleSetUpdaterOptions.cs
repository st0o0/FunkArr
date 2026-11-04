namespace FunkArr.Core;

public sealed class RuleSetUpdaterOptions
{
    public const string SectionName = "FunkArr:RuleSet";

    public string Repository { get; set; } = "st0o0/funkarr";

    public string Version { get; set; } = "latest";

    public bool RefreshEnabled { get; set; } = true;
}
