## Context

The ArrApi project contains two endpoint files (`IndexerApiEndpoints.cs`, `DownloadApiEndpoints.cs`) plus five helper/projection classes that add indirection. The Newznab side has JSON projection models that no arr client uses. The SABnzbd POST handler manually extracts `IFormFile` from `HttpContext` instead of using Minimal API binding.

Current file inventory (excluding models):
- `XmlHelper.cs` — `Serialize<T>()` (5 lines) + `EmptyRss()` factory
- `NzbGenerator.cs` — `Generate()` + `TryDecodeBase64()`
- `NzbParser.cs` — `Parse()` returning `(Title, Url)` tuple
- `CapsJsonProjection.cs` — 60-line JSON mapping for caps
- `RssJsonProjection.cs` — 47-line JSON mapping for RSS

## Goals / Non-Goals

**Goals:**
- Eliminate all helper classes — inline into endpoint files
- Remove unused JSON output path from Newznab
- Use ASP.NET Minimal API IFormFile binding instead of manual HttpContext extraction
- Flatten nested static method chains to lambda-style switch expressions
- Keep XML model classes (`Nzb.cs`, `Rss.cs`, `Caps.cs`, `NewznabError.cs`) unchanged

**Non-Goals:**
- Changing external API contracts (Newznab XML, SABnzbd JSON)
- Replacing `XmlSerializer` with string templates or `XDocument`
- Refactoring XML model classes
- Adding new endpoints or capabilities

## Decisions

### 1. XML serialization stays as a private static method

`XmlHelper.Serialize<T>()` is used by both endpoint files (indexer serializes Caps/Rss/Nzb, download deserializes Nzb). Rather than duplicating it, keep one `Serialize<T>` as a private static method in `IndexerApiEndpoints.cs` and one `Deserialize<T>` in `DownloadApiEndpoints.cs`. Both are under 10 lines — no separate file needed.

Alternative considered: shared static class. Rejected because it's the same indirection we're removing, and the two methods have different shapes (serialize vs. deserialize).

### 2. NZB parsing inlined into download endpoint

`NzbParser.Parse()` deserializes XML and reads two meta values. That's 15 lines of straightforward code — inline in the POST handler lambda. The `XmlSerializer` instance becomes a static field in `DownloadApiEndpoints`.

### 3. IFormFile as direct endpoint parameter, not in the record

The `DownloadPostRequest` record uses `[FromQuery]` for `Mode`, `Cat`, `Priority`. Adding `IFormFile` into the same `[AsParameters]` record works per the docs (form-mapping layer is shared), but keeping it as a separate parameter is cleaner — query params and form file are different binding sources:

```csharp
group.MapPost("/", async ([AsParameters] DownloadPostRequest req, IFormFile? nzbfile) => ...)
    .DisableAntiforgery();
```

This removes the `HttpContext` parameter entirely.

### 4. Flat lambda with switch expression

Each endpoint's `MapGet`/`MapPost` gets a lambda body with a switch expression on the action parameter (`t` for Newznab, `mode` for SABnzbd). No intermediate `Handle*` methods except where multi-step validation logic would make a switch arm unreadable (NZB ID decoding in the `get` handler).

### 5. DisableAntiforgery on SABnzbd POST

Since .NET 8, `IFormFile` endpoints require antiforgery tokens. SABnzbd clients (Sonarr/Radarr) don't send them. `.DisableAntiforgery()` is required for the POST endpoint. This is safe because the endpoint is API-key-authenticated.

## Risks / Trade-offs

- **Larger lambda bodies** — flattening means longer lambdas (~30-40 lines for SABnzbd GET). Acceptable for adapter code that's a direct translation table.
- **XmlSerializer static fields** — one per endpoint file. Thread-safe for reads (XmlSerializer is documented as thread-safe after construction). No risk.
- **DisableAntiforgery** — explicitly trading antiforgery for API-key auth, which is the correct model for machine-to-machine API calls.
