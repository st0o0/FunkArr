## MODIFIED Requirements

### Requirement: Single matching path
MovieSearchActor SHALL use `RuleSetMatchingEngine` as the matching path when rules are available. When no rules exist after auto-generation attempt, MovieActor SHALL return all ContentFilter-passing items as fallback results with null episode metadata. Matched results SHALL propagate through quality expansion and scoring into the final `SearchResult`.

#### Scenario: Rules available — matched results
- **WHEN** MovieActor has rules and matching produces results
- **THEN** the final SearchResult SHALL carry matched movie metadata through QualityExpander and ResultScorer

#### Scenario: No rules available — fallback results
- **WHEN** MovieActor has no rules after auto-generation attempt and items were fetched
- **THEN** the MovieActor SHALL return ContentFilter-passing items as `MatchedItemInfo(item, episode: null)` and the pipeline SHALL expand and score them normally

#### Scenario: No rules and no items
- **WHEN** MovieActor has no rules and Mediathek returned zero items
- **THEN** the pipeline SHALL return empty results
