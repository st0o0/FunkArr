## MODIFIED Requirements

### Requirement: DownloadManager persistence path
The DownloadManager SHALL use `DataPaths.Database` for the Akka.Persistence SQLite database path instead of `FunkArrOptions.PersistencePath`.

#### Scenario: Persistence configuration
- **WHEN** the actor system configures Akka.Persistence
- **THEN** the SQLite connection string SHALL use `DataPaths.Database` as the database file path
