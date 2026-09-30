## MODIFIED Requirements

### Requirement: Rule structure
Each rule SHALL contain: **id (required string, stable identifier)**, priority (int, 0 = highest), filter group (FilterGroup with AND/OR/NOT composition), strategy (MatchingStrategy enum), optional confidence (float 0.0-1.0, overrides file-level default), optional seasonRegex (string), optional episodeRegex (string), optional captureGroup (int, default last group), and titleRules (array of TitleRule).

#### Scenario: Rule with ID
- **WHEN** a rule is deserialized from JSON `{ "id": "se-main", "priority": 0, "strategy": "seasonAndEpisodeNumber", ... }`
- **THEN** the `Rule` record SHALL have `Id = "se-main"`

#### Scenario: Missing ID fails deserialization
- **WHEN** a rule JSON has no `"id"` field
- **THEN** deserialization SHALL fail (Id is required)

#### Scenario: Multiple rules with priority ordering
- **WHEN** a ruleset has rules with priorities [10, 0, 5]
- **THEN** the matching engine SHALL evaluate them in order [0, 5, 10] and stop at the first match

#### Scenario: Rule-level confidence
- **WHEN** rule #1 has confidence 0.95 and rule #2 has no confidence set, and the file-level confidence is 0.7
- **THEN** rule #1 SHALL report confidence 0.95 and rule #2 SHALL report confidence 0.7

#### Scenario: Configurable capture group
- **WHEN** a rule has seasonRegex "S(\\d+)/E(\\d+)" with captureGroup=1
- **THEN** the engine SHALL use capture group 1 (season number), not the last group

### Requirement: Override configuration
Local and generated rulesets MAY include an overrides section specifying how to compose with lower-priority layers by adding, replacing, or removing individual rules by stable ID.

#### Scenario: Merge with Add rules
- **WHEN** a local ruleset has `overrides: { base: "community", add: [{ id: "mine-1", ... }] }`
- **THEN** the merge SHALL add the new rule to the community base, preserving all community rules

#### Scenario: Merge with Replace rules
- **WHEN** a local ruleset has `overrides: { base: "community", replace: [{ id: "airdate", ... }] }` and community has a rule with id "airdate"
- **THEN** the merge SHALL replace the community "airdate" rule with the local version

#### Scenario: Merge with Remove rules
- **WHEN** a local ruleset has `overrides: { base: "community", remove: ["airdate"] }` and community has a rule with id "airdate"
- **THEN** the merge SHALL remove the "airdate" rule from the effective set

#### Scenario: Standalone (no overrides, backward compat)
- **WHEN** a ruleset has no overrides section
- **THEN** the system SHALL treat it as a standalone layer (winner-takes-all, current behavior)

#### Scenario: Base references a layer
- **WHEN** `overrides.base` is `"community"`
- **THEN** the merge SHALL resolve the community layer as the base for this override

## REMOVED Requirements

### Requirement: Override configuration
**Reason**: Replaced by the new Override configuration with RuleSetLayer enum, separate Add/Replace/Remove lists keyed by rule ID, and removal of OverrideMode enum (Replace/Merge) and index-based Remove.
**Migration**: `OverrideMode` enum deleted. `OverrideConfig.Mode` removed. `OverrideConfig.Base` changes from `string?` to `RuleSetLayer`. `OverrideConfig.Remove` changes from `IReadOnlyList<int>` to `IReadOnlyList<string>` (by ID, not index). `OverrideConfig.Replace` added as new list.
