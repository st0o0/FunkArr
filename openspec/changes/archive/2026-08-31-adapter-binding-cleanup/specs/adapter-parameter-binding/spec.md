## ADDED Requirements

### Requirement: IndexerApi uses FromQuery request record
The IndexerApi endpoint SHALL bind query parameters via a `[AsParameters]` request record with `[FromQuery]` attributes instead of manual `HttpContext.Request.Query` extraction.

#### Scenario: Caps request with output format
- **WHEN** a GET request arrives with `?t=caps&o=json`
- **THEN** the `T` and `O` fields are bound from query parameters without manual parsing

#### Scenario: Search request with pagination
- **WHEN** a GET request arrives with `?t=search&q=test&offset=10&limit=50`
- **THEN** `Offset` and `Limit` are bound as `int?` values directly, no `int.TryParse` needed

#### Scenario: Get request with ID
- **WHEN** a GET request arrives with `?t=get&id=abc123`
- **THEN** `Id` is bound as `string?` from the query parameter

#### Scenario: Missing optional parameters
- **WHEN** a GET request arrives with only `?t=search`
- **THEN** optional parameters (`Q`, `Offset`, `Limit`, etc.) are null

### Requirement: DownloadApi GET uses FromQuery request record
The DownloadApi GET endpoint SHALL bind query parameters via a `[AsParameters]` request record with `[FromQuery]` attributes.

#### Scenario: Queue request with pagination
- **WHEN** a GET request arrives with `?mode=queue&start=5&limit=10`
- **THEN** `Mode`, `Start`, and `Limit` are bound from query parameters

#### Scenario: Delete queue item
- **WHEN** a GET request arrives with `?mode=queue&name=delete&value=abc123`
- **THEN** `Name` and `Value` are bound from query parameters

### Requirement: DownloadApi POST uses FromQuery request record
The DownloadApi POST endpoint SHALL bind query parameters via a `[AsParameters]` request record with `[FromQuery]` attributes for `mode`, `cat`, and `priority`.

#### Scenario: Add file with category
- **WHEN** a POST request arrives with `?mode=addfile&cat=sonarr&priority=High`
- **THEN** `Mode`, `Cat`, and `Priority` are bound from query parameters
- **AND** the NZB file is still read from the form body separately

### Requirement: No HttpContext for query parameter access
Endpoint handler methods SHALL NOT access `HttpContext.Request.Query` for parameter extraction. `HttpContext` MAY still be used for form file access in the POST handler.

#### Scenario: All query access through request records
- **WHEN** reviewing IndexerApi and DownloadApi endpoint code
- **THEN** no `context.Request.Query["..."]` calls exist for parameter extraction
