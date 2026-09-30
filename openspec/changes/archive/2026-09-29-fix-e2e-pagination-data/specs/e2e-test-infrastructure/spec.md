## ADDED Requirements

### Requirement: E2E history pagination data generation

The E2E test plan SHALL generate at least 26 download history entries before
running history pagination tests. The entries SHALL include both "show" and
"movie" categories to enable category filter verification.

#### Scenario: History entries exceed page size
- **WHEN** the E2E test reaches the history pagination verification step
- **THEN** the download history contains at least 26 entries (exceeding pageSize=25)

#### Scenario: Mixed categories present
- **WHEN** the E2E test reaches the category filter test
- **THEN** the download history contains entries in both "show" and "movie" categories

### Requirement: E2E scoring pagination data generation

The E2E test plan SHALL generate at least 21 scoring history entries for the
Tatort ruleset before running scoring pagination tests. Searches SHALL target
episodes across multiple seasons to avoid pagination cache deduplication.

#### Scenario: Scoring entries exceed page size
- **WHEN** the E2E test reaches the scoring pagination verification step
- **THEN** the Tatort scoring history contains at least 21 entries (exceeding pageSize=20)
