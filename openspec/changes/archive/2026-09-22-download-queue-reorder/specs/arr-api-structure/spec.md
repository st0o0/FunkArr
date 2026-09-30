# Arr API Structure (Delta)

## ADDED Requirements

### Requirement: SABnzbd queue manipulation operations
The SABnzbd API queue mode handler SHALL support `name=priority` and `name=switch` operations in addition to the existing `name=delete`.

#### Scenario: Priority operation
- **WHEN** GET `/download/api?mode=queue&name=priority&value={nzo_id}&value2={priority_int}`
- **THEN** the download's priority SHALL be changed according to the SABnzbd priority mapping

#### Scenario: Switch operation
- **WHEN** GET `/download/api?mode=queue&name=switch&value={nzo_id1}&value2={nzo_id2}`
- **THEN** the two downloads SHALL be swapped

#### Scenario: Unknown operation
- **WHEN** GET `/download/api?mode=queue&name=unknown`
- **THEN** the response SHALL indicate an error

### Requirement: DownloadGetRequest value2 parameter
The `DownloadGetRequest` model SHALL include a `Value2` property bound to the `value2` query parameter for SABnzbd switch and priority operations.

#### Scenario: Value2 binding
- **WHEN** a request contains `&value2=xyz`
- **THEN** `DownloadGetRequest.Value2` SHALL be `"xyz"`
