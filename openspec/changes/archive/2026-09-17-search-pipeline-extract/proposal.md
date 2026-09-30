## Why

TvSearchWorker (292 LOC) and MovieSearchWorker (284 LOC) each contain ~80 LOC of nearly identical response-building methods (BuildResultItems, BuildScoredResult, BuildUnscoredResult). This logic is pure transformation (no Akka, no state mutation) but lives inside the actors, making it untestable without TestKit and duplicated across both workers.

## What Changes

- Extract shared response-building logic into a static `SearchPipeline` class in FunkArr.Search
- Both workers call `SearchPipeline.Build*()` instead of their own private methods
- Workers shrink by ~80 LOC each, duplication eliminated
- No State-pattern changes, no partial classes, no new abstractions

## Capabilities

### New Capabilities

- `search-pipeline`: Static response-building logic shared between TV and Movie search workers

### Modified Capabilities

(none — existing actor behavior is unchanged, just code moved)

## Impact

- FunkArr.Search: new SearchPipeline.cs, TvSearchWorker.cs and MovieSearchWorker.cs simplified
- FunkArr.Search.Tests: existing tests still pass, optional new unit tests for SearchPipeline
- No API changes, no message changes
