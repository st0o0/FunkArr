namespace FunkArr.Configuration;

public sealed class FunkArrOptions
{
    public const string SectionName = "FunkArr";

    public string ApiKey { get; set; } = string.Empty;
    public string DownloadPath { get; set; } = "/media/downloads";
    public string TempPath { get; set; } = "data/temp";
    public string PersistencePath { get; set; } = "data/funkarr.db";
    public PostgresOptions Postgres { get; set; } = new();
    public int ConcurrentDownloads { get; set; } = 3;
    public string? PathMapping { get; set; }
    public string LogFormat { get; set; } = "text";
    public string RuleSetSourceUrl { get; set; } =
        "https://raw.githubusercontent.com/rundfunkarr/rundfunkarr/main/data/rulesets.json";
    public string RuleSetRepository { get; set; } = "st0o0/funkarr";
    public string RuleSetVersion { get; set; } = "latest";
    public string RuleSetRefreshMode { get; set; } = "github-release";
    public string RuleSetPath { get; set; } = "data/rulesets";
    public int RuleSetRefreshIntervalMinutes { get; set; } = 60;
    public int MatchLedgerCapacity { get; set; } = 10000;
    public ArrConnection? Prowlarr { get; set; }
    public List<ArrInstanceConnection> ArrInstances { get; set; } = [];
    public bool QualityProbing { get; set; } = true;
    public int QualityCacheTtlMinutes { get; set; } = 360;
    public int QualityCacheCapacity { get; set; } = 50000;
    public int QualityProbeLimit { get; set; } = 30;
}
