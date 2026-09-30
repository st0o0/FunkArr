## ADDED Requirements

### Requirement: Channels property on RuleSetFile
The `RuleSetFile` schema SHALL support an optional `channels` property (string array). Community rulesets MAY declare the channels a show airs on. When present, this information is used to build more precise Mediathek queries.

#### Scenario: RuleSet with channels
- **WHEN** a community ruleset has `"channels": ["ARD", "Das Erste"]`
- **THEN** the deserialized `RuleSetFile` SHALL have `Channels = ["ARD", "Das Erste"]`

#### Scenario: RuleSet without channels
- **WHEN** a community ruleset has no `channels` property
- **THEN** the deserialized `RuleSetFile` SHALL have `Channels` as null or empty

## MODIFIED Requirements

### Requirement: V2 schema format
Each JSON file SHALL be a valid `RuleSetFile` object with fields: topic (string), aliases (string array, default empty), channels (string array, optional), media (MediaReference with optional tvdbId, imdbId, tmdbId, name, type), source set to `"community"`, confidence (float), rules (array of Rule with FilterGroup, strategy, regexes, titleRules), and optional overrides. Rules SHALL reference actual API fields (`topic`, `title`, `description`, `channel`, `duration`, `timestamp`) — the `topicTitle` composite field is NOT valid.

#### Scenario: Valid v2 structure with channels
- **WHEN** reading any file from `data/community/rulesets/`
- **THEN** it SHALL deserialize into a `RuleSetFile` record without errors, including the optional `channels` field

#### Scenario: No topicTitle in rules
- **WHEN** reading any community ruleset file
- **THEN** no TitleRule or Filter SHALL reference `field: "topicTitle"`

#### Scenario: Source field
- **WHEN** reading any community ruleset file
- **THEN** the `source` field SHALL be `"community"`
