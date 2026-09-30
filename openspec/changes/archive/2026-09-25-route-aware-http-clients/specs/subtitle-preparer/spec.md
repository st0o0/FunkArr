## MODIFIED Requirements

### Requirement: SubtitlePreparer interface
The `ISubtitlePreparer` interface SHALL define a single async method for preparing subtitles, accepting a route name for HttpClient selection.

#### Scenario: Interface signature
- **WHEN** a consumer needs to prepare a subtitle
- **THEN** the interface SHALL expose `Task<string?> PrepareAsync(string url, string outputDirectory, string routeName, CancellationToken ct)`
- **AND** return the local file path on success or null when subtitles are unavailable

### Requirement: SubtitlePreparer uses HttpClient via DI
The `SubtitlePreparer` SHALL receive `IHttpClientFactory` through dependency injection and create clients by route name.

#### Scenario: DI registration
- **WHEN** the SubtitlePreparer is registered in DI
- **THEN** it SHALL use `IHttpClientFactory` for HttpClient creation

### Requirement: SubtitlePreparer routes through proxy
When a route with a proxy is configured, the SubtitlePreparer SHALL use the named HttpClient registered for that route, which already has the correct proxy handler configured.

#### Scenario: Subtitle download with proxy
- **WHEN** `routeName` is "Austria" and a named HttpClient "route:Austria" is registered with a proxy handler
- **THEN** the SubtitlePreparer SHALL call `httpClientFactory.CreateClient("route:Austria")`
- **AND** the returned client SHALL route traffic through the configured proxy

#### Scenario: Subtitle download without proxy
- **WHEN** `routeName` is "Direct" and a named HttpClient "route:Direct" is registered without a proxy
- **THEN** the SubtitlePreparer SHALL call `httpClientFactory.CreateClient("route:Direct")`
- **AND** the returned client SHALL connect directly
