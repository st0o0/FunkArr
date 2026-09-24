using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using FunkArr.Core;
using FunkArr.RuleSet.DiskModel;

namespace FunkArr.RuleSet;

public sealed class RuleSetStore(IDataFiles dataFiles, DataPaths dataPaths)
{
    public (DiskRuleSet? Community, DiskRuleSet? Local) Load(string ruleSetId)
    {
        var communityPath = CommunityPath(ruleSetId);
        var localPath = LocalPath(ruleSetId);

        var community = dataFiles.Exists(communityPath)
            ? JsonSerializer.Deserialize<DiskRuleSet>(dataFiles.ReadText(communityPath), DiskJsonOptions.Default)
            : null;
        var local = dataFiles.Exists(localPath)
            ? JsonSerializer.Deserialize<DiskRuleSet>(dataFiles.ReadText(localPath), DiskJsonOptions.Default)
            : null;

        return (community, local);
    }

    public DiskRuleSet? LoadMerged(string ruleSetId)
    {
        var (community, local) = Load(ruleSetId);
        return RuleSetMerger.Resolve(community, local);
    }

    public string SaveLocal(string ruleSetId, DiskRuleSet disk)
    {
        dataFiles.CreateDirectory(dataPaths.LocalRuleSets);
        var json = JsonSerializer.Serialize(disk, DiskJsonOptions.WriteFormatted);
        dataFiles.WriteAtomic(LocalPath(ruleSetId), json);
        return json;
    }

    public bool DeleteLocal(string ruleSetId)
    {
        var path = LocalPath(ruleSetId);
        if (!dataFiles.Exists(path)) return false;
        dataFiles.Remove(path);
        return true;
    }

    public bool ExistsLocal(string ruleSetId) => dataFiles.Exists(LocalPath(ruleSetId));

    public bool ExistsCommunity(string ruleSetId) => dataFiles.Exists(CommunityPath(ruleSetId));

    public string? ExportMergedJson(string ruleSetId)
    {
        var (community, local) = Load(ruleSetId);
        var resolved = RuleSetMerger.Resolve(community, local);
        if (resolved is null) return null;

        var node = JsonSerializer.SerializeToNode(resolved, DiskJsonOptions.WriteFormatted);
        if (node is JsonObject obj)
        {
            obj.Remove("standalone");
            obj.Remove("disable");
        }

        return node?.ToJsonString(DiskJsonOptions.WriteFormatted);
    }

    public ImmutableDictionary<string, RuleSetPaths> Scan()
    {
        var communityFiles = dataFiles.ListFiles(dataPaths.CommunityRuleSets, "*.json");
        var localFiles = dataFiles.ListFiles(dataPaths.LocalRuleSets, "*.json");

        var result = ImmutableDictionary.CreateBuilder<string, RuleSetPaths>(StringComparer.Ordinal);

        foreach (var file in communityFiles)
        {
            var id = Path.GetFileNameWithoutExtension(file);
            result[id] = new RuleSetPaths(file, null, File.GetLastWriteTimeUtc(file), null);
        }

        foreach (var file in localFiles)
        {
            var id = Path.GetFileNameWithoutExtension(file);
            if (result.TryGetValue(id, out var existing))
            {
                result[id] = existing with { LocalPath = file, LocalModified = File.GetLastWriteTimeUtc(file) };
            }
            else
            {
                result[id] = new RuleSetPaths(null, file, null, File.GetLastWriteTimeUtc(file));
            }
        }

        return result.ToImmutable();
    }

    public RuleSetPaths CheckPaths(string ruleSetId)
    {
        var communityPath = CommunityPath(ruleSetId);
        var localPath = LocalPath(ruleSetId);
        var communityExists = dataFiles.Exists(communityPath);
        var localExists = dataFiles.Exists(localPath);

        return new RuleSetPaths(
            communityExists ? communityPath : null,
            localExists ? localPath : null,
            communityExists ? File.GetLastWriteTimeUtc(communityPath) : null,
            localExists ? File.GetLastWriteTimeUtc(localPath) : null);
    }

    private string CommunityPath(string ruleSetId) => Path.Join(dataPaths.CommunityRuleSets, $"{ruleSetId}.json");
    private string LocalPath(string ruleSetId) => Path.Join(dataPaths.LocalRuleSets, $"{ruleSetId}.json");
}
