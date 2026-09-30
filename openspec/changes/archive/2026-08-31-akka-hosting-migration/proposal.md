## Why

The actor system setup in `AkkaSetupContainer` uses Akka.Hosting's `WithActors` lambda but manually wires everything inside — `system.ActorOf`, `ClusterSharding.Get(system).Start(...)`, `registry.Register<T>()`. This is pre-Hosting style wrapped in a Hosting shell. Manual `IActorRef` constructor injection between actors creates tight coupling and ordering constraints. Akka.Hosting provides `WithSingleton<T>()` and `WithShardRegion<T>()` that handle lifecycle, registry, and cluster topology automatically.

## What Changes

- Replace manual `ClusterSharding.Get(system).Start(...)` calls with `WithShardRegion<T>()` for all `*Worker` actors
- Replace manual `system.ActorOf()` + `registry.Register<T>()` with `WithSingleton<T>()` for all `*Manager` actors
- Add explicit `WithClustering()` call (currently relying on implicit single-node cluster)
- Remove `IActorRef` constructor parameters from actors — use Servus.Akka's `Context.GetActor<T>()` at runtime instead
- Use `resolver.Props<T>()` for actors with DI dependencies (e.g., `IHttpClientFactory`)
- Add `Akka.Cluster.Hosting` NuGet package

## Capabilities

### New Capabilities

_None — this is a refactor of existing infrastructure._

### Modified Capabilities

- `application-bootstrap`: Actor system configuration requirement changes from manual `WithActors` wiring to idiomatic `WithSingleton<T>()` / `WithShardRegion<T>()` / `WithClustering()` setup

## Impact

- `AkkaSetupContainer.cs` — full rewrite of `BuildSystem` method
- `SearchGatewayManager.cs` — constructor signature change (remove `IActorRef` params)
- `TvSearchWorker.cs`, `MovieSearchWorker.cs` — constructor signature changes
- `MediathekViewWebManager.cs`, `MatchMagicManager.cs` — constructor changes if they take actor refs
- `Directory.Packages.props` — add `Akka.Cluster.Hosting` version entry
- `FunkArr.Core.csproj` — add `Akka.Cluster.Hosting` package reference
- Test projects — update actor instantiation in tests to match new constructors
