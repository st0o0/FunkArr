## MODIFIED Requirements

### Requirement: Shared quality expansion and scoring
TextSearchActor SHALL use the shared `QualityExpander` and `ResultScorer` components instead of a local `ExpandQualities` method. The dead `MatchingPipeline.Execute()` call is removed.

#### Scenario: Quality expansion
- **WHEN** MediathekGateway returns items
- **THEN** the entity SHALL apply ContentFilter, then QualityExpander, then ResultScorer to produce the final result set

#### Scenario: ContentFilter applied
- **WHEN** items arrive from the gateway
- **THEN** the entity SHALL filter items through `ContentFilter.ShouldSkip(title, topic)` before quality expansion
