## MODIFIED Requirements

### Requirement: Generated ruleset format
The generator SHALL emit rulesets in the new format with FilterGroup composition, per-rule confidence scores, and **stable rule IDs**. Generated rule IDs SHALL follow the pattern `"gen-{index}"` (e.g., `"gen-0"`, `"gen-1"`).

#### Scenario: Generated rules have IDs
- **WHEN** the generator creates a ruleset with 2 rules
- **THEN** the rules SHALL have `Id = "gen-0"` and `Id = "gen-1"` respectively

#### Scenario: Generated filters use FilterGroup
- **WHEN** the generator creates a duration filter
- **THEN** it SHALL emit it as `all: [Filter(field="duration", op="greaterThan", value=22)]` instead of a flat filter array

#### Scenario: Generated accessibility filters
- **WHEN** the generator creates a ruleset
- **THEN** it SHALL include `not: [Filter(field="title", op="regex", value="(?i)audiodesk|gebärden|gebardensprache|hörfassung|klare sprache")]`

#### Scenario: Per-rule confidence on generated rules
- **WHEN** the generator creates a rule with detected confidence 0.8
- **THEN** the rule SHALL have confidence=0.8 on the individual rule record

### Requirement: Movie ruleset generation
The generator SHALL expose a `GenerateForMovie(MediathekResultItem[] items, TmdbMovieInfo movieInfo)` method that generates a movie-specific ruleset with stable rule IDs.

#### Scenario: Generate movie ruleset with IDs
- **WHEN** `RuleSetGenerator.GenerateForMovie(items, movieInfo)` is called
- **THEN** the generated rules SHALL have IDs (e.g., `"gen-0"` for the primary title rule, `"gen-1"` for the original title fallback rule)

#### Scenario: Movie duration filter
- **WHEN** the movie runtime is 149 minutes
- **THEN** the generated duration filter SHALL require items longer than 70% of runtime

#### Scenario: Movie title matching rule
- **WHEN** the movie title is "Das Boot"
- **THEN** the generated rule SHALL use `movieTitleMatch` strategy with `Id = "gen-0"`
