using FunkArr.RuleSet.DiskModel;

namespace FunkArr.RuleSet;

public sealed class RuleSetWorkerState
{
    public DiskRuleSet? Merged { get; set; }
    public RuleSetPaths Paths { get; set; } = new(null, null, null, null);
}
