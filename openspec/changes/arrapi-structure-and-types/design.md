## Context

`FunkArr.ArrApi` is a controller-based adapter for Newznab (`/index/api`) and SABnzbd (`/download/api`). Existing tests call services and filters directly. SABnzbd queue/history responses are typed records, but `status`, `nzo_ids`, `get_config`, and the API-key error are anonymous objects; `SabnzbdResult.Ok(object Data)` loses the type. The NZB id is built in `NewznabSearchService.ToItem` (tab-joined, Base64) and parsed by index in `NzbService.GetNzb`.

Constraints: wire format of SABnzbd JSON and Newznab XML stays identical; the feature is unreleased so the NZB id format may change; enum rules from `AGENTS.md` (`Unknown = 0`, `[JsonStringEnumMemberName]` only in the API layer, enum mapping via `switch`).

## Goals / Non-Goals

**Goals**
- Files and namespaces match the folder structure.
- No anonymous types in SABnzbd responses; typed results end to end.
- One definition of the NZB id (`NzbPayload`).

**Non-Goals**
- Changing controllers, request records, binding, or error handling.
- Persistent/sharded entity for NZB data (later; `NzbPayload` is the seam).

## Decisions

### D1: Folder layout
```
ArrApi/  ArrApiOptions, ApiKeyActionFilter, ServiceCollectionExtensions, AssemblyMarker
  Newznab/  NewznabController, Requests/ (IndexerRequest), Models/ (Rss, Caps, Nzb, NzbPayload, NewznabError, NewznabCategory), Services/ (search, nzb, caches, results)
  Sabnzbd/  SabnzbdController, SabnzbdConstants, Requests/ (DownloadGetRequest, DownloadPostRequest), Models/, Services/ (queue, download, mapper, result)
```
Namespaces follow folders. Moves are done first and verified by the unchanged test suite.

### D2: Typed SABnzbd responses
Add `SabnzbdStatusResponse(bool Status)`, `SabnzbdAddResponse(bool Status, IReadOnlyList<string> NzoIds)`, and typed config records (`misc`, `categories`, `sorters`) with `[JsonPropertyName]`. `SabnzbdResult<T>` replaces the untyped `SabnzbdResult.Ok(object)`: nested `Ok(T Data)` and `Error(string Message, int StatusCode)`, implicit conversions from `T` and from a non-generic `SabnzbdError`, so each service method returns its concrete response type and the controller maps with `MapResult<T>`. The API-key filter uses `SabnzbdErrorResponse`.

Slot `status` and `priority` become enums with `[JsonConverter(typeof(JsonStringEnumConverter<T>))]` on the enum type (MVC controllers use their own `JsonOptions`, not `ConfigureHttpJsonOptions`) and `[JsonStringEnumMemberName]` wire names. Messages enums stay free of serialization attributes; mapping uses `switch` with `_ => throw`.

JSON snapshot tests are written before the refactor against the unchanged code and must pass unchanged afterwards.

Alternative considered: leave status strings as `string`. Rejected: the values are a closed set and the enum rule applies.

### D3: NzbPayload
`NzbPayload(Title, Url, SubtitleUrl?, Channel, Duration (int), Size (long), Category (MediaType))` in `Newznab/Models/`. `Encode()` returns Base64 of UTF-8 JSON; `Decode(string)` returns `NzbPayload?` (null on invalid Base64/JSON or missing mandatory fields). Serialization options are local to `NzbPayload` (string enum converter for `Category`); no attributes on Messages types. `NewznabSearchService.ToItem` calls `Encode()`, `NzbService.GetNzb` calls `Decode()`. NZB `head/meta` elements are built from the payload; `addfile` reads the metas back into a payload, so `NzbParseResult` and the `Meta("url")` fallback are removed.

Alternatives: keep the tab format behind a typed record (no benefit once compatibility does not matter); persistent entity with plain GUID (later change, needs persistence and cleanup).

## Risks / Trade-offs

- [JSON drift in SABnzbd responses breaks Sonarr/Radarr] → Snapshot tests first; enums use the exact current wire strings.
- [Enum JSON behavior in MVC differs from assumption] → Verified by the snapshot tests on the real serializer used by controllers; if the type-level converter is not honored, fall back to explicit converter registration in `AddControllers().AddJsonOptions` for ArrApi.
- [Longer ids from JSON payload] → Acceptable; links stay well below URL limits for typical titles; covered by a round-trip test with a long title.
- [Moves cause large diffs] → Done as a separate commit before any logic change.

## Migration Plan

No data migration. Single deploy; rollback is a revert. Previously issued NZB ids stop working (unreleased feature).

## Open Questions

- None blocking. Exact enum member set for slot status/priority is derived from the strings currently produced (`Downloading`, `Queued`, `Completed`, `Failed`; priority values from `DownloadPriority`).
