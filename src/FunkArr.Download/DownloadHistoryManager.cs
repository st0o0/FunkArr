using Akka.Actor;
using Akka.Persistence;
using FunkArr.Messages.Download;
using FunkArr.Persistence.Events.Download;

namespace FunkArr.Download;

public sealed class DownloadHistoryManager : ReceivePersistentActor
{
    public override string PersistenceId => "download-history";

    private DownloadHistoryManagerState _state = DownloadHistoryManagerState.Empty;

    public DownloadHistoryManager()
    {
        Command<RecordDownload>(HandleRecord);
        Command<RemoveHistoryEntry>(HandleRemove);
        Command<QueryHistory>(HandleQueryHistory);

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
            cmd.DownloadId, cmd.Title, cmd.Category, cmd.Size,
            (int)cmd.Status, cmd.FilePath, cmd.FailMessage,
            cmd.DownloadTimeSeconds, cmd.CompletedAt);

        Persist(evt, e => _state = _state.Apply(e));
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
}
