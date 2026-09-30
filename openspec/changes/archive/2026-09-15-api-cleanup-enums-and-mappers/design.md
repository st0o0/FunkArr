## Context

The API layer (FunkArr.Api) serves the Vue.js frontend via JSON. Currently enum values are serialized as hardcoded strings in mapper methods, mapper logic lives as private/internal static methods inside endpoint classes, and there's duplicated string→enum switch logic between RuleSetMerger and TestRuleMapper. The `IdentificationStrategy` enum has only 3 members but represents 5 conceptual strategies, requiring a secondary `TitleMatchMode` enum and complex switch blocks.

## Goals / Non-Goals

**Goals:**
- All API enum fields use proper C# enums serialized as integers
- Single mapping path per direction (Message→API via ToApi(), API→Message via ToMessage())
- IdentificationStrategy is a true 1:1 mapping to JSON strategy strings
- Endpoint classes contain only route definitions

**Non-Goals:**
- Typed request models for Create/UpdateRuleSet (JsonExtensionData is intentional for schema validation)
- Frontend migration to int-based enums (separate follow-up)
- Persistence DTO changes (extend-only policy)

## Decisions

### 1. API enums are eigenständig and int-serialized

API models define their own enum types in `FunkArr.Api.Models`. No shared enums between Messages and Api — the boundary is explicit. System.Text.Json defaults to int serialization for enums, so no converter needed on API enums.

Mapping between layers uses explicit `ToApi()` extension methods. This replaces the current fragile `(ApiModels.RuleOutcome)msg.Outcome` integer cast.

### 2. Messages enums get JsonStringEnumConverter for JSON file deserialization

The Messages-layer enums (`FilterField`, `FilterOp`, `TitlePartType`, `IdentificationStrategy`) are used in JSON ruleset files. Adding `[JsonConverter(typeof(JsonStringEnumConverter<T>))]` with `JsonNamingPolicy.CamelCase` makes `JsonSerializer.Deserialize` handle the string→enum mapping automatically.

For `IdentificationStrategy`, the JSON strings don't match camelCase of the enum members, so each member needs `[JsonPropertyName("...")]`:
```csharp
[JsonConverter(typeof(JsonStringEnumConverter<IdentificationStrategy>))]
public enum IdentificationStrategy
{
    [JsonPropertyName("seasonAndEpisodeNumber")] SeasonAndEpisodeNumber,
    [JsonPropertyName("byAbsoluteEpisodeNumber")] AbsoluteEpisodeNumber,
    [JsonPropertyName("itemTitleExact")] TitleExact,
    [JsonPropertyName("itemTitleIncludes")] TitleIncludes,
    [JsonPropertyName("itemTitleEqualsAirdate")] AirdateExtraction,
}
```

### 3. IdentificationStrategy expanded to 5 members (Option B)

The current 3-member enum with a secondary `TitleMatchMode` is replaced by a 5-member enum. `TitleMatchMode` is deleted. `IdentificationSpec.MatchMode` parameter is removed.

The MatchMagic evaluation switches on the 5-member enum directly:
```csharp
strategy switch
{
    SeasonAndEpisodeNumber => EvaluateRegexCapture(spec, candidate, hasSeason: true),
    AbsoluteEpisodeNumber => EvaluateRegexCapture(spec, candidate, hasSeason: false),
    TitleExact => EvaluateTitleMatch(spec, candidate, exact: true),
    TitleIncludes => EvaluateTitleMatch(spec, candidate, exact: false),
    AirdateExtraction => EvaluateAirdate(spec, candidate),
}
```

### 4. Extension method organization

```
src/FunkArr.Api/Extensions/
  ScoringMappingExtensions.cs     # ItemTrace.ToApi(), RuleTrace.ToApi(), etc.
  DownloadMappingExtensions.cs    # QueueResult.ToApi(), HistoryResult.ToApi(), etc.
  MediathekMappingExtensions.cs   # MediathekItem.ToApi()
  TestScoreMappingExtensions.cs   # TestScoreRequest.ToMessage(), TestRule.ToMessage()
  RuleSetMappingExtensions.cs     # RuleSetDetailResult.ToApi(), ScoringHistoryResult.ToApi()
```

Each file contains static extension methods. The `this` parameter is always the source type. Method name is always `ToApi()` or `ToMessage()`.

### 5. Shared GatewayTimeout helper

Extract `GatewayTimeout()` into a shared static method (e.g., `ApiResults.GatewayTimeout()`) to replace the 3 duplicate private methods.

### 6. Registry access pattern

Unify to `await registry.GetAsync<T>()` everywhere. The sync `registry.Get<T>()` in DownloadsApiEndpoints works but is inconsistent with the async pattern used elsewhere.

## Risks / Trade-offs

**[Breaking API change]** → Enum fields change from string to int. Frontend needs a follow-up change. Mitigated by 0.x version — no external consumers.

**[MatchMagic evaluation change]** → The strategy switch changes shape but the actual evaluation logic is identical. Unit tests validate correctness.

**[Persistence compatibility]** → `IdentificationSpec` stored in Akka persistence snapshots may reference `TitleMatchMode`. Since persistence DTOs are extend-only and separate from domain types, this only matters if snapshots are deserialized. At 0.x, a clean restart is acceptable.
