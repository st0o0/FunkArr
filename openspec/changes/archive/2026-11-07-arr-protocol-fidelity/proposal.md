## Why

Several small protocol-level response gaps produce visible warnings in Sonarr/Radarr and swallow error messages during setup. The SABnzbd `get_config` response is missing fields that Arr health checks evaluate, all error responses use HTTP 403/400 instead of the protocol-standard 200, and numeric formatting is culture-dependent. These are output-only fixes that remove friction without changing download or search behavior.

## What Changes

- Add `history_retention_option` and `history_retention_number` to SABnzbd `get_config` response — removes "history retention too short" warning
- Populate `tv_categories` and `movie_categories` in `get_config` from configured categories — enables Arr sorting validation
- Change all Newznab and SABnzbd error responses to HTTP 200 with error body — matches real server behavior so Arr parses and displays the specific error message
- Fix SABnzbd API key error: "API Key Required" (not "Incorrect"), HTTP 200 (not 403)
- Fix `mb`/`mbleft` formatting: invariant culture with decimal precision instead of culture-dependent whole-MB rounding

## Capabilities

### New Capabilities

- `sab-config-fidelity`: SABnzbd `get_config` response fields that Arr health checks evaluate

### Modified Capabilities

- `error-handling-standards`: Error responses change from HTTP 4xx to HTTP 200 with structured error body (Newznab XML `<error>`, SABnzbd JSON `{"status":false,"error":"..."}`)
- `api-authentication`: SABnzbd API key rejection message and status code change

## Impact

- `FunkArr.ArrApi` only — `ConfigResponse.cs`, `SabnzbdResponseMapper.cs`, `NewznabXmlResult.cs`, `ApiKeyActionFilter.cs`, `SabnzbdController.cs`
- No persistence, no messages, no domain logic changes
- Arr health checks pass clean after this change
- Error messages become visible in Arr UI during setup troubleshooting
