## MODIFIED Requirements

### Requirement: DownloadHistoryManager trims records to MaxHistoryRecords

The DownloadHistoryManager SHALL trim its in-memory record list to `MaxHistoryRecords` (from DownloadOptions) after adding new records. Trimming SHALL remove the oldest records first (FIFO), but SHALL NOT trim records whose `CompletedAt` timestamp is within the last 30 minutes.

#### Scenario: Trim after exceeding max

- **WHEN** a `HistoryRecorded` event is applied and the record count exceeds MaxHistoryRecords
- **THEN** the oldest records SHALL be removed until the count equals MaxHistoryRecords or only age-protected records remain
- **AND** a `HistoryTrimmed(int Count)` event SHALL be persisted if any records were trimmed

#### Scenario: No trim when under max

- **WHEN** a `HistoryRecorded` event is applied and the record count is at or below MaxHistoryRecords
- **THEN** no trimming SHALL occur

#### Scenario: Default max records

- **WHEN** MaxHistoryRecords is not configured
- **THEN** the default value SHALL be 1000

#### Scenario: Recent records protected from trim

- **WHEN** trimming is needed and the oldest record has `CompletedAt` within the last 30 minutes
- **THEN** that record SHALL NOT be trimmed
- **AND** the record count MAY temporarily exceed MaxHistoryRecords

#### Scenario: Mixed old and recent records

- **WHEN** trimming is needed and some oldest records are older than 30 minutes while others are recent
- **THEN** only the records older than 30 minutes SHALL be trimmed
- **AND** recent records SHALL be preserved even if count still exceeds MaxHistoryRecords

#### Scenario: All records are recent

- **WHEN** trimming would be needed but all records have `CompletedAt` within the last 30 minutes
- **THEN** no records SHALL be trimmed
- **AND** no `HistoryTrimmed` event SHALL be persisted
