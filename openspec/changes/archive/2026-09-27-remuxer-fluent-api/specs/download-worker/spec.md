## MODIFIED Requirements

### Requirement: DownloadWorker builds RemuxOptions
The DownloadWorker SHALL construct a `RemuxOptions` using the fluent builder and pass it to `IRemuxer.RunAsync`. It SHALL NOT pass routeName or proxyUrl to the Remuxer.

#### Scenario: Starting FFmpeg
- **WHEN** the DownloadWorker starts a download
- **THEN** it SHALL build `RemuxOptions.Create(videoUrl, outputPath).WithSubtitle(subtitleUrl).WithChannel(channel)` and call `IRemuxer.RunAsync(options, onProgress, ct)`

### Requirement: DownloadWorkerState without route fields
The `DownloadWorkerState` record SHALL NOT contain `RouteName` or `ProxyUrl` fields. The channel from `Media` is sufficient for route resolution.

#### Scenario: State after initialization
- **WHEN** a DownloadWorker receives an InitDownload command
- **THEN** the state SHALL contain Media (with Channel) but no RouteName or ProxyUrl
