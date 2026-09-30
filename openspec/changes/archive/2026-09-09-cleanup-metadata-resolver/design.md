## Context

The MetadataResolver project is the only domain project that bypasses Servus.Akka's DI resolution. While every other actor in the codebase is created via `resolver.Props<T>()` at the hosting level, the `MetadataResolverManager` manually creates its child pools with `Props.Create(() => new X(client))`, threading `TvdbClient` and `TmdbClient` through its own constructor. The resolution logic lives in static classes (`EpisodeResolver`, `MovieResolver`) that can't be independently tested or swapped.

Current actor hierarchy:
```
MetadataResolverManager(TvdbClient, TmdbClient)  <- manual DI threading
  ├── tvdb-pool [SmallestMailboxPool(2)]
  │     └── TvdbResolverActor(tvdbClient)         <- Props.Create, no DI
  └── tmdb-pool [SmallestMailboxPool(2)]
        └── TmdbResolverActor(tmdbClient)          <- Props.Create, no DI
```

## Goals / Non-Goals

**Goals:**
- Align MetadataResolver with the project's DI patterns (`ResolveChildActor`, `DependencyResolver.For`)
- Enable pooled DI-resolved children via a `Func<Props, Props>` overload on `ResolveChildActor`
- Make `EpisodeResolver` and `MovieResolver` testable instance classes
- Simplify the manager to a pure dispatcher + cache owner

**Non-Goals:**
- Changing resolution behavior (matching logic, thresholds, strategies)
- Modifying the cache TTL strategy or eviction
- Moving the extension to Servus.Akka upstream (stays in FunkArr.Core for now)
- Changing message contracts or persistence

## Decisions

### 1. `ResolveChildActor<T>` with `Func<Props, Props>` configure parameter

Add an overload to `ResolveChildActor` that accepts a `Func<Props, Props>` between DI prop resolution and `ActorOf`. This lets callers attach routers, dispatchers, or mailbox config while keeping DI resolution.

```csharp
Context.ResolveChildActor<TvdbResolverActor>("tvdb-pool",
    props => props.WithRouter(new SmallestMailboxPool(2)));
```

**Why not a dedicated `ResolveChildPool` method?** The `Func<Props, Props>` approach is more general - it handles routers, dispatchers, and any future Props configuration. One extension covers all cases.

**Why in FunkArr.Core, not Servus.Akka?** The pattern needs validation in a real codebase before upstreaming. FunkArr.Core already bundles Servus.Akka refs for all domain projects.

The extension mirrors the existing Servus.Akka `ResolveExtensions.cs` private `Resolve` helper pattern, adding the configure step between `resolver.Props<TActor>(args)` and `factory.ActorOf(props, name)`.

### 2. Instance-based resolvers via DI

`EpisodeResolver` and `MovieResolver` become sealed classes registered as singletons in DI. They are injected into the resolver actors via constructor.

**Why singletons?** They hold no state - pure functions over their inputs. Singleton avoids unnecessary allocations while making them injectable and testable.

**Why not keep static?** Instance classes integrate with DI naturally, can be mocked in unit tests, and follow the project's general pattern. Static utility (`LevenshteinDistance`) is reserved for truly zero-dependency math.

### 3. Manager always forwards to pool

The manager no longer resolves on cache hit. Instead it caches resolved results (`ResolvedEpisode[]`, `MovieResolved[]`) and replies directly from that cache. Cache miss forwards to pool.

**Why change the cache?** Currently the manager caches raw API data (`TvdbEpisode[]`, `TmdbMovie`) and re-runs resolution logic on each cache hit. By caching resolved results, the manager doesn't need resolve logic at all - it's a pure dispatcher + cache.

Cache key remains `(Provider, Id)`. Cache entry changes from `object Data` to a discriminated approach with typed fields.

### 4. Typed cache entries

Replace `CacheEntry(object Data, ...)` with typed variants to eliminate the casts and make the cache self-documenting:

```csharp
sealed record EpisodeCacheEntry(TvdbEpisode[] Episodes, DateTimeOffset FetchedAt, TimeSpan Ttl);
sealed record MovieCacheEntry(MovieResolved[] Results, DateTimeOffset FetchedAt, TimeSpan Ttl);
```

The manager keeps two dictionaries instead of one `Dictionary<(string, int), CacheEntry>`.

## Risks / Trade-offs

- **Cache now stores resolved results** - If a resolve request comes with different candidates for the same TVDB/TMDB ID, the cached result won't match. Mitigation: this matches current behavior - the existing cache also doesn't account for different candidate sets. The cache is a performance optimization for repeated identical queries from the search workers.
- **`Context.Parent.Tell` stays for cache updates** - Pool children still need to report results back to the manager for caching. This coupling is inherent to the pool pattern where the parent owns shared state. Mitigation: the `CacheUpdate` internal message is explicit and typed.
- **FunkArr.Core extension may diverge from Servus.Akka** - If Servus.Akka adds its own `Func<Props, Props>` overload later, we'll have a conflict. Mitigation: the extension is small (one method), easy to delete when upstreaming.
