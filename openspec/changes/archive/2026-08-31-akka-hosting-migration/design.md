## Context

The actor system setup in `AkkaSetupContainer.BuildSystem` currently uses Akka.Hosting's `WithActors` lambda but manually wires everything: `system.ActorOf`, `ClusterSharding.Get(system).Start(...)`, `registry.Register<T>()`. Actors receive dependencies as `IActorRef` constructor parameters, requiring strict ordering and manual ref passing. Akka.Hosting provides `WithSingleton<T>()` and `WithShardRegion<T>()` that handle lifecycle, IActorRegistry registration, and cluster topology automatically.

Current actors:
- `MediathekViewWebManager(IHttpClientFactory)` — singleton, has DI dependency
- `MatchMagicManager()` — singleton, no DI dependencies
- `SearchGatewayManager(IActorRef tv, IActorRef movie)` — singleton, depends on shard regions
- `TvSearchWorker(IActorRef mediathek, IActorRef matchMagic)` — sharded entity, depends on singletons
- `MovieSearchWorker(IActorRef mediathek, IActorRef matchMagic)` — sharded entity, depends on singletons

## Goals / Non-Goals

**Goals:**
- Use `WithSingleton<T>()` for all `*Manager` actors
- Use `WithShardRegion<T>()` for all `*Worker` actors
- Make cluster setup explicit via `WithClustering()`
- Actors resolve peer actors via `Context.GetActor<T>()` (Servus.Akka) instead of constructor injection
- Actors with non-actor DI dependencies use `resolver.Props<T>()` for prop creation
- Tests continue to work with injectable test probes

**Non-Goals:**
- Changing actor behavior or message flows
- Multi-node clustering
- Removing Servus.Akka dependency (keep `Context.GetActor<T>()`)
- Changing persistence configuration

## Decisions

### Decision: Use `Context.GetActor<T>()` for actor-to-actor resolution

Actors resolve dependencies from `IActorRegistry` at construction time via Servus.Akka's `Context.GetActor<T>()` extension. This removes `IActorRef` constructor parameters and decouples actors from their creation order.

Alternative considered: `IRequiredActor<T>` injected via DI into actor constructors. Rejected because the project convention is to use Servus.Akka extensions inside actors, and `IRequiredActor<T>` adds DI ceremony that doesn't match the existing patterns.

### Decision: `resolver.Props<T>()` only for actors with non-actor DI deps

Only `MediathekViewWebManager` has a non-actor DI dependency (`IHttpClientFactory`). Use `resolver.Props<T>()` for it. All other actors use `Props.Create(() => new T())` since they have no constructor parameters after removing `IActorRef` params.

Alternative considered: Use `resolver.Props<T>()` for all actors uniformly. Rejected because it adds DI overhead for actors that don't need it and makes test construction harder.

### Decision: Test actors with IActorRegistry setup instead of constructor injection

Current tests create actors via `Props.Create(() => new Worker(probe1, probe2))`. After migration, actors resolve refs from `IActorRegistry` in their constructor. Tests must register test probes in the `IActorRegistry` before creating the actor under test.

With Akka.Hosting TestKit, tests register probes via `ActorRegistry`:
```csharp
var registry = ActorRegistry.For(Sys);
registry.Register<MediathekViewWebManager>(mediathekProbe);
registry.Register<MatchMagicManager>(matchMagicProbe);

var worker = Sys.ActorOf(Props.Create(() => new TvSearchWorker()));
```

### Decision: `WithClustering()` with single-node defaults

Add explicit `WithClustering(new ClusterOptions { SeedNodes = [selfAddress] })` or use the parameterless overload that defaults to self-join for single-node. This makes the cluster requirement visible rather than relying on implicit behavior.

### Decision: Singleton configuration with `ClusterSingletonOptions`

Use `WithSingleton<T>()` with default `ClusterSingletonOptions`. For single-node deployment, singleton behavior is trivial (always local). The naming convention `*Manager` maps directly.

## Risks / Trade-offs

**Actor startup ordering** — `WithSingleton` and `WithShardRegion` register actors in `IActorRegistry` asynchronously. Actors using `Context.GetActor<T>()` need their dependencies registered first. Akka.Hosting guarantees registration order matches builder chain order, and singleton/shard startup waits for the cluster to form. For single-node this is immediate.
→ Mitigation: Order builder calls so singletons without actor deps come first, then shard regions, then singletons that depend on shard regions.

**Test complexity** — Tests now need `IActorRegistry` setup instead of simple constructor injection. Slightly more boilerplate per test class.
→ Mitigation: Create a test helper or base class in `FunkArr.Tests.Shared` that pre-configures the registry pattern.

**Package addition** — Adding `Akka.Cluster.Hosting` introduces a new NuGet dependency. It's a first-party Akka package and already transitively referenced by existing packages, so risk is minimal.
