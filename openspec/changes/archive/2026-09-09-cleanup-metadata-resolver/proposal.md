## Why

The MetadataResolver actors bypass DI with manual `Props.Create(() => new X(client))`, thread dependencies through the manager constructor, and rely on static classes (`EpisodeResolver`, `MovieResolver`) for resolution logic. This makes the actors harder to test, inconsistent with the rest of the codebase (which uses `resolver.Props<T>()` and `Context.GetActor<T>()`), and couples the manager to implementation details of its children.

## What Changes

- Add a `ResolveChildActor<T>` overload accepting `Func<Props, Props>` to FunkArr.Core, enabling DI-resolved pooled children in one call
- Refactor `MetadataResolverManager` to create pools via the new `ResolveChildActor` overload instead of manual `Props.Create`
- Convert `EpisodeResolver` and `MovieResolver` from static classes to instance classes registered in DI
- Inject resolvers + clients directly into `TvdbResolverActor` and `TmdbResolverActor` via DI
- Remove `TvdbClient`/`TmdbClient` constructor parameters from the manager
- Manager always forwards to pool (no cache-hit resolve shortcut) - cache stores resolved results
- `LevenshteinDistance` stays static (pure math utility, no state)

## Capabilities

### New Capabilities

- `resolve-child-actor-configure`: `ResolveChildActor<T>` overload with `Func<Props, Props>` for DI-resolved actors with router/dispatcher/mailbox configuration

### Modified Capabilities

None - all behavioral specs (episode-resolution, movie-resolution, metadata-cache, tmdb-client, tvdb-client) remain unchanged. This is a pure internal restructuring.

## Impact

- `FunkArr.Core`: new extension method file
- `FunkArr.MetadataResolver`: all actor files refactored, `EpisodeResolver` and `MovieResolver` become non-static
- `FunkArr/Configuration/ServiceSetupContainer.cs`: register `EpisodeResolver` and `MovieResolver` in DI
- `FunkArr.MetadataResolver.Tests`: tests may need adjustment for instance-based resolvers
- No API changes, no persistence changes, no message changes
