## Why

Controllers bypass ASP.NET model binding by manually reading `Request.Body` and `Request.Query` instead of using `[FromBody]`, `[FromQuery]`, `[FromRoute]`, and `[FromForm]` attributes. This prevents automatic validation, breaks OpenAPI/Scalar schema generation for request parameters, and diverges from Microsoft's controller conventions. Additionally, several endpoints return anonymous objects instead of typed responses, making the API contract invisible to tooling.

The Newznab RSS feed (empty-query `?t=tvsearch`) also bypasses the SearchActor pipeline by calling MediathekClient directly, skipping caching, content filtering, and quality probing.

## What Changes

- Configure global `JsonSerializerOptions` on `AddControllers().AddJsonOptions()` (camelCase, WhenWritingNull, JsonStringEnumConverter) so `[FromBody]` works without manual deserialization
- Replace all manual `Request.Body` / `Request.Query` reads with proper `[FromBody]`, `[FromQuery]`, `[FromRoute]`, `[FromForm]` attributes
- Replace anonymous response objects with typed records in `Api/Models/`
- Move `TestRulesRequest` from private nested record in RulesetController to `Api/Models/`
- Route Newznab RSS feed through `SearchActor.TextSearchRequest("")` instead of direct `MediathekClient` call
- Remove `HandleRssFeed` method that bypasses the actor pipeline
- Add missing `[ProducesResponseType]` attributes for complete OpenAPI coverage

## Capabilities

### New Capabilities

_None — this is a refactor of existing controller internals._

### Modified Capabilities

- `ruleset-api`: Request binding changes from manual body deserialization to `[FromBody]`; response shapes change from anonymous objects to typed records for delete/reload/test endpoints
- `sabnzbd-download-client`: Request binding changes from manual query/form reads to `[FromQuery]`/`[FromForm]`; response shapes formalized as typed records
- `newznab-indexer`: Request binding changes from manual query reads to `[FromQuery]`; RSS feed routing changes from direct MediathekClient to SearchActor pipeline
- `setup-validation`: Request binding changes from manual body read to `[FromBody]` for validate endpoint

## Impact

- **Controllers**: All 6 controllers in `Api/Controllers/` touched, but external API contracts (URLs, response JSON shapes) remain identical
- **DI/Startup**: `FunkArrServiceSetup` gains `.AddJsonOptions()` configuration
- **Models**: New typed response records added to `Api/Models/`
- **Search pipeline**: RSS feed now flows through SearchActor → TextSearchActor → MatchingPipeline (gains caching + content filter + quality probing)
- **No breaking changes**: All Newznab/SABnzbd protocol responses maintain identical JSON/XML structure for Sonarr/Radarr/Prowlarr compatibility
