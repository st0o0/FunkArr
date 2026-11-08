## MODIFIED Requirements

### Requirement: ArrApi HTTP logging SHALL log headers at Information level

At `Information` log level, ArrApi HTTP logging SHALL capture request method, request path, query string, response status code, and request duration. The `apikey` query parameter value SHALL be redacted from all log output.

#### Scenario: Newznab search request logged at Information

- **WHEN** Prowlarr sends `GET /index/api?t=tvsearch&q=Tatort&tvdbid=123&apikey=secret123` and the log level is Information
- **THEN** a log entry SHALL contain the method (GET), path (/index/api), query string with apikey redacted (t=tvsearch&q=Tatort&tvdbid=123&apikey=[REDACTED]), response status code, and duration

#### Scenario: SABnzbd request logged at Information

- **WHEN** Sonarr sends `GET /download/api?mode=queue&apikey=secret123` and the log level is Information
- **THEN** a log entry SHALL contain the method (GET), path (/download/api), query string with apikey redacted (mode=queue&apikey=[REDACTED]), response status code, and duration

#### Scenario: Non-apikey parameters preserved

- **WHEN** any ArrApi request is logged
- **THEN** all query parameters except `apikey` SHALL be logged with their original values

### Requirement: API key redaction SHALL use IHttpLoggingInterceptor

The application SHALL implement `IHttpLoggingInterceptor` to redact the `apikey` query parameter value from HTTP log entries. The `[HttpLogging(HttpLoggingFields.All)]` attribute SHALL be removed from both controllers.

#### Scenario: Interceptor registered

- **WHEN** the application starts
- **THEN** an `IHttpLoggingInterceptor` implementation SHALL be registered that handles apikey redaction

#### Scenario: Controller logging attribute removed

- **WHEN** examining `NewznabController` and `SabnzbdController`
- **THEN** neither SHALL have a `[HttpLogging]` attribute

#### Scenario: Global logging config includes query string

- **WHEN** the global HTTP logging configuration is applied
- **THEN** `RequestQuery` SHALL be included in the logged fields (the interceptor handles redaction)
