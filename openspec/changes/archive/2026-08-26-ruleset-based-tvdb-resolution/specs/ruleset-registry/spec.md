## MODIFIED Requirements

### Requirement: Query by topic

The system SHALL respond to GetRulesForTopic messages with the matching ruleset's rules, sorted by priority, and the resolved show name from the matching ruleset's MediaReference. Queries SHALL match against primary topic, aliases, and TVDB ID. The `RulesResponse` record SHALL include a `ResolvedShowName` field populated from `MediaReference.Name` when a match is found, or null when no match exists.

#### Scenario: Exact topic match with show name
- **WHEN** a query arrives for topic "heute-show"
- **THEN** the registry SHALL return all rules from the "heute-show" ruleset, sorted by priority ascending, with `ResolvedShowName` set to the MediaReference.Name of the matched ruleset

#### Scenario: TVDB ID match with show name
- **WHEN** a query arrives with `TvdbId = 83214` and the "Tatort" ruleset has `Media.TvdbId = 83214`
- **THEN** the registry SHALL return the Tatort rules with `ResolvedShowName = "Tatort"`

#### Scenario: Alias match with show name
- **WHEN** a query arrives for topic "Tatort aus Österreich" which is an alias for "Tatort"
- **THEN** the registry SHALL return the Tatort rules with `ResolvedShowName = "Tatort"`

#### Scenario: No match returns null name
- **WHEN** a query arrives for an unknown topic with no matching TVDB ID in any layer
- **THEN** the registry SHALL return an empty rules list with `ResolvedShowName = null`

#### Scenario: Auto-generation trigger preserves null name
- **WHEN** a query arrives for tvdbId 999999 with no existing ruleset and auto-generation is triggered
- **THEN** the registry SHALL return an empty rules list with `ResolvedShowName = null`
