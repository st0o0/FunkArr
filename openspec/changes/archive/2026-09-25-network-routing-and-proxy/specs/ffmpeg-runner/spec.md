## MODIFIED Requirements

### Requirement: FfmpegRunner is a static facade
The FfmpegRunner SHALL be a static class that encapsulates FFmpeg process lifecycle, progress parsing, and result reporting behind a single `Run` method. The `Run` method SHALL accept an optional proxy URL for routing traffic through an HTTP proxy.

#### Scenario: Run signature
- **WHEN** a caller invokes `FfmpegRunner.Run`
- **THEN** it SHALL accept `IActorRef self`, `string videoUrl`, `string? subtitlePath`, `string outputPath`, `string? proxyUrl`
- **AND** `subtitlePath` SHALL be a local file path (not a URL), or null
- **AND** `proxyUrl` SHALL be an HTTP proxy URI or null for direct connection
- **AND** return a `CancellationTokenSource` for cancellation control

## ADDED Requirements

### Requirement: FfmpegRunner injects proxy as input-level argument
When a proxy URL is provided, the FfmpegRunner SHALL inject it as a `-http_proxy` custom argument on the `FromUrlInput` call so it appears before the `-i` flag in the FFmpeg command line.

#### Scenario: Download with proxy
- **WHEN** `proxyUrl` is "http://proxy-at:8888" and `videoUrl` is "https://example.com/stream.m3u8"
- **THEN** FFmpeg SHALL be invoked with `-http_proxy http://proxy-at:8888` before `-i https://example.com/stream.m3u8`

#### Scenario: Download without proxy
- **WHEN** `proxyUrl` is null
- **THEN** FFmpeg SHALL be invoked without any `-http_proxy` argument
