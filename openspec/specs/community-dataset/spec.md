# Community Dataset

## Purpose

V2 dataset format for community rulesets: slug-based filenames, 1:1 upstream parity, and version tracking via release-please.

## Requirements

### Requirement: Community ruleset directory structure
The repository SHALL contain a `data/community/rulesets/` directory with one JSON file per topic in v2 `RuleSetFile` schema format. Filenames SHALL be slug-based (lowercase, hyphens, no special characters) matching the output of `TopicSlugGenerator.Generate()`.

#### Scenario: One file per topic
- **WHEN** the upstream dataset contains 59 unique topics
- **THEN** the `data/community/rulesets/` directory SHALL contain 59 JSON files

#### Scenario: Filename matches slug
- **WHEN** a ruleset has topic "Feuer & Flamme"
- **THEN** the filename SHALL be `feuer-und-flamme.json`

#### Scenario: Filename for topic with umlauts
- **WHEN** a ruleset has topic "Lowenzahn"
- **THEN** the filename SHALL be `loewenzahn.json`

### Requirement: V2 schema format
Each JSON file SHALL be a valid `RuleSetFile` object with fields: topic (string), aliases (string array, default empty), media (MediaReference with optional tvdbId, imdbId, tmdbId, name, type), source set to `"community"`, confidence (float), rules (array of Rule with FilterGroup, strategy, regexes, titleRules), and optional overrides.

#### Scenario: Valid v2 structure
- **WHEN** reading any file from `data/community/rulesets/`
- **THEN** it SHALL deserialize into a `RuleSetFile` record without errors

#### Scenario: Multi-rule topics grouped into single file
- **WHEN** the upstream has 3 entries for topic "Tatort" at priorities 0, 10, 20
- **THEN** the `tatort.json` file SHALL contain one `RuleSetFile` with 3 rules sorted by priority

#### Scenario: Filters use FilterGroup structure
- **WHEN** an upstream entry has a flat filter list `[{duration > 35}]`
- **THEN** the v2 file SHALL have `filters: { all: [{ field: "duration", op: "greaterThan", value: "35" }], any: [], not: [] }`

#### Scenario: Source field
- **WHEN** reading any community ruleset file
- **THEN** the `source` field SHALL be `"community"`

### Requirement: 1:1 parity with upstream
The initial dataset SHALL be a direct transformation of the upstream `rundfunkarr/rundfunkarr` rulesets. No enrichment (aliases, composite filters, channel filters) SHALL be applied in the initial port.

#### Scenario: All upstream topics present
- **WHEN** comparing the ported dataset to the upstream JSON
- **THEN** every unique topic in the upstream SHALL have a corresponding file in `data/community/rulesets/`

#### Scenario: Media references preserved
- **WHEN** an upstream entry has tvdbId 329324 and imdbId "tt7995922"
- **THEN** the v2 file SHALL have media.tvdbId = 329324 and media.imdbId = "tt7995922"

#### Scenario: Title rules preserved
- **WHEN** an upstream entry has titleRegexRules with a regex pattern
- **THEN** the v2 file SHALL have equivalent TitleRule entries with type, field, pattern, and value

### Requirement: Version tracking file
A `data/community/version.txt` file SHALL exist containing the current ruleset version managed by release-please. The version SHALL follow semantic versioning.

#### Scenario: Version file content
- **WHEN** reading `data/community/version.txt`
- **THEN** it SHALL contain a single line with a semver version string (e.g., `1.0.0`)
