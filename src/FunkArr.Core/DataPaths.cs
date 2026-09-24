using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

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

    public DataPaths(IOptions<FunkArrOptions> funkArrOptions, IOptions<DownloadOptions> downloadOptions)
    {
        DataRoot = Path.GetFullPath(funkArrOptions.Value.DataPath);
        Database = Path.Join(DataRoot, "funkarr.db");
        CommunityRuleSets = Path.Join(DataRoot, "rulesets", "community");
        LocalRuleSets = Path.Join(DataRoot, "rulesets", "local");
        RuleSetVersion = Path.Join(DataRoot, "rulesets", "version.txt");
        Temp = Path.Join(DataRoot, "temp");

        DownloadRoot = Path.GetFullPath(downloadOptions.Value.Path);
        Incomplete = Path.Join(DownloadRoot, "incomplete");
        Complete = Path.Join(DownloadRoot, "complete");
    }

    public void EnsureDirectories()
    {
        EnsureWritableDirectory(Incomplete);
        EnsureWritableDirectory(Complete);
    }

    private static void EnsureWritableDirectory(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            throw new InvalidOperationException(
                $"Cannot create directory '{path}'. Ensure PUID/PGID has write access to the parent directory.", ex);
        }

        var probe = Path.Join(path, $".write-test-{Guid.NewGuid():N}");
        try
        {
            File.WriteAllBytes(probe, []);
            File.Delete(probe);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            throw new InvalidOperationException(
                $"Cannot write to '{path}'. Ensure PUID/PGID has write access to this directory.", ex);
        }
    }

    public sealed record ResolvedDownload(
        string IncompletePath,
        string CompletePath,
        string RelativePath);

    public ResolvedDownload ResolveDownload(string entityId, string title, string? category,
        List<DownloadCategory> categories)
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

    internal static bool HasEpisodeIdentifier(string title)
        => EpisodePattern().IsMatch(title);

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
