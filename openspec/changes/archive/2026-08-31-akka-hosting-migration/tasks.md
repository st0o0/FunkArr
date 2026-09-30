## 1. Package Setup

- [x] 1.1 Add `Akka.Cluster.Hosting` to `Directory.Packages.props`
- [x] 1.2 Add `Akka.Cluster.Hosting` package reference to `FunkArr.Core.csproj`

## 2. Actor Constructor Migration

- [x] 2.1 Remove `IActorRef` params from `TvSearchWorker` — resolve `MediathekViewWebManager` and `MatchMagicManager` via `Context.GetActor<T>()`
- [x] 2.2 Remove `IActorRef` params from `MovieSearchWorker` — resolve `MediathekViewWebManager` and `MatchMagicManager` via `Context.GetActor<T>()`
- [x] 2.3 Remove `IActorRef` params from `SearchGatewayManager` — resolve `TvSearchWorker` and `MovieSearchWorker` shard regions via `Context.GetActor<T>()`

## 3. Setup Container Migration

- [x] 3.1 Rewrite `AkkaSetupContainer.BuildSystem` — add `WithClustering()`, replace `WithActors` lambda with `WithSingleton<T>()` for managers and `WithShardRegion<T>()` for workers, use `resolver.Props<T>()` for `MediathekViewWebManager`

## 4. Test Updates

- [x] 4.1 Update `TvSearchWorkerTests` — register test probes in `IActorRegistry` instead of constructor injection
- [x] 4.2 Update `MovieSearchWorkerTests` — register test probes in `IActorRegistry` instead of constructor injection
- [x] 4.3 Verify `MatchMagicManagerTests` still compile (no constructor change)

## 5. Verification

- [x] 5.1 Run `dotnet build FunkArr.slnx` — all projects compile
- [x] 5.2 Run `dotnet format --verify-no-changes` — no formatting violations
- [x] 5.3 Run all test projects — all tests pass
