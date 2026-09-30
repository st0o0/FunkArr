## Context

FunkArr downloads media from German/Austrian/Swiss public broadcasters via MediathekViewWeb. All network traffic currently flows directly, which fails for geo-restricted ORF and SRF content. Users run VPN gateways externally (e.g., AirVPN + WireGuard in Docker) and need FunkArr to route traffic through HTTP proxies placed in front of those gateways.

The download pipeline is: MediathekViewWeb returns direct media URLs (.mp4/.m3u8) -> SubtitlePreparer downloads subtitles via HttpClient -> FfmpegRunner downloads and remuxes video via FFMpegCore -> DownloadWorker orchestrates the lifecycle. None of these components currently accept proxy configuration.

## Goals / Non-Goals

**Goals:**
- Route different Mediathek channels through different HTTP proxies based on configurable patterns
- Proxy awareness flows through the entire download pipeline (subtitle download, FFmpeg invocation)
- Configuration via environment variables following existing `FunkArr__` convention
- Validation on startup catches misconfigured routes before downloads fail
- Clean separation: FunkArr speaks HTTP proxy only, VPN infrastructure is external

**Non-Goals:**
- SOCKS5 proxy support (HTTP proxy layer in Docker infra abstracts this away)
- yt-dlp integration (MediathekViewWeb returns direct media URLs for all current targets)
- VPN/WireGuard management (external Docker infrastructure concern)
- UI for route configuration (config-only for now)
- Proxy health checks or automatic failover
- Routing for search/discovery API calls (MediathekViewWeb is not geo-restricted)

## Decisions

### 1. HTTP proxy only, no SOCKS5

FFmpeg's SOCKS5 support is unreliable (env var workaround only, not all builds). HTTP proxy support is rock-solid in both FFmpeg (`-http_proxy` flag) and .NET (`WebProxy`). Users place an HTTP proxy (tinyproxy, squid) in front of their VPN gateway in Docker. FunkArr only needs to know HTTP proxy URIs.

Alternative considered: native SOCKS5 via yt-dlp as download wrapper. Rejected because it adds a second download engine to manage, and the HTTP proxy approach keeps FunkArr simpler while pushing VPN complexity to infrastructure.

### 2. Glob pattern matching for channel-to-route mapping

Channel-to-route mapping uses `FileSystemName.MatchesSimpleExpression` with first-match-wins semantics. This handles current MVW channels (ORF, SRF are single strings) and future sub-channels (ORF 1, SRF zwei) with patterns like `ORF*`, `SRF*`.

Alternatives considered:
- Exact string match per channel: requires listing every sub-channel variant; brittle when MVW adds new ones.
- Provider-level grouping: requires an additional channel-to-provider mapping layer that doesn't exist in the codebase.
- Regex: overpowered for prefix/suffix matching; harder to validate and document.

### 3. Route resolver lives in FunkArr.Core

The route resolver is a service (`IRouteResolver`) in FunkArr.Core so both FunkArr.Download and FunkArr.Search can reference it without cross-domain coupling. It takes a channel string and returns a resolved route (name + optional proxy URI).

### 4. FFMpegCore proxy injection via FromUrlInput overload

`FFMpegArguments.FromUrlInput(uri, options => options.WithCustomArgument("-http_proxy ..."))` places the proxy argument before the `-i` flag, which is the correct FFmpeg syntax. No need to leave the FFMpegCore library or set process-level environment variables.

### 5. Route flows through messages, not actor state

The resolved proxy URI is passed through the download pipeline as a parameter, not persisted in actor state. The route is a runtime concern derived from current configuration - if the user changes a route definition, new downloads pick up the change. Persisting the proxy URI would mean old downloads carry stale proxy addresses.

The `InitDownload` message gains an optional `ProxyUrl` field. The DownloadWorker passes it through to the Remuxer, which passes it to SubtitlePreparer and FfmpegRunner.

### 6. No auto-fallback from proxy to direct

When a proxy is unreachable, the download fails rather than silently falling back to a direct connection. Fallback would bypass the geo-restriction the user explicitly configured a proxy for. Failed downloads can be retried, and proxy issues surface clearly in error messages.

### 7. Route resolution happens at download dispatch time

The DownloadManager resolves the route when dispatching a download (it knows the channel from the media entry). The resolved proxy URL is included in the InitDownload message. This means:
- The worker doesn't need access to RoutingOptions or IRouteResolver
- Route resolution is centralized in one place
- The channel-to-route mapping is evaluated once per download

## Risks / Trade-offs

- [FFMpegCore WithCustomArgument may not work for all proxy URI formats] -> Mitigation: integration test with a real proxy; fall back to raw Process invocation if needed
- [Proxy URI in InitDownload is not persisted; on recovery, worker re-resolves from config] -> This is intentional: config may have changed. Worker receives proxy URL again via a new InitDownload after recovery reset.
- [No health check for proxy endpoints] -> Downloads fail with a network error that surfaces in the UI. Users can check proxy connectivity independently. Health checks are a future enhancement.
- [FileSystemName.MatchesSimpleExpression is case-sensitive on Linux] -> Channel names from MVW are consistent (verified via API). Document that patterns are case-sensitive.
