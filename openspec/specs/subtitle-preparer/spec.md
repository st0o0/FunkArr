# Subtitle Preparer

## Purpose

Downloads subtitle content from a remote URL via HttpClient with proxy support. Format detection, parsing, and conversion are delegated to the subtitle-pipeline and subtitle-formats capabilities.

## Requirements

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
