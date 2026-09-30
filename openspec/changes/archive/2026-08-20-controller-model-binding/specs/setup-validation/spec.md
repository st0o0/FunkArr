## MODIFIED Requirements

### Requirement: Supplied credentials are not persisted
Given that `Prowlarr` and `ArrInstances` are removed from `FunkArrOptions`, the validation endpoint SHALL accept these credentials exclusively from the request body via `[FromBody]` model binding and SHALL NOT attempt to read them from options or configuration. The endpoint SHALL use `EmptyBodyBehavior.Allow` to support requests with no body (self-checks only).

#### Scenario: Validation uses [FromBody] model binding
- **WHEN** a validation request is sent with a JSON body containing Prowlarr or Arr instance credentials
- **THEN** the system SHALL deserialize via `[FromBody]` model binding using the global JSON options

#### Scenario: Validation without body (self-checks only)
- **WHEN** a validation request is sent with no body (Content-Length 0 or omitted)
- **THEN** the system SHALL bind the parameter to null via `EmptyBodyBehavior.Allow` and run only self-checks (API key, FFmpeg, paths)

#### Scenario: Validation response uses global JSON options
- **WHEN** the validation endpoint returns its result
- **THEN** the response SHALL be serialized via `Ok(result)` using the global JSON options instead of a custom `JsonResult` with `SetupValidationJsonOptions`
