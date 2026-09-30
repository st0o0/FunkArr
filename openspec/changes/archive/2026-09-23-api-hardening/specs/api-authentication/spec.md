## MODIFIED Requirements

### Requirement: ApiKey query parameter authentication
`ApiKeyActionFilter` SHALL validate the `apikey` query parameter against `FunkArrOptions.ApiKey` using ordinal string comparison. On successful validation, the filter SHALL store the validated API key in `HttpContext.Items["ApiKey"]`. Controllers SHALL read the API key from `HttpContext.Items["ApiKey"]` instead of re-parsing the query string. The Newznab filter SHALL return a Newznab XML error on failure. The SABnzbd filter SHALL return JSON `{ status: false, error: "API Key Incorrect" }` with 403.

#### Scenario: API key validated and stored in HttpContext
- **WHEN** a request with a valid `apikey` query parameter reaches the filter
- **THEN** the filter stores the key in `HttpContext.Items["ApiKey"]`
- **THEN** the controller reads the key from `HttpContext.Items["ApiKey"]`

#### Scenario: Controller does not re-parse query string
- **WHEN** `NewznabController` needs the API key for feed URL generation
- **THEN** it reads from `HttpContext.Items["ApiKey"]`, not from `Request.Query["apikey"]`

#### Scenario: Invalid API key on Newznab endpoint
- **WHEN** a request to `/index/api` has an invalid or missing `apikey`
- **THEN** the filter returns a Newznab XML error with code 100

#### Scenario: Invalid API key on SABnzbd endpoint
- **WHEN** a request to `/download/api` has an invalid or missing `apikey`
- **THEN** the filter returns JSON `{ status: false, error: "API Key Incorrect" }` with HTTP 403
