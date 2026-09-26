using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using FunkArr.Core;
using FunkArr.Messages.Download;
using FunkArr.Persistence.Events.Download;
using Microsoft.Extensions.Options;

namespace FunkArr.Download;

public sealed class DownloadHistoryManager : ReceivePersistentActor
{
    private const int _snapshotInterval = 25;

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IOptionsMonitor<DownloadOptions> _optionsMonitor;
    private DownloadHistoryManagerState _state = DownloadHistoryManagerState.Empty;

    public override string PersistenceId => "download-history";

    public DownloadHistoryManager(IOptionsMonitor<DownloadOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;

        Command<RecordDownload>(HandleRecord);
        Command<RemoveHistoryEntry>(HandleRemove);
        Command<QueryHistory>(HandleQueryHistory);
        Command<QueryHistoryStats>(_ => Sender.Tell(_state.ToHistoryStats()));
        Command<QueryHistoryCategories>(_ => Sender.Tell(_state.ToHistoryCategories()));
        Command<SaveSnapshotSuccess>(_ => { });
        Command<SaveSnapshotFailure>(f => _log.Warning(f.Cause, "Snapshot save failed at sequence {SequenceNr}", f.Metadata.SequenceNr));

        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is PersistedDownloadHistoryManagerState persisted)
            {
                _state = DownloadHistoryManagerStateExtensions.FromPersistence(persisted);
            }
        });
        Recover<DownloadHistoryRecorded>(evt => _state = _state.Apply(evt));
        Recover<HistoryRemoved>(evt => _state = _state.Apply(evt));
        Recover<HistoryTrimmed>(evt => _state = _state.Apply(evt));
    }

    private void HandleRecord(RecordDownload cmd)
    {
        if (_state.Contains(cmd.DownloadId))
        {
            return;
        }

        var evt = new DownloadHistoryRecorded(
            cmd.DownloadId, cmd.Title, cmd.Category.ToPersistence(), cmd.Size,
            cmd.Status.ToPersistence(), cmd.RelativePath, cmd.FailMessage,
            cmd.DownloadTimeSeconds, cmd.CompletedAt);

        Persist(evt, e =>
        {
            _state = _state.Apply(e);

            var maxRecords = _optionsMonitor.CurrentValue.MaxHistoryRecords;
            var (trimmed, trimCount) = _state.TrimIfNeeded(maxRecords);
            if (trimCount > 0)
            {
                var trimEvt = new HistoryTrimmed(trimCount);
                Persist(trimEvt, t => _state = _state.Apply(t));
            }

            MaybeSnapshot();

            if (cmd.Status == DownloadStatus.Completed)
            {
                Telemetry.DownloadsCompleted.Add(1);
                if (cmd.Size > 0)
                {
                    Telemetry.DownloadBytes.Add(cmd.Size);
                }
            }
            else if (cmd.Status == DownloadStatus.Failed)
            {
                Telemetry.DownloadsFailed.Add(1,
                    new KeyValuePair<string, object?>("reason", ClassifyFailure(cmd.FailMessage)));
            }
        });
    }

    private void HandleRemove(RemoveHistoryEntry cmd)
    {
        if (!_state.Contains(cmd.DownloadId))
        {
            Sender.Tell(new DeleteDownloadResult(false, "Item not found"));
            return;
        }

        Persist(new HistoryRemoved(cmd.DownloadId), e =>
        {
            _state = _state.Apply(e);
            MaybeSnapshot();
            Sender.Tell(new DeleteDownloadResult(true, null));
        });
    }

    private void HandleQueryHistory(QueryHistory query) =>
        Sender.Tell(_state.ToHistoryResult(query));

    private void MaybeSnapshot()
    {
        if (LastSequenceNr % _snapshotInterval == 0)
        {
            SaveSnapshot(_state.GetPersistenceState());
        }
    }

    private static string ClassifyFailure(string? message) => message switch
    {
        not null when message.Contains("HTTP error", StringComparison.OrdinalIgnoreCase) => "network",
        not null when message.Contains("Server returned", StringComparison.OrdinalIgnoreCase) => "network",
        not null when message.Contains("ffmpeg", StringComparison.OrdinalIgnoreCase) => "ffmpeg",
        not null when message.Contains("disk", StringComparison.OrdinalIgnoreCase) => "disk",
        not null when message.Contains("space", StringComparison.OrdinalIgnoreCase) => "disk",
        _ => "other",
    };
}
