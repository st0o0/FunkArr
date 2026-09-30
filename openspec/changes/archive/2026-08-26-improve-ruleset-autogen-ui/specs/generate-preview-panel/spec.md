## ADDED Requirements

### Requirement: Generate preview panel component
The system SHALL provide a `GeneratePreviewPanel` Vue component that displays auto-generated rules with test traces inline on the RulesetDetail page. The panel SHALL be shown after the user clicks "Generate Rules" and the preview endpoint returns.

#### Scenario: Panel renders generated rules
- **WHEN** the generate preview returns a `GeneratePreviewResult` with rules and traces
- **THEN** the panel SHALL render each generated rule using the existing `RuleCard` component, showing strategy, filters, regex patterns, and confidence

#### Scenario: Panel renders test trace summary
- **WHEN** the preview result includes `ItemEvaluation[]` traces
- **THEN** the panel SHALL display a summary bar showing matched count (green), filtered count (neutral), and unmatched count (amber), followed by a collapsible detail section

#### Scenario: Panel shows confidence score
- **WHEN** the preview result includes a confidence value
- **THEN** the panel SHALL display the confidence as a percentage with color coding (green >= 0.7, amber >= 0.4, red < 0.4)

### Requirement: Generate preview accept/discard actions
The panel SHALL provide Accept, Edit, and Discard buttons. Accept calls `/generate/apply` with the generated ruleset. Edit navigates to the RulesetEditor pre-filled with the generated rules. Discard closes the panel.

#### Scenario: User accepts generated rules
- **WHEN** the user clicks "Accept" in the preview panel
- **THEN** the system SHALL POST to `/api/v1/generate/apply` with the generated `RuleSetFile` and the correct entity ID and type, then reload the detail page

#### Scenario: User edits generated rules
- **WHEN** the user clicks "Edit" in the preview panel
- **THEN** the system SHALL navigate to the RulesetEditor route with the generated `RuleSetFile` pre-populated in the editor form

#### Scenario: User discards generated rules
- **WHEN** the user clicks "Discard" in the preview panel
- **THEN** the panel SHALL close without making any API calls, returning to the normal detail view

### Requirement: Universal generate button on RulesetDetail
The RulesetDetail page SHALL show a "Generate Rules" button for all rulesets regardless of source type (community, generated, or local). Clicking it SHALL call the preview endpoint and open the GeneratePreviewPanel.

#### Scenario: Generate button on community ruleset
- **WHEN** viewing a RulesetDetail page for a community ruleset (source = "community")
- **THEN** a "Generate Rules" button SHALL be visible in the action bar

#### Scenario: Generate button on local override
- **WHEN** viewing a RulesetDetail page for a local override (source = "local")
- **THEN** a "Generate Rules" button SHALL be visible in the action bar

#### Scenario: Generate button triggers preview
- **WHEN** the user clicks "Generate Rules" on any detail page
- **THEN** the system SHALL POST to `/api/v1/generate/preview` with the correct type and entity ID, show a loading indicator, and display the GeneratePreviewPanel when the response arrives
