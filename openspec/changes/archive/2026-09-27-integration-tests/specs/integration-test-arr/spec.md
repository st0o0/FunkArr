## ADDED Requirements

### Requirement: Newznab endpoints have contract tests
Every Newznab endpoint SHALL have at least one integration test verifying the HTTP response shape with typed assertions.

#### Scenario: Caps returns valid XML
- **WHEN** `GET /index/api?t=caps&apikey=test-key` is requested
- **THEN** the response SHALL be 200 with XML content type containing server and searching capabilities

#### Scenario: TV search returns RSS XML
- **WHEN** a tvsearch request is sent and the SearchManager probe responds with results
- **THEN** the response SHALL be RSS XML containing item elements with Newznab attributes

#### Scenario: Missing API key returns 401
- **WHEN** a Newznab request is sent without an apikey parameter
- **THEN** the response SHALL be 401 Unauthorized

#### Scenario: Wrong API key returns 401
- **WHEN** a Newznab request is sent with an incorrect apikey
- **THEN** the response SHALL be 401 Unauthorized

### Requirement: SABnzbd endpoints have contract tests
Every SABnzbd endpoint mode SHALL have at least one integration test verifying the HTTP response shape with typed assertions.

#### Scenario: Version returns typed response
- **WHEN** `GET /download/api?mode=version&apikey=test-key` is requested
- **THEN** the response SHALL be 200 with `{"version":"4.3.3"}`

#### Scenario: Queue with items returns SABnzbd-format slots
- **WHEN** queue mode is requested and the DownloadManager probe responds with queue items
- **THEN** the response SHALL contain `queue.slots` with `nzo_id`, `filename`, `percentage`, `status` fields

#### Scenario: Invalid mode returns typed error
- **WHEN** an unrecognized mode is requested
- **THEN** the response SHALL be 400 with `{"status":false,"error":"Invalid mode"}`

#### Scenario: Missing API key returns 401
- **WHEN** a SABnzbd request is sent without an apikey parameter
- **THEN** the response SHALL be 401 Unauthorized
