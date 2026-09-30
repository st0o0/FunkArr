## MODIFIED Requirements

### Requirement: Recent matches view
The UI SHALL display recent search evaluations, showing search topic, timestamp, matched/filtered/unmatched counts derived from the items array by outcome. Each record SHALL link to the match detail view at `/matches/:id`.

#### Scenario: Show recent matches
- **WHEN** the user navigates to the matches view
- **THEN** the UI SHALL display the most recent search evaluations with topic, timestamp, and result counts computed from `items.filter(i => i.outcome === 0).length` etc.

#### Scenario: Navigate to match detail
- **WHEN** the user clicks a search evaluation record
- **THEN** the UI SHALL navigate to `/matches/:id` to show the full evaluation visualization
