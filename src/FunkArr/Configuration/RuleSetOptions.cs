namespace FunkArr.Configuration;

public sealed class RuleSetOptions
{
    public const string SectionName = "FunkArr:RuleSet";

    public string Repository { get; set; } = "st0o0/funkarr";
    public string Version { get; set; } = "latest";
    public string Path { get; set; } = "data/rulesets";
    public int RefreshIntervalMinutes { get; set; } = 60;
}
