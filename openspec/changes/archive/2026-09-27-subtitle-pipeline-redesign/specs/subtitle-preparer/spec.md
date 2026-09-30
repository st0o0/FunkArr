## REMOVED Requirements

### Requirement: SubtitlePreparer downloads and converts subtitles
**Reason**: Replaced by the `subtitle-pipeline` capability which returns `SubtitleResult` instead of `string?`, always emits SRT, and delegates to per-format parsers.
**Migration**: The new `ISubtitlePreparer.PrepareAsync` returns `SubtitleResult`. Consumers pattern-match on `Success`/`Failed`/`Unavailable` instead of checking for null.

### Requirement: SubtitlePreparer detects format by content sniffing
**Reason**: Replaced by the `subtitle-parsing` capability's `SubtitleFormatDetector` which strips BOM first, caches XDocument for TTML, and uses the same detection heuristics.
**Migration**: Detection logic moves to `SubtitleFormatDetector`. Same content-sniffing approach, cleaner separation.

### Requirement: SubtitlePreparer converts TTML to SRT
**Reason**: Replaced by `TtmlParser` + `SrtEmitter` in the `subtitle-parsing` capability. The `TtmlToSrtConverter` static class is replaced by a parser that produces `SubtitleCue` entries.
**Migration**: `TtmlToSrtConverter.Convert(string)` is replaced by `TtmlParser.Parse(XDocument)` returning cues, then `SrtEmitter.Emit(cues)` for SRT output.

### Requirement: SubtitlePreparer interface
**Reason**: Replaced by new interface signature in `subtitle-pipeline` capability.
**Migration**: `Task<string?> PrepareAsync(...)` becomes `Task<SubtitleResult> PrepareAsync(...)`.

## MODIFIED Requirements

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
