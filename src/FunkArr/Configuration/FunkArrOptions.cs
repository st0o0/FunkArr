namespace FunkArr.Configuration;

public sealed class FunkArrOptions
{
    public const string SectionName = "FunkArr";

    public string ApiKey { get; set; } = "funkarr-default-api-key";

    public string PersistencePath { get; set; } = "data/funkarr.db";

    public string DownloadPath { get; set; } = "downloads";

    public string DataPath { get; set; } = "data";

    public int ScoringPoolSize { get; set; } = 4;
}
