# ruleset-list-enrichment Specification

## Purpose
TBD - created by archiving change ruleset-polish. Update Purpose after archive.
## Requirements
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

### Requirement: RuleSetManager provides list summaries
The `RuleSetManager` SHALL handle `QueryRuleSetSummaries` messages by responding with `RuleSetSummaryResult` containing per-ruleset `RuleCount` and `SourceType`.

#### Scenario: Summary with rulesets
- **WHEN** `QueryRuleSetSummaries` is received and rulesets are registered
- **THEN** the response SHALL contain one `RuleSetSummaryEntry` per known ruleset with `RuleSetId`, `RuleCount` (number of matching rules in the merged config), and `SourceType` ("community", "local", or "merged")

#### Scenario: Source type determination
- **WHEN** a ruleset has both `CommunityPath` and `LocalPath` in `RuleSetPaths`
- **THEN** `SourceType` SHALL be "merged"
- **WHEN** a ruleset has only `CommunityPath`
- **THEN** `SourceType` SHALL be "community"
- **WHEN** a ruleset has only `LocalPath`
- **THEN** `SourceType` SHALL be "local"

#### Scenario: Rule count from merged config
- **WHEN** a ruleset config is loaded and merged
- **THEN** `RuleCount` SHALL be the number of rules in the merged `MatchingRule[]` array

#### Scenario: Config load failure
- **WHEN** a ruleset config cannot be loaded or parsed
- **THEN** `RuleCount` SHALL be 0

### Requirement: MatchHistoryWorker provides scoring stats
The `MatchHistoryWorker` SHALL handle `QueryScoringStats` messages by responding with `ScoringStatsResult` containing the latest scoring run timestamp and average match rate.

#### Scenario: Stats with scoring history
- **WHEN** `QueryScoringStats` is received for a ruleset with scoring snapshots
- **THEN** the response SHALL contain `LastRun` (timestamp of the most recent snapshot) and `MatchRate` (average of `MatchedCount / CandidateCount` over all snapshots where `CandidateCount > 0`)

#### Scenario: Stats with no scoring history
- **WHEN** `QueryScoringStats` is received for a ruleset with no scoring snapshots
- **THEN** the response SHALL contain `LastRun` as `null` and `MatchRate` as `null`

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

