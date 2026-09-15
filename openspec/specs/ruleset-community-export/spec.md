## Purpose

Produces standalone, community-ready JSON files from local rulesets by flattening merged results and validating against the schema.

## Requirements

### Requirement: Flatten merged ruleset for export
The system SHALL produce a standalone, community-ready JSON file from any ruleset that has a local component. For merged rulesets (community + local), the export SHALL contain the fully resolved merge result.

#### Scenario: Export standalone local ruleset
- **WHEN** a ruleset exists only as a local file (no community counterpart)
- **THEN** the export returns the local JSON as-is

#### Scenario: Export merged ruleset
- **WHEN** a ruleset has both community and local files with local overriding 2 of 5 rules
- **THEN** the export contains all 5 rules (3 community originals + 2 local overrides) merged into a single ruleset

#### Scenario: Export standalone override
- **WHEN** a local ruleset has `standalone: true` and a community counterpart exists
- **THEN** the export contains only the local rules (community ignored), without the `standalone` field

#### Scenario: Strip override-specific fields
- **WHEN** a local ruleset contains `standalone` or `disable` fields
- **THEN** the exported JSON does not contain these fields

### Requirement: Export schema validation
The exported JSON SHALL be validated against the schema before being returned.

#### Scenario: Export passes validation
- **WHEN** a valid ruleset is exported
- **THEN** the export succeeds and returns the JSON

#### Scenario: Export fails validation
- **WHEN** the merged result would produce invalid JSON (edge case)
- **THEN** the export returns a 422 error with the validation errors

### Requirement: Export produces canonical JSON format
The exported JSON SHALL use consistent formatting suitable for community contribution.

#### Scenario: Formatted output
- **WHEN** a ruleset is exported
- **THEN** the JSON is indented with 2 spaces, properties are in schema order, and the file ends with a newline
