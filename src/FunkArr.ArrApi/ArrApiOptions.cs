namespace FunkArr.ArrApi;

public sealed class ArrApiOptions
{
    public const string SectionName = "FunkArr:ArrApi";

    public int SearchTimeoutSeconds { get; set; } = 30;

    public int DownloadTimeoutSeconds { get; set; } = 10;

    public int SearchCacheTtlSeconds { get; set; } = 60;
}
