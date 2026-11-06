# integration-test-arr Specification

## Purpose

Contract tests for the Newznab indexer API and SABnzbd download client API, verifying HTTP response shapes for Sonarr/Radarr/Prowlarr integration.

## Requirements

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

### Requirement: Feed-mode Newznab requests are tested across feed states
The contract tests for empty-criteria Newznab requests SHALL cover the combinations of feed state (empty, only TV entries, only movie entries, both) and probe type (`tvsearch`, `movie`, `search` with TV categories, `search` with movie categories). The simulated feed answer SHALL report the total across all media types and the filtered total separately, as the real feed store does.

#### Scenario: Probe for a media type the feed does not hold
- **WHEN** the simulated feed holds TV entries only (total 3, filtered total 0 for movies) and a movie probe is sent
- **THEN** the response SHALL contain exactly one item, the validation item, in the movie category

#### Scenario: Probe for a media type the feed holds
- **WHEN** the simulated feed holds TV entries only and a TV probe is sent
- **THEN** the response SHALL contain the feed entries and no validation item

#### Scenario: Total and filtered total differ
- **WHEN** the simulated feed answers with a total larger than the filtered total
- **THEN** the response `total` SHALL equal the filtered total
