## ADDED Requirements

### Requirement: Enrichment section in builder form
The RuleSet Builder form SHALL include an enrichment config section that allows editing all enrichment settings (enabled, methods, thresholds, tolerances, runtime mode).

#### Scenario: Enrichment section visible
- **WHEN** user opens the RuleSet Builder (create or edit)
- **THEN** an "Enrichment" section is visible between Default Confidence and Matching Rules

### Requirement: Enrichment config included in save
The serializeForm function SHALL include enrichment config in the request body sent to create/update endpoints.

#### Scenario: Save includes enrichment
- **WHEN** user modifies enrichment settings and saves the ruleset
- **THEN** the API request body includes the enrichment object with current values

### Requirement: Enrichment config loaded in edit mode
When loading a ruleset for editing, the builder SHALL populate the enrichment section from the detail API response.

#### Scenario: Load enrichment in edit mode
- **WHEN** user navigates to edit an existing ruleset
- **THEN** the enrichment section shows the ruleset's current enrichment config from the API
