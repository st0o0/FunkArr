using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Event;
using Akka.Persistence;
using FunkArr.Persistence;

namespace FunkArr.DownloadClient;

public interface IWithNzoId
{
    string NzoId { get; }
}

public sealed class DownloadRequestTrackerMessageExtractor : HashCodeMessageExtractor
{
    public DownloadRequestTrackerMessageExtractor() : base(maxNumberOfShards: 10) { }

    public override string? EntityId(object message) => message switch
    {
        IWithNzoId m => m.NzoId,
        _ => null,
    };
}

public sealed class DownloadRequestTracker : ReceivePersistentActor
{
    public override string PersistenceId { get; }

    private readonly ILoggingAdapter _log = Context.GetLogger();

    private string _nzoId;
    private string _title = string.Empty;
    private string _downloadUrl = string.Empty;
    private string _status = "Queued";
    private string? _outputPath;
    private string? _errorMessage;
    private DateTimeOffset _enqueuedAt;
    private DateTimeOffset? _completedAt;

    public sealed record CreateRequest(string NzoId, string Title, string DownloadUrl, DateTimeOffset EnqueuedAt) : IWithNzoId;
    public sealed record UpdateStatus(string NzoId, string Status) : IWithNzoId;
    public sealed record MarkCompleted(string NzoId, string OutputPath) : IWithNzoId;
    public sealed record MarkFailed(string NzoId, string Error) : IWithNzoId;
    public sealed record GetStatus(string NzoId) : IWithNzoId;
    public sealed record GetHistoryEntry(string NzoId) : IWithNzoId;

    public sealed record StatusResponse(string NzoId, string Title, string Status, DateTimeOffset EnqueuedAt);
    public sealed record HistoryEntryResponse(
        string NzoId, string Title, string Status, string? OutputPath,
        DateTimeOffset? CompletedAt, string? ErrorMessage);

    public DownloadRequestTracker()
    {
        _nzoId = Context.Self.Path.Name;
        PersistenceId = $"download-request-{_nzoId}";

        Recovering();
    }

    private void Recovering()
    {
        Recover<RequestCreatedDto>(dto => Apply(DownloadRequestTrackerEventDtoMapping.ToDomain(dto)));
        Recover<RequestStatusChangedDto>(dto => Apply(DownloadRequestTrackerEventDtoMapping.ToDomain(dto)));
        Recover<RequestCompletedDto>(dto => Apply(DownloadRequestTrackerEventDtoMapping.ToDomain(dto)));
        Recover<RequestFailedDto>(dto => Apply(DownloadRequestTrackerEventDtoMapping.ToDomain(dto)));
        Recover<RecoveryCompleted>(_ => Become(Ready));
    }

    private void Ready()
    {
        Command<CreateRequest>(HandleCreateRequest);
        Command<UpdateStatus>(HandleUpdateStatus);
        Command<MarkCompleted>(HandleMarkCompleted);
        Command<MarkFailed>(HandleMarkFailed);
        Command<GetStatus>(HandleGetStatus);
        Command<GetHistoryEntry>(HandleGetHistoryEntry);
    }

    private void HandleCreateRequest(CreateRequest cmd)
    {
        if (!string.IsNullOrEmpty(_title)) return;

        var evt = new DownloadRequestTrackerEvents.RequestCreated(
            cmd.NzoId, cmd.Title, cmd.DownloadUrl, cmd.EnqueuedAt);

        Persist(DownloadRequestTrackerEventDtoMapping.ToDto(evt), _ =>
        {
            Apply(evt);
            _log.Debug("Tracker created for {NzoId} '{Title}'", cmd.NzoId, cmd.Title);
        });
    }

    private void HandleUpdateStatus(UpdateStatus cmd)
    {
        var evt = new DownloadRequestTrackerEvents.StatusChanged(cmd.NzoId, cmd.Status);

        Persist(DownloadRequestTrackerEventDtoMapping.ToDto(evt), _ =>
        {
            Apply(evt);
        });
    }

    private void HandleMarkCompleted(MarkCompleted cmd)
    {
        var evt = new DownloadRequestTrackerEvents.Completed(
            cmd.NzoId, cmd.OutputPath, DateTimeOffset.UtcNow);

        Persist(DownloadRequestTrackerEventDtoMapping.ToDto(evt), _ =>
        {
            Apply(evt);
            _log.Debug("Tracker {NzoId} marked completed: {Path}", cmd.NzoId, cmd.OutputPath);
        });
    }

    private void HandleMarkFailed(MarkFailed cmd)
    {
        var evt = new DownloadRequestTrackerEvents.Failed(
            cmd.NzoId, cmd.Error, DateTimeOffset.UtcNow);

        Persist(DownloadRequestTrackerEventDtoMapping.ToDto(evt), _ =>
        {
            Apply(evt);
            _log.Debug("Tracker {NzoId} marked failed: {Error}", cmd.NzoId, cmd.Error);
        });
    }

    private void HandleGetStatus(GetStatus _)
    {
        Sender.Tell(new StatusResponse(_nzoId, _title, _status, _enqueuedAt));
    }

    private void HandleGetHistoryEntry(GetHistoryEntry _)
    {
        Sender.Tell(new HistoryEntryResponse(
            _nzoId, _title, _status, _outputPath, _completedAt, _errorMessage));
    }

    private void Apply(DownloadRequestTrackerEvents.RequestCreated evt)
    {
        _nzoId = evt.NzoId;
        _title = evt.Title;
        _downloadUrl = evt.DownloadUrl;
        _status = "Queued";
        _enqueuedAt = evt.EnqueuedAt;
    }

    private void Apply(DownloadRequestTrackerEvents.StatusChanged evt)
    {
        _status = evt.Status;
    }

    private void Apply(DownloadRequestTrackerEvents.Completed evt)
    {
        _status = "Completed";
        _outputPath = evt.OutputPath;
        _completedAt = evt.CompletedAt;
    }

    private void Apply(DownloadRequestTrackerEvents.Failed evt)
    {
        _status = "Failed";
        _errorMessage = evt.Error;
        _completedAt = evt.CompletedAt;
    }
}
