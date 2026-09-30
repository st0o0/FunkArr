## MODIFIED Requirements

### Requirement: Single matching path
TvSearchActor SHALL use `RuleSetMatchingEngine` as the sole matching path when rules are available. When no rules exist after auto-generation attempt, ShowActor SHALL return all ContentFilter-passing items as fallback results with null episode metadata. Matched results SHALL propagate `MatchedEpisodeInfo` through quality expansion and scoring into the final `SearchResult`.

#### Scenario: Rules available — episode info propagated
- **WHEN** RulesResponse contains rules and matching produces MatchedEpisodeInfo with season=2, episode=5, episodeName="Köpfe", airDate="2024-03-15"
- **THEN** the final SearchResult SHALL carry ResolvedSeason=2, ResolvedEpisode=5, EpisodeName="Köpfe", AirDate="2024-03-15"

#### Scenario: No rules available — fallback results
- **WHEN** ShowActor has no rules after auto-generation attempt and items were fetched
- **THEN** the ShowActor SHALL return ContentFilter-passing items as `MatchedItemInfo(item, episode: null)` and the pipeline SHALL expand and score them normally without episode metadata

#### Scenario: No rules and no items
- **WHEN** ShowActor has no rules and Mediathek returned zero items
- **THEN** the pipeline SHALL return empty results
