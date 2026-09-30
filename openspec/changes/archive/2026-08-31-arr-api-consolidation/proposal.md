## Why

IndexerApi and DownloadApi share duplicated code (Nzb model, ApiKeyEndpointFilter), the NZB format is their common transport envelope, and DownloadApi contains business logic (DownloadState) that violates the "thin translator" rule. Consolidating into a single ArrApi project eliminates duplication, enforces adapter thinness, and lets categories flow from the RuleSet domain instead of being hardcoded in two places.

## What Changes

- **Merge** `FunkArr.IndexerApi` and `FunkArr.DownloadApi` into `FunkArr.ArrApi`
- **Deduplicate** `Nzb.cs` model — one shared NZB object model within ArrApi
- **Deduplicate** `ApiKeyEndpointFilter` — one filter, format-aware error responses (XML for Newznab, JSON for SABnzbd)
- **Unify** `NzbGenerator` and `NzbParser` — co-located as complementary operations on one model
- **Remove** `DownloadState` from the adapter — queue/history management is domain logic, belongs in Download domain (not implemented in this change, just removed from adapter)
- **Remove hardcoded categories** from Caps and BuildConfig — categories will come from RuleSet domain via Messages (actual message wiring is a future change, this change removes the hardcoding and defines the contract)
- **Merge test projects** `FunkArr.IndexerApi.Tests` and `FunkArr.DownloadApi.Tests` into `FunkArr.ArrApi.Tests`
- **Internal namespace structure**: `FunkArr.ArrApi.Newznab` (indexer wire format) and `FunkArr.ArrApi.Sabnzbd` (download client wire format), shared types at `FunkArr.ArrApi` root

## Capabilities

### New Capabilities

- `arr-api-structure`: Internal project layout of the unified ArrApi — namespace organization (Newznab/, Sabnzbd/, shared root), deduplicated NZB model, unified ApiKeyEndpointFilter with format-aware error responses

### Modified Capabilities

- `project-structure`: Adapter projects list changes from IndexerApi + DownloadApi to ArrApi, test projects change accordingly
- `nzb-object-model`: Duplication requirement removed — single NZB model shared within ArrApi, NzbGenerator and NzbParser co-located

## Impact

- **Projects**: `FunkArr.IndexerApi`, `FunkArr.DownloadApi` deleted; `FunkArr.ArrApi` created
- **Test projects**: `FunkArr.IndexerApi.Tests`, `FunkArr.DownloadApi.Tests` deleted; `FunkArr.ArrApi.Tests` created
- **Solution file**: `FunkArr.slnx` updated
- **Host project**: `FunkArr.csproj` references change
- **CLAUDE.md**: Solution structure section updated
- **Architecture tests**: Updated for new project name
- **No behavioral changes**: All Newznab and SABnzbd endpoints continue to work identically
- **DownloadState removed**: Endpoints that depend on it will return stub/error responses until the Download domain actor is built
