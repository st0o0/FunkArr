## Why

The search workers (TvSearchWorker, MovieSearchWorker) carry unnecessary complexity from manual timer management — 4 private timeout records per worker, explicit StartSingleTimer/Cancel calls, and IWithTimers boilerplate. The enrichers (EpisodeEnricher, MovieEnricher) are stateless pure-function classes instantiated for no reason. And SearchSeries/SearchMovie duplicate shared fields without a common base, preventing code reuse in state initialization.

## What Changes

- Replace Tell+Timer communication in both search workers with Ask+PipeTo+Become. Each pipeline step uses `Ask<XxxResponse>(request, timeout).PipeTo(Self, failure: ex => new XxxFailed(ex))` followed by `Become(NextState)`. Removes IWithTimers, all timeout records, and all timer management. Each Become state handles exactly Completed + Failed.
- Extract `SearchRequest` abstract base record with shared fields (SearchId, Source, Query, Limit, Offset). `SearchSeries` and `SearchMovie` inherit from it. State `Init` methods accept the base type for common field assignment.
- Convert `EpisodeEnricher` and `MovieEnricher` to static classes with static `Resolve` methods. All private methods are already static; only the public entry point changes.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `tv-search`: Worker switches from Tell+Timer to Ask+PipeTo+Become. Removes IWithTimers, timeout records, manual timer management.
- `movie-search`: Same changes as tv-search — Ask+PipeTo+Become, remove timer boilerplate.
- `search-messages`: Add `SearchRequest` abstract base record. SearchSeries and SearchMovie inherit from it.
- `search-worker-state`: State `Init` methods accept `SearchRequest` base for common fields. State classes remain separate.

## Impact

- **FunkArr.Search**: TvSearchWorker, MovieSearchWorker — structural change to communication pattern (Become states stay, timers go)
- **FunkArr.Search**: TvSearchWorkerState, MovieSearchWorkerState — Init signature change
- **FunkArr.Messages**: SearchSeries.cs, SearchMovie.cs — new abstract base record
- **FunkArr.Enrichment**: EpisodeEnricher, MovieEnricher — class becomes static
- **FunkArr.Search.Tests**: Worker tests need updated setup (no more timer expectations, expect Ask pattern)
- **FunkArr.Enrichment.Tests**: `new XxxEnricher().Resolve(...)` → `XxxEnricher.Resolve(...)`
- No API changes. No message shape changes for external consumers. No persistence impact.
