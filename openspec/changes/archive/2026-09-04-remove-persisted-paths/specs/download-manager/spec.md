## MODIFIED Requirements

### Requirement: DownloadManager accepts AddDownload
The DownloadManager SHALL handle `AddDownload` messages by assigning a new `Guid` as `DownloadId`, persisting a `DownloadEnqueued` event, forwarding an `InitDownload` message (with domain metadata only, no paths) to the DownloadWorker shard region, and responding with `DownloadAdded`.

#### Scenario: Successful add
- **WHEN** an `AddDownload` message is received
- **THEN** the Manager SHALL generate a new DownloadId
- **AND** persist a `DownloadEnqueued` event with DownloadId
- **AND** send `InitDownload` with domain metadata (DownloadId, Title, VideoUrl, SubtitleUrl, Channel, Duration, Size, Category) to the Worker shard region
- **AND** the `InitDownload` message SHALL NOT contain IncompletePath or OutputPath
- **AND** respond with `DownloadAdded(DownloadId)`
- **AND** call DispatchNext to check if the download can start immediately
