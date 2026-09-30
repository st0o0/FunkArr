## MODIFIED Requirements

### Requirement: WorkerStatusResult uses typed WorkerStatus enum
WorkerStatusResult.Status SHALL be of type WorkerStatus (not int).

#### Scenario: Status field type
- **WHEN** DownloadWorker creates a WorkerStatusResult
- **THEN** the Status field SHALL be the WorkerStatus enum value directly
- **AND** no (int) cast SHALL be used
