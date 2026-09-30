# data-paths (delta)

## ADDED Requirements

### Requirement: Auto-create download directories at startup

DataPaths SHALL ensure the `Complete` and `Incomplete` directories exist when constructed. If they do not exist, they SHALL be created recursively.

#### Scenario: Directories created on fresh start

- **WHEN** DataPaths is constructed and `/shared/downloads/complete` does not exist
- **THEN** the directory SHALL be created recursively
- **AND** `/shared/downloads/incomplete` SHALL also be created if missing

#### Scenario: Directories already exist

- **WHEN** DataPaths is constructed and both directories already exist
- **THEN** no error SHALL occur (creation is idempotent)
