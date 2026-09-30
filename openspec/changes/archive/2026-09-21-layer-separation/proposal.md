## Why

FunkArr.Persistence directly references FunkArr.Messages and stores Messages types (`ItemTrace[]`, `SearchSource`, `MediaType`) in the Akka journal. If any Messages type changes shape, persisted journal entries become undeserializable — data loss. Similarly, FunkArr.Api.Models uses Messages enums directly as field types instead of owning its copies. The three layers (Api, Messages, Persistence) must be fully independent: each layer owns its types, connected only by explicit `ToXxx()` mapping methods.

## What Changes

- **Persistence owns its types**: Create `Persisted*` copies of all Messages types stored in journal events (`ItemTrace` tree, enums `MediaType`/`SearchSource`). Remove `FunkArr.Persistence` → `FunkArr.Messages` project reference. Add `ToPersistence()`/`FromPersistence()` mapping methods.
- **Api.Models owns its enums**: Create Api-layer copies of 9 enums currently referenced from Messages (`MediaType`, `SearchSource`, `FilterOp`, `FilterField`, `IdentificationStrategy`, `TitlePartType`, `EnrichmentMethod`, `RuntimeMode`, `MatchMethod`). Add `ToApi()`/`FromApi()` mappings. Move `[JsonStringEnumConverter]` attributes to Api copies only.
- **Api.Models owns its complex types**: Create Api copies of `FilterGroupOutput` and `TitleRuleOutput` which are currently used as Messages types directly in Api.Models fields.
- **Messages enums become plain**: Remove System.Text.Json serialization attributes from Messages enums (those are API concerns).
- **State uses own types**: Domain state records that currently hold Messages types directly (`ScoringManagerState` with `MatchingConfig`, `StatsCollectorState` with `AllStatsResult`, `RuleSetResolverState` with `RegisterRuleSet`) get own internal types with `ToMessage()`/`FromMessage()` mapping.

## Capabilities

### New Capabilities

- `persistence-type-isolation`: Persistence project owns all its types independently — no reference to Messages. Persisted types have own `Persisted*` copies of shared enums and complex records with explicit mapping.
- `api-type-isolation`: Api.Models owns all its types independently — no direct use of Messages enums or complex records as field types. All connected via `ToApi()`/`FromApi()` mapping methods.
- `state-type-isolation`: Domain actor state records own their internal types — no direct storage of Messages types. Connected via `ToMessage()`/`FromMessage()` mapping.

### Modified Capabilities

_(none — no existing specs to modify)_

## Impact

- **FunkArr.Persistence**: New `Persisted*` types for `ItemTrace` tree (6 records), `PersistedMediaType` enum, `PersistedSearchSource` enum. New mapping extensions. Remove ProjectReference to FunkArr.Messages.
- **FunkArr.Api**: New enum copies (9 enums), new complex type copies (`FilterGroupOutput`, `TitleRuleOutput`). Update all mapping extensions. `[JsonStringEnumConverter]` attributes move here.
- **FunkArr.Messages**: Remove `[JsonStringEnumConverter]`/`[JsonStringEnumMemberName]` from enums. No structural changes.
- **FunkArr.Scoring**: `ScoringManagerState` gets internal config type.
- **FunkArr.History**: `StatsCollectorState` gets internal stats type.
- **FunkArr.RuleSet**: `RuleSetResolverState` gets internal registration type.
- **FunkArr.Download**: Persistence events already use primitives for most fields — only `MediaType` enum needs Persistence copy.
- **All test projects**: Update references to moved/renamed types.
