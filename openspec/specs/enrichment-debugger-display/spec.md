# enrichment-debugger-display

## Purpose

Enrichment trace display in the LiveMatchPreview full test mode -- shows enrichment results for matched items below the rule pipeline trace, with method badges, confidence, and resolution details.

## Requirements

### Requirement: Enrichment trace in full test results
The LiveMatchPreview full test mode SHALL display enrichment results for each matched item below the rule pipeline trace, when enrichment data is present in the response.

#### Scenario: Matched item with title match enrichment
- **WHEN** an item has EnrichmentTrace with method=TitleMatch, enriched=true, confidence=0.85, resolvedTitle="Der Fall"
- **THEN** the enrichment section shows: green indicator, "Title Match" method badge, confidence 0.85, resolved title "Der Fall", and season/episode if present

#### Scenario: Matched item with airdate match enrichment
- **WHEN** an item has EnrichmentTrace with method=AirdateMatch, enriched=true, daysDiff=2
- **THEN** the enrichment section shows: green indicator, "Airdate Match" method badge, confidence, and "2 days difference" detail

#### Scenario: Matched item with regex-extracted lookup
- **WHEN** an item has EnrichmentTrace with method=RegexExtracted, enriched=true
- **THEN** the enrichment section shows: a distinct style (not a match attempt), "Confirmed via TVDB" label, and the resolved episode name

#### Scenario: Matched item where enrichment failed to resolve
- **WHEN** an item has EnrichmentTrace with enriched=false and detail="similarity 0.65 < threshold 0.7"
- **THEN** the item shows an amber "not enriched" indicator with the detail string visible

#### Scenario: Matched item without enrichment data
- **WHEN** a matched item has null EnrichmentTrace
- **THEN** no enrichment section is shown for that item

### Requirement: Enrichment config wired to test request
The full test button in LiveMatchPreview SHALL include the builder's current enrichment config and identity (tvdbId, tmdbId, imdbId, mediaType) in the test request.

#### Scenario: Full test with enrichment
- **WHEN** user clicks "Full Test" and enrichment is enabled with external IDs set
- **THEN** the test request includes the current enrichment config and identity fields
- **THEN** results show both scoring traces and enrichment results

#### Scenario: Full test without external IDs
- **WHEN** user clicks "Full Test" and no external IDs are set
- **THEN** the test request omits identity fields
- **THEN** results show scoring traces only, no enrichment

### Requirement: Two-phase loading indicator
The full test button SHALL show phased progress during the test request to distinguish scoring from enrichment latency.

#### Scenario: Loading with enrichment enabled
- **WHEN** user clicks "Full Test" with enrichment enabled
- **THEN** the button shows "Scoring..." initially then "Enriching..." during the enrichment phase

#### Scenario: Loading without enrichment
- **WHEN** user clicks "Full Test" without enrichment
- **THEN** the button shows "Testing..." as before (single phase)

### Requirement: Re-test reflects enrichment changes
When the user changes enrichment thresholds and re-runs the full test, the updated enrichment config SHALL be sent and results SHALL reflect the new thresholds.

#### Scenario: Change threshold and re-test
- **WHEN** user changes title threshold from 0.7 to 0.9 and clicks "Full Test" again
- **THEN** enrichment runs with the new threshold
- **THEN** items that previously matched at 0.75 similarity now show as "not enriched" with detail
