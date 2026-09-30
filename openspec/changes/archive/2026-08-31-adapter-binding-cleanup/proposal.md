## Why

Both adapter projects (IndexerApi, DownloadApi) manually extract query parameters from `HttpContext.Request.Query` instead of using ASP.NET Minimal API's `[FromQuery]` binding. The NZB generator builds XML via string interpolation while every other XML model uses `XmlSerializer` with attribute-decorated classes. Both patterns are inconsistent with the rest of the codebase and harder to maintain.

## What Changes

- Replace manual `HttpContext.Request.Query` extraction with `[FromQuery]`-annotated request records in both IndexerApi and DownloadApi endpoints.
- Replace `NzbGenerator.Generate` string interpolation with an `[XmlRoot("nzb")]` object model using `<head><meta>` elements for title/url metadata (NZB spec-compliant).
- Update `NzbParser` (DownloadApi) to read `<head><meta>` elements instead of XML comments.
- Remove the comment-based title/url encoding from `NzbGenerator.ParseNzb` (IndexerApi).

## Capabilities

### New Capabilities

- `adapter-parameter-binding`: Replace manual query string parsing with `[FromQuery]` request records in IndexerApi and DownloadApi endpoints.
- `nzb-object-model`: Replace NZB string interpolation with XmlSerializer-based object model using spec-compliant `<head><meta>` metadata.

### Modified Capabilities

## Impact

- `FunkArr.IndexerApi`: `IndexerApiEndpoints.cs` (parameter binding), `NzbGenerator.cs` (object model), new request record.
- `FunkArr.DownloadApi`: `DownloadApiEndpoints.cs` (parameter binding), `NzbParser.cs` (meta parsing), new request record.
- `FunkArr.IndexerApi.Tests` / `FunkArr.DownloadApi.Tests`: Tests for NZB generation/parsing need updating for new format.
- **BREAKING**: NZB format changes from XML comments to `<head><meta>`. In-flight NZBs generated before this change won't parse with the new parser. Acceptable at 0.x.
