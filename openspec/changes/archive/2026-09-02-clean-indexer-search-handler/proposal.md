## Why

`IndexerApiEndpoints` threads a `bool isMovieSearch` flag through four methods, scatters Newznab category IDs as magic numbers (`"2040"`, `"5030"`, …), builds category display strings at multiple sites, and passes `IActorRef` as a parameter to every free function. The same category constants are duplicated between `Caps.cs` and the endpoint logic with no connection between them. This makes the code fragile and hard to follow — a single Newznab concept (media category) has no representation in the type system.

## What Changes

- Introduce a `NewznabCategory` value type that owns all category IDs, labels, and display formatting — single source of truth replacing scattered magic numbers and string interpolation.
- Add an `ISearchCommand` marker interface to the existing search command records, replacing `object cmd` in the gateway ask pattern.
- Extract a `SearchHandler` class that holds the `IActorRef` gateway as a field (set once via constructor), eliminating per-call `IActorRef` parameter passing and collapsing three near-identical `Handle*` methods into one dispatch flow with clear separation: validate → build command → ask gateway (domain result) → format HTTP response.
- Slim `IndexerApiEndpoints` down to pure routing registration.
- Derive `Caps.cs` category entries from `NewznabCategory` instead of duplicating IDs.

## Capabilities

### New Capabilities

_None — this is a structural refactoring with no new features._

### Modified Capabilities

_None — the external Newznab API contract (XML format, category IDs, HTTP status codes) is unchanged._

## Impact

- **FunkArr.ArrApi** — `IndexerApiEndpoints.cs` shrinks to routing; new `SearchHandler.cs` and `NewznabCategory.cs` in `Newznab/` and `Newznab/Models/`. `Caps.cs` updated to derive from `NewznabCategory`.
- **FunkArr.Messages** — `ISearchCommand` marker interface added; `TvSearchCommand`, `MovieSearchCommand`, `GeneralSearchCommand` implement it.
- **FunkArr.ArrApi.Tests** — `SearchResultMappingTests` adapted to new API surface (`NewznabCategory` instead of `bool`, `SearchHandler` methods instead of static endpoints).
- **No external behavior change** — wire format, status codes, and API paths remain identical.
