# Setup Health Check

## MODIFIED Requirements

### Requirement: Directory checks
The setup health check SHALL verify both the `complete` and `incomplete` subdirectories under `DownloadPath` instead of the single `DownloadPath` directory.

#### Scenario: Both directories exist and are writable
- **WHEN** `GET /api/health/setup` is requested and both `{DownloadPath}/complete` and `{DownloadPath}/incomplete` exist and are writable
- **THEN** the `downloadDirectory` check SHALL have `"status": "ok"` and report both paths

#### Scenario: Complete directory not writable
- **WHEN** `{DownloadPath}/complete` does not exist or is not writable
- **THEN** the `downloadDirectory` check SHALL have `"status": "fail"` with a message identifying the complete directory

#### Scenario: Incomplete directory not writable
- **WHEN** `{DownloadPath}/incomplete` does not exist or is not writable
- **THEN** the `downloadDirectory` check SHALL have `"status": "fail"` with a message identifying the incomplete directory
