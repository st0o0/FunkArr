## Why

The API endpoint files have grown organically and lack consistent naming and grouping. RuleSet endpoints are split across three files that each independently register the same route group. File and method names don't match their route prefixes or OpenAPI tags (e.g., `QueueApiEndpoints` maps to `/api/downloads`). The ArrApi adapter files use role-based names (`IndexerApiEndpoints`, `DownloadApiEndpoints`) that collide conceptually with the internal API files.

## What Changes

- **Merge RuleSet endpoints**: Combine `RuleSetApiEndpoints`, `RuleSetWriteApiEndpoints`, and `RuleSetTestApiEndpoints` into a single `RuleSetApiEndpoints` with one route group registration. Extract the test request JSON parsing into a dedicated `RuleSetTestRequestParser` helper class.
- **Rename QueueApiEndpoints → DownloadsApiEndpoints**: Align file/class/method name with the route prefix `/api/downloads` and tag `"Downloads"`.
- **Rename SetupApiEndpoints → SystemApiEndpoints**: Route prefix changes from `/api/health` to `/api/system`, tag from `"Health"` to `"System"`. **BREAKING**: Frontend must update API paths.
- **Rename IndexerApiEndpoints → NewznabApiEndpoints**: Protocol-named, tag changes from `"Indexer (Newznab)"` to `"Newznab"`.
- **Rename DownloadApiEndpoints → SabnzbdApiEndpoints**: Protocol-named, tag changes from `"Download Client (SABnzbd)"` to `"SABnzbd"`.
- **Update setup containers** to use the new extension method names.
- **Update test files** that reference old method names.

## Capabilities

### New Capabilities

None — this is a pure structural refactor with no new behavior.

### Modified Capabilities

- `ruleset-api`: Merging three endpoint files into one; no requirement changes, only file structure.
- `ruleset-write-api`: Routes move into `ruleset-api`; spec becomes part of `ruleset-api`.
- `ruleset-test-api`: Routes move into `ruleset-api`; spec becomes part of `ruleset-api`.
- `setup-health-check`: Route prefix changes from `/api/health` to `/api/system`, tag from `"Health"` to `"System"`.
- `download-api-internal`: File and method rename only (`QueueApiEndpoints` → `DownloadsApiEndpoints`).
- `newznab-indexer-api`: File/class/method rename, tag change.
- `sabnzbd-download-api`: File/class/method rename, tag change.

## Impact

- **FunkArr.Api**: 4 files renamed/merged, 2 files deleted, 1 new parser helper created
- **FunkArr.ArrApi**: 2 files renamed
- **FunkArr (host)**: 3 setup containers updated (method call names)
- **FunkArr.Api.Tests**: Test files updated for new method names
- **FunkArr.UI**: Frontend API calls updated from `/api/health/*` to `/api/system/*`
- **Breaking**: `/api/health/setup`, `/api/health/storage`, `/api/health/cache` routes change to `/api/system/*`
