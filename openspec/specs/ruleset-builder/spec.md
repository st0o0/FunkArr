## Purpose

UI views for browsing, inspecting, editing, and testing rulesets, including a visual rule builder with filter groups, title rules, and live Mediathek testing.

## Requirements

### Requirement: Ruleset browser
The UI SHALL display a searchable list of all rulesets with topic name, source (community/generated/local), rule count, and match rate from the MatchLedger.

#### Scenario: List all rulesets
- **WHEN** the user navigates to the rulesets view
- **THEN** the UI SHALL display all registered topics with their source, rule count, and match rate

#### Scenario: Search rulesets
- **WHEN** the user types "tatort" in the search field
- **THEN** the list SHALL filter to topics containing "tatort" (case-insensitive)

#### Scenario: Low match rate indicator
- **WHEN** a topic has a match rate below 75%
- **THEN** the UI SHALL display a warning indicator next to that topic

#### Scenario: Local override indicator
- **WHEN** a topic has a local override
- **THEN** the UI SHALL display an edit indicator and show source as "local"

### Requirement: Ruleset detail view
The UI SHALL display a single ruleset's full configuration: media reference, rules with filters and title rules, per-rule hit counts, and unmatched items with failure reasons.

#### Scenario: Show rules
- **WHEN** the user opens the detail view for "heute journal"
- **THEN** the UI SHALL display each rule with its priority, strategy, filters, title rules, and hit count from the MatchLedger

#### Scenario: Show unmatched items
- **WHEN** the topic has 4 unmatched items
- **THEN** the detail view SHALL list each unmatched item with its title, duration, and per-rule failure reasons (filter-failed, strategy-no-match)

#### Scenario: Show media reference
- **WHEN** the ruleset has a TVDB ID and media name
- **THEN** the detail view SHALL display the media reference (TVDB ID, name, type)

### Requirement: Ruleset editor for local overrides
The UI SHALL provide an editor for creating and modifying local ruleset overrides. The editor SHALL support both replace mode (full custom ruleset) and merge mode (add/remove rules on top of community/generated base).

#### Scenario: Create local override
- **WHEN** the user clicks "Create Local Override" on a community ruleset
- **THEN** the editor SHALL open with the community rules pre-filled and mode set to "replace"

#### Scenario: Merge mode override
- **WHEN** the user selects merge mode
- **THEN** the editor SHALL show which rules are from the base and allow adding new rules or marking base rules for removal

#### Scenario: Edit existing local override
- **WHEN** the user clicks edit on a topic with an existing local override
- **THEN** the editor SHALL load the current local override for editing

#### Scenario: Delete local override
- **WHEN** the user clicks "Delete Override" on a local ruleset
- **THEN** the system SHALL delete the local file and fall back to community/generated

### Requirement: New ruleset from scratch
The UI SHALL allow creating a new ruleset for a topic that has no existing rules in any layer.

#### Scenario: Create new ruleset
- **WHEN** the user navigates to `/rulesets/new`
- **THEN** the editor SHALL present empty fields for topic, media reference, and rules

#### Scenario: TVDB lookup
- **WHEN** the user enters a TVDB ID and clicks "Lookup"
- **THEN** the system SHALL fetch the show name and populate the media reference fields

### Requirement: Filter group editor
The editor SHALL provide a visual builder for filter groups supporting All, Any, and Not logical operators, with each filter having field, operator, and value inputs.

#### Scenario: Add filter
- **WHEN** the user clicks "Add Filter" in a rule
- **THEN** a new filter row SHALL appear with dropdowns for field (duration, title, description, topic, channel), operator (GreaterThan, LessThan, ExactMatch, Contains, Regex, Eq, NotContains), and a text input for value

#### Scenario: Nested filter groups
- **WHEN** the user adds filters to the "All" group and the "Not" group
- **THEN** the filter group SHALL evaluate as: all "All" filters must pass AND none of the "Not" filters may pass

#### Scenario: Remove filter
- **WHEN** the user clicks remove on a filter
- **THEN** that filter SHALL be removed from the group

### Requirement: Title rule editor
The editor SHALL provide inputs for title rules of type regex (field, pattern, capture group) and static (literal value).

#### Scenario: Add regex title rule
- **WHEN** the user adds a regex title rule
- **THEN** the editor SHALL show inputs for field, pattern, and optional capture group number

#### Scenario: Add static title rule
- **WHEN** the user adds a static title rule
- **THEN** the editor SHALL show an input for the literal value

### Requirement: Live test against Mediathek
The editor SHALL provide a "Test" button that sends the current rules to the backend for evaluation against real Mediathek data and displays the match results.

#### Scenario: Test succeeds
- **WHEN** the user clicks "Test" with valid rules for topic "Tatort"
- **THEN** the UI SHALL display matched items (with season/episode), filtered items (with reason), and unmatched items (with per-rule failure details)

#### Scenario: Test with no matches
- **WHEN** the rules fail to match any Mediathek items
- **THEN** the UI SHALL display all items as unmatched with their failure reasons

### Requirement: Save ruleset
The editor SHALL save the ruleset to the local layer via the backend API and trigger a registry reload.

#### Scenario: Save new ruleset
- **WHEN** the user clicks "Save" on a new ruleset
- **THEN** the system SHALL write the ruleset to `data/rulesets/local/` and reload the registry

#### Scenario: Save override
- **WHEN** the user clicks "Save" on an override
- **THEN** the system SHALL write the override to `data/rulesets/local/` and reload the registry
