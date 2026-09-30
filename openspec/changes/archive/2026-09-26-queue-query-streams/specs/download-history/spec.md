## MODIFIED Requirements

### Requirement: HistoryRecorded uses PersistedDownloadStatus enum
HistoryRecorded.Status SHALL be of type PersistedDownloadStatus (not int).
The enum values SHALL match the existing int values for backward compatibility.

#### Scenario: Recording a completed download
- **WHEN** DownloadHistoryManager persists a HistoryRecorded event
- **THEN** Status SHALL be PersistedDownloadStatus.Completed (value 2)
- **AND** no (int) cast SHALL be used

#### Scenario: Recovering old events
- **WHEN** a HistoryRecorded event with int Status value is deserialized
- **THEN** it SHALL deserialize to the matching PersistedDownloadStatus enum value
