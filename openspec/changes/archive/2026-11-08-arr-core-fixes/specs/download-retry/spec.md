## MODIFIED Requirements

### Requirement: SABnzbd retry removes history entry only on success
The SABnzbd retry handler SHALL remove the history entry only after the DownloadManager confirms the retry was successfully re-queued.

#### Scenario: Successful retry
- **WHEN** a `retry` request is received for a failed download ID
- **THEN** the handler SHALL send a retry command to the DownloadManager
- **AND** await the response
- **AND** only upon success, send `RemoveHistoryEntry` to the DownloadHistoryManager
- **AND** return a success response

#### Scenario: Failed retry
- **WHEN** a `retry` request is received for a download ID
- **AND** the DownloadManager returns a failure (not found, already queued, etc.)
- **THEN** the handler SHALL NOT remove the history entry
- **AND** return an error response with the failure reason
