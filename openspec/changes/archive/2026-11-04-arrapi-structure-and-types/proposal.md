## Why

`FunkArr.ArrApi` has files in the wrong place (`Nzb.cs` and `SabnzbdConstants.cs` in the project root), SABnzbd responses that are partly anonymous objects, and an NZB id that is a tab-separated Base64 string encoded in `NewznabSearchService.ToItem` and decoded by position in `NzbService.GetNzb`. The feature is not released yet, so this is the moment to fix structure and types without compatibility concerns, and to leave a clean seam for later work (for example a persistent entity keyed by a plain GUID instead of a self-contained id).

## What Changes

- Move `Nzb.cs` to `Newznab/Models/` and `SabnzbdConstants.cs` to `Sabnzbd/`. Add `Requests/` and `Services/` folders per protocol next to the existing `Models/`; namespaces follow folders. Request records are moved only, their content is not changed.
- Replace anonymous SABnzbd responses (`{status:true}`, `nzo_ids`, `get_config`, API-key error) with named records. `SabnzbdResult.Ok` becomes generic (`Ok<T>`).
- Make slot status and priority in the queue and history responses ArrApi-owned enums (`Unknown = 0`, `[JsonStringEnumMemberName]`), mapped from Messages enums via exhaustive `switch`. JSON output stays byte-identical.
- Introduce `NzbPayload` (title, url, subtitle url, channel, duration, size, category) with `Encode()`/`Decode()` as the only code that knows the NZB id format (JSON in Base64). Remove `NzbParseResult` and the `Meta("url")` fallback. The id format changes; acceptable because the feature is unreleased.

Not part of this change: splitting request records, action selection, exception filter, typed query binding, shared RSS item factory, replacing the NZB payload by a persistent entity.

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `arr-api-structure`: folder/namespace layout, `Nzb` and `SabnzbdConstants` locations.
- `arrapi-service-results`: SABnzbd results use named records, generic `Ok<T>`, and enums for slot status/priority.
- `nzb-object-model`: `Nzb` location; the NZB id is defined by `NzbPayload`.

## Impact

- **Code**: `FunkArr.ArrApi` (file moves, SABnzbd services/controller/mapper/models, `NewznabSearchService`, `NzbService`, API-key filter), `FunkArr.ArrApi.Tests` (namespaces, NZB tests, new JSON snapshot tests).
- **APIs**: SABnzbd JSON unchanged. The `t=get` id and the `guid` of search results change format.
- **Dependencies**: none.
- **Docs**: none; `AGENTS.md` enum rules already cover the new enums.
