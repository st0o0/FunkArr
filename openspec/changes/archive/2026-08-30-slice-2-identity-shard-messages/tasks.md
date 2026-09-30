## 1. Add new interfaces (non-breaking, additive)

- [x] 1.1 Create `Messages/Sharding/ISeriesShard.cs`, `IMovieShard.cs`, `IDownloadShard.cs`, `ISearchShard.cs` interfaces
- [x] 1.2 Create `Messages/IRequest.cs` with `IRequest<TResponse>` marker interface
- [x] 1.3 Build to verify green

## 2. Create per-region extractors

- [x] 2.1 Create `FunkArr/Configuration/Sharding/SeriesShardExtractor.cs`
- [x] 2.2 Create `MovieShardExtractor.cs`
- [x] 2.3 Create `DownloadShardExtractor.cs`
- [x] 2.4 Create `SearchShardExtractor.cs`
- [x] 2.5 Build to verify green

## 3. Update ShowActor messages and constructor

- [x] 3.1 Update ShowActor's 10 message records: replace `IShardedMessage` with `ISeriesShard`, replace `EntityKey` with `int Id`
- [x] 3.2 Update ShowActor constructor to receive `int tvdbId` parameter, remove `Self.Path.Name` parsing
- [x] 3.3 Update ShowActor shard region config to use `SeriesShardExtractor` and pass typed id via factory
- [x] 3.4 Update RulesetController message construction for ShowActor messages
- [x] 3.5 Update ShowActor tests
- [x] 3.6 Build and run tests to verify green

## 4. Update MovieActor messages and constructor

- [x] 4.1 Update MovieActor's 10 message records: replace `IShardedMessage` with `IMovieShard`, replace `EntityKey` with `string Id`
- [x] 4.2 Update MovieActor constructor to receive `string imdbId` parameter, remove `Self.Path.Name` parsing
- [x] 4.3 Update MovieActor shard region config to use `MovieShardExtractor` and pass typed id via factory
- [x] 4.4 Update RulesetController and MatchIntelligenceController message construction for MovieActor messages
- [x] 4.5 Update MovieActor tests
- [x] 4.6 Build and run tests to verify green

## 5. Update Download messages and constructors

- [x] 5.1 Replace `IWithNzoId : IShardedMessage` with `IDownloadShard` on DownloadCoordinatorMessages and DownloadRequestActor messages
- [x] 5.2 Update DownloadActor constructor to receive id, remove `Self.Path.Name` parsing
- [x] 5.3 Update DownloadRequestActor constructor to receive id, remove `Self.Path.Name` parsing
- [x] 5.4 Update shard region config for both download regions to use `DownloadShardExtractor`
- [x] 5.5 Update SabnzbdController and any other callers
- [x] 5.6 Update download actor tests
- [x] 5.7 Build and run tests to verify green

## 6. Update Search messages

- [x] 6.1 Update `SearchRequest.Tv`, `.Movie`, `.Text` to implement `ISearchShard` instead of `IShardedMessage`
- [x] 6.2 Update `SearchRequestActor` — renamed internal EntityKey to Key, uses request.Id
- [x] 6.3 Update search shard region config to use `SearchShardExtractor`
- [x] 6.4 No search test updates needed
- [x] 6.5 Build and run tests to verify green

## 7. Move remaining messages and cleanup

- [x] 7.1 Move `SearchMessages.cs` to `FunkArr.Messages/Search/`
- [x] 7.2 Skipped — SearchCoordinatorMessages depends on MediathekSearchQuery still in FunkArr
- [x] 7.3 Skipped — DownloadCoordinatorMessages has internal records and FailureKind dependency
- [x] 7.4 Delete `IShardedMessage.cs` and `ShardedMessageExtractor.cs` from `Shared/`
- [x] 7.5 IWithNzoId already removed by download agent
- [x] 7.6 Build and run tests to verify green (578 pass, 0 fail)

## 8. Final verification

- [x] 8.1 Run `dotnet format`
- [x] 8.2 Full build and test run — all tests pass
- [x] 8.3 Commit
