## MODIFIED Requirements

### Requirement: Shared quality expansion and scoring
MovieSearchActor SHALL use the shared `QualityExpander` and `ResultScorer` components instead of a local `ExpandQualities` method. ShowMatcher SHALL be used for show name matching when movie info is available.

#### Scenario: Quality expansion
- **WHEN** MediathekGateway returns items
- **THEN** the entity SHALL apply ContentFilter, ShowMatcher (when show name available), then QualityExpander, then ResultScorer to produce the final result set

#### Scenario: ShowMatcher filters by movie title
- **WHEN** movie info has title "Der Untergang" and items contain unrelated content
- **THEN** ShowMatcher SHALL filter to items whose Topic or Title contains the normalized movie title
