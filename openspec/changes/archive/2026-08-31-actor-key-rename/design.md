## Context

Actor keys are marker interfaces in `FunkArr.Core/ActorKeys.cs` used with Servus `WithSingleton<T>`/`WithShardRegion<T>` registration and `Context.GetActor<T>()` resolution. Current naming uses arbitrary suffixes (`Gateway`, `Service`, `Region`) with no correlation to registration type.

## Goals / Non-Goals

**Goals:**
- Consistent suffix convention: `*Manager` for singletons, `*Region` for shard regions, descriptive name for special singletons (e.g. `*Resolver`).
- Remove redundant "Gateway" from `SearchGatewayManager` actor class.

**Non-Goals:**
- Changing actor behavior, message contracts, or persistence DTOs.
- Renaming actors that already follow the convention (`IRuleSetResolver`, `IRuleSetRegion`, `ITvSearchRegion`, `IMovieSearchRegion`).

## Decisions

**Suffix convention maps to registration type.**
Singletons get `*Manager` (matching the actor class naming convention). Shard regions get `*Region`. Special-purpose singletons like `RuleSetResolver` keep their descriptive name. This makes the registration type readable at every call site without checking `AkkaSetupContainer`.

**Rename `SearchGatewayManager` class to `SearchManager`.**
"GatewayManager" is redundant — the actor is a singleton manager that dispatches to search shard regions. The simpler name aligns with the convention. The actor name string changes from `"search-gateway-manager"` to `"search-manager"`, which changes the persistence ID. Acceptable at 0.x (no migration needed per project policy).

**Single commit, no phased rollout.**
All renames are mechanical find-and-replace across a small number of files. No risk of partial state.

## Risks / Trade-offs

- **Persistence ID change** (`search-gateway-manager` → `search-manager`) — existing journal entries for `SearchGatewayManager` won't recover. Acceptable at 0.x with clean breaks policy.
- No other risks — purely mechanical rename.
