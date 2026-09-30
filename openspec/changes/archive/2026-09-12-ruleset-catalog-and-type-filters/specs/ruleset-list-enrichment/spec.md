## MODIFIED Requirements

### Requirement: RegisteredRuleSetEntry includes MediaName
The `RegisteredRuleSetEntry` message SHALL include an optional `MediaName` field containing the resolved TMDB/TVDB media name from `RuleSetResolverState.MediaNameByRuleSetId`. It SHALL also include an optional `MediaType` field (`string?`) containing the `media.type` value from the parsed ruleset config (`"show"`, `"movie"`, or `null` when not set).

#### Scenario: Resolver includes media name
- **WHEN** `QueryRegisteredRuleSets` is handled and a ruleset has a resolved media name
- **THEN** the `RegisteredRuleSetEntry` for that ruleset SHALL include the `MediaName` value

#### Scenario: Resolver without media name
- **WHEN** a ruleset has no resolved media name in the resolver state
- **THEN** the `RegisteredRuleSetEntry.MediaName` SHALL be `null`

#### Scenario: Resolver includes media type
- **WHEN** `QueryRegisteredRuleSets` is handled and a ruleset config has `media.type` set
- **THEN** the `RegisteredRuleSetEntry` for that ruleset SHALL include the `MediaType` value

#### Scenario: Resolver without media type
- **WHEN** a ruleset config has no `media` or no `media.type`
- **THEN** the `RegisteredRuleSetEntry.MediaType` SHALL be `null`

### Requirement: Enriched RuleSetListEntry API model
The `RuleSetListEntry` API model SHALL include `MediaName` (string?), `MediaType` (string?), `RuleCount` (int), `SourceType` (string), `LastScoringRun` (string?, ISO 8601), and `MatchRate` (double?).

#### Scenario: Fully enriched entry
- **WHEN** all data sources respond for a ruleset
- **THEN** the API response SHALL include all enrichment fields populated, including `mediaType`

#### Scenario: Missing scoring stats
- **WHEN** the MatchHistory worker times out for a ruleset
- **THEN** `LastScoringRun` and `MatchRate` SHALL be `null` in the response

#### Scenario: MediaType in list response
- **WHEN** the API returns the ruleset list and a ruleset has `media.type` = `"movie"`
- **THEN** the `mediaType` field in the JSON response SHALL be `"movie"`
