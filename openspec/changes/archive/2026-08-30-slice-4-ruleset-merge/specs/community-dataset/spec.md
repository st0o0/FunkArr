## MODIFIED Requirements

### Requirement: V2 schema format
Each JSON file SHALL be a valid `RuleSetFile` object with fields: topic (string), aliases (string array, default empty), channels (string array, optional), media (MediaReference with optional tvdbId, imdbId, tmdbId, name, type), source set to `"community"`, confidence (float), rules (array of Rule with **required id**, FilterGroup, strategy, regexes, titleRules), and optional overrides. The `media.type` field SHALL support both `"show"` and `"movie"` values. Rules SHALL reference actual API fields (`topic`, `title`, `description`, `channel`, `duration`, `timestamp`). Movie rulesets MAY use movie-specific strategies (`movieTitleMatch`, `movieOriginalTitleMatch`).

#### Scenario: Every rule has an ID
- **WHEN** reading any community ruleset file
- **THEN** every rule SHALL have a non-empty `"id"` field with a descriptive slug (e.g., `"se-main"`, `"airdate"`, `"title-exact"`)

#### Scenario: Rule IDs unique within file
- **WHEN** reading any community ruleset file with multiple rules
- **THEN** no two rules SHALL have the same `"id"` value

#### Scenario: Valid v2 structure with channels
- **WHEN** reading any file from `data/community/rulesets/`
- **THEN** it SHALL deserialize into a `RuleSetFile` record without errors, including the required `id` on each rule

#### Scenario: No topicTitle in rules
- **WHEN** reading any community ruleset file
- **THEN** no TitleRule or Filter SHALL reference `field: "topicTitle"`

#### Scenario: Multi-rule topics grouped into single file
- **WHEN** the upstream has 3 entries for topic "Tatort" at priorities 0, 10, 20
- **THEN** the `tatort.json` file SHALL contain one `RuleSetFile` with 3 rules, each with a unique id, sorted by priority

#### Scenario: Filters use FilterGroup structure
- **WHEN** an upstream entry has a flat filter list `[{duration > 35}]`
- **THEN** the v2 file SHALL have `filters: { all: [{ field: "duration", op: "greaterThan", value: "35" }], any: [], not: [] }`

#### Scenario: Source field
- **WHEN** reading any community ruleset file
- **THEN** the `source` field SHALL be `"community"`

#### Scenario: Movie ruleset with movie strategy
- **WHEN** reading a movie community ruleset file
- **THEN** the rules SHALL use `movieTitleMatch` or `movieOriginalTitleMatch` strategies and each rule SHALL have an id
