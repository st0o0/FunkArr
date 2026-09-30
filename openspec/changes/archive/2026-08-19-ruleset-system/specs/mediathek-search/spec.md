## MODIFIED Requirements

### Requirement: Runtime duration filtering
The system SHALL filter search results by comparing the content's runtime duration against the expected episode duration, rejecting items that deviate by more than a configurable percentage (default 35%). When a RuleSet is available for the searched show, RuleSet-defined duration filters SHALL take precedence over this generic filter.

#### Scenario: Content with matching duration (no ruleset)
- **WHEN** no RuleSet exists for the searched show, Sonarr expects a 45-minute episode, and MediathekViewWeb returns a 42-minute result
- **THEN** the result passes the generic duration filter (7% deviation, below 35% threshold)

#### Scenario: Content with wrong duration (no ruleset)
- **WHEN** no RuleSet exists for the searched show, Sonarr expects a 45-minute episode, and MediathekViewWeb returns a 5-minute trailer
- **THEN** the result is filtered out by the generic duration filter (89% deviation, above 35% threshold)

#### Scenario: RuleSet duration filter takes precedence
- **WHEN** a RuleSet exists with a duration greaterThan 35 filter
- **THEN** the RuleSet filter SHALL be applied instead of the generic percentage-based filter

### Requirement: Episode pattern matching
The system SHALL extract season and episode numbers from result titles using configurable regex patterns from RuleSets when available, falling back to the built-in S##E## pattern matching when no RuleSet exists.

#### Scenario: RuleSet regex extraction
- **WHEN** a RuleSet provides seasonRegex and episodeRegex patterns
- **THEN** the system SHALL use those patterns instead of the built-in S##E## pattern

#### Scenario: Fallback to built-in pattern
- **WHEN** no RuleSet exists for the searched show and a result title contains "S01E03"
- **THEN** the built-in S##E## pattern match SHALL be used

### Requirement: TVDB metadata lookup
The system SHALL look up show metadata (name, episode titles, air dates) from TheTVDB using the TVDB ID provided by Sonarr/Radarr.

#### Scenario: TVDB lookup for show name
- **WHEN** a tvsearch request includes `tvdbid=12345`
- **THEN** the system resolves the TVDB ID to the show's German title for use in the MediathekViewWeb query

#### Scenario: TVDB lookup failure
- **WHEN** the TVDB API is unavailable or the ID is unknown
- **THEN** the system falls back to the `q` parameter text if provided, or returns empty results

#### Scenario: TVDB episode lookup for RuleSet matching
- **WHEN** a RuleSet strategy requires TVDB episode data (seasonAndEpisodeNumber, itemTitleExact, itemTitleIncludes, itemTitleEqualsAirdate, byAbsoluteEpisodeNumber)
- **THEN** the system SHALL fetch episode data from TVDB and provide it to the matching engine

## ADDED Requirements

### Requirement: RuleSet-driven search flow
The SearchActor SHALL ask the RuleSetRegistryActor for applicable rules before processing Mediathek results. When rules are available, the search SHALL use the RuleSet matching engine. When no rules are available, the search SHALL fall back to the existing generic MatchingPipeline.

#### Scenario: Search with available ruleset
- **WHEN** a TV search arrives for a show with tvdbId 329324 and a RuleSet exists
- **THEN** the SearchActor SHALL apply the RuleSet matching engine to filter and match results

#### Scenario: Search without ruleset
- **WHEN** a TV search arrives for a show with no matching RuleSet
- **THEN** the SearchActor SHALL use the existing MatchingPipeline (skip keywords, duration filter, quality variants)

#### Scenario: Search triggers auto-generation
- **WHEN** a TV search arrives for a TVDB ID with no existing RuleSet
- **THEN** the SearchActor SHALL still return results using the generic pipeline, while the registry triggers auto-generation in the background for future searches
