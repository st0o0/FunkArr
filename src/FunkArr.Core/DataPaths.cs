using System.Text.RegularExpressions;

namespace FunkArr.Core;

public sealed partial class DataPaths
{
    public string DataRoot { get; }
    public string Database { get; }
    public string CommunityRuleSets { get; }
    public string LocalRuleSets { get; }
    public string RuleSetVersion { get; }
    public string Temp { get; }

    public string DownloadRoot { get; }
    public string Incomplete { get; }
    public string Complete { get; }

    public DataPaths(FunkArrOptions funkArrOptions, DownloadOptions downloadOptions)
    {
        DataRoot = Path.GetFullPath(funkArrOptions.DataPath);
        Database = Path.Join(DataRoot, "funkarr.db");
        CommunityRuleSets = Path.Join(DataRoot, "rulesets", "community");
        LocalRuleSets = Path.Join(DataRoot, "rulesets", "local");
        RuleSetVersion = Path.Join(DataRoot, "rulesets", "version.txt");
        Temp = Path.Join(DataRoot, "temp");

        DownloadRoot = Path.GetFullPath(downloadOptions.Path);
        Incomplete = Path.Join(DownloadRoot, "incomplete");
        Complete = Path.Join(DownloadRoot, "complete");
    }

    public sealed record ResolvedDownload(
        string IncompletePath,
        string CompletePath,
        string RelativePath);

    public ResolvedDownload ResolveDownload(
        string entityId, string title, string? category, List<DownloadCategory> categories)
    {
        var categoryDir = ResolveCategoryDir(category, categories);
        var dirName = HasEpisodeIdentifier(title) ? title : $"{title}-{entityId[..8]}";
        var fileName = title + ".mkv";

        var relativePath = string.IsNullOrEmpty(categoryDir)
            ? Path.Join(dirName, fileName)
            : Path.Join(categoryDir, dirName, fileName);

        var incompletePath = Path.Join(Incomplete, entityId, fileName);
        var completePath = Path.Join(Complete, relativePath);

        return new ResolvedDownload(incompletePath, completePath, relativePath);
    }

    internal static bool HasEpisodeIdentifier(string title) =>
        EpisodePattern().IsMatch(title);

    private static string ResolveCategoryDir(string? category, List<DownloadCategory> categories)
    {
        if (string.IsNullOrEmpty(category))
        {
            return "";
        }

        var match = categories.Find(c =>
            string.Equals(c.Name, category, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            return "";
        }

        return string.IsNullOrEmpty(match.Dir) ? match.Name : match.Dir;
    }

    [GeneratedRegex(@"S\d{2,}E\d{2,}|\.E\d{2,}\.|\d{4}-\d{2}-\d{2}", RegexOptions.IgnoreCase)]
    private static partial Regex EpisodePattern();
}
