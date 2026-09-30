# Tasks - Logging Coverage

## 1. Download domain - DownloadWorker
- Add `ILoggingAdapter _log = Context.GetLogger()`
- Info: HandleStart (download ID + title), HandleFfmpegResult success, HandleFfmpegResult fail
- Warning: empty video URL fault, subtitle fallback, HandleFailure

## 2. Download domain - DownloadManager
- Add `ILoggingAdapter _log = Context.GetLogger()`
- Info: enqueue, dispatch to worker, slot freed
- Debug: queue depth after state changes

## 3. Search domain - TvSearchWorker
- Add `ILoggingAdapter _log = Context.GetLogger()`
- Info: search start with query details
- Warning: MediathekQueryFailed, RuleSetNotFound, EpisodeResolutionFailed, Status.Failure

## 4. Search domain - MovieSearchWorker
- Add `ILoggingAdapter _log = Context.GetLogger()`
- Same pattern as TvSearchWorker: Info on start, Warning on failures

## 5. Search domain - SearchManager
- Add `ILoggingAdapter _log = Context.GetLogger()`
- Warning: search timeout

## 6. Search domain - MediathekViewWebManager
- Add `ILoggingAdapter _log = Context.GetLogger()`
- Warning: HttpFailed with reason
- Debug: stashing due to capacity

## 7. MetadataResolver - fix MetadataResolverManager
- Actually use the existing `_log` field
- Debug: cache hit/miss
- Info: resolve request forwarded to child

## 8. MetadataResolver - TmdbResolverActor
- Add Info log for fetch start (parity with TvdbResolverActor)

## 9. MatchMagic - MatchHistoryWorker
- Add `ILoggingAdapter _log = Context.GetLogger()`
- Warning: SaveSnapshotFailure (replace empty handler)

## 10. MatchMagic - MatchMagicManager
- Add `ILoggingAdapter _log = Context.GetLogger()`
- Debug: config lookup miss

## 11. Non-actor services - TvdbClient
- Add `ILogger<TvdbClient>` via constructor injection
- Warning: auth failure with status code, HTTP non-success responses
- Debug: successful authentication

## 12. Non-actor services - TmdbClient
- Add `ILogger<TmdbClient>` via constructor injection
- Warning: HTTP non-success with URL path and status code

## 13. Verify
- Build + format check
- Run all test projects
