## 1. Newznab Error Format & Auth

- [x] 1.1 Add `NewznabError` XML model (`<error code="X" description="Y"/>`) with serialization
- [x] 1.2 Refactor `IndexerApi.ApiKeyEndpointFilter` to return `NewznabError` XML with code 100
- [x] 1.3 Replace HTTP 404 for unknown `t=` with `NewznabError` code 202
- [x] 1.4 Tests for all error codes (100, 200, 201, 202)

## 2. Standard NZB Download (`t=get`)

- [x] 2.1 Add `t=get` handler accepting `id` param (base64-encoded `title|url` GUID)
- [x] 2.2 Return `NewznabError` code 200 for missing `id`, code 201 for invalid base64
- [x] 2.3 Remove `/nzb` endpoint and `NzbGenerator.TryDecodeBase64` (replaced by GUID parsing)
- [x] 2.4 Update enclosure URL generation in RSS items to use `?t=get&id=<guid>`
- [x] 2.5 Tests for `t=get` success, missing param, invalid encoding

## 3. Newznab Search Parameters & Pagination

- [x] 3.1 Parse `offset`/`limit` on search endpoints, reflect offset in `<newznab:response>`
- [x] 3.2 Parse filter params (`cat`, `maxage`, `minsize`, `maxsize`, `extended`, `attrs`) — accept without error, ignore for now
- [x] 3.3 Update `Caps` model to reflect `offset`/`limit` support in `<limits>`
- [x] 3.4 Tests for pagination params reflected in response, filter params accepted

## 4. Newznab JSON Output (`o=json`)

- [x] 4.1 Add JSON projection records for caps and RSS responses with `JsonPropertyName` attributes
- [x] 4.2 Add `o=json` query param check in handler, route to JSON serialization when present
- [x] 4.3 Tests for `o=json` on caps and search endpoints, default remains XML

## 5. SABnzbd `fullstatus` Mode

- [x] 5.1 Add `FullStatusResponse` model with `status` object (`paused`, `speedlimit`, `diskspace1`, `diskspace2`, `completedir`)
- [x] 5.2 Add `mode=fullstatus` handler returning stubbed status
- [x] 5.3 Tests for fullstatus response structure and `skip_dashboard` param acceptance

## 6. SABnzbd Multipart `addfile`

- [x] 6.1 Refactor `addfile` handler to read NZB from `IFormFile` (field name `nzbfile`) instead of raw body
- [x] 6.2 Move NZB comment parsing to shared `NzbGenerator.ParseNzb` (already exists, reuse)
- [x] 6.3 Accept optional `priority` query parameter
- [x] 6.4 Tests for multipart upload success, missing file, invalid NZB content

## 7. SABnzbd Queue Delete & Response Fields

- [x] 7.1 Add queue `name=delete` subcommand to `HandleGet` with routing for `mode=queue`
- [x] 7.2 Add `DeleteQueueItem` method to `DownloadState`
- [x] 7.3 Add `priority` field to `QueueSlot` model
- [x] 7.4 Add wrapper fields to `QueueResponse`: `paused`, `speedlimit`, `noofslots_total`, `diskspace1`, `diskspace2`, `speed`
- [x] 7.5 Return error for unknown queue subcommands
- [x] 7.6 Tests for queue delete (success, not found), wrapper fields, unknown subcommand

## 8. SABnzbd History Response Fields

- [x] 8.1 Add `fail_message` and `completed_on` fields to `HistorySlot` model
- [x] 8.2 Add `noofslots` wrapper field to `HistoryResponse`
- [x] 8.3 Set `completed_on` timestamp when items are added to history
- [x] 8.4 Tests for new history fields present in response

## 9. SABnzbd Pagination

- [x] 9.1 Parse `start`/`limit` params on `mode=queue` and `mode=history`
- [x] 9.2 Apply slice logic to `DownloadState.GetQueue()` and `GetHistory()` results
- [x] 9.3 Tests for paginated queue and history responses

## 10. SABnzbd Retry Mode

- [x] 10.1 Add `mode=retry` handler accepting `value` (nzo_id)
- [x] 10.2 Add `RetryItem` method to `DownloadState` (move from history back to queue if status is "Failed")
- [x] 10.3 Tests for retry success, not found, non-failed item
