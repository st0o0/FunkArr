## Context

The RuleSet CRUD endpoints currently accept opaque JSON via `[JsonExtensionData]`, manually extract fields, serialize back to JSON for schema validation, and write to disk. The TestScore endpoint has its own typed models (`TestRule`, `TestFilterSpec`, etc.) that mirror the same RuleSet structure. These can be unified into shared input records.

## Goals / Non-Goals

**Goals:**
- Typed request models for Create/Update RuleSet with full OpenAPI schema visibility
- Shared input records between CRUD and TestScore endpoints
- Use Messages enums directly in input models (automatic deserialization via JsonStringEnumConverter)
- Clean JSON output for disk files (no nulls, matching existing schema)

**Non-Goals:**
- Changing the JSON schema itself
- Changing how RuleSetMerger reads from disk
- Frontend changes (existing RuleSetWriteRequest matches the target structure)

## Decisions

### 1. Shared input model hierarchy

```
RuleSetBody (used by Create, Update, and as base for TestScore)
├── topic: string (required)
├── aliases: string[]?
├── media: MediaInput (required)
├── confidence: float?
├── rules: RuleInput[] (required)
├── standalone: bool?
├── disable: string[]?
│
MediaInput
├── name: string (required)
├── type: string (required, "show"/"movie")
├── tvdbId: int?
├── imdbId: string?
├── tmdbId: int?
│
RuleInput
├── id: string (required)
├── priority: int
├── confidence: float?
├── strategy: IdentificationStrategy (enum, from Messages)
├── seasonRegex: string?
├── episodeRegex: string?
├── captureGroup: int?
├── filters: FilterGroupInput?
├── titleRules: TitleRuleInput[]?
│
FilterGroupInput
├── all: FilterNodeInput[]?
├── any: FilterNodeInput[]?
├── not: FilterNodeInput[]?
│
FilterNodeInput (union: condition or nested group)
├── field: FilterField? (enum)
├── op: FilterOp? (enum)
├── value: string?
├── all: FilterNodeInput[]? (nested group)
├── any: FilterNodeInput[]?
├── not: FilterNodeInput[]?
│
TitleRuleInput
├── type: TitlePartType (enum)
├── field: FilterField?
├── pattern: string?
├── captureGroup: int?
├── value: string?
```

### 2. Request model composition

`CreateRuleSetRequest` flattens `ruleSetId` alongside the body:
```csharp
public sealed record CreateRuleSetRequest(
    string RuleSetId,
    string Topic,
    MediaInput Media,
    RuleInput[] Rules,
    string[]? Aliases = null,
    float? Confidence = null,
    bool? Standalone = null,
    string[]? Disable = null);
```

`UpdateRuleSetRequest` is identical but without `RuleSetId` (comes from route).

Both share `RuleInput`, `MediaInput`, etc. No separate `RuleSetBody` wrapper needed — the fields are flattened into the request records.

### 3. TestScoreRequest reuse

```csharp
public sealed record TestScoreRequest(
    float DefaultConfidence,
    RuleInput[] Rules,
    TestCandidate[] Candidates);
```

`TestCandidate` stays as-is (it has no overlap with RuleSet input). `TestRuleConfig` wrapper is eliminated.

### 4. Serialization for disk write

The endpoint serializes the typed request to JSON for disk storage:
```csharp
var json = JsonSerializer.Serialize(new {
    request.Topic, request.Aliases, request.Media,
    request.Confidence, request.Rules,
    request.Standalone, request.Disable
}, diskJsonOptions);
```

Using `JsonSerializerOptions` with:
- `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`
- `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`
- `WriteIndented = true`

This produces clean JSON matching the existing schema format.

### 5. media.type stays as string in MediaInput

`MediaInput.Type` stays `string` (not an enum) because:
- It's written to disk as JSON and must match the schema (`"show"` / `"movie"`)
- The schema may add more types later
- It's a simple two-value field, not worth an enum round-trip

## Risks / Trade-offs

**[Serialization fidelity]** → The typed model must serialize back to JSON that passes schema validation. `WhenWritingNull` and camelCase policy handle this. The enums serialize as strings (via `JsonStringEnumConverter` on the Messages types) which matches the JSON schema expectations.

**[Required field validation]** → ASP.NET model binding returns 400 automatically for missing required fields. This replaces the manual `TryGetValue("topic", ...)` check. Schema validation remains as a second layer.
