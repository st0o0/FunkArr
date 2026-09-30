## ADDED Requirements

### Requirement: Movie title matching strategy
The matching engine SHALL support a `movieTitleMatch` strategy that compares a Mediathek item's topic and title against the known movie title (case-insensitive, whitespace-normalized, umlaut-normalized).

#### Scenario: Topic matches movie title
- **WHEN** the movie title is "Das Boot" and the item has topic "Das Boot"
- **THEN** the strategy SHALL return a match

#### Scenario: Title contains movie title
- **WHEN** the movie title is "Das Boot" and the item has title "Das Boot - Director's Cut"
- **THEN** the strategy SHALL return a match

#### Scenario: Umlaut normalization
- **WHEN** the movie title is "Die Blechtrommel" and the item topic uses "Blechtrommel"
- **THEN** the strategy SHALL normalize umlauts (ae/oe/ue ↔ ä/ö/ü) and match

#### Scenario: No match
- **WHEN** the movie title is "Das Boot" and the item has topic "Boot Camp" and title "Boot Camp Folge 1"
- **THEN** the strategy SHALL NOT match (substring match requires topic-level match, not arbitrary substring)

### Requirement: Movie original title matching strategy
The matching engine SHALL support a `movieOriginalTitleMatch` strategy that compares against the original title of the movie, using the same normalization as `movieTitleMatch`.

#### Scenario: Original title matches
- **WHEN** the movie has original title "The Tin Drum" and the item topic is "The Tin Drum"
- **THEN** the strategy SHALL return a match

#### Scenario: Original title differs from primary
- **WHEN** the movie has title "Die Blechtrommel" (primary) and original title "The Tin Drum", and the item topic is "The Tin Drum"
- **THEN** the `movieOriginalTitleMatch` strategy SHALL match where `movieTitleMatch` would not

### Requirement: Movie duration validation
The matching engine SHALL support a duration validation filter for movie matches that verifies the item duration is at least 70% of the known movie runtime.

#### Scenario: Duration within range
- **WHEN** the movie runtime is 149 minutes and the item duration is 8400 seconds (140 minutes, 94%)
- **THEN** the duration validation SHALL pass

#### Scenario: Duration too short
- **WHEN** the movie runtime is 149 minutes and the item duration is 1800 seconds (30 minutes, 20%)
- **THEN** the duration validation SHALL fail (below 70% threshold)

### Requirement: EvaluateMovieRules method
The matching engine SHALL provide an `EvaluateMovieRules(items, rules, movieInfo)` method that evaluates rules using movie-specific strategies against movie identity instead of TVDB episodes.

#### Scenario: Movie rules evaluation
- **WHEN** `EvaluateMovieRules` is called with items and rules containing `movieTitleMatch` strategy
- **THEN** the engine SHALL evaluate filters, apply movie-specific strategies, and return matched items with movie identity info

#### Scenario: ContentFilter still applies
- **WHEN** an item title contains "Audiodeskription"
- **THEN** the engine SHALL skip it via `ContentFilter.ShouldSkip` before evaluating movie rules
