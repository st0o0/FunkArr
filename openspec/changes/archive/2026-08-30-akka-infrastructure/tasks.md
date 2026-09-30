## 1. Message Contract

- [x] 1.1 Create `IWithDownloadId` interface in `src/FunkArr.Messages/` with `Guid DownloadId` property

## 2. Shard Extractor

- [x] 2.1 Create `ShardMessageExtractor : HashCodeMessageExtractor` in `src/FunkArr.Core/` with switch on `IWithDownloadId`, default maxShards 25

## 3. Persistence and Health Checks

- [x] 3.1 Update `FunkArrActorSystemSetup` — add `WithSqlPersistence` (SQLite from FunkArrOptions.PersistencePath), journal/snapshot `.WithHealthCheck()`, and `.WithActorSystemLivenessCheck()`

## 4. Verify

- [x] 4.1 Build, format check, and verify: `dotnet build` succeeds, `dotnet format --verify-no-changes` passes, `dotnet run` boots with SQLite DB created, `/healthz` returns 200
