## 1. ResolveChildActor Extension

- [x] 1.1 Add `ResolveChildActorExtensions.cs` in `FunkArr.Core` with `ResolveChildActor<TActor>(this IActorContext, string, Func<Props, Props>, params object[])` overload using `DependencyResolver.For(context.System)`

## 2. Instance Resolvers

- [x] 2.1 Convert `EpisodeResolver` from static to sealed instance class (remove `static` keyword, make methods instance methods)
- [x] 2.2 Convert `MovieResolver` from static to sealed instance class
- [x] 2.3 Register `EpisodeResolver` and `MovieResolver` as singletons in `ServiceSetupContainer`

## 3. Resolver Actor Refactor

- [x] 3.1 Refactor `TvdbResolverActor` - inject `TvdbClient` and `EpisodeResolver` via constructor (DI), remove manual client parameter
- [x] 3.2 Refactor `TmdbResolverActor` - inject `TmdbClient` and `MovieResolver` via constructor (DI), remove manual client parameter

## 4. Manager Refactor

- [x] 4.1 Refactor `MetadataResolverManager` - remove `TvdbClient`/`TmdbClient` constructor params, create pools via `Context.ResolveChildActor<T>("name", props => props.WithRouter(...))`
- [x] 4.2 Change cache to store resolved results (`ResolvedEpisode[]`, `MovieResolved[]`) instead of raw API data, update `CacheEntry` and `CacheUpdate` types accordingly
- [x] 4.3 Update cache-hit path to reply directly with cached resolved results (no resolve logic in manager)

## 5. Cleanup

- [x] 5.1 Remove unused `EpisodeResolver`/`MovieResolver` static method calls from manager
- [x] 5.2 Run `dotnet format` and fix any style violations
- [x] 5.3 Build and run all MetadataResolver tests to verify no regressions
