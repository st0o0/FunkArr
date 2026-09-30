## Why

FunkArr.ArrApi already uses `[AsParameters]` request records for clean parameter binding, but FunkArr.Api endpoints still use inline lambda parameters (7 query params in MediathekApiEndpoints, 3 in DownloadsApiEndpoints history). The RuleSetApiEndpoints list handler has a complex fan-out lambda that should be extracted like its sibling CRUD handlers. Timeout values are scattered as magic numbers. Aligning FunkArr.Api to the same pattern as ArrApi makes endpoints consistent, testable, and self-documenting via OpenAPI.

## What Changes

- Introduce `[AsParameters]` request records for `MediathekApiEndpoints` search and `DownloadsApiEndpoints` history endpoints
- Extract `RuleSetApiEndpoints` list lambda into a `HandleList` private static method (matching existing `HandleCreate`, `HandleUpdate`, `HandleDelete`, `HandleExport`)
- Consolidate duplicate `SerializeForDisk` methods into a single shared helper
- Introduce timeout constants (shared or per-endpoint-class) to replace scattered `TimeSpan.FromSeconds(...)` literals

## Capabilities

### New Capabilities
- `api-timeout-constants`: Centralized timeout configuration for API endpoint actor asks

### Modified Capabilities
- `api-request-models`: Extend with request records for MediathekApi search and DownloadsApi history endpoints

## Impact

- **FunkArr.Api**: MediathekApiEndpoints, DownloadsApiEndpoints, RuleSetApiEndpoints modified
- **FunkArr.Api.Models**: New request record types added
- **No external API changes** — query parameter names and behavior stay identical
- **No new dependencies**
