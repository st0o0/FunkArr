## MODIFIED Requirements

### Requirement: Get show ruleset by TVDB ID
The `GET /api/v1/rulesets/{tvdbId}` endpoint SHALL return the full ruleset from the corresponding ShowActor. The response SHALL include the `channels` property from the `RuleSetFile`.

#### Scenario: Ruleset with channels
- **WHEN** `GET /api/v1/rulesets/83214` is requested and the ruleset has `channels: ["ARD"]`
- **THEN** the response SHALL include `channels: ["ARD"]` in the returned `RuleSetFile`

#### Scenario: Ruleset without channels
- **WHEN** `GET /api/v1/rulesets/329324` is requested and the ruleset has no channels
- **THEN** the response SHALL include `channels: null` or omit the field

### Requirement: List all rulesets
The `GET /api/v1/rulesets` endpoint SHALL return metadata for all known show rulesets. The `RulesetSummary` response SHALL include a `channels` field when available on the source `RuleSetFile`.

#### Scenario: Summary includes channels
- **WHEN** a `GET /api/v1/rulesets` request arrives and Tatort has `channels: ["ARD"]`
- **THEN** the corresponding summary SHALL include `channels: ["ARD"]`
