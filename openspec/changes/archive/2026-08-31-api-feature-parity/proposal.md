## Why

Prowlarr, Sonarr, and Radarr expect specific Newznab and SABnzbd API behaviors that FunkArr currently doesn't implement. Connection tests fail because `fullstatus` mode is missing, downloads fail because `addfile` expects multipart form data, and standard NZB download by GUID (`t=get`) isn't supported. Closing these gaps is required before FunkArr can function as a drop-in replacement in the *arr ecosystem.

## What Changes

### DownloadApi (SABnzbd)
- Add `mode=fullstatus` endpoint for Sonarr connection test
- Fix `mode=addfile` to accept multipart/form-data file upload (not raw body)
- Add queue delete subcommand (`mode=queue&name=delete&value=<id>`)
- Add `start`/`limit` pagination on queue and history
- Add missing response fields: `priority` on queue slots, wrapper fields (`speed`, `paused`, `noofslots`, `diskspace1`, `diskspace2`), `fail_message` and `completed_on` on history slots
- Add `mode=retry` for retrying failed downloads

### IndexerApi (Newznab)
- Add `t=get` standard NZB download by GUID, replacing custom `/nzb` endpoint
- Return proper Newznab error XML (`<error code="X" description="Y"/>`) with standard codes
- Accept `offset`/`limit` pagination on search endpoints
- Accept and validate search parameters (`cat`, `maxage`, `minsize`, `maxsize`, `extended`, `attrs`)
- Support `o=json` output format option on caps and search responses

## Capabilities

### New Capabilities

_(none — all changes modify existing capabilities)_

### Modified Capabilities

- `newznab-indexer-api`: Add `t=get` NZB download, Newznab error XML format, search pagination, search parameter acceptance, `o=json` output option
- `sabnzbd-download-api`: Add `fullstatus` mode, fix multipart `addfile`, add queue delete, pagination, missing response fields, `retry` mode

## Impact

- **FunkArr.IndexerApi**: `IndexerApiEndpoints.cs`, `ApiKeyEndpointFilter.cs`, new error model, modified `Caps`/`Rss` models
- **FunkArr.DownloadApi**: `DownloadApiEndpoints.cs`, `DownloadState.cs`, modified `QueueResponse`/`HistoryResponse` models, new `FullStatusResponse` model
- **FunkArr.IndexerApi.Tests**: New/updated tests for all Newznab changes
- **FunkArr.DownloadApi.Tests**: New/updated tests for all SABnzbd changes
- **No domain project changes** — adapters remain thin translators
