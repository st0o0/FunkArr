## Purpose

Validates rulesets against an embedded JSON schema and business rules, providing structured, human-readable error messages.

## Requirements

### Requirement: Embedded JSON schema resource
The `FunkArr.RuleSet` project SHALL embed `data/community/ruleset.schema.json` as an assembly embedded resource. The schema SHALL be loaded once at startup and cached as a singleton.

#### Scenario: Schema available at runtime
- **WHEN** the application starts
- **THEN** `IRuleSetValidator` resolves from DI with the parsed schema ready for validation

#### Scenario: Schema matches source
- **WHEN** the project builds
- **THEN** the embedded resource is copied from `data/community/ruleset.schema.json` via a linked `<EmbeddedResource>` item

### Requirement: JSON schema validation
The `IRuleSetValidator` SHALL validate a ruleset JSON string against the embedded Draft 2020-12 schema and return all structural errors at once.

#### Scenario: Valid ruleset
- **WHEN** a structurally valid ruleset JSON is validated
- **THEN** validation returns no errors

#### Scenario: Missing required fields
- **WHEN** a ruleset JSON is missing the required `topic` field
- **THEN** validation returns an error with field path and a message indicating `topic` is required

#### Scenario: Wrong field types
- **WHEN** a ruleset JSON has `confidence` set to a string instead of a number
- **THEN** validation returns an error indicating the expected type

#### Scenario: Multiple errors returned at once
- **WHEN** a ruleset JSON has three structural errors (missing topic, invalid confidence type, malformed rule)
- **THEN** validation returns all three errors in a single response

### Requirement: Business rule validation
The `IRuleSetValidator` SHALL perform additional business rule checks beyond what the JSON schema covers.

#### Scenario: Confidence out of range
- **WHEN** a ruleset has `confidence` set to `1.5`
- **THEN** validation returns an error: "Confidence must be between 0 and 1"

#### Scenario: Invalid strategy
- **WHEN** a rule has `strategy` set to `"fuzzyMatch"`
- **THEN** validation returns an error listing the valid strategies: `seasonAndEpisodeNumber`, `byAbsoluteEpisodeNumber`, `itemTitleExact`, `itemTitleIncludes`, `itemTitleEqualsAirdate`

#### Scenario: Invalid regex pattern
- **WHEN** a title rule has `type` "regex" and `pattern` set to `"(unclosed"`
- **THEN** validation returns an error indicating the regex is invalid with the regex engine's error message

### Requirement: Human-readable error messages
Validation errors SHALL reference rules by their `id` field (or index if no id) and provide actionable fix instructions.

#### Scenario: Error references rule by ID
- **WHEN** rule with `id` "airdate" has an invalid filter
- **THEN** the error field reads `rules[airdate].filters.all[0].field` and the message explains what values are valid

#### Scenario: Error references rule by index when no ID
- **WHEN** the second rule (index 1) has no `id` field and has an error
- **THEN** the error field reads `rules[1].strategy` with the error message

#### Scenario: Error includes fix instruction
- **WHEN** a rule has an unsupported strategy
- **THEN** the message includes the list of supported strategies, not just "invalid value"
