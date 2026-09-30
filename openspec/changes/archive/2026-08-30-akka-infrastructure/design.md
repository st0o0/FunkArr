## Context

FunkArrActorSystemSetup currently only configures loggers. Domain actors need persistence
(journal + snapshots) and sharded workers need a message extractor to route by entity ID.
The reference projects njord (SQLite persistence) and Akka.Pathfinder (MongoDB persistence
+ sharding with IEntityId) inform the design.

## Goals / Non-Goals

**Goals:**
- SQLite persistence ready for first domain actor
- Health checks integrated with existing `/healthz` endpoint
- Shard message routing infrastructure in place
- Entity ID interface pattern established in Messages

**Non-Goals:**
- No actors registered yet (no domain logic)
- No ClusterSingleton registration yet (arrives with first Manager)
- No ShardRegion registration yet (arrives with first Worker)
- No PostgreSQL support yet (SQLite only for now)

## Decisions

### SQLite persistence via WithSqlPersistence

Following njord's pattern: build connection string from `FunkArrOptions.PersistencePath`
as `Data Source={fullPath}`, use `ProviderName.SQLiteMS`, `autoInitialize: true`.

**Why:** SQLite is the default persistence backend per CLAUDE.md. The path is already
configurable via `FunkArrOptions.PersistencePath` (default `data/funkarr.db`). This
matches the Docker volume at `/app/data`.

### Health checks on journal and snapshot builders

Both `journalBuilder` and `snapshotBuilder` get `.WithHealthCheck()`. Combined with
the existing ASP.NET health check at `/healthz`, this means persistence failures
(corrupt DB, unmounted volume) surface automatically.

### WithActorSystemLivenessCheck, not ClusterReadinessCheck

FunkArr uses single-node cluster sharding. `WithAkkaClusterReadinessCheck()` would
check for cluster join which doesn't apply. `WithActorSystemLivenessCheck()` checks
that the actor system is running — sufficient for single-node.

### IWith*Id interfaces in Messages, extractor in Core

Entity ID interfaces (`IWithDownloadId`) live in FunkArr.Messages (no external
dependencies). The `ShardMessageExtractor` lives in FunkArr.Core (has Akka dependency)
and pattern-matches on these interfaces to extract `string EntityId` for Akka sharding.

New entity types add: one interface in Messages, one case in the extractor switch.

**Why IWithDownloadId not IWithEntityId:** Messages must not reference Servus.Akka.
Domain-specific interfaces (`IWithDownloadId { Guid DownloadId }`) are more expressive
than a generic `IWithEntityId { string EntityId }` and prevent mixing entity types.

### Guid as entity ID type

All entity IDs use `Guid`. Consistent with Pathfinder's pattern. The extractor
calls `.ToString()` to produce the string Akka needs.

## Risks / Trade-offs

- **SQLite DB created on first boot** — Unlike the previous change where persistence
  was deferred, the DB file is now created at startup (`autoInitialize: true`). This
  is intentional: health checks need something to check against.
- **Single extractor for all entity types** — The switch expression grows with each
  new entity type. This is fine — it's a single file, one line per type.
