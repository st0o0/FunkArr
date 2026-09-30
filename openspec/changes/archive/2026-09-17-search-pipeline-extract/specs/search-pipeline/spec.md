# search-pipeline

## Purpose

Static response-building logic shared between TV and Movie search workers, extracted from duplicated private methods.

## ADDED Requirements

### Requirement: SearchPipeline builds unscored results

SearchPipeline SHALL provide a static method that takes raw MediathekItems and search context and returns a SearchCompleted response with unscored items (score 0.0).

#### Scenario: Unscored TV results
- **WHEN** `SearchPipeline.BuildUnscoredResult(searchId, rawItems, mediaName, tvdbId, imdbId, "tv")` is called
- **THEN** it SHALL return a `SearchCompleted` with items built from all quality variants, score 0.0

### Requirement: SearchPipeline builds scored results

SearchPipeline SHALL provide a static method that takes raw items, scoring results, and optional enrichment data and returns a SearchCompleted response sorted by score descending.

#### Scenario: Scored results without enrichment
- **WHEN** `SearchPipeline.BuildScoredResult(searchId, rawItems, scored, mediaName, ..., "tv")` is called
- **THEN** it SHALL return a `SearchCompleted` with items ordered by score descending

#### Scenario: Scored results with episode enrichment
- **WHEN** enriched episode data is provided
- **THEN** enriched season/episode metadata SHALL override the scoring metadata

### Requirement: SearchPipeline builds result items per quality variant

For each MediathekItem, SearchPipeline SHALL produce one SearchResultItem per quality variant returned by VideoQuality.GetVariants. Items with no variants SHALL be skipped.

#### Scenario: Item with multiple quality variants
- **WHEN** a MediathekItem has HD and SD variants
- **THEN** two SearchResultItems SHALL be produced with appropriate quality and estimated size

#### Scenario: Item with no variants
- **WHEN** VideoQuality.GetVariants returns empty
- **THEN** no SearchResultItem SHALL be produced for that item
