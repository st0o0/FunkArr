# Newznab Indexer API — Delta

## ADDED Requirements

### Requirement: Standard NZB download by GUID
The system SHALL respond to `GET /index/api?t=get&id=<guid>` with a minimal NZB XML file. The GUID SHALL be a base64-encoded string containing the download URL and title separated by a pipe character (`|`).

#### Scenario: Valid GUID download
- **WHEN** `?t=get&id=<base64>` is requested with a valid base64-encoded GUID
- **THEN** the response SHALL be `application/x-nzb` with a valid NZB XML containing the URL and title as XML comments

#### Scenario: Missing id parameter
- **WHEN** `?t=get` is requested without an `id` parameter
- **THEN** the system SHALL return Newznab error XML with code 200 (missing parameter)

#### Scenario: Invalid GUID encoding
- **WHEN** `?t=get&id=<invalid>` is requested with non-base64 content
- **THEN** the system SHALL return Newznab error XML with code 201 (incorrect parameter)

### Requirement: Newznab error XML format
The system SHALL return errors as XML `<error code="X" description="Y"/>` using standard Newznab error codes.

#### Scenario: Invalid API key error
- **WHEN** a request is made with an invalid or missing `apikey` parameter
- **THEN** the response SHALL be `application/xml` with `<error code="100" description="Invalid API Key"/>` and HTTP 403

#### Scenario: Missing parameter error
- **WHEN** a required parameter is missing (e.g., `t=get` without `id`)
- **THEN** the response SHALL be `<error code="200" description="Missing parameter"/>` with HTTP 400

#### Scenario: Incorrect parameter error
- **WHEN** a parameter value is malformed (e.g., invalid base64 in `id`)
- **THEN** the response SHALL be `<error code="201" description="Incorrect parameter"/>` with HTTP 400

#### Scenario: Function undefined error
- **WHEN** an unrecognized `t` parameter value is provided
- **THEN** the response SHALL be `<error code="202" description="No such function"/>` with HTTP 400

### Requirement: Search pagination parameters
The system SHALL accept `offset` (int, default 0) and `limit` (int, default 5000) query parameters on all search endpoints (`t=search`, `t=tvsearch`, `t=movie`).

#### Scenario: Pagination parameters accepted
- **WHEN** `?t=tvsearch&q=Tatort&offset=10&limit=25` is requested
- **THEN** the response SHALL be valid Newznab RSS XML with `<newznab:response offset="10" total="0"/>` reflecting the requested offset

#### Scenario: Default pagination
- **WHEN** a search request omits `offset` and `limit`
- **THEN** the system SHALL use offset=0 and limit=5000 (matching caps declaration)

### Requirement: Search filter parameters
The system SHALL accept optional filter parameters on search endpoints: `cat` (comma-separated category IDs), `maxage` (int, days), `minsize` (long, bytes), `maxsize` (long, bytes), `extended` (int, 0 or 1), `attrs` (comma-separated attribute names).

#### Scenario: Filter parameters accepted without error
- **WHEN** `?t=search&q=Tatort&cat=5000&maxage=30&extended=1` is requested
- **THEN** the response SHALL be valid Newznab RSS XML (parameters are parsed and available for forwarding to the Search domain when ready)

#### Scenario: Invalid category ID ignored
- **WHEN** `?t=search&cat=invalid` is requested
- **THEN** the system SHALL treat the parameter as absent and return valid RSS XML

### Requirement: JSON output format
The system SHALL support `o=json` query parameter on all Newznab endpoints to return JSON instead of XML.

#### Scenario: Caps as JSON
- **WHEN** `?t=caps&o=json` is requested
- **THEN** the response SHALL be `application/json` with a JSON representation of the capabilities document

#### Scenario: Search results as JSON
- **WHEN** `?t=search&q=Tatort&o=json` is requested
- **THEN** the response SHALL be `application/json` with a JSON representation of the RSS result set

#### Scenario: Default output is XML
- **WHEN** `o` parameter is absent or set to `xml`
- **THEN** the response SHALL be `application/xml` as before

## MODIFIED Requirements

### Requirement: Unknown function type
The system SHALL return Newznab error XML with code 202 for unrecognized `t` parameter values.

#### Scenario: Unknown t parameter
- **WHEN** `?t=unknown` is requested
- **THEN** the system SHALL return `<error code="202" description="No such function"/>` with HTTP 400

### Requirement: TV search endpoint
The system SHALL respond to `GET /index/api?t=tvsearch` with Newznab RSS XML containing search results. It SHALL accept optional parameters: `tvdbid` (int), `season` (string), `ep` (string), `q` (string), `offset` (int), `limit` (int), `cat` (string), `maxage` (int), `extended` (int), `attrs` (string).

#### Scenario: Search by tvdbid with season and episode
- **WHEN** `?t=tvsearch&tvdbid=83214&season=01&ep=05` is requested
- **THEN** the response SHALL be valid Newznab RSS XML (currently with zero items since Search domain is not built)

#### Scenario: Search by query string
- **WHEN** `?t=tvsearch&q=Tatort` is requested
- **THEN** the response SHALL be valid Newznab RSS XML

#### Scenario: Empty results format
- **WHEN** no results are found
- **THEN** the RSS XML SHALL have `<newznab:response offset="0" total="0"/>` and an empty items list

### Requirement: General search endpoint
The system SHALL respond to `GET /index/api?t=search` with Newznab RSS XML. It SHALL accept parameters: `q` (string), `offset` (int), `limit` (int), `cat` (string), `maxage` (int), `extended` (int), `attrs` (string).

#### Scenario: Text search
- **WHEN** `?t=search&q=Tatort` is requested
- **THEN** the response SHALL be valid Newznab RSS XML

### Requirement: Movie search endpoint
The system SHALL respond to `GET /index/api?t=movie` with Newznab RSS XML. It SHALL accept optional parameters: `imdbid` (string), `q` (string), `offset` (int), `limit` (int), `cat` (string), `maxage` (int), `extended` (int), `attrs` (string).

#### Scenario: Search by IMDB ID
- **WHEN** `?t=movie&imdbid=tt0806910` is requested
- **THEN** the response SHALL be valid Newznab RSS XML

## REMOVED Requirements

### Requirement: Fake NZB download endpoint
**Reason:** Replaced by standard `t=get` NZB download by GUID (Newznab standard path).
**Migration:** Enclosure URLs in search results will use `?t=get&id=<guid>` instead of `/nzb?url=<base64>&title=<base64>`.
