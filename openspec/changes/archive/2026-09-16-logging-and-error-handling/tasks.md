## 1. Message fixes

- [x] 1.1 Add `Exception? Cause = null` parameter to `SearchFailed` record in FunkArr.Messages and update all call sites
- [x] 1.2 Fix `MatchHistoryWorker` PersistenceId dead `field` reference — use proper property initialization from `Context.Self.Path.Name`
- [x] 1.3 Replace `object` return type in `MatchMagicActor` QueryDetail handler with a typed response record

## 2. PipeTo and failure handler fixes

- [x] 2.1 Fix `MovieSearchWorker` PipeTo failure handler for `RuleSetNotFound` — forward exception instead of discarding with `_ =>`
- [x] 2.2 Fix `TvSearchWorker` PipeTo failure handler for `RuleSetNotFound` — forward exception into failure message
- [x] 2.3 Fix bare `catch` in `RuleSetManagerState` summary building — capture exception variable and log at Warning level

## 3. API endpoint logging — FunkArr.Api

- [x] 3.1 Add `ILogger<MediathekApiEndpoints>` parameter to endpoint methods in `MediathekApiEndpoints` and log in catch blocks with structured context
- [x] 3.2 Add `ILogger<DownloadsApiEndpoints>` parameter to endpoint methods in `DownloadsApiEndpoints` and log in all 7 catch blocks
- [x] 3.3 Add `ILogger<RuleSetApiEndpoints>` parameter to endpoint methods in `RuleSetApiEndpoints` and log in all 5 catch blocks plus the stats fan-out catch
- [x] 3.4 Add `ILogger<SystemApiEndpoints>` parameter to endpoint methods in `SystemApiEndpoints` and log in cache stats timeout catch

## 4. API endpoint logging — FunkArr.ArrApi

- [x] 4.1 Add `ILogger<SearchHandler>` to `SearchHandler` and log in `AskAndFormat` catch block with search query context
- [x] 4.2 Add `ILogger<SabnzbdApiEndpoints>` to SABnzbd POST handler and log in NZB parse catch block

## 5. Actor logging improvements

- [x] 5.1 Add Warning-level logging to Search worker failure paths (`MediathekQueryFailed`, `ScoringFailed`) with Reason and Cause
- [x] 5.2 Add Warning-level logging to `DownloadWorker` fault paths where missing
- [x] 5.3 Add Debug-level logging to `MatchMagicManager` for scoring requests (RuleSetId, candidate count)
- [x] 5.4 Add Warning-level logging to `SubtitlePreparer` silent catch block for HTTP failures

## 6. Verify and format

- [x] 6.1 Run `dotnet build src/FunkArr.slnx` and fix any compilation errors
- [x] 6.2 Run `dotnet format src/FunkArr.slnx` to apply code style
- [x] 6.3 Run test projects to verify no regressions
