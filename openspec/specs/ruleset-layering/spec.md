# ruleset-layering

## Purpose

Defines how community and local rulesets for the same topic are merged into an effective ruleset at runtime.

## Requirements

### Requirement: RuleSet resolver merges community and local rulesets

The system SHALL provide a pure `RuleSetResolver.Resolve(RuleSet? community, RuleSet? local)` method that returns the effective `RuleSet` for a topic.

#### Scenario: Only community exists

- **WHEN** a community ruleset exists for topic "Tatort" and no local ruleset exists
- **THEN** the resolver SHALL return the community ruleset unchanged

#### Scenario: Only local exists

- **WHEN** a local ruleset exists for topic "Tatort" and no community ruleset exists
- **THEN** the resolver SHALL return the local ruleset unchanged

#### Scenario: Both null

- **WHEN** both community and local are null
- **THEN** the resolver SHALL return null

### Requirement: Local rulesets extend community by default

When both a community and a local ruleset exist for the same topic and the local ruleset does not declare `Standalone = true`, the system SHALL merge them using ID-based rule matching.

#### Scenario: Local adds new rules to community base

- **WHEN** community has rules `["season-episode", "title-fallback"]` and local has rule `["orf-airdate"]`
- **THEN** the effective ruleset SHALL contain all three rules: `["season-episode", "title-fallback", "orf-airdate"]`

#### Scenario: Local replaces community rule by same ID

- **WHEN** community has rule id `"season-episode"` with confidence 0.95 and local has rule id `"season-episode"` with confidence 0.98
- **THEN** the effective ruleset SHALL contain the local version of `"season-episode"` (confidence 0.98) and the community version SHALL be discarded

#### Scenario: Local disables community rule

- **WHEN** community has rules `["season-episode", "title-fallback"]` and local has `Disable = ["title-fallback"]`
- **THEN** the effective ruleset SHALL contain only `["season-episode"]`

#### Scenario: Combined replace, add, and disable

- **WHEN** community has rules `["season-episode", "title-fallback", "airdate"]`, local has rule `["season-episode", "orf-channel"]` and `Disable = ["airdate"]`
- **THEN** the effective ruleset SHALL contain `["season-episode" (local version), "title-fallback", "orf-channel"]` — airdate is disabled, season-episode is replaced, orf-channel is added

### Requirement: Standalone local rulesets replace community entirely

When a local ruleset declares `Standalone = true`, the system SHALL use only the local ruleset and ignore the community ruleset for that topic.

#### Scenario: Standalone local ignores community

- **WHEN** community has rules `["season-episode", "title-fallback"]` and local has `Standalone = true` with rules `["custom-rule"]`
- **THEN** the effective ruleset SHALL contain only `["custom-rule"]`

#### Scenario: Standalone with disable is valid but disable has no effect

- **WHEN** local has `Standalone = true` and `Disable = ["season-episode"]`
- **THEN** the effective ruleset SHALL use only the local rules (disable is ignored since there is no base to disable from)

### Requirement: Merged rules are sorted by priority

After merging, the system SHALL sort the effective rules by `Priority` ascending (0 = highest priority, evaluated first).

#### Scenario: Rules from different sources interleave by priority

- **WHEN** community has rule `"title-fallback"` at priority 10 and local adds rule `"orf-channel"` at priority 5
- **THEN** the effective rules SHALL be ordered: `["orf-channel" (5), "title-fallback" (10)]`

### Requirement: Aliases are union-merged

When merging, the system SHALL compute the union of community and local aliases, deduplicated.

#### Scenario: Aliases from both sources

- **WHEN** community has aliases `["Tatort - Münster", "Tatort - Schimanski"]` and local has aliases `["Tatort - Wien"]`
- **THEN** the effective aliases SHALL be `["Tatort - Münster", "Tatort - Schimanski", "Tatort - Wien"]`

#### Scenario: Duplicate aliases are deduplicated

- **WHEN** community has aliases `["Tatort - Münster"]` and local has aliases `["Tatort - Münster", "Tatort - Wien"]`
- **THEN** the effective aliases SHALL be `["Tatort - Münster", "Tatort - Wien"]`

#### Scenario: Local with no aliases inherits community aliases

- **WHEN** community has aliases `["Tatort - Münster"]` and local has no aliases (null)
- **THEN** the effective aliases SHALL be `["Tatort - Münster"]`

### Requirement: Confidence uses local-wins semantics

When merging, the system SHALL use the local `Confidence` value if present, otherwise fall back to the community value.

#### Scenario: Local overrides confidence

- **WHEN** community has confidence 0.9 and local has confidence 0.8
- **THEN** the effective confidence SHALL be 0.8

#### Scenario: Local has no confidence, inherits community

- **WHEN** community has confidence 0.9 and local has no confidence set
- **THEN** the effective confidence SHALL be 0.9

### Requirement: Media uses local-wins semantics

When merging, the system SHALL use the local `Media` value if present, otherwise fall back to the community value.

#### Scenario: Local overrides media

- **WHEN** community has media with tvdbId 83214 and local has media with tvdbId 99999
- **THEN** the effective media SHALL have tvdbId 99999

#### Scenario: Local has no media, inherits community

- **WHEN** community has media with tvdbId 83214 and local has no media (null)
- **THEN** the effective media SHALL have tvdbId 83214

### Requirement: Topic uses community canonical name

When merging, the system SHALL always use the community `Topic` value as the canonical topic name, regardless of what the local ruleset specifies.

#### Scenario: Local topic differs from community

- **WHEN** community has topic "Tatort" and local has topic "tatort"
- **THEN** the effective topic SHALL be "Tatort"

### Requirement: Disable references non-existent IDs without error

The system SHALL silently ignore IDs in `Disable` that do not exist in the community base. This allows local rulesets to remain valid even if the community ruleset removes a rule in a future update.

#### Scenario: Disable references removed community rule

- **WHEN** local has `Disable = ["old-rule"]` but community has no rule with id `"old-rule"`
- **THEN** the resolver SHALL produce the effective ruleset without error
