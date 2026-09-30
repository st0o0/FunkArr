# Logging Coverage

## Problem

11 of 17 actors have zero logging. The entire Search, Download, and MatchMagic domains are silent. External API clients (TvdbClient, TmdbClient) silently swallow errors. MetadataResolverManager declares `_log` but never uses it. MatchHistoryWorker explicitly swallows `SaveSnapshotFailure`.

The RuleSet domain (RuleSetUpdater, RuleSetManager, RuleSetWorker) has good logging that serves as the baseline for what other domains should achieve.

## Approach

Add `ILoggingAdapter` to all actors that do meaningful work. Follow the level convention:

- **Info**: lifecycle events (download started/completed, search kicked off, metadata resolved)
- **Warning**: recoverable failures (HTTP errors, timeouts, missing config, snapshot failures)
- **Debug**: internal state changes (cache hits/misses, stash pressure, queue depth, state transitions)

For non-actor DI services (TvdbClient, TmdbClient), add `ILogger<T>` via constructor injection.

## Scope

### Download domain (critical - zero visibility)
- DownloadManager - queue lifecycle, slot management
- DownloadWorker - download start/success/fail/cancel, subtitle fallback

### Search domain (zero visibility)
- SearchManager - search timeout, failure forwarding
- TvSearchWorker - search steps, failures, fallbacks
- MovieSearchWorker - same as TvSearchWorker
- MediathekViewWebManager - HTTP failures, stash pressure

### MatchMagic domain (zero visibility)
- MatchHistoryWorker - snapshot failures (currently swallowed)
- MatchMagicManager - config registration misses

### MetadataResolver
- MetadataResolverManager - actually use the declared `_log` (cache hits/misses)
- TmdbResolverActor - add Info log for fetch start (TvdbResolverActor already has it)

### Non-actor services
- TvdbClient - auth failures, HTTP errors
- TmdbClient - HTTP errors with URL and status code

## Out of scope

- RuleSet domain (already good)
- RuleSetResolver (pure lookup, acceptable)
- DownloadHistoryManager (pure CRUD/query)
- DataFiles (already has ILogger)
- MatchMagicActor (pure computation, regex timeouts are edge case)
- Logging infrastructure changes (Serilog config, sinks, enrichers)
