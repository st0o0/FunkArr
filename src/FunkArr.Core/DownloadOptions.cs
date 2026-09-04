namespace FunkArr.Core;

public sealed class DownloadOptions
{
    public const string SectionName = "FunkArr:Download";

    public string Path { get; set; } = "data/downloads";
    public int ConcurrentDownloads { get; set; } = 3;
    public List<DownloadCategory> Categories { get; set; } = [];
}

public sealed class DownloadCategory
{
    public string Name { get; set; } = "";
    public string Dir { get; set; } = "";
}
