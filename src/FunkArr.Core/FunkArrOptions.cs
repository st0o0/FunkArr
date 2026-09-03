namespace FunkArr.Core;

public sealed class FunkArrOptions
{
    public const string SectionName = "FunkArr";

    public string ApiKey { get; set; } = "funkarr-default-api-key";

    public string DataPath { get; set; } = "data";

    public string PersistencePath => Path.Combine(DataPath, "funkarr.db");

    public string RuleSetDataPath => Path.Combine(DataPath, "community");

    public string LocalRuleSetDataPath => Path.Combine(DataPath, "local");
}
