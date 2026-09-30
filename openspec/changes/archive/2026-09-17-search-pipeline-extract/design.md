## Context

Both search workers have private methods BuildResultItems, BuildScoredResult, BuildUnscoredResult that are pure functions — they take state data + scoring results and produce SearchCompleted responses. The TV and Movie variants differ only in: field names for metadata fields (TvdbId vs TmdbId), enrichment data types (EnrichedEpisode vs EnrichedMovie), release title type ("tv" vs "movie"), and query field targeting ("topic" vs "title,topic"). The transformation logic is identical.

## Goals / Non-Goals

**Goals:**
- Eliminate ~160 LOC of duplication (80 per worker)
- Make response-building logic directly unit-testable
- Workers become shorter and focused on orchestration

**Non-Goals:**
- Changing the State pattern or actor structure
- Merging TV and Movie workers into one generic actor
- Touching non-Search actors

## Decisions

### Extract as static methods, not a service

The build methods are pure functions (data in → data out). A static class with static methods is the simplest option — no DI, no interfaces, no allocation.

### Handle TV/Movie differences via parameters

Rather than two sets of methods, use parameters for the small differences (media type string, metadata field mapping). The `BuildResultItems` signature becomes generic enough for both, with a delegate or simple parameter for the metadata-specific parts.

### SearchPipeline lives in FunkArr.Search

Same project as the workers. Not a new project — it's an internal implementation detail.
