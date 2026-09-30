## Why

Sonarr and Radarr validate specific SABnzbd API response fields during connection testing and runtime that FunkArr currently returns as empty or hardcoded. Source code analysis of both clients reveals concrete gaps: empty category lists fail connection validation, missing pagination support ignores client-sent parameters, and delete operations discard the `del_files` flag. These gaps prevent reliable integration with the *arr ecosystem.

## What Changes

- Fill `config.categories` with standard entries (sonarr, radarr, tv, movies) so connection tests pass
- Add `config.misc.pre_check` field that Sonarr reads during connection testing
- Forward `start`/`limit` pagination parameters through query messages to DownloadManager
- Accept and forward `del_files` parameter on queue and history delete operations
- Accept `category` filter parameter on queue and history list operations (Radarr server-side filtering)
- Forward `priority` parameter from addfile POST to AddDownload message
- Populate speed fields in queue slots and fullstatus with actual values instead of hardcoded "0"
- Add intermediate history status values (Extracting, Moving, Verifying) for download pipeline stages
- Accept `archive` parameter on history delete (treat as regular delete)

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `sabnzbd-download-api`: Add missing response fields (categories, pre_check), pagination forwarding, delete parameters (del_files, archive), category filtering, and speed fields
- `download-messages`: Extend QueryQueue, QueryHistory with pagination and category filter; extend DeleteDownload with del_files flag; extend AddDownload with priority; add intermediate DownloadStatus values

## Impact

- **ArrApi adapter**: DownloadApiEndpoints.cs request/response changes, DownloadGetRequest.cs new query params
- **Messages**: QueryQueue, QueryHistory, DeleteDownload, AddDownload record changes, DownloadStatus enum extension
- **Response models**: QueueResponse, HistoryResponse, FullStatusResponse field additions
- **Spec updates**: sabnzbd-download-api spec and download-messages spec
- **No breaking changes**: All additions are backward-compatible (new optional parameters, additional response fields)
