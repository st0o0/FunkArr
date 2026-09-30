# Download Manager

## MODIFIED Requirements

### Requirement: DownloadManager accepts AddDownload
The DownloadManager SHALL handle `AddDownload` messages by assigning a new `Guid` as `DownloadId`, persisting a `DownloadEnqueued` event, forwarding an `InitDownload` message to the DownloadWorker shard region, and responding with `DownloadAdded`.

#### Scenario: Successful add
- **WHEN** an `AddDownload` message is received
- **THEN** the Manager SHALL generate a new DownloadId
- **AND** persist a `DownloadEnqueued` event with DownloadId
- **AND** build `incompletePath` as `Path.Combine(downloadOptions.IncompletePath, downloadId.ToString())`
- **AND** build `outputDir` using `downloadOptions.ResolveCategoryDir(cmd.Category)` to resolve the category subfolder under `downloadOptions.CompletePath`, with the title as a final subfolder
- **AND** build `outputPath` as `Path.Combine(outputDir, cmd.Title + ".mkv")`
- **AND** send `InitDownload` (with all metadata including IncompletePath and OutputPath) to the Worker shard region
- **AND** respond with `DownloadAdded(DownloadId)`
- **AND** call DispatchNext to check if the download can start immediately

### Requirement: DownloadManager enforces concurrency limit
The DownloadManager SHALL limit the number of concurrent downloads to the configured `DownloadOptions.ConcurrentDownloads` (default 3). When a slot is available, the Manager SHALL persist a `DownloadDispatched` event and send a bare `StartDownload(DownloadId)` go-signal to the Worker shard region.

#### Scenario: Under capacity
- **WHEN** DispatchNext runs and fewer than `DownloadOptions.ConcurrentDownloads` downloads are in the Dispatched set
- **THEN** the Manager SHALL move the next Queued item to the Dispatched set
- **AND** persist a `DownloadDispatched` event with DownloadId
- **AND** send `StartDownload(DownloadId)` to the Worker shard region

#### Scenario: At capacity
- **WHEN** DispatchNext runs and the Dispatched set has reached the configured maximum
- **THEN** the Manager SHALL not dispatch any further downloads
