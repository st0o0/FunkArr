using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using FunkArr.Persistence;
using FunkArr.Shared;

namespace FunkArr.DownloadClient.Tracker;

public interface IWithNzoId : IShardedMessage
{
    string NzoId { get; }
    string IShardedMessage.EntityKey => NzoId;
}

public sealed class DownloadRequestActor : ReceivePersistentActor
{
    public override string PersistenceId { get; }

    private readonly ILoggingAdapter _log = Context.GetLogger();

    private string _nzoId;
    private string _title = string.Empty;
    private string _status = "Queued";
    private string? _category;
    private string? _outputPath;
    private string? _errorMessage;
    private DateTimeOffset _enqueuedAt;
    private DateTimeOffset? _completedAt;

    public sealed record TrackDownload(string NzoId, string Title, string DownloadUrl, string? Category, DateTimeOffset EnqueuedAt) : IWithNzoId;
    public sealed record ReportProgress(string NzoId, string Status) : IWithNzoId;
    public sealed record CompleteDownload(string NzoId, string OutputPath) : IWithNzoId;
    public sealed record FailDownload(string NzoId, string Error) : IWithNzoId;
    public sealed record QueryStatus(string NzoId) : IWithNzoId;
    public sealed record QueryHistory(string NzoId) : IWithNzoId;

    public sealed record DownloadStatus(string NzoId, string Title, string Status, string? Category, DateTimeOffset EnqueuedAt);
    public sealed record DownloadHistoryEntry(
        string NzoId, string Title, string Status, string? Category, string? OutputPath,
        DateTimeOffset? CompletedAt, string? ErrorMessage);

    public DownloadRequestActor()
    {
        _nzoId = Context.Self.Path.Name;
        PersistenceId = $"download-request-{_nzoId}";

        Recovering();
    }

    private void Recovering()
    {
        Recover<Persistence.RequestCreated>(dto => Apply(dto.ToDomain()));
        Recover<Persistence.RequestStatusChanged>(dto => Apply(dto.ToDomain()));
        Recover<Persistence.RequestCompleted>(dto => Apply(dto.ToDomain()));
        Recover<Persistence.RequestFailed>(dto => Apply(dto.ToDomain()));
        Recover<RecoveryCompleted>(_ => Become(Ready));
    }

    private void Ready()
    {
        Command<TrackDownload>(HandleTrackDownload);
        Command<ReportProgress>(HandleReportProgress);
        Command<CompleteDownload>(HandleCompleteDownload);
        Command<FailDownload>(HandleFailDownload);
        Command<QueryStatus>(HandleQueryStatus);
        Command<QueryHistory>(HandleQueryHistory);
    }

    private void HandleTrackDownload(TrackDownload cmd)
    {
        if (!string.IsNullOrEmpty(_title))
        {
            return;
        }

        var evt = new DownloadRequestActorEvents.RequestCreated(
            cmd.NzoId, cmd.Title, cmd.DownloadUrl, cmd.Category, cmd.EnqueuedAt);

        Persist(evt.ToJournal(), _ =>
        {
            Apply(evt);
            _log.Debug("Tracker created for {NzoId} '{Title}'", cmd.NzoId, cmd.Title);
        });
    }

    private void HandleReportProgress(ReportProgress cmd)
    {
        var evt = new DownloadRequestActorEvents.StatusChanged(cmd.NzoId, cmd.Status);

        Persist(evt.ToJournal(), _ =>
        {
            Apply(evt);
        });
    }

    private void HandleCompleteDownload(CompleteDownload cmd)
    {
        var evt = new DownloadRequestActorEvents.Completed(
            cmd.NzoId, cmd.OutputPath, DateTimeOffset.UtcNow);

        Persist(evt.ToJournal(), _ =>
        {
            Apply(evt);
            _log.Debug("Tracker {NzoId} marked completed: {Path}", cmd.NzoId, cmd.OutputPath);
        });
    }

    private void HandleFailDownload(FailDownload cmd)
    {
        var evt = new DownloadRequestActorEvents.Failed(
            cmd.NzoId, cmd.Error, DateTimeOffset.UtcNow);

        Persist(evt.ToJournal(), _ =>
        {
            Apply(evt);
            _log.Debug("Tracker {NzoId} marked failed: {Error}", cmd.NzoId, cmd.Error);
        });
    }

    private void HandleQueryStatus(QueryStatus _)
    {
        Sender.Tell(new DownloadStatus(_nzoId, _title, _status, _category, _enqueuedAt));
    }

    private void HandleQueryHistory(QueryHistory _)
    {
        Sender.Tell(new DownloadHistoryEntry(
            _nzoId, _title, _status, _category, _outputPath, _completedAt, _errorMessage));
    }

    private void Apply(DownloadRequestActorEvents.RequestCreated evt)
    {
        _nzoId = evt.NzoId;
        _title = evt.Title;
        _status = "Queued";
        _category = evt.Category;
        _enqueuedAt = evt.EnqueuedAt;
    }

    private void Apply(DownloadRequestActorEvents.StatusChanged evt)
    {
        _status = evt.Status;
    }

    private void Apply(DownloadRequestActorEvents.Completed evt)
    {
        _status = "Completed";
        _outputPath = evt.OutputPath;
        _completedAt = evt.CompletedAt;
    }

    private void Apply(DownloadRequestActorEvents.Failed evt)
    {
        _status = "Failed";
        _errorMessage = evt.Error;
        _completedAt = evt.CompletedAt;
    }
}
