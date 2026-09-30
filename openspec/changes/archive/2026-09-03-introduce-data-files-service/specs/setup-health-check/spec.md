## MODIFIED Requirements

### Requirement: Data directory check
The endpoint SHALL verify that the directory at `DataPaths.DataRoot` exists and is writable using `IDataFiles.CanWrite()`.

#### Scenario: Data directory writable
- **WHEN** `IDataFiles.CanWrite(dataPaths.DataRoot)` returns true
- **THEN** the `dataDirectory` check has `"status": "ok"` and includes the resolved path

#### Scenario: Data directory not writable
- **WHEN** `IDataFiles.CanWrite(dataPaths.DataRoot)` returns false
- **THEN** the `dataDirectory` check has `"status": "fail"` and a message with the path

### Requirement: Directory checks
The setup health check SHALL verify both `DataPaths.Complete` and `DataPaths.Incomplete` directories using `IDataFiles.CanWrite()`.

#### Scenario: Both directories exist and are writable
- **WHEN** `GET /api/health/setup` is requested and `IDataFiles.CanWrite()` returns true for both `DataPaths.Complete` and `DataPaths.Incomplete`
- **THEN** the `completeDirectory` and `incompleteDirectory` checks SHALL have `"status": "ok"`

#### Scenario: Complete directory not writable
- **WHEN** `IDataFiles.CanWrite(dataPaths.Complete)` returns false
- **THEN** the `completeDirectory` check SHALL have `"status": "fail"` with a message identifying the path

#### Scenario: Incomplete directory not writable
- **WHEN** `IDataFiles.CanWrite(dataPaths.Incomplete)` returns false
- **THEN** the `incompleteDirectory` check SHALL have `"status": "fail"` with a message identifying the path
