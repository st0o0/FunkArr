## MODIFIED Requirements

### Requirement: DownloadWorker handles InitDownload
The DownloadWorker SHALL handle `InitDownload` messages by persisting all download metadata as a `DownloadInitialized` event and setting status to Initialized. The message SHALL include a route name and optional proxy URL resolved from the channel's route.

#### Scenario: First initialization
- **WHEN** an InitDownload message is received and the Worker has no persisted state
- **THEN** the Worker SHALL persist a DownloadInitialized event with domain metadata (Title, VideoUrl, SubtitleUrl, Channel, Duration, Size, Category)
- **AND** store the RouteName and ProxyUrl in-memory (not persisted) for use during download
- **AND** set status to Initialized

#### Scenario: Already initialized
- **WHEN** an InitDownload message is received but the Worker already has persisted state
- **THEN** the Worker SHALL ignore the message

### Requirement: DownloadWorker handles StartDownload
The DownloadWorker SHALL handle `StartDownload` as a bare go-signal (DownloadId only, no payload). It SHALL delegate media remuxing to `IRemuxer.RunAsync` and pass both the stored route name and proxy URL. It SHALL store the returned `CancellationTokenSource`.

#### Scenario: Start with proxy
- **WHEN** a StartDownload is received and the Worker has a non-null ProxyUrl and RouteName stored from InitDownload
- **THEN** the Worker SHALL pass both RouteName and ProxyUrl through to `IRemuxer.RunAsync`

#### Scenario: Start without proxy
- **WHEN** a StartDownload is received and the Worker has RouteName "Direct" and null ProxyUrl
- **THEN** the Worker SHALL pass RouteName "Direct" and null ProxyUrl to `IRemuxer.RunAsync`

#### Scenario: Start with persisted subtitle
- **WHEN** a StartDownload is received and the persisted metadata has a non-null SubtitleUrl
- **THEN** the Worker SHALL persist a `DownloadStarted` event and call `IRemuxer.RunAsync(videoUrl, subtitleUrl, outputPath, routeName, proxyUrl, onProgress, ct)`

#### Scenario: Start without subtitle
- **WHEN** a StartDownload is received and the persisted metadata has a null SubtitleUrl
- **THEN** the Worker SHALL persist a `DownloadStarted` event and call `IRemuxer.RunAsync(videoUrl, null, outputPath, routeName, proxyUrl, onProgress, ct)`

#### Scenario: Start when not initialized
- **WHEN** a StartDownload is received but no InitDownload has been processed
- **THEN** the Worker SHALL ignore the message
