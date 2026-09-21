# enrichment-ui-builder

## Purpose

Enrichment config section in the RuleSet Builder form -- UI controls for editing enrichment settings (enabled, methods, thresholds, tolerances, runtime mode) with conditional visibility and external ID hints.

## Requirements

### Requirement: Enrichment config section in RuleSet Builder
The RuleSet Builder form SHALL include an "Enrichment" section between the Default Confidence section and the Matching Rules section. The section SHALL be collapsible and expanded by default.

#### Scenario: New ruleset shows enrichment defaults
- **WHEN** user opens the RuleSet Builder to create a new ruleset
- **THEN** the enrichment section displays with defaults: enabled=true, methods=[Title, Airdate], title threshold=0.7, airdate tolerance=7, runtime tolerance=0.35, runtime mode=Tiebreaker, year tolerance=1

#### Scenario: Edit mode loads existing enrichment config
- **WHEN** user opens the RuleSet Builder in edit mode for a ruleset with custom enrichment config
- **THEN** the enrichment section displays the ruleset's current enrichment values

#### Scenario: Edit mode loads defaults when no enrichment config exists
- **WHEN** user opens the RuleSet Builder in edit mode for a ruleset without enrichment config in JSON
- **THEN** the enrichment section displays the same defaults as new ruleset creation

### Requirement: Enrichment enabled toggle
The enrichment section SHALL include a toggle to enable/disable enrichment for this ruleset.

#### Scenario: Disable enrichment
- **WHEN** user toggles enrichment to disabled
- **THEN** the method and threshold controls are visually dimmed but remain visible
- **THEN** the saved JSON includes `"enrichment": { "enabled": false }`

### Requirement: Enrichment method selection
The enrichment section SHALL include checkboxes for each enrichment method: Title, Airdate, Runtime, Year.

#### Scenario: Select methods
- **WHEN** user checks Title and Airdate methods
- **THEN** the saved JSON includes `"methods": ["title", "airdate"]`

#### Scenario: No methods selected
- **WHEN** user unchecks all methods
- **THEN** the methods array is empty in the saved JSON

### Requirement: Per-method threshold controls with conditional visibility
The enrichment section SHALL display threshold/tolerance inputs only for methods whose checkbox is checked. Title: threshold (0-1 float, step 0.05). Airdate: tolerance (integer days). Runtime: tolerance (0-1 float, step 0.05) and mode (select: Tiebreaker/Filter). Year: tolerance (integer years).

#### Scenario: Adjust title threshold
- **WHEN** user sets title threshold to 0.85
- **THEN** the saved JSON includes `"title": { "threshold": 0.85 }`

#### Scenario: Change runtime mode to Filter
- **WHEN** user selects runtime mode "Filter"
- **THEN** the saved JSON includes `"runtime": { "tolerance": 0.35, "mode": "filter" }`

#### Scenario: Unchecked method hides controls
- **WHEN** user unchecks the "Runtime" method
- **THEN** the runtime tolerance and mode inputs are hidden
- **THEN** the saved JSON omits the runtime object from methods array but preserves the threshold values for when the method is re-enabled

### Requirement: Enrichment config saved to disk
When the user saves the ruleset, the enrichment config SHALL be included in the serialized JSON written to the local rulesets directory.

#### Scenario: Save ruleset with enrichment config
- **WHEN** user edits enrichment settings and clicks Save
- **THEN** the local JSON file includes the `enrichment` object with all configured values

### Requirement: Missing external IDs hint
When enrichment is enabled but no tvdbId (for shows) or tmdbId/imdbId (for movies) is set, the enrichment section SHALL display a hint that enrichment requires external IDs.

#### Scenario: Show with enrichment enabled but no tvdbId
- **WHEN** mediaType is "show" and enrichment is enabled and tvdbId is empty
- **THEN** a hint is displayed: enrichment requires a TVDB ID to resolve episodes

#### Scenario: Movie with enrichment enabled but no tmdbId
- **WHEN** mediaType is "movie" and enrichment is enabled and both tmdbId and imdbId are empty
- **THEN** a hint is displayed: enrichment requires a TMDB or IMDB ID to resolve movies
