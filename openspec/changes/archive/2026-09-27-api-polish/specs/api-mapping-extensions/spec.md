## MODIFIED Requirements

### Requirement: Mapping extensions have test coverage
All API mapping extension classes SHALL have unit tests verifying correct field mapping from domain types to API models.

#### Scenario: DownloadMapping produces correct API model
- **WHEN** a domain `QueueItem` is mapped via `ToApi()`
- **THEN** the resulting API model SHALL have all fields correctly populated

#### Scenario: MediathekMapping produces correct API model
- **WHEN** a domain `MediathekItem` is mapped via the mapping extension
- **THEN** the resulting API model SHALL have all fields correctly populated

#### Scenario: RuleSetMapping produces correct API model
- **WHEN** a domain `RuleSetDetailResult` is mapped via `ToApi()`
- **THEN** the resulting API model SHALL have all fields correctly populated

#### Scenario: ScoringMapping produces correct API model
- **WHEN** a domain `ScoringDetailResult` is mapped via `ToApi()`
- **THEN** the resulting API model SHALL have all fields correctly populated

#### Scenario: TestScoreMapping produces correct API model
- **WHEN** a domain test score result is mapped via the mapping extension
- **THEN** the resulting API model SHALL have all fields correctly populated
