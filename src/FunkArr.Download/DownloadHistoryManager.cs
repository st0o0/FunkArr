using Akka.Actor;
using Akka.Persistence;
using FunkArr.Messages.Download;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download;

public sealed class DownloadHistoryManager : ReceivePersistentActor
{
    private DownloadHistoryManagerState _state = DownloadHistoryManagerState.Empty;

    public override string PersistenceId => "download-history";

    public DownloadHistoryManager()
    {
        Command<RecordDownload>(HandleRecord);
        Command<RemoveHistoryEntry>(HandleRemove);
        Command<QueryHistory>(HandleQueryHistory);
        Command<QueryHistoryStats>(_ => Sender.Tell(_state.ToHistoryStats()));
        Command<QueryHistoryCategories>(_ => Sender.Tell(_state.ToHistoryCategories()));

        Recover<HistoryRecorded>(evt => _state = _state.Apply(evt));
        Recover<HistoryRemoved>(evt => _state = _state.Apply(evt));
    }

    private void HandleRecord(RecordDownload cmd)
    {
        if (_state.Contains(cmd.DownloadId))
        {
            return;
        }

        var evt = new HistoryRecorded(
            cmd.DownloadId, cmd.Title, cmd.Category.ToPersistence(), cmd.Size,
            (int)cmd.Status, cmd.RelativePath, cmd.FailMessage,
            cmd.DownloadTimeSeconds, cmd.CompletedAt);

        Persist(evt, e =>
        {
            _state = _state.Apply(e);

            if (cmd.Status == DownloadStatus.Completed)
            {
                Telemetry.DownloadsCompleted.Add(1);
                if (cmd.Size > 0)
                    Telemetry.DownloadBytes.Add(cmd.Size);
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

        var sender = Sender;
        Persist(new HistoryRemoved(cmd.DownloadId), e =>
        {
            _state = _state.Apply(e);
            sender.Tell(new DeleteDownloadResult(true, null));
        });
    }

    private void HandleQueryHistory(QueryHistory query) =>
        Sender.Tell(_state.ToHistoryResult(query));

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
