## MODIFIED Requirements

### Requirement: SearchResultItem contains scored media information

SearchResultItem SHALL carry all information needed by the Newznab response formatter, including optional media IDs for *arr result matching.

#### Scenario: SearchResultItem fields

- **WHEN** a search result item is created
- **THEN** it SHALL contain: Title (string), Channel (string), Topic (string), Url (string), Duration (int), Size (long), Quality (int), AiredAt (DateTimeOffset?), Score (double), TvdbId (int?), ImdbId (string?), TmdbId (int?)

### Requirement: ResolveRuleSet supports ID-based lookup

The `ResolveRuleSet` message SHALL accept optional media ID fields alongside the existing topic/alias string for ID-based resolution.

#### Scenario: ResolveRuleSet with IDs

- **WHEN** a resolve request is constructed
- **THEN** `ResolveRuleSet` SHALL contain: TopicOrAlias (string?), TvdbId (int?), ImdbId (string?), TmdbId (int?)

### Requirement: RuleSetResolved includes topic

The `RuleSetResolved` response SHALL include the topic string so callers can use it for subsequent queries without a second resolver round-trip.

#### Scenario: RuleSetResolved fields

- **WHEN** a ruleset is resolved
- **THEN** `RuleSetResolved` SHALL contain: RuleSetId (string), Topic (string)

### Requirement: RegisterRuleSet includes media IDs

The `RegisterRuleSet` message SHALL include optional media ID fields for ID-based resolver indexing.

#### Scenario: RegisterRuleSet fields

- **WHEN** a ruleset is registered
- **THEN** `RegisterRuleSet` SHALL contain: RuleSetId (string), Topic (string), Aliases (string[]), TvdbId (int?), ImdbId (string?), TmdbId (int?)
