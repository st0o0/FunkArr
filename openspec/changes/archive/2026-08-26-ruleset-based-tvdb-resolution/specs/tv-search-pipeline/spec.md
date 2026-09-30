## MODIFIED Requirements

### Requirement: Parallel resolution stage

TvSearchActor SHALL Ask SeriesResolver (for TVDB show info and episodes) and RuleSetActor (for rules and show name) in parallel as the first pipeline stage. The RuleSet-provided show name SHALL be used as the primary search term. If SeriesResolver also returns a show name, it MAY override the RuleSet name. If SeriesResolver fails, the pipeline SHALL continue with the RuleSet-provided name.

#### Scenario: RuleSet provides show name, SeriesResolver fails
- **WHEN** a pipeline execution starts for tvdbId 83214
- **AND** RuleSetActor responds with `ResolvedShowName = "Tatort"` and matching rules
- **AND** SeriesResolver fails (API unreachable or returns null)
- **THEN** the entity SHALL use "Tatort" as the search term for MediathekGateway

#### Scenario: Both resolve successfully
- **WHEN** RuleSetActor responds with `ResolvedShowName = "Tatort"`
- **AND** SeriesResolver responds with `ShowName = "Tatort"`
- **THEN** the entity SHALL use the SeriesResolver name as the final search term

#### Scenario: Neither provides a name
- **WHEN** RuleSetActor responds with `ResolvedShowName = null` (no matching ruleset)
- **AND** SeriesResolver responds with `ShowName = null` (API failure)
- **THEN** the entity SHALL use the caller-provided `ShowName` or `Query` as fallback, which may be empty

#### Scenario: Parallel resolution
- **WHEN** a pipeline execution starts
- **THEN** the entity SHALL send Ask messages to both SeriesResolver and RuleSetActor concurrently and wait for both responses before proceeding

### Requirement: SeriesResolver failure handling

TvSearchActor SHALL handle SeriesResolver failures (Ask timeout or exception) gracefully by marking `ShowResolved = true` with a null name and logging a warning. The pipeline SHALL NOT abort on SeriesResolver failure.

#### Scenario: SeriesResolver timeout
- **WHEN** the SeriesResolver Ask times out after 10 seconds
- **THEN** TvSearchActor SHALL log a warning, set `ShowResolved = true` with null ShowName, and continue the pipeline

#### Scenario: SeriesResolver exception
- **WHEN** the SeriesResolver Ask fails with an exception
- **THEN** TvSearchActor SHALL log a warning, set `ShowResolved = true` with null ShowName, and continue the pipeline
