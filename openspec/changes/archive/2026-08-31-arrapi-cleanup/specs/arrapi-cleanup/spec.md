## ADDED Requirements

### Requirement: ArrApi adapter contains no helper classes

The ArrApi project SHALL NOT contain standalone static helper classes for XML serialization, NZB generation, or NZB parsing. All logic SHALL be inlined into the endpoint files or expressed as private static methods within them.

#### Scenario: No standalone helper files exist
- **WHEN** the ArrApi project is built
- **THEN** no files named `XmlHelper.cs`, `NzbGenerator.cs`, or `NzbParser.cs` exist in the project

### Requirement: Newznab endpoints return XML only

The Newznab indexer API SHALL return XML responses exclusively. The `o=json` query parameter SHALL NOT be supported.

#### Scenario: Caps request returns XML
- **WHEN** a GET request is made to `/index/api?t=caps`
- **THEN** the response content type is `application/xml`

#### Scenario: Search request returns XML
- **WHEN** a GET request is made to `/index/api?t=tvsearch&q=test`
- **THEN** the response content type is `application/xml`

#### Scenario: JSON output parameter is ignored
- **WHEN** a GET request is made to `/index/api?t=caps&o=json`
- **THEN** the response content type is `application/xml` (JSON output not supported)

### Requirement: SABnzbd file upload uses IFormFile binding

The SABnzbd download API POST endpoint SHALL bind the uploaded NZB file using ASP.NET Minimal API `IFormFile` parameter binding. The endpoint SHALL NOT access `HttpContext.Request.Form.Files` directly.

#### Scenario: NZB file uploaded via IFormFile
- **WHEN** a POST request with multipart form data containing field `nzbfile` is made to `/download/api?mode=addfile`
- **THEN** the file is bound via `IFormFile` parameter and parsed for title and URL

#### Scenario: Antiforgery disabled for API clients
- **WHEN** the SABnzbd POST endpoint is registered
- **THEN** antiforgery validation is disabled via `.DisableAntiforgery()`
