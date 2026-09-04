using System.IO.Abstractions;
using Akka.Actor;
using Akka.Event;
using FunkArr.Core;
using FunkArr.Messages.RuleSet;
using Servus.Akka;

namespace FunkArr.RuleSet;

public sealed class RuleSetManager : ReceiveActor
{
    public sealed record ScanRuleSets;

    private sealed record FileChanged(string RuleSetId);

    private sealed record FullRescanRequested;

    private sealed record FlushChanges;

    private static readonly TimeSpan _debounceWindow = TimeSpan.FromSeconds(2);

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IDataFiles _dataFiles;
    private readonly DataPaths _dataPaths;
    private RuleSetManagerState _state = RuleSetManagerState.Empty;
    private IFileSystemWatcher? _communityWatcher;
    private IFileSystemWatcher? _localWatcher;
    private ICancelable? _flushSchedule;

    public RuleSetManager(IDataFiles dataFiles, DataPaths dataPaths)
    {
        _dataFiles = dataFiles;
        _dataPaths = dataPaths;

        Receive<ScanRuleSets>(_ => HandleScan());
        Receive<FileChanged>(HandleFileChanged);
        Receive<FullRescanRequested>(_ => HandleFullRescanRequested());
        Receive<FlushChanges>(_ => HandleFlush());
        Receive<QueryRuleSetDetail>(HandleQueryDetail);
    }

    protected override void PreStart()
    {
        var current = ScanDirectories();

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
        var current = ScanDirectories();

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
        var current = ScanDirectories();
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

        _state = _state with
        {
            KnownRuleSets = current,
            FullRescanRequested = false,
            PendingIds = _state.PendingIds.Clear(),
        };

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
            var current = RuleSetManagerStateExtensions.CheckRuleSetPaths(
                id, _dataPaths.CommunityRuleSets, _dataPaths.LocalRuleSets, _dataFiles);
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
        var result = _state.BuildDetail(msg.RuleSetId, _dataFiles);
        if (result is null)
        {
            _state = _state with { KnownRuleSets = _state.KnownRuleSets.Remove(msg.RuleSetId) };
            Sender.Tell(new RuleSetNotFound(msg.RuleSetId));
            return;
        }

        Sender.Tell(result);
    }

    private System.Collections.Immutable.ImmutableDictionary<string, RuleSetPaths> ScanDirectories()
    {
        var communityFiles = _dataFiles.ListFiles(_dataPaths.CommunityRuleSets, "*.json");
        var localFiles = _dataFiles.ListFiles(_dataPaths.LocalRuleSets, "*.json");

        var result = System.Collections.Immutable.ImmutableDictionary.CreateBuilder<string, RuleSetPaths>(StringComparer.Ordinal);

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
