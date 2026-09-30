## MODIFIED Requirements

### Requirement: Persistence DTOs for all persisted events
The system MUST provide a separate DTO in `Persistence/DownloadEventDtos.cs` for every event persisted via Akka.Persistence. DTOs MUST be `sealed class` with default constructor and public setters. Every DTO MUST have a `[JsonProperty("v")] public int Version { get; set; } = 1;` field.

#### Scenario: All persisted events have a corresponding DTO
- **WHEN** listing the persisted event types (`DownloadEnqueued`, `DownloadStarted`, `DownloadCompleted`, `DownloadFailed`, `MuxingStarted`, `MuxingCompleted`, `MuxingFailed`)
- **THEN** a corresponding DTO (`DownloadEnqueuedDto`, `DownloadStartedDto`, etc.) exists in `Persistence/DownloadEventDtos.cs`

#### Scenario: Non-persisted events have no DTO
- **WHEN** an event is only used as an in-memory message (e.g. progress reports)
- **THEN** no DTO exists for it in `Persistence/`

## REMOVED Requirements

### Requirement: DownloadProgressUpdated as domain event
**Reason**: Progress reporting moves from actor messages to `IProgress<DownloadProgress>` carried on the `DownloadRequest`. `DownloadProgressUpdated` was never persisted and is no longer a domain event.
**Migration**: Stream stages use `req.Progress.Report(...)` instead of `self.Tell(new DownloadProgressUpdated(...))`.
