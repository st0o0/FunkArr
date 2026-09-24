using System.Collections.Immutable;
using System.IO.Abstractions;
using Akka.Actor;
using Akka.Event;
using FunkArr.Core;
using FunkArr.Messages.History;
using FunkArr.Messages.RuleSet;
using Servus.Akka;

namespace FunkArr.RuleSet;

public sealed class RuleSetManager : ReceiveActor
{
    public sealed record ScanRuleSets;

    private sealed record FileChanged(string RuleSetId);

    private sealed record FullRescanRequested;

    private sealed record FlushChanges;

    private static readonly TimeSpan _defaultDebounceWindow = TimeSpan.FromSeconds(2);

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly RuleSetStore _store;
    private readonly IDataFiles _dataFiles;
    private readonly DataPaths _dataPaths;
    private readonly TimeSpan _debounceWindow;
    private RuleSetManagerState _state = RuleSetManagerState.Empty;
    private ImmutableDictionary<string, WorkerSummary> _summaries =
        ImmutableDictionary<string, WorkerSummary>.Empty.WithComparers(StringComparer.Ordinal);
    private IFileSystemWatcher? _communityWatcher;
    private IFileSystemWatcher? _localWatcher;
    private ICancelable? _flushSchedule;

    public RuleSetManager(RuleSetStore store, IDataFiles dataFiles, DataPaths dataPaths, TimeSpan? debounceWindow = null)
    {
        _store = store;
        _dataFiles = dataFiles;
        _dataPaths = dataPaths;
        _debounceWindow = debounceWindow ?? _defaultDebounceWindow;

        Receive<ScanRuleSets>(_ => HandleScan());
        Receive<FileChanged>(HandleFileChanged);
        Receive<FullRescanRequested>(_ => HandleFullRescanRequested());
        Receive<FlushChanges>(_ => HandleFlush());
        Receive<QueryRuleSetDetail>(HandleQueryDetail);
        Receive<QueryRuleSetSummaries>(_ => HandleQuerySummaries());
        Receive<WorkerReady>(HandleWorkerReady);
        Receive<WorkerRemoved>(HandleWorkerRemoved);
        ReceiveAsync<QueryRuleSetListWithStats>(_ => HandleQueryListWithStats());
    }

    protected override void PreStart()
    {
        var current = _store.Scan();

        var shardRegion = Context.GetActor<IRuleSetRegion>();
        foreach (var (id, paths) in current)
        {
            shardRegion.Tell(new RuleSetWorker.LoadRuleSet(id, paths.CommunityPath, paths.LocalPath));
        }

        _state = _state with { KnownRuleSets = current };
        _log.Info("Initial scan: discovered {Count} rulesets", current.Count);

        SetupWatchers();
    }

    private void HandleScan()
    {
        var current = _store.Scan();

        var shardRegion = Context.GetActor<IRuleSetRegion>();
        foreach (var (id, paths) in current)
        {
            shardRegion.Tell(new RuleSetWorker.LoadRuleSet(id, paths.CommunityPath, paths.LocalPath));
        }

        _state = _state with { KnownRuleSets = current };
        _log.Info("Re-scan: discovered {Count} rulesets", current.Count);
    }

    private void HandleFileChanged(FileChanged msg)
    {
        _state = _state with { PendingIds = _state.PendingIds.Add(msg.RuleSetId) };
        ScheduleFlushIfNeeded();
    }

    private void HandleFullRescanRequested()
    {
        _state = _state with { FullRescanRequested = true };
        ScheduleFlushIfNeeded();
    }

    private void ScheduleFlushIfNeeded()
    {
        if (_flushSchedule is not null)
        {
            return;
        }

        _flushSchedule = Context.System.Scheduler.ScheduleTellOnceCancelable(
            _debounceWindow, Self, new FlushChanges(), ActorRefs.NoSender);
    }

    private void HandleFlush()
    {
        _flushSchedule = null;

        if (_state.FullRescanRequested)
        {
            HandleFullRescan();
            return;
        }

        HandleTargetedFlush();
    }

    private void HandleFullRescan()
    {
        var current = _store.Scan();
        var shardRegion = Context.GetActor<IRuleSetRegion>();
        var added = 0;
        var updated = 0;
        var removed = 0;

        foreach (var (id, paths) in current)
        {
            if (!_state.KnownRuleSets.TryGetValue(id, out var known))
            {
                shardRegion.Tell(new RuleSetWorker.LoadRuleSet(id, paths.CommunityPath, paths.LocalPath));
                added++;
            }
            else if (known != paths)
            {
                shardRegion.Tell(new RuleSetWorker.LoadRuleSet(id, paths.CommunityPath, paths.LocalPath));
                updated++;
            }
        }

        foreach (var id in _state.KnownRuleSets.Keys)
        {
            if (!current.ContainsKey(id))
            {
                shardRegion.Tell(new RuleSetWorker.RemoveRuleSet(id));
                removed++;
            }
        }

        _state = new RuleSetManagerState(KnownRuleSets: current, FullRescanRequested: false, PendingIds: _state.PendingIds.Clear());

        if (added > 0 || updated > 0 || removed > 0)
        {
            _log.Info("Full rescan: added={Added}, updated={Updated}, removed={Removed}", added, updated, removed);
        }
    }

    private void HandleTargetedFlush()
    {
        var shardRegion = Context.GetActor<IRuleSetRegion>();
        var added = 0;
        var updated = 0;
        var removed = 0;
        var knownRuleSets = _state.KnownRuleSets;

        foreach (var id in _state.PendingIds)
        {
            var current = _store.CheckPaths(id);
            var hasFiles = current.CommunityPath is not null || current.LocalPath is not null;

            if (!knownRuleSets.TryGetValue(id, out var known))
            {
                if (hasFiles)
                {
                    shardRegion.Tell(new RuleSetWorker.LoadRuleSet(id, current.CommunityPath, current.LocalPath));
                    knownRuleSets = knownRuleSets.SetItem(id, current);
                    added++;
                }
            }
            else if (!hasFiles)
            {
                shardRegion.Tell(new RuleSetWorker.RemoveRuleSet(id));
                knownRuleSets = knownRuleSets.Remove(id);
                removed++;
            }
            else if (known != current)
            {
                shardRegion.Tell(new RuleSetWorker.LoadRuleSet(id, current.CommunityPath, current.LocalPath));
                knownRuleSets = knownRuleSets.SetItem(id, current);
                updated++;
            }
        }

        _state = _state with
        {
            KnownRuleSets = knownRuleSets,
            PendingIds = _state.PendingIds.Clear(),
        };

        if (added > 0 || updated > 0 || removed > 0)
        {
            _log.Info("Targeted flush: added={Added}, updated={Updated}, removed={Removed}", added, updated, removed);
        }
    }

    private void HandleQueryDetail(QueryRuleSetDetail msg)
    {
        var shardRegion = Context.GetActor<IRuleSetRegion>();
        shardRegion.Forward(msg);
    }

    private void HandleQuerySummaries()
    {
        var entries = _summaries.Select(kv =>
            new RuleSetSummaryEntry(kv.Key, kv.Value.RuleCount, kv.Value.SourceType)).ToArray();
        Sender.Tell(new RuleSetSummaryResult(entries));
    }

    private void HandleWorkerReady(WorkerReady msg)
    {
        _summaries = _summaries.SetItem(msg.RuleSetId, new WorkerSummary(msg.RuleCount, msg.SourceType));
    }

    private void HandleWorkerRemoved(WorkerRemoved msg)
    {
        _summaries = _summaries.Remove(msg.RuleSetId);
        _state = _state with { KnownRuleSets = _state.KnownRuleSets.Remove(msg.RuleSetId) };
    }

    private async Task HandleQueryListWithStats()
    {
        var historyRegion = Context.GetActor<IHistoryRegion>();
        var statsTimeout = TimeSpan.FromSeconds(3);

        var statsTasks = _summaries.Keys.Select(async ruleSetId =>
        {
            try
            {
                var stats = await historyRegion.Ask<ScoringStatsResult>(
                    new QueryScoringStats(ruleSetId), statsTimeout);
                return (RuleSetId: ruleSetId, Stats: stats);
            }
            catch
            {
                return (RuleSetId: ruleSetId, Stats: new ScoringStatsResult(null, null));
            }
        }).ToArray();

        var results = await Task.WhenAll(statsTasks);
        var statsMap = results.ToDictionary(r => r.RuleSetId, r => r.Stats);

        var entries = _summaries.Select(kv =>
        {
            statsMap.TryGetValue(kv.Key, out var stats);
            return new RuleSetListWithStatsEntry(
                kv.Key, kv.Value.RuleCount, kv.Value.SourceType,
                stats?.LastRun, stats?.MatchRate);
        }).ToArray();

        Sender.Tell(new RuleSetListWithStatsResult(entries));
    }

    private void SetupWatchers()
    {
        _communityWatcher = SetupWatcher(_dataPaths.CommunityRuleSets);
        _localWatcher = SetupWatcher(_dataPaths.LocalRuleSets);
    }

    private IFileSystemWatcher SetupWatcher(string directory)
    {
        var self = Self;
        var watcher = _dataFiles.Watch(directory, "*.json");

        watcher.Created += (_, e) => self.Tell(new FileChanged(Path.GetFileNameWithoutExtension(e.FullPath)));
        watcher.Changed += (_, e) => self.Tell(new FileChanged(Path.GetFileNameWithoutExtension(e.FullPath)));
        watcher.Deleted += (_, e) => self.Tell(new FileChanged(Path.GetFileNameWithoutExtension(e.FullPath)));
        watcher.Renamed += (_, e) =>
        {
            self.Tell(new FileChanged(Path.GetFileNameWithoutExtension(e.OldFullPath)));
            self.Tell(new FileChanged(Path.GetFileNameWithoutExtension(e.FullPath)));
        };
        watcher.Error += (_, _) => self.Tell(new FullRescanRequested());

        return watcher;
    }

    protected override void PostStop()
    {
        _communityWatcher?.Dispose();
        _localWatcher?.Dispose();
        _flushSchedule?.Cancel();
    }
}

internal sealed record WorkerSummary(int RuleCount, string SourceType);
