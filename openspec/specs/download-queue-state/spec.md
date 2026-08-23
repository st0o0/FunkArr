## Purpose

Pure state projection class for download queue — encapsulates event application, job queries, and in-flight reset without any Akka dependencies.

## Requirements

### Requirement: Pure state projection class
`DownloadQueueState` SHALL be a plain C# class (no Akka dependencies) in the `FunkArr.DownloadClient` namespace that encapsulates all download job state. It SHALL maintain an internal `Dictionary<string, DownloadJob>` and expose state query methods. The class SHALL be constructable with `new` and testable without any Akka infrastructure.

#### Scenario: Instantiation without Akka
- **WHEN** a test creates `new DownloadQueueState()`
- **THEN** it is usable immediately with no `ActorSystem`, `TestKit`, or service mocks required

### Requirement: Event application methods
`DownloadQueueState` SHALL expose an `Apply` method for each domain event type: `DownloadEnqueued`, `DownloadStarted`, `DownloadCompleted`, `DownloadFailed`, `MuxingStarted`, `MuxingCompleted`, `MuxingFailed`. Each method SHALL mutate the internal jobs dictionary to reflect the state transition. The methods SHALL match the exact logic of the current `ApplyEvent` overloads in `DownloadQueueActor`.

#### Scenario: Apply DownloadEnqueued creates a new job
- **WHEN** `Apply(new DownloadEnqueued(nzoId, url, title, subtitleUrl, timestamp))` is called
- **THEN** a new `DownloadJob` with `Status = Queued` is added to the internal dictionary

#### Scenario: Apply DownloadStarted transitions to Downloading
- **WHEN** a job exists with `Status = Queued` and `Apply(new DownloadStarted(nzoId))` is called
- **THEN** the job's status changes to `Downloading`

#### Scenario: Apply DownloadCompleted transitions to Muxing
- **WHEN** a job exists with `Status = Downloading` and `Apply(new DownloadCompleted(nzoId, path, subPath))` is called
- **THEN** the job's status changes to `Muxing`

#### Scenario: Apply DownloadFailed marks job as Failed
- **WHEN** `Apply(new DownloadFailed(nzoId, error))` is called
- **THEN** the job's status changes to `Failed`, `ErrorMessage` is set, and `CompletedAt` is set

#### Scenario: Apply MuxingCompleted marks job as Completed
- **WHEN** `Apply(new MuxingCompleted(nzoId, outputPath))` is called
- **THEN** the job's status changes to `Completed`, `OutputPath` is set, and `CompletedAt` is set

#### Scenario: Apply for unknown NzoId is a no-op
- **WHEN** `Apply` is called with an event whose NzoId does not exist in the dictionary
- **THEN** no exception is thrown and the state remains unchanged

### Requirement: Active jobs query
`DownloadQueueState` SHALL expose an `ActiveJobs` property or method that returns all jobs with status `Queued`, `Downloading`, or `Muxing`.

#### Scenario: Active jobs filtered correctly
- **WHEN** the state contains jobs in all five statuses
- **THEN** `ActiveJobs` returns only jobs with `Queued`, `Downloading`, or `Muxing` status

### Requirement: History query
`DownloadQueueState` SHALL expose a `History` property or method that returns all jobs with status `Completed` or `Failed`, ordered by `CompletedAt` descending.

#### Scenario: History sorted by completion time
- **WHEN** the state contains 3 completed and 2 failed jobs
- **THEN** `History` returns all 5 sorted by `CompletedAt` descending

### Requirement: Reset in-flight jobs
`DownloadQueueState` SHALL expose a `ResetInFlight()` method that resets all jobs with status `Downloading` or `Muxing` back to `Queued`. This is used during recovery and stream failure handling.

#### Scenario: In-flight jobs reset after recovery
- **WHEN** the state contains 2 jobs with `Downloading` status and 1 with `Muxing` status
- **THEN** after `ResetInFlight()`, all 3 jobs have `Queued` status

#### Scenario: Queued and terminal jobs unaffected by reset
- **WHEN** the state contains jobs with `Queued`, `Completed`, and `Failed` status
- **THEN** after `ResetInFlight()`, their statuses are unchanged

### Requirement: Job lookup by NzoId
`DownloadQueueState` SHALL expose a method to look up a job by NzoId, returning the `DownloadJob` or null.

#### Scenario: Lookup existing job
- **WHEN** a job with NzoId "abc123" exists
- **THEN** looking up "abc123" returns the job

#### Scenario: Lookup non-existing job
- **WHEN** no job with NzoId "xyz" exists
- **THEN** looking up "xyz" returns null
