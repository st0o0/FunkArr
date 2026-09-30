## Context

FunkArr's 6 API controllers were migrated from Minimal API to MVC controllers but retained manual request handling patterns: `Request.Body` deserialization with custom `JsonSerializerOptions`, `Request.Query` reads instead of `[FromQuery]`, and `Request.ReadFormAsync()` instead of `[FromForm]`. The global `AddControllers()` call has no JSON configuration, forcing each controller to bring its own serialization. Several endpoints return anonymous objects, making the API contract invisible to OpenAPI/Scalar.

The Newznab RSS feed (empty-query tvsearch/search) was half-implemented as a direct `MediathekClient` call, bypassing the SearchActor pipeline and losing caching, content filtering, and quality probing.

## Goals / Non-Goals

**Goals:**
- All controllers use ASP.NET model binding attributes (`[FromBody]`, `[FromQuery]`, `[FromRoute]`, `[FromForm]`) exclusively
- Global `JsonSerializerOptions` configured once on `AddControllers().AddJsonOptions()`
- All API responses use typed records (no anonymous objects)
- Complete `[ProducesResponseType]` coverage for OpenAPI schema generation
- Newznab RSS feed routed through SearchActor pipeline

**Non-Goals:**
- Changing external API contracts (URL paths, response JSON shapes, XML formats)
- Splitting Newznab/SABnzbd single-endpoint routing into separate action methods (they emulate foreign protocols)
- Replacing `RuleSetJsonOptions` / `SetupValidationJsonOptions` for non-controller usage (file I/O, actor internals)
- Adding request validation beyond what model binding provides (e.g. FluentValidation)

## Decisions

### D1: Global JSON options on AddControllers

Configure `AddControllers().AddJsonOptions()` in `FunkArrServiceSetup` with:
- `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`
- `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`
- `Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }`

**No `WriteIndented`** — compact JSON is correct for API responses. `RuleSetJsonOptions.Default` (which has `WriteIndented = true`) stays for file I/O in `RuleSetFileWriter` and `RuleSetRegistryActor`.

**Why not a named options pattern?** The controllers all share the same serialization needs. Named options add complexity for no benefit here.

### D2: SetupController — [FromBody] with nullable body

`Validate()` currently handles empty bodies (`Content-Length: 0`) by constructing a default `ValidationRequest`. With `[FromBody]`, ASP.NET returns 400 for empty bodies by default.

**Decision:** Use `[FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]` and declare the parameter as nullable: `[FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ValidationRequest? request`. This preserves the current behavior where the frontend can POST with no body to run self-checks only.

`SetupValidationJsonOptions` can be removed if its options match the global config (they do: camelCase + enum converter, no WriteIndented). The `Validate` endpoint currently returns `new JsonResult(result, SetupValidationJsonOptions.Default)` — this becomes `Ok(result)`.

### D3: RulesetController — [FromBody] with custom JSON options

The RulesetController uses `RuleSetJsonOptions.Default` (camelCase + WhenWritingNull + JsonStringEnumConverter + WriteIndented) for both reading and writing RuleSetFile objects.

**Reading (Save/Test):** `[FromBody]` with global JSON options works — camelCase + enum converter matches. `WriteIndented` is irrelevant for deserialization.

**Writing (GetOne):** Currently returns `new JsonResult(response.RuleSet, RuleSetJsonOptions.Default)` with `WriteIndented`. Change to `Ok(response.RuleSet)` — API responses don't need indentation.

**Decision:** Replace manual deserialization with `[FromBody]`. Move `TestRulesRequest` to `Api/Models/TestRulesRequest.cs` as a public record.

### D4: NewznabController — [FromQuery] parameters with single dispatch

Keep the single `HandleNewznabRequest()` entry point (Newznab protocol routes everything through one URL with `?t=` parameter). But extract all query parameters via `[FromQuery]` in the action signature:

```csharp
[HttpGet("")]
public async Task<IActionResult> HandleNewznabRequest(
    [FromQuery] string? t,
    [FromQuery] string? q,
    [FromQuery] int? tvdbid,
    [FromQuery] int? season,
    [FromQuery(Name = "ep")] int? episode,
    [FromQuery] string? imdbid)
```

**Why `[FromQuery(Name = "ep")]`?** The Newznab spec uses `ep` as the parameter name, but `ep` is not a meaningful C# parameter name. Map it to `episode`.

For `HandleFakeNzbDownload`, same pattern with `[FromQuery] string? url`, `[FromQuery] string? title`, `[FromQuery] string? subtitle`.

### D5: RSS feed through SearchActor

When `t=tvsearch` or `t=search` arrives with no query/tvdbid, route through `SearchActor.TextSearchRequest("")` instead of calling MediathekClient directly.

The pipeline already handles this:
- `SearchChildHelpers.SearchMediathekAsync("")` → `isBlank=true` → empty queries array, size 100
- `MatchingPipeline.ExecuteAsync` with empty `MatchContext` → all filter predicates return true
- `ContentFilter.ShouldSkip` still removes accessibility/trailer content
- Quality probing runs normally
- Results get cached in SearchActor (55-min cache)

Remove the `HandleRssFeed` method entirely. The `ToNewznabResult` conversion already handles results without tvdbId/season/episode.

### D6: SabnzbdController — [FromQuery] mode + [FromForm] file

**GET handler:** Add `[FromQuery] string? mode` parameter. Remove manual `Request.Query["mode"]` reads.

**POST handler (addfile):** Replace `Request.ReadFormAsync()` + `form.Files.FirstOrDefault()` with `[FromForm] IFormFile? file`. This is the standard ASP.NET pattern for file uploads.

### D7: Typed SABnzbd response models

Create typed records in `Api/Models/SabnzbdResponses.cs` for all SABnzbd JSON responses:
- `SabnzbdVersionResponse(string Version)`
- `SabnzbdConfigResponse` (with nested `SabnzbdMiscConfig`, `SabnzbdCategory`)
- `SabnzbdQueueResponse` (with nested `SabnzbdQueueSlot`)
- `SabnzbdHistoryResponse` (with nested `SabnzbdHistorySlot`)
- `SabnzbdAddFileResponse(bool Status, string[]? NzoIds, string? Error)`
- `SabnzbdErrorResponse(bool Status, string Error)`

**Important:** Property names in these records must match SABnzbd's JSON format exactly (snake_case: `nzo_id`, `mbleft`, `timeleft`, `complete_dir`). Use `[JsonPropertyName("nzo_id")]` attributes since the global naming policy is camelCase.

### D8: Typed Ruleset response models

Add to `Api/Models/`:
- `DeletedResponse(bool Deleted)` — replaces `new { deleted = true }`
- `ReloadedResponse(bool Reloaded)` — replaces `new { reloaded = true }`
- `TestRulesResponse` with Matched/Filtered/Unmatched/TotalItems — replaces anonymous object in Test()

## Risks / Trade-offs

**[Risk] SABnzbd snake_case property names vs global camelCase policy**
→ Mitigation: Use `[JsonPropertyName]` on SABnzbd response records. Test with Sonarr/Radarr after change.

**[Risk] Empty body on Validate endpoint**
→ Mitigation: `EmptyBodyBehavior.Allow` preserves existing behavior. Covered by existing `SetupValidationEndpointTests`.

**[Risk] RSS feed quality differs from previous HandleRssFeed**
→ Mitigation: RSS through SearchActor is strictly better — it runs content filter, quality probing, and caching. The previous implementation was incomplete/untested anyway.

**[Risk] Newznab `[FromQuery]` parameter binding for optional ints**
→ Mitigation: ASP.NET handles `int?` correctly for missing query params (binds to null). Non-numeric values bind to null too (no 400 error), which matches current `int.TryParse` behavior.
