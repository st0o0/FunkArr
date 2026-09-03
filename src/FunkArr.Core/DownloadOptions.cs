namespace FunkArr.Core;

public sealed class DownloadOptions
{
    public const string SectionName = "FunkArr:Download";

    public string DownloadPath { get; set; } = "data/downloads";
    public int ConcurrentDownloads { get; set; } = 3;
    public List<DownloadCategory> Categories { get; set; } = [];

    public string CompletePath => Path.Combine(DownloadPath, "complete");
    public string IncompletePath => Path.Combine(DownloadPath, "incomplete");

    public string ResolveCategoryDir(string? category)
    {
        if (string.IsNullOrEmpty(category))
        {
            return "";
        }

        var match = Categories.Find(c =>
            string.Equals(c.Name, category, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            return "";
        }

        return string.IsNullOrEmpty(match.Dir) ? match.Name : match.Dir;
    }
}

public sealed class DownloadCategory
{
    public string Name { get; set; } = "";
    public string Dir { get; set; } = "";
}
