## ADDED Requirements

### Requirement: PersistedDownloadStatus enum
A PersistedDownloadStatus enum SHALL exist in FunkArr.Persistence with explicit
int values matching existing journal data.

#### Scenario: Enum values
- **WHEN** PersistedDownloadStatus is defined
- **THEN** it SHALL have values: Queued=0, Processing=1, Completed=2, Failed=3

## MODIFIED Requirements

### Requirement: Domain enum mappings use switch expressions
All domain enum mappings between Messages and Persistence types SHALL use
explicit switch expressions. Direct (int) casts between enum types SHALL NOT
be used.

#### Scenario: DownloadPriority mapping
- **WHEN** DownloadPriority is mapped to PersistedDownloadPriority
- **THEN** a switch expression SHALL map each value explicitly
- **AND** no (PersistedDownloadPriority)(int) cast SHALL be used

#### Scenario: DownloadStatus mapping
- **WHEN** DownloadStatus is mapped to PersistedDownloadStatus
- **THEN** a switch expression SHALL map each value explicitly

#### Scenario: New enum value added
- **WHEN** a new value is added to a domain enum without updating the mapping
- **THEN** the switch expression SHALL produce a compile-time warning or runtime exception
