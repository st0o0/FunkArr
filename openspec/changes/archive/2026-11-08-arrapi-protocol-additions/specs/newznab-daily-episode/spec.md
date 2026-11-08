## ADDED Requirements

### Requirement: Daily show episode parsing deferred to enrichment

Daily shows (e.g. Tagesschau, heute journal) use date-based episode identifiers (MMDD format). Sonarr sends `ep=1108` for November 8th, which already parses as integer 1108. The mismatch is in enrichment metadata: `Metadata.Episode` stores episode-of-season numbers (e.g. "17"), not MMDD format. Fixing this requires enrichment domain changes.

#### Scenario: MMDD episode parameter parses as integer

- **WHEN** `?t=tvsearch&season=2026&ep=1108` is requested
- **THEN** the `ep` value SHALL parse as integer 1108

#### Scenario: Filtering fails when metadata uses episode-of-season

- **WHEN** enriched metadata has `Episode="17"` and the request has `ep=1108`
- **THEN** the filter SHALL NOT match (known limitation until enrichment supports MMDD format)
