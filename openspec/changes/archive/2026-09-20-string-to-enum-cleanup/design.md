## Context

Several closed-set string values flow through Messages, Search, Scoring, and Download without type safety:
- `Source` (`"sonarr"`, `"radarr"`, `"prowlarr"`, `"ui"`, `"test"`) — identifies who triggered a search
- `Category` / `MediaType` (`"tv"`, `"movie"`, `"show"`) — identifies series vs film, but with inconsistent naming across layers

The API layer already has a `MediaType` enum in `FunkArr.Api.Models`, but it doesn't reach into the domain. The ruleset JSON uses `"show"`/`"movie"`, the download pipeline uses `"tv"`/`"movie"`, and the API maps between them with a string switch.

## Goals / Non-Goals

**Goals:**
- Replace `string Source` with a `SearchSource` enum across all message and state types
- Replace `string Category` / `string MediaType` with a shared `MediaType` enum across all layers
- Unify the `"show"` / `"tv"` split into a single enum value (`MediaType.Show`)
- Keep Newznab `"tv"` mapping isolated in the ArrApi translation layer
- Keep ruleset JSON compatibility via `JsonStringEnumMemberName` attributes

**Non-Goals:**
- Channel (`"ARD"`, `"ZDF"`, etc.) — open set from external API, stays `string`
- Ruleset JSON schema changes — the JSON still says `"show"` / `"movie"`, we just parse to enum
- Enrichment `MatchMethod` — already an enum, no work needed

## Decisions

### Decision 1: Both enums live in FunkArr.Core

`SearchSource` and `MediaType` go in `FunkArr.Core` because Messages, Search, Download, Scoring, and API all need them. Core is the shared dependency all domain projects reference.

Alternative: Put them in `FunkArr.Messages`. Rejected because `MediaType` is also needed in `FunkArr.Api.Models` which doesn't reference Messages.

### Decision 2: MediaType uses JsonStringEnumMemberName for ruleset compatibility

```csharp
[JsonConverter(typeof(JsonStringEnumConverter<MediaType>))]
public enum MediaType
{
    [JsonStringEnumMemberName("show")]  Show,
    [JsonStringEnumMemberName("movie")] Movie,
}
```

The ruleset JSON uses `"show"` / `"movie"` — the converter handles deserialization. The Newznab `"tv"` label is mapped only in the ArrApi layer (just like it maps Newznab category ints today).

Alternative: Separate `RuleSetMediaType` and `DownloadCategory` enums. Rejected — they're the same concept (series vs film), just with different external labels.

### Decision 3: SearchSource uses JsonStringEnumMemberName for persistence compatibility

```csharp
[JsonConverter(typeof(JsonStringEnumConverter<SearchSource>))]
public enum SearchSource
{
    [JsonStringEnumMemberName("sonarr")]   Sonarr,
    [JsonStringEnumMemberName("radarr")]   Radarr,
    [JsonStringEnumMemberName("prowlarr")] Prowlarr,
    [JsonStringEnumMemberName("ui")]       Ui,
    [JsonStringEnumMemberName("test")]     Test,
}
```

This ensures existing persisted `ScoringRecorded` events with `"sonarr"` string values deserialize correctly into the new enum. No DTO versioning needed — Akka's default serializer handles the string-to-enum mapping via the converter.

### Decision 4: No persistence DTO versioning needed

Because both enums use `JsonStringEnumMemberName` that matches the existing string values, the serialized form is identical. A persisted `"sonarr"` string deserializes to `SearchSource.Sonarr`, and `"tv"` in download events... wait — download events used `"tv"` not `"show"`. So `MediaType` needs **both** labels to map correctly.

Revised approach for `MediaType` on persistence DTOs: The `DownloadInitialized` and `HistoryRecorded` events have `Category: "tv"`. Since `MediaType.Show` serializes as `"show"` (for ruleset compat), old download events with `"tv"` would fail to deserialize.

Solution: Since we're at 0.x with breaking changes OK, old persisted events can be discarded (journal reset). The enum serializes as `"show"` everywhere going forward.

### Decision 5: API layer keeps int serialization

The existing `api-enum-types` spec requires all API enums to serialize as integers. The shared `MediaType` enum has `JsonStringEnumConverter` for domain/persistence use, but the API response serializer overrides this with int serialization (ASP.NET's `JsonSerializerOptions` in the API layer).

## Risks / Trade-offs

- [Persistence break for download events] → Acceptable at 0.x. Users doing a clean install or can reset journal. The scoring events survive because they already used `"sonarr"` etc. which maps 1:1.
- [Two serialization modes for MediaType] → `JsonStringEnumConverter` on the type for domain use, API layer's `JsonSerializerOptions` overrides to int. This is standard ASP.NET pattern.
- [New SearchSource values in future] → Adding to the enum is backwards-compatible. Unknown values from old persistence would fail, but at 0.x that's fine.
