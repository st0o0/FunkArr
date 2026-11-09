using FunkArr.RuleSet.DiskModel;
using FunkArr.RuleSet.Manager;

namespace FunkArr.RuleSet.Worker;

public sealed class RuleSetWorkerState
{
    public DiskRuleSet? Merged { get; private set; }
    public RuleSetPaths Paths { get; private set; } = new(null, null, null, null);

    public void Apply(DiskRuleSet? merged, RuleSetPaths paths)
    {
        Merged = merged;
        Paths = paths;
    }

    public void Clear()
    {
        Merged = null;
    }
}
