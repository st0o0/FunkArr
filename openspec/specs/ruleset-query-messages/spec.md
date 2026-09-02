## Purpose

Query messages and response records for retrieving ruleset data (list and detail) from the actor system.

## Requirements

### Requirement: QueryRegisteredRuleSets message and response

The system SHALL define a `QueryRegisteredRuleSets` query record in `FunkArr.Messages/RuleSet/` and a corresponding `RegisteredRuleSetsResult` response record. The response SHALL contain an array of `RegisteredRuleSetEntry` records, each with `RuleSetId` (string), `Topic` (string), `Aliases` (string[]), `TvdbId` (int?), `ImdbId` (string?), and `TmdbId` (int?). `RegisteredRuleSetsResult` SHALL implement `IRuleSetResponse`.

#### Scenario: Message shape

- **WHEN** `QueryRegisteredRuleSets` is constructed
- **THEN** it is a parameterless sealed record in namespace `FunkArr.Messages.RuleSet`

#### Scenario: Response implements IRuleSetResponse

- **WHEN** `RegisteredRuleSetsResult` is returned
- **THEN** it SHALL be assignable to `IRuleSetResponse`

#### Scenario: Response contains all registered rulesets

- **WHEN** the Resolver holds 3 registered rulesets
- **THEN** `RegisteredRuleSetsResult.Entries` contains 3 `RegisteredRuleSetEntry` items with their identity data

### Requirement: QueryRuleSetDetail message and response

The system SHALL define a `QueryRuleSetDetail` query record with a `RuleSetId` (string) parameter and a corresponding `RuleSetDetailResult` response record. `RuleSetDetailResult` SHALL implement `IRuleSetResponse`. `RuleSetNotFound` SHALL implement `IRuleSetResponse`. The response SHALL contain: `RuleSetId` (string), `Topic` (string), `Aliases` (string[]), `TvdbId` (int?), `ImdbId` (string?), `TmdbId` (int?), `CommunityPath` (string?), `LocalPath` (string?), `CommunityModified` (DateTime?), `LocalModified` (DateTime?), `DefaultConfidence` (float), and `Rules` (array of rule detail records).

Each rule detail record SHALL contain: `Id` (string), `Priority` (int), `Confidence` (float?), `Strategy` (string), and a human-readable summary of its filters and identification spec.

#### Scenario: Message targets RuleSetManager

- **WHEN** `QueryRuleSetDetail("tatort")` is sent to the RuleSetManager
- **THEN** the Manager reads the JSON files for that ruleSetId and returns a `RuleSetDetailResult`

#### Scenario: Unknown ruleSetId

- **WHEN** `QueryRuleSetDetail("nonexistent")` is sent and the ruleSetId is not in KnownRuleSets
- **THEN** the Manager responds with `RuleSetNotFound("nonexistent")` which implements `IRuleSetResponse`

#### Scenario: RuleSetResolved implements IRuleSetResponse

- **WHEN** `RuleSetResolved` is returned from a `ResolveRuleSet` query
- **THEN** it SHALL be assignable to `IRuleSetResponse`

### Requirement: RuleSetDetailRule record
The `RuleSetDetailResult` SHALL contain an array of `RuleSetDetailRule` records. Each record SHALL expose: `Id` (string), `Priority` (int), `Confidence` (float?), `Strategy` (string -- the identification strategy as a human-readable string), `FilterSummary` (string? -- a textual summary of filter conditions), `SeasonPattern` (string?), `EpisodePattern` (string?), `MatchMode` (string?), and `TitleParts` (string[]? -- textual representations of title construction parts).

#### Scenario: RegexCapture rule detail
- **WHEN** a rule uses `IdentificationStrategy.RegexCapture` with `SeasonPattern: "/S(\\d+)/"` and `EpisodePattern: "/E(\\d+)/"`
- **THEN** the `RuleSetDetailRule` has `Strategy: "RegexCapture"`, `SeasonPattern: "/S(\\d+)/"`, `EpisodePattern: "/E(\\d+)/"`

#### Scenario: TitleConstruction rule detail
- **WHEN** a rule uses `IdentificationStrategy.TitleConstruction` with `TitleMatchMode.Exact` and 2 title parts
- **THEN** the `RuleSetDetailRule` has `Strategy: "TitleConstruction"`, `MatchMode: "Exact"`, and `TitleParts` with 2 entries

#### Scenario: AirdateExtraction rule detail
- **WHEN** a rule uses `IdentificationStrategy.AirdateExtraction`
- **THEN** the `RuleSetDetailRule` has `Strategy: "AirdateExtraction"` and null for pattern/title fields

#### Scenario: Rule with filters
- **WHEN** a rule has filters `all: [{ field: "channel", op: "eq", value: "Das Erste" }]`
- **THEN** the `RuleSetDetailRule` has a `FilterSummary` describing the conditions (e.g. `"all: channel eq 'Das Erste'"`)
