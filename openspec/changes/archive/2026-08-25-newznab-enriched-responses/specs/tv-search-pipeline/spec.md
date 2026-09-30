## MODIFIED Requirements

### Requirement: Single matching path

TvSearchActor SHALL use `RuleSetMatchingEngine` as the sole matching path when rules are available. The heuristic fallback via MatchingPipeline is removed. Matched results SHALL propagate `MatchedEpisodeInfo` through quality expansion and scoring into the final `SearchResult`.

#### Scenario: Rules available — episode info propagated
- **WHEN** RulesResponse contains rules and matching produces MatchedEpisodeInfo with season=2, episode=5, episodeName="Köpfe", airDate="2024-03-15"
- **THEN** the final SearchResult SHALL carry ResolvedSeason=2, ResolvedEpisode=5, EpisodeName="Köpfe", AirDate="2024-03-15"

#### Scenario: No rules available
- **WHEN** RulesResponse contains no rules and items were fetched
- **THEN** the entity SHALL return empty results and forward items to RuleSetActor for auto-generation via `GenerateFromItems`

### Requirement: Shared quality expansion

TvSearchActor SHALL use the shared `QualityExpander` for quality variant expansion. QualityExpander SHALL preserve episode metadata from the input when creating quality variants.

#### Scenario: Quality expansion preserves episode info
- **WHEN** a matched item with ResolvedSeason=1, ResolvedEpisode=3, EpisodeName="Köpfe" is expanded to HD720 and HD1080 variants
- **THEN** both resulting SearchResults SHALL carry the same ResolvedSeason=1, ResolvedEpisode=3, EpisodeName="Köpfe"

### Requirement: SearchResult enrichment

SearchResult SHALL include optional fields for resolved episode metadata: `ResolvedSeason` (int?), `ResolvedEpisode` (int?), `EpisodeName` (string?), `AirDate` (string?), `ResolvedShowName` (string?). These fields SHALL be populated from MatchedEpisodeInfo when RuleSet matching is used.

#### Scenario: TV search result with full metadata
- **WHEN** RuleSet matching resolves an item to Tatort S01E03 "Köpfe" aired 2024-03-15
- **THEN** SearchResult SHALL have ResolvedSeason=1, ResolvedEpisode=3, EpisodeName="Köpfe", AirDate="2024-03-15", ResolvedShowName="Tatort"

#### Scenario: TV search result without match
- **WHEN** an item passes content filter but has no RuleSet match (shouldn't happen in TV pipeline, but defensive)
- **THEN** SearchResult SHALL have all resolved fields as null
