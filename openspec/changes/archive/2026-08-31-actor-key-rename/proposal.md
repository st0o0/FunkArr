## Why

Actor keys (marker interfaces in `ActorKeys.cs`) use inconsistent suffixes — `Gateway`, `Service`, `Region` — with no pattern tying suffix to registration type. `IMatchHistoryService` is a shard, `IMatchMagicService` is a singleton, and `ISearchGateway` is also a singleton. This makes it impossible to know what you're resolving without checking `AkkaSetupContainer`.

## What Changes

- Rename 5 actor key interfaces to follow a consistent convention: `*Manager` for singletons, `*Region` for shard regions, descriptive name (e.g. `*Resolver`) for other singletons.
- Rename `SearchGatewayManager` actor class to `SearchManager` (drop redundant "Gateway").
- Update the actor name string from `"search-gateway-manager"` to `"search-manager"`. **BREAKING** (persistence ID changes — requires clean state or migration).
- Update all usages across production code and tests.

| Current | New | Type |
|---|---|---|
| `IMediathekGateway` | `IMediathekManager` | Singleton |
| `IRuleSetService` | `IRuleSetManager` | Singleton |
| `IMatchMagicService` | `IMatchMagicManager` | Singleton |
| `ISearchGateway` | `ISearchManager` | Singleton |
| `IMatchHistoryService` | `IMatchHistoryRegion` | ShardRegion |

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None — this is a pure rename refactor with no behavioral changes.

## Impact

- `FunkArr.Core/ActorKeys.cs` — 5 interface renames
- `FunkArr/Configuration/AkkaSetupContainer.cs` — key type + actor name string updates
- `FunkArr.Search/SearchGatewayManager.cs` — file + class rename to `SearchManager`
- `FunkArr.Search/*.cs`, `FunkArr.RuleSet/*.cs` — `Context.GetActor<T>()` call updates
- `FunkArr.Search.Tests/*.cs` — `registry.Register<T>()` call updates
- **Persistence**: `SearchGatewayManager` actor name change means existing journal entries won't match. Acceptable at 0.x.
