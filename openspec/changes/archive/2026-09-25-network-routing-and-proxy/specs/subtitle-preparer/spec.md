## MODIFIED Requirements

### Requirement: SubtitlePreparer interface
The `ISubtitlePreparer` interface SHALL define a single async method for preparing subtitles, accepting an optional proxy URL for routing traffic through an HTTP proxy.

#### Scenario: Interface signature
- **WHEN** a consumer needs to prepare a subtitle
- **THEN** the interface SHALL expose `Task<string?> PrepareAsync(string url, string outputDirectory, string? proxyUrl, CancellationToken ct)`
- **AND** return the local file path on success or null when subtitles are unavailable

### Requirement: SubtitlePreparer uses HttpClient via DI
The `SubtitlePreparer` SHALL receive `IHttpClientFactory` through dependency injection for HttpClient creation and SHALL configure the proxy per-request based on the provided proxy URL.

#### Scenario: DI registration
- **WHEN** the SubtitlePreparer is registered in DI
- **THEN** it SHALL use `IHttpClientFactory` for HttpClient creation

## ADDED Requirements

### Requirement: SubtitlePreparer routes through proxy
When a proxy URL is provided, the SubtitlePreparer SHALL create an HttpClient configured with a `WebProxy` pointing to the specified proxy URI.

#### Scenario: Subtitle download with proxy
- **WHEN** `proxyUrl` is "http://proxy-at:8888"
- **THEN** the SubtitlePreparer SHALL create an HttpClient with `WebProxy("http://proxy-at:8888")` on its handler
- **AND** route the subtitle download through the proxy

#### Scenario: Subtitle download without proxy
- **WHEN** `proxyUrl` is null
- **THEN** the SubtitlePreparer SHALL use the default HttpClient without proxy configuration
