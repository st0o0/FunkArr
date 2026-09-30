## ADDED Requirements

### Requirement: Movie rulesets in community dataset
The community dataset SHALL support movie rulesets alongside show rulesets. Movie rulesets SHALL have `media.type = "movie"` and MUST include `media.imdbId`. Movie rulesets use movie-specific matching strategies (`movieTitleMatch`, `movieOriginalTitleMatch`).

#### Scenario: Movie ruleset file
- **WHEN** a community ruleset has `media.type = "movie"` and `media.imdbId = "tt0082096"`
- **THEN** it SHALL be a valid `RuleSetFile` with movie-specific strategies in its rules

#### Scenario: Movie ruleset pushed to MovieActor
- **WHEN** the `RuleSetRegistryActor` loads a movie ruleset with `media.imdbId = "tt0082096"`
- **THEN** it SHALL push it to `MovieActor("tt0082096")` via `ApplyCommunityRules`

#### Scenario: Show vs movie distinction
- **WHEN** a community ruleset has `media.type = "show"` and `media.tvdbId = 329324`
- **THEN** it SHALL be pushed to `ShowActor("329324")`, not `MovieActor`

## MODIFIED Requirements

### Requirement: V2 schema format
Each JSON file SHALL be a valid `RuleSetFile` object with fields: topic (string), aliases (string array, default empty), channels (string array, optional), media (MediaReference with optional tvdbId, imdbId, tmdbId, name, type), source set to `"community"`, confidence (float), rules (array of Rule with FilterGroup, strategy, regexes, titleRules), and optional overrides. The `media.type` field SHALL support both `"show"` and `"movie"` values. Rules SHALL reference actual API fields (`topic`, `title`, `description`, `channel`, `duration`, `timestamp`) -- the `topicTitle` composite field is NOT valid. Movie rulesets MAY use movie-specific strategies (`movieTitleMatch`, `movieOriginalTitleMatch`).

#### Scenario: Valid v2 structure with channels
- **WHEN** reading any file from `data/community/rulesets/`
- **THEN** it SHALL deserialize into a `RuleSetFile` record without errors, including the optional `channels` field

#### Scenario: No topicTitle in rules
- **WHEN** reading any community ruleset file
- **THEN** no TitleRule or Filter SHALL reference `field: "topicTitle"`

#### Scenario: Multi-rule topics grouped into single file
- **WHEN** the upstream has 3 entries for topic "Tatort" at priorities 0, 10, 20
- **THEN** the `tatort.json` file SHALL contain one `RuleSetFile` with 3 rules sorted by priority

#### Scenario: Filters use FilterGroup structure
- **WHEN** an upstream entry has a flat filter list `[{duration > 35}]`
- **THEN** the v2 file SHALL have `filters: { all: [{ field: "duration", op: "greaterThan", value: "35" }], any: [], not: [] }`

#### Scenario: Source field
- **WHEN** reading any community ruleset file
- **THEN** the `source` field SHALL be `"community"`

#### Scenario: Movie ruleset with movie strategy
- **WHEN** reading a movie community ruleset file
- **THEN** the rules SHALL use `movieTitleMatch` or `movieOriginalTitleMatch` strategies
