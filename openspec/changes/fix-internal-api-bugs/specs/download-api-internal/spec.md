## MODIFIED Requirements

### Requirement: Retry failed download endpoint
The system SHALL respond to `POST /api/downloads/{id}/retry` by sending exactly one `RetryDownload` to the DownloadManager. The endpoint SHALL NOT contact the DownloadHistoryManager; history cleanup is owned by the DownloadManager (see `download-retry`).

#### Scenario: Successful retry
- **WHEN** `POST /api/downloads/{id}/retry` is requested for a failed download
- **THEN** the system SHALL send `RetryDownload` to the Manager
- **AND** respond with HTTP 200 and JSON `{"success":true}`

#### Scenario: Retry failure keeps history
- **WHEN** `POST /api/downloads/{id}/retry` is requested and the Manager returns failure
- **THEN** the response SHALL be HTTP 400 with JSON `{"success":false,"error":"<message>"}`
- **AND** the download's history entry SHALL still exist
