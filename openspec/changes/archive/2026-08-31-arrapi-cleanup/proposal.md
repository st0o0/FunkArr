## Why

The ArrApi adapter layer is over-structured for what it does. Static helper classes (XmlHelper, NzbGenerator, NzbParser), nested method dispatch chains, and an unused JSON output mode add indirection without value. The SABnzbd POST handler manually extracts IFormFile from HttpContext when ASP.NET Minimal APIs can bind it directly.

## What Changes

- **Drop Newznab JSON output** — remove `o=json` support. Prowlarr/Sonarr/Radarr always use XML. Delete `CapsJsonProjection`, `RssJsonProjection`, and the `O` query parameter.
- **Eliminate helper classes** — inline XmlHelper, NzbGenerator, NzbParser into endpoint files. XML serialization becomes a private method; NZB generation/parsing and base64 decode become inline expressions.
- **Flatten endpoint handlers** — replace `HandleRequest` → `CapsResult` → `SearchResult` chains with switch expressions directly in lambda bodies. No more nested static method dispatch.
- **IFormFile binding** — bind `IFormFile? nzbfile` as a direct endpoint parameter instead of extracting from `HttpContext.Request.Form.Files`. Add `.DisableAntiforgery()` since SABnzbd clients don't send antiforgery tokens. Remove HttpContext dependency.

## Capabilities

### New Capabilities

_None — this is a structural cleanup, not a feature change._

### Modified Capabilities

_None — no spec-level behavior changes. The external API contracts (Newznab XML, SABnzbd JSON) remain identical. Only `o=json` is removed, which no arr client uses._

## Impact

- **FunkArr.ArrApi** — 5 files deleted, 4 files modified. Public API surface unchanged (XML responses, NZB file download, SABnzbd endpoints all behave identically).
- **FunkArr.ArrApi.Tests** — `JsonOutputTests.cs` deleted. NzbGenerator/NzbParser tests adapted to test behavior through endpoints or inlined logic.
- **No downstream impact** — adapter layer is a leaf; no other projects reference these internal helpers.
