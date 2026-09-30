## MODIFIED Requirements

### Requirement: RuleSet detail page
The ruleset detail page SHALL use the shared `EmptyState` component when a ruleset has no rules defined. The empty state SHALL show a rules/list icon, a title indicating no rules are defined, and a description explaining that rules can be added via the editor.

#### Scenario: Detail with no rules
- **WHEN** the user views a ruleset that has zero matching rules
- **THEN** the matching rules section displays an EmptyState with icon, title, and description instead of plain text

#### Scenario: Detail with rules
- **WHEN** the user views a ruleset with one or more rules
- **THEN** the matching rules section displays collapsible rule cards as before (no change)
