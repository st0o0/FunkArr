# storage-status Specification

## Purpose
TBD - created by archiving change dashboard-richness. Update Purpose after archive.
## Requirements
### Requirement: Storage status endpoint
The system SHALL respond to `GET /api/health/storage` with disk space information for the configured complete and incomplete directories.

#### Scenario: Storage info available
- **WHEN** `GET /api/health/storage` is requested and the configured directories exist
- **THEN** the response SHALL be JSON with `completeDirectory` and `incompleteDirectory` objects, each containing `path` (string), `availableBytes` (long), `totalBytes` (long)

#### Scenario: Directory not configured or missing
- **WHEN** a configured directory does not exist or the drive cannot be resolved
- **THEN** the corresponding object SHALL have `availableBytes` and `totalBytes` as 0

### Requirement: Dashboard storage indicator
The Dashboard SHALL display a storage usage indicator showing disk space for the complete directory.

#### Scenario: Storage displayed
- **WHEN** the Dashboard loads and storage info is available
- **THEN** a compact storage bar SHALL show used vs. total space with formatted labels (e.g., "142.3 GB / 500.0 GB")

#### Scenario: Storage loading
- **WHEN** the Dashboard is loading storage data
- **THEN** a skeleton placeholder SHALL be shown

#### Scenario: High disk usage
- **WHEN** disk usage exceeds 90%
- **THEN** the storage bar SHALL use a warning color (`status-warn`)

