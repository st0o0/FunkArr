## ADDED Requirements

### Requirement: Enrichment config in RuleSet Detail view
The RuleSetDetail view SHALL display enrichment config in a read-only section, showing enabled/disabled status, active methods, and key threshold values.

#### Scenario: Detail view shows enrichment config
- **WHEN** user views a ruleset detail page
- **THEN** an enrichment section shows: enabled/disabled badge, active methods as tags, and threshold values for active methods

#### Scenario: Detail view with enrichment disabled
- **WHEN** a ruleset has enrichment disabled
- **THEN** the enrichment section shows a "disabled" badge and no method details

### Requirement: Enrichment results in full test mode
The LiveMatchPreview full test mode SHALL display enrichment results for matched items when enrichment data is present in the test response.

#### Scenario: Show enrichment trace for resolved item
- **WHEN** a matched item has an EnrichmentTrace with enriched=true
- **THEN** the item's expanded view shows: resolved title, season/episode, match method, and confidence below the rule pipeline

#### Scenario: Show unresolved indicator
- **WHEN** a matched item has an EnrichmentTrace with enriched=false
- **THEN** the item shows an amber "not enriched" indicator

#### Scenario: No enrichment data
- **WHEN** a matched item has null EnrichmentTrace
- **THEN** no enrichment section is shown

### Requirement: Test request includes enrichment from builder
The LiveMatchPreview full test SHALL include the builder's current enrichment config and identity fields in the test API request.

#### Scenario: Full test sends enrichment config
- **WHEN** user clicks "Full Test" with enrichment enabled and tvdbId set
- **THEN** the POST /api/rulesets/test request includes enrichment config, tvdbId, and mediaType
