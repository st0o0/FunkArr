using FunkArr.Core;

namespace FunkArr.RuleSet;

public sealed class RuleSetExporter(IDataFiles dataFiles, DataPaths dataPaths, IRuleSetValidator validator)
    : IRuleSetExporter
{
    public RuleSetExportResult Export(string ruleSetId)
    {
        var localPath = Path.Join(dataPaths.LocalRuleSets, $"{ruleSetId}.json");
        var communityPath = Path.Join(dataPaths.CommunityRuleSets, $"{ruleSetId}.json");

        if (!dataFiles.Exists(localPath))
        {
            return new RuleSetExportResult(false, Error: "No local ruleset found to export");
        }

        var localJson = dataFiles.ReadText(localPath);
        var communityJson = dataFiles.Exists(communityPath) ? dataFiles.ReadText(communityPath) : null;

        var resolvedJson = RuleSetMerger.ResolveToJson(communityJson, localJson);
        if (resolvedJson is null)
        {
            return new RuleSetExportResult(false, Error: "Failed to resolve ruleset");
        }

        var errors = validator.Validate(resolvedJson);
        if (errors.Count > 0)
        {
            return new RuleSetExportResult(false, Errors: errors);
        }

        return new RuleSetExportResult(true, Json: resolvedJson);
    }
}
