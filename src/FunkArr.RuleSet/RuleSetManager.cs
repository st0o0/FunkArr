using Akka.Actor;
using Akka.Event;
using FunkArr.Core;
using Servus.Akka;

namespace FunkArr.RuleSet;

public sealed class RuleSetManager : ReceiveActor
{
    public sealed record ScanRuleSets(string DataDirectory);

    private readonly ILoggingAdapter _log = Context.GetLogger();

    public RuleSetManager()
    {
        Receive<ScanRuleSets>(HandleScan);
    }

    private void HandleScan(ScanRuleSets msg)
    {
        var communityDir = Path.Combine(msg.DataDirectory, "community", "rulesets");
        var localDir = Path.Combine(msg.DataDirectory, "local", "rulesets");

        var communityFiles = Directory.Exists(communityDir)
            ? Directory.GetFiles(communityDir, "*.json")
            : [];

        var localFiles = Directory.Exists(localDir)
            ? Directory.GetFiles(localDir, "*.json")
            : [];

        var ruleSetPaths = new Dictionary<string, (string? Community, string? Local)>(StringComparer.Ordinal);

        foreach (var file in communityFiles)
        {
            var id = Path.GetFileNameWithoutExtension(file);
            ruleSetPaths[id] = (file, null);
        }

        foreach (var file in localFiles)
        {
            var id = Path.GetFileNameWithoutExtension(file);
            if (ruleSetPaths.TryGetValue(id, out var existing))
            {
                ruleSetPaths[id] = (existing.Community, file);
            }
            else
            {
                ruleSetPaths[id] = (null, file);
            }
        }

        _log.Info("Discovered {Count} rulesets", ruleSetPaths.Count);

        var shardRegion = Context.GetActor<IRuleSetRegion>();

        foreach (var (id, paths) in ruleSetPaths)
        {
            shardRegion.Tell(new RuleSetWorker.InitializeRuleSet(id, paths.Community, paths.Local));
        }
    }
}
