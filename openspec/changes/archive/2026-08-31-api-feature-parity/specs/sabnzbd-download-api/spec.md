# SABnzbd Download API — Delta

## ADDED Requirements

### Requirement: Full status endpoint
The system SHALL respond to `GET /download/api?mode=fullstatus` with a JSON status object. This endpoint is called by Sonarr/Radarr during connection testing.

#### Scenario: Full status response structure
- **WHEN** `?mode=fullstatus` is requested
- **THEN** the response SHALL be JSON with a `status` object containing `paused` (bool, default false), `speedlimit` (string, default ""), `diskspace1` (string, free GB), `diskspace2` (string, free GB), and `completedir` (string, configured download path)

#### Scenario: Skip dashboard parameter accepted
- **WHEN** `?mode=fullstatus&skip_dashboard=1` is requested
- **THEN** the response SHALL be the same as without `skip_dashboard` (parameter accepted but ignored)

### Requirement: Queue delete subcommand
The system SHALL respond to `GET /download/api?mode=queue&name=delete&value=<nzo_id>` by removing the item from the download queue.

#### Scenario: Successful queue item deletion
- **WHEN** `?mode=queue&name=delete&value=existing-id` is requested
- **THEN** the response SHALL be JSON `{"status":true}` and the item SHALL be removed from the queue

#### Scenario: Queue delete with file removal
- **WHEN** `?mode=queue&name=delete&value=existing-id&del_files=1` is requested
- **THEN** the item SHALL be removed from the queue (file deletion is a no-op for stubbed implementation)

#### Scenario: Queue delete non-existent item
- **WHEN** `?mode=queue&name=delete&value=non-existent-id` is requested
- **THEN** the response SHALL be JSON `{"status":false,"error":"Item not found"}`

### Requirement: Retry failed download
The system SHALL respond to `GET /download/api?mode=retry&value=<nzo_id>` by re-queuing a failed download from history.

#### Scenario: Successful retry
- **WHEN** `?mode=retry&value=failed-item-id` is requested and the item exists in history with status "Failed"
- **THEN** the response SHALL be JSON `{"status":true}` and the item SHALL be moved back to the queue with status "Queued"

#### Scenario: Retry non-existent item
- **WHEN** `?mode=retry&value=non-existent-id` is requested
- **THEN** the response SHALL be JSON `{"status":false,"error":"Item not found"}`

#### Scenario: Retry non-failed item
- **WHEN** `?mode=retry&value=completed-item-id` is requested and the item has status "Completed"
- **THEN** the response SHALL be JSON `{"status":false,"error":"Item is not failed"}`

### Requirement: Queue and history pagination
The system SHALL accept `start` (int, default 0) and `limit` (int, default 50) query parameters on queue and history endpoints.

#### Scenario: Paginated queue
- **WHEN** `?mode=queue&start=5&limit=10` is requested
- **THEN** the response SHALL contain at most 10 queue slots starting from index 5

#### Scenario: Paginated history
- **WHEN** `?mode=history&start=0&limit=25` is requested
- **THEN** the response SHALL contain at most 25 history slots starting from index 0

#### Scenario: Default pagination
- **WHEN** queue or history is requested without `start`/`limit`
- **THEN** the system SHALL return all items (start=0, limit=unlimited for stub)

## MODIFIED Requirements

### Requirement: Add file endpoint
The system SHALL respond to `POST /download/api?mode=addfile&cat=<category>` by accepting an NZB file as a multipart/form-data upload (field name `nzbfile`), parsing the download URL and title from XML comments, and adding the item to the download queue. It SHALL accept optional `priority` (int) query parameter.

#### Scenario: Successful addfile via multipart
- **WHEN** a valid NZB is POSTed as multipart/form-data with field `nzbfile` and `?mode=addfile&cat=sonarr`
- **THEN** the response SHALL be JSON `{"status":true,"nzo_ids":["<generated-id>"]}` and the item SHALL appear in the queue

#### Scenario: Missing NZB file
- **WHEN** a POST is made with `?mode=addfile` but no `nzbfile` form field
- **THEN** the response SHALL be JSON `{"status":false,"error":"No NZB file uploaded"}` with HTTP 400

#### Scenario: Invalid NZB format
- **WHEN** the uploaded NZB file does not contain parseable URL/title comments
- **THEN** the response SHALL be JSON `{"status":false,"error":"Invalid NZB format"}` with HTTP 400

### Requirement: Queue endpoint
The system SHALL respond to `GET /download/api?mode=queue` with a JSON object wrapping the current download queue. It SHALL accept optional `start` (int), `limit` (int), and `name` (string, subcommand) parameters.

#### Scenario: Empty queue
- **WHEN** no downloads are in progress
- **THEN** the response SHALL be JSON `{"queue":{"paused":false,"speedlimit":"","noofslots_total":0,"diskspace1":"0","diskspace2":"0","speed":"0","slots":[]}}`

#### Scenario: Queue item structure
- **WHEN** a download is in the queue
- **THEN** each slot SHALL contain `nzo_id` (string), `status` (string: "Queued", "Downloading", "Extracting"), `index` (int), `timeleft` (string), `mb` (string, total MB), `filename` (string), `cat` (string, category), `mbleft` (string, remaining MB), `percentage` (string, 0-100), `priority` (string, default "Normal")

### Requirement: History endpoint
The system SHALL respond to `GET /download/api?mode=history` with a JSON object wrapping the download history. It SHALL accept optional `start` (int), `limit` (int), and `name` (string, subcommand) parameters.

#### Scenario: Empty history
- **WHEN** no downloads have completed
- **THEN** the response SHALL be JSON `{"history":{"noofslots":0,"slots":[]}}`

#### Scenario: History item structure
- **WHEN** a download is in history
- **THEN** each slot SHALL contain `nzo_id` (string), `name` (string), `nzb_name` (string), `category` (string), `bytes` (long), `download_time` (int, seconds), `storage` (string, file path), `status` (string: "Completed", "Failed"), `fail_message` (string, empty for non-failed), `completed_on` (long, Unix timestamp)

### Requirement: Unknown mode
The system SHALL return HTTP 400 for unrecognized `mode` parameter values, including when `mode=queue` or `mode=history` receives an unrecognized `name` subcommand.

#### Scenario: Unknown mode
- **WHEN** `?mode=unknown` is requested
- **THEN** the response SHALL be JSON `{"status":false,"error":"Invalid mode"}` with HTTP 400

#### Scenario: Unknown queue subcommand
- **WHEN** `?mode=queue&name=unknown` is requested
- **THEN** the response SHALL be JSON `{"status":false,"error":"Invalid queue command"}` with HTTP 400
