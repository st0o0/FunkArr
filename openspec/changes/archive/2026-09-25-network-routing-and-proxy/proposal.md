## Why

FunkArr currently connects to all Mediatheken directly, which works for German broadcasters (ARD, ZDF) but fails for geo-restricted content from Austrian (ORF) and Swiss (SRF) Mediatheken. Users need to route traffic through country-specific proxies to access this content. The routing decision must flow through the entire pipeline - from discovery through subtitle download to FFmpeg invocation - because geo-restrictions apply at the media URL level, not just at search time.

## What Changes

- New `RoutingOptions` configuration section (`FunkArr:Routes`) with named route definitions, glob-based channel-to-route mapping, and a configurable default route
- Route resolver service that maps MVW channel strings to routes using `FileSystemName.MatchesSimpleExpression` (first match wins)
- Proxy-aware FFmpeg invocation via `FromUrlInput` overload with `-http_proxy` custom argument
- Proxy-aware subtitle download via `WebProxy` on the `SubtitlePreparer` HttpClient handler
- Route information flows through download messages (`AddDownload`/`InitDownload`) so the entire download pipeline uses the correct network path
- Configuration validation on startup (route references, URI format, uniqueness)
- Updated `appsettings.json` and `docker-compose.example.yml` with route configuration

## Capabilities

### New Capabilities

- `network-routing`: Named route definitions (Direct, Austria, Switzerland, ...) with optional HTTP proxy URIs. Glob-based channel-to-route mapping. Route resolver service. Configuration validation.

### Modified Capabilities

- `ffmpeg-runner`: Add proxy URL parameter to `BuildArguments`; inject `-http_proxy` via `WithCustomArgument` on `FromUrlInput`
- `subtitle-preparer`: Proxy-aware `HttpMessageHandler` based on resolved route
- `download-worker`: Carry route information through download lifecycle; pass proxy to remuxer/FFmpeg

## Impact

- **FunkArr.Core**: New `RoutingOptions`, `RouteDefinition`, `ChannelRoute` classes; new route resolver service interface
- **FunkArr.Download**: `FfmpegRunner`, `SubtitlePreparer`, `Remuxer`, `DownloadWorker` gain route/proxy awareness
- **FunkArr.Messages**: `AddDownload`/`InitDownload` messages extended with route info
- **FunkArr (Host)**: New `RoutingSetupContainer` for DI wiring and options validation
- **Configuration**: New `FunkArr:Routes` section in appsettings and docker-compose example
- **Docker infrastructure** (outside FunkArr): Users deploy HTTP proxy sidecars (e.g., tinyproxy) in front of VPN gateways; FunkArr only speaks HTTP proxy
