## Why

The actor system boots but has no persistence, no health checks, and no sharding
infrastructure. No domain actor can be built until persistence is wired up (actors
need journals/snapshots), shard routing is in place (workers need a MessageExtractor),
and the entity ID pattern is established (messages need IWith*Id interfaces).

## What Changes

- Wire up SQLite persistence in FunkArrActorSystemSetup via `WithSqlPersistence`
  using `FunkArrOptions.PersistencePath` for the connection string
- Add journal and snapshot health checks (`WithHealthCheck`) so `/healthz`
  reports persistence status
- Add actor system liveness check (`WithActorSystemLivenessCheck`)
- Create `ShardMessageExtractor : HashCodeMessageExtractor` in FunkArr.Core
  that pattern-matches on `IWith*Id` interfaces from Messages
- Create the first entity ID interface `IWithDownloadId { Guid DownloadId }`
  in FunkArr.Messages as the template pattern for future entity types

## Capabilities

### New Capabilities

- `shard-message-contract`: ShardMessageExtractor and IWith*Id interfaces for shard routing
- `akka-persistence`: SQLite persistence configuration with health checks

### Modified Capabilities

- `application-bootstrap`: ActorSystemSetup gains persistence, health checks, and liveness check

## Impact

- `src/FunkArr/Configuration/FunkArrActorSystemSetup.cs` — add persistence + health checks
- `src/FunkArr.Core/ShardMessageExtractor.cs` — new file
- `src/FunkArr.Messages/IWithDownloadId.cs` — new file
- `src/FunkArr.Core/FunkArr.Core.csproj` — may need `Akka.Cluster.Sharding` explicit ref for `HashCodeMessageExtractor`
- No new NuGet packages (all already in Directory.Packages.props)
