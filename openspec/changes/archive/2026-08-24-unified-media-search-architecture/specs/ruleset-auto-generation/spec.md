## MODIFIED Requirements

### Requirement: Static generator class
The ruleset generator SHALL be a static class `RuleSetGenerator` (not an actor). It SHALL expose a `Generate(MediathekResultItem[] items, int tvdbId, string showName, TvdbEpisodeInfo[] episodes)` method that returns a `RuleSetFile`. The method accepts TVDB episodes for validation of generated rules. The method performs pure CPU work (pattern analysis, regex generation, confidence scoring, episode validation) and does NOT make any HTTP calls.

#### Scenario: Generate from provided items with episodes
- **WHEN** `RuleSetGenerator.Generate(items, 83214, "Tatort", episodes)` is called with 200 MediathekResultItems and TVDB episodes
- **THEN** it SHALL analyze patterns, detect strategy, generate rules, validate against episodes, and return a complete `RuleSetFile`

#### Scenario: No HTTP calls
- **WHEN** the generator runs
- **THEN** it SHALL NOT use `MediathekClient` or make any network requests

### Requirement: Confidence scoring
The generator SHALL validate the generated ruleset against the sample results AND the provided TVDB episodes, computing a real confidence score based on actual episode matches rather than pattern-only heuristics.

#### Scenario: High confidence with episode validation
- **WHEN** the generated ruleset matches 80%+ of samples to correct TVDB episodes
- **THEN** the per-rule confidence SHALL be 0.8 or higher

#### Scenario: Low confidence with fallback
- **WHEN** the generated ruleset matches fewer than 30% of samples to TVDB episodes
- **THEN** the system SHALL set per-rule confidence to 0.3 and fall back to itemTitleIncludes

#### Scenario: Confidence improvement over heuristic
- **WHEN** pattern heuristics suggest confidence 0.9 but actual episode matching yields only 0.5
- **THEN** the actual episode-validated confidence (0.5) SHALL be used, not the heuristic (0.9)

## ADDED Requirements

### Requirement: Movie ruleset generation
The generator SHALL expose a `GenerateForMovie(MediathekResultItem[] items, TmdbMovieInfo movieInfo)` method that generates a movie-specific ruleset with simpler strategy detection.

#### Scenario: Generate movie ruleset
- **WHEN** `RuleSetGenerator.GenerateForMovie(items, movieInfo)` is called with Mediathek items and TMDB movie info
- **THEN** it SHALL generate a ruleset with `movieTitleMatch` strategy, duration filter derived from runtime, and channel derivation

#### Scenario: Movie duration filter
- **WHEN** the movie runtime is 149 minutes
- **THEN** the generated duration filter SHALL require items longer than 70% of runtime (approximately 104 minutes)

#### Scenario: Movie title matching rule
- **WHEN** the movie title is "Das Boot"
- **THEN** the generated rule SHALL use `movieTitleMatch` strategy with the movie title

#### Scenario: Movie original title fallback rule
- **WHEN** the movie has a different original title "The Tin Drum" from the primary title "Die Blechtrommel"
- **THEN** the generator SHALL create a second rule with `movieOriginalTitleMatch` strategy at lower priority

## MODIFIED Requirements

### Requirement: RuleSetActor integration
The `RuleSetGenerator` SHALL be called directly by `ShowActor` and `MovieActor` during `Match` processing when no rules exist. The generator is no longer triggered via a `GenerateFromItems` message to the `RuleSetActor`.

#### Scenario: Generation triggered by ShowActor
- **WHEN** `ShowActor` receives a `Match` message and has no rules
- **THEN** it SHALL call `RuleSetGenerator.Generate(items, tvdbId, showName, episodes)` inline and persist the result

#### Scenario: Generation triggered by MovieActor
- **WHEN** `MovieActor` receives a `Match` message and has no rules
- **THEN** it SHALL call `RuleSetGenerator.GenerateForMovie(items, movieInfo)` inline and persist the result

#### Scenario: Generation failure
- **WHEN** `RuleSetGenerator.Generate()` returns null
- **THEN** the calling actor SHALL log a warning, persist no event, and return empty match results
