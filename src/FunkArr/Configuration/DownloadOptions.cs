namespace FunkArr.Configuration;

public sealed class DownloadOptions
{
    public const string SectionName = "FunkArr:Download";

    public string DownloadPath { get; set; } = "/media/downloads";
    public string TempPath { get; set; } = "data/temp";
    public int ConcurrentDownloads { get; set; } = 3;
    public string? PathMapping { get; set; }
}
