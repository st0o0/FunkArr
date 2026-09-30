## Context

FunkArr exposes two compatibility API surfaces — a Newznab XML indexer for Prowlarr and a SABnzbd JSON download client for Sonarr/Radarr. Both are implemented as thin ASP.NET Minimal API adapters with no business logic. The current implementations cover the basic happy paths but miss several endpoints and response fields that Sonarr, Radarr, and Prowlarr expect during connection testing and normal operation.

The adapter projects (`FunkArr.IndexerApi`, `FunkArr.DownloadApi`) use in-memory state stubs (`DownloadState`) that will later be replaced by actor communication via Messages. This change keeps that boundary intact — all new endpoints work against the same stub surface.

## Goals / Non-Goals

**Goals:**
- Make Sonarr/Radarr connection test succeed (requires `fullstatus` mode)
- Make Sonarr/Radarr download submission work (requires multipart `addfile`)
- Make Prowlarr use standard `t=get` NZB download path
- Return proper Newznab error codes so Prowlarr can display meaningful messages
- Accept all standard query parameters on search endpoints (ready for Search domain)
- Add pagination support on all list endpoints

**Non-Goals:**
- Implementing actual search logic (Search domain is a separate concern)
- SABnzbd admin modes (pause/resume server, shutdown, get_scripts, server_stats)
- User/registration Newznab endpoints (registration is always "no")
- Actual file deletion on disk when `del_files=1` (stub behavior preserved)

## Decisions

### D1: Standard `t=get` replaces custom `/nzb` endpoint

The current `/nzb` endpoint uses base64-encoded URL/title query params. The Newznab standard uses `t=get&id=<guid>` where the GUID maps to a known item. Since FunkArr doesn't have a persistent item store yet, the `t=get` handler will accept a GUID that encodes the download URL (same base64 approach), maintaining the current data flow while using the standard path.

The `/nzb` route will be removed. Enclosure URLs in search results will point to `?t=get&id=<guid>` instead.

**Alternative considered:** Keep `/nzb` alongside `t=get` for backward compatibility. Rejected — there are no external consumers yet (version 0.x), and maintaining two paths adds complexity for no benefit.

### D2: Newznab error responses as XML model

Errors will use a dedicated `NewznabError` XML-serializable model returning `<error code="X" description="Y"/>`. The `ApiKeyEndpointFilter` will return code 100 for invalid keys. The main handler will return code 202 for unknown `t=` values instead of HTTP 404.

Error codes used: 100 (invalid credentials), 200 (missing parameter), 201 (incorrect parameter), 202 (function undefined).

### D3: Multipart form parsing for `addfile`

Sonarr sends `multipart/form-data` with the NZB file as a form file field named `nzbfile`. The handler will use `IFormFile` from `context.Request.Form.Files` to read the uploaded NZB content, then parse URL/title from XML comments as before. The `cat` and `priority` parameters come from query string.

**Alternative considered:** Support both raw body and multipart. Rejected — only Sonarr/Radarr call this endpoint, and they always use multipart.

### D4: `fullstatus` returns minimal viable response

Sonarr checks `fullstatus` during connection test. The response only needs a `status` object with `paused` (bool), `speedlimit` (string), and basic disk/folder info. All values will be stubbed (paused=false, speedlimit="", diskspace=free space of download path or "0").

### D5: Pagination via `start`/`limit` on SABnzbd, `offset`/`limit` on Newznab

SABnzbd uses `start` (0-based index) and `limit` (page size, default 50). Newznab uses `offset` and `limit` (default from caps). Both will slice the in-memory collections. When the Search domain is built, pagination params will be forwarded via Messages.

### D6: `o=json` output via System.Text.Json serialization

When `o=json` is present on Newznab requests, the response will be JSON instead of XML. The caps and RSS models will be serialized via `System.Text.Json` with `JsonPropertyName` attributes matching the Newznab JSON convention. This requires dual-format models or a separate JSON projection — a JSON projection record set is cleaner since XML serialization attributes would conflict.

**Alternative considered:** Single model with both XML and JSON attributes. Rejected — `XmlElement` and `JsonPropertyName` on the same class creates maintenance burden, and the JSON field names differ from XML element names in Newznab convention.

## Risks / Trade-offs

- **[GUID encoding for `t=get`]** → Using base64-encoded URLs as GUIDs is a temporary solution. When items have persistent IDs (from the Search domain), `t=get` will need to look up by real GUID. This is acceptable because the NZB content embeds the actual URL regardless of how the GUID is structured.

- **[Multipart-only `addfile`]** → Dropping raw body support is a breaking change from the current stub behavior. Mitigated by: no external consumers yet (0.x version), and Sonarr/Radarr always use multipart.

- **[JSON output complexity]** → Supporting `o=json` doubles the serialization surface for Newznab responses. Mitigated by: separate JSON projection records keep XML models untouched, and Prowlarr rarely uses JSON format.
