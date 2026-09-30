## Context

The release title pipeline transforms `EnrichedItem` (scored + enriched
Mediathek data) into `SearchResultItem[]` (Newznab-compatible releases). Today
this involves three static classes (`ReleaseTitleBuilder`, `ReleaseVariant`,
`VideoQuality`) with scattered responsibilities: title formatting in Core,
display resolution and identifier building in Search, quality expansion in
Search. The `ReleaseDisplay` record exists but is nullable on `EnrichedItem`,
causing fallback cascades (`EnsureDisplay`, `UnscoredItems`, fallback in
`Expand`) that all call `CleanTitle`.

## Goals / Non-Goals

**Goals:**
- One cohesive type (`SceneRelease`) owns the Mediathek-to-Newznab transformation
- Show vs Movie identifier logic is separated into distinct, readable methods
- Display is always resolved (never null) after scoring, eliminating fallback code
- `CleanTitle` lives on `ReleaseDisplay` where it semantically belongs
- `ReleaseTitleBuilder` removed from Core (only Search uses it)
- Every piece of title/identifier logic is testable through `SceneRelease` or
  `ReleaseDisplay` alone

**Non-Goals:**
- Changing the title format string (output stays identical)
- Changing `SearchResultItem` or the Messages boundary
- Changing the Newznab XML output
- Refactoring `VideoQuality` (it does one thing well)
- Changing enrichment or scoring pipelines

## Decisions

### SceneRelease record shape

```
SceneRelease(MediaName, Identifier, EpisodeTitle, Item: EnrichedItem)
```

The record captures only the **resolved** fields (display + identifier). All
other data (Source, Identity, Score, Match) is accessed through the original
`Item` reference. This avoids duplicating 10+ fields from EnrichedItem.

**Alternative considered:** Flat record with all fields copied out. Rejected
because it duplicates data, is fragile to EnrichedItem changes, and adds no
value since the Item is immutable.

### ForShow / ForMovie instead of MediaType parameter

Callers know their media type statically (TvSearchWorkerState always calls
ForShow, MovieSearchWorkerState always calls ForMovie). Passing MediaType as a
runtime parameter and switching internally is unnecessary indirection.

**Alternative considered:** Single `From(item, mediaType, name)` with internal
switch. Rejected because the Show and Movie identifier logic share nothing,
and splitting makes each path a linear 3-5 line method.

### ReleaseDisplay.From integrates CleanTitle

`CleanTitle` is only ever called to produce a `ReleaseDisplay`. Making it a
factory method on `ReleaseDisplay` is natural: `ReleaseDisplay.From(title,
mediaName)` returns a fully constructed display. The clean logic (prefix
stripping, suffix stripping, S##E## removal) becomes a private implementation
detail.

**Alternative considered:** Standalone `TitleCleaner` utility class. Rejected
because every call site immediately wraps the result in `ReleaseDisplay`, so
the extra type adds no value.

### Display always set (never null on EnrichedItem)

Today `Apply(ScoreCompleted)` sets `Display = null` for unmatched items, then
`EnsureDisplay` patches them later. Instead, all items get a Display during
scoring using `ReleaseDisplay.From(source.Title, mediaName)`. Matched items
may later get their EpisodeTitle upgraded by scoring's ConstructedTitle or
enrichment's EpisodeName. This eliminates `EnsureDisplay()` and
`UnscoredItems()` fallback methods.

### Formatting helpers as private static methods on SceneRelease

`Sanitize`, `MapQuality`, `CollapseDots`, `FormatSeasonEpisode`, `PadNumber`
become private static methods on `SceneRelease`. They are pure string
functions with no reason to be public or in a separate class.

**Alternative considered:** Keep them in a shared utility. Rejected because no
other code calls them. If that changes, they can be extracted then.

## Risks / Trade-offs

- **[Risk] Test coverage gap during migration** -- Existing tests in
  `ReleaseTitleBuilderTests` and `ReleaseVariantTests` must be migrated to
  `SceneReleaseTests`. Mitigated by migrating test-by-test, not deleting first.
- **[Risk] EnrichedItem reference in SceneRelease** -- SceneRelease holds a
  reference to the full EnrichedItem. This is fine because both are short-lived
  (created in ToSearchCompleted, consumed immediately). No leak concern.
- **[Trade-off] Display never null changes EnrichedItem semantics** -- Code
  that checked `Display is not null` to distinguish scored vs unscored items
  must use `HasScoringMetadata` instead. Limited to WorkerState internals.
