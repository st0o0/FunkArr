## ADDED Requirements

### Requirement: Failure pattern classification
The system SHALL provide a stateless failure pattern analysis service that classifies unmatched items into failure patterns based on their `ItemEvaluation` traces. The classification SHALL examine all rule evaluations per item and assign the most specific applicable pattern.

#### Scenario: Duration filter pattern
- **WHEN** an unmatched item failed a duration filter check on ALL rules (all rules have a duration filter and it failed)
- **THEN** the item SHALL be classified as `duration_filter` with summary "Duration too short — likely clips"

#### Scenario: Title format mismatch pattern
- **WHEN** an unmatched item passed filters on at least one rule but all title construction attempts returned null (regex miss)
- **THEN** the item SHALL be classified as `title_format` with summary "Title format doesn't match regex"

#### Scenario: No TVDB match pattern
- **WHEN** an unmatched item passed filters and title construction succeeded on at least one rule, but no TVDB episode matched the constructed title
- **THEN** the item SHALL be classified as `no_tvdb_match` with summary "Title matched but no TVDB episode found"

#### Scenario: Channel filter pattern
- **WHEN** an unmatched item failed a channel filter check on ALL rules
- **THEN** the item SHALL be classified as `channel_filter` with summary "Channel not in whitelist"

#### Scenario: Content type pattern (heuristic)
- **WHEN** an unmatched item has duration under 5 minutes AND the title contains keywords like "Highlights", "Zusammenfassung", "Best of", "Kompakt"
- **THEN** the item SHALL be classified as `content_type` with summary "Likely compilation or clip content"

#### Scenario: Uncategorized fallback
- **WHEN** an unmatched item does not match any specific failure pattern
- **THEN** the item SHALL be classified as `unknown` with summary "Could not determine failure reason"

### Requirement: Failure pattern suggestions
Each failure pattern SHALL include a human-readable suggestion for how to address it.

#### Scenario: Duration filter suggestion
- **WHEN** the pattern is `duration_filter`
- **THEN** the suggestion SHALL be "These items are very short. Consider adding their titles to the content filter if they are clips."

#### Scenario: Title format suggestion
- **WHEN** the pattern is `title_format` and the common missing element is identified (e.g., missing colon separator)
- **THEN** the suggestion SHALL describe the pattern mismatch (e.g., "Titles use 'ShowName ...' instead of 'ShowName: ...'")

#### Scenario: No TVDB match suggestion
- **WHEN** the pattern is `no_tvdb_match`
- **THEN** the suggestion SHALL be "Episode may not be in TVDB yet, or the constructed title doesn't match the TVDB episode name"

### Requirement: Failure pattern API endpoint
The system SHALL expose `GET /api/v1/rulesets/{tvdbId}/failures` returning classified failure patterns for a show's unmatched items.

#### Scenario: Failures with patterns
- **WHEN** `GET /api/v1/rulesets/83214/failures` is called and 12 unmatched items exist
- **THEN** the endpoint SHALL return `{ totalUnmatched: 12, patterns: [{ type, count, summary, suggestion, items: [...] }, ...] }` grouped by pattern type, sorted by count descending

#### Scenario: No unmatched items
- **WHEN** `GET /api/v1/rulesets/83214/failures` is called and no unmatched items exist
- **THEN** the endpoint SHALL return `{ totalUnmatched: 0, patterns: [] }`

#### Scenario: Unknown show
- **WHEN** `GET /api/v1/rulesets/999999/failures` is called
- **THEN** the endpoint SHALL return HTTP 404

### Requirement: Movie failure pattern API endpoint
The system SHALL expose `GET /api/v1/rulesets/movies/{id}/failures` returning failure patterns for a movie's unmatched items.

#### Scenario: Movie failures
- **WHEN** `GET /api/v1/rulesets/movies/tt0082096/failures` is called
- **THEN** the endpoint SHALL return failure patterns in the same format as shows
