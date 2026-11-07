## MODIFIED Requirements

### Requirement: API endpoint catch blocks SHALL log exceptions

API endpoint methods that contain try/catch blocks SHALL log the caught exception using `ILogger`. TimeoutException SHALL be logged at Warning level and produce a 504 response. Other exceptions SHALL be logged at Error level and produce a 500 response with a generic message. The `EndpointExceptionFilter` SHALL NOT include `ex.Message` in the response body — it SHALL use a fixed generic message ("An unexpected error occurred") and log the detail server-side only.

**Newznab and SABnzbd protocol error responses are exempt from HTTP status code conventions.** These APIs follow their respective protocol specifications:
- Newznab errors SHALL return HTTP 200 with an XML `<error>` element containing `code` and `description` attributes.
- SABnzbd errors SHALL return HTTP 200 with a JSON body `{"status": false, "error": "<message>"}`.

This exemption applies only to protocol-level errors (invalid API key, missing parameter, invalid mode, etc.), not to unhandled exceptions which continue to use the `EndpointExceptionFilter` with 500 status.

#### Scenario: Timeout exception in endpoint

- **WHEN** an endpoint catches a `TimeoutException`
- **THEN** the exception is logged at Warning level
- **THEN** a 504 Gateway Timeout response is returned

#### Scenario: General exception in endpoint

- **WHEN** an endpoint catches a non-timeout exception
- **THEN** the exception is logged at Error level
- **THEN** a 500 response is returned with title "Internal Server Error" and no exception detail in the body

#### Scenario: Exception message not leaked

- **WHEN** the `EndpointExceptionFilter` catches any exception
- **THEN** the response body contains "An unexpected error occurred" as the detail, not the exception message

#### Scenario: Newznab protocol error returns HTTP 200

- **WHEN** a Newznab request has an invalid API key, missing parameter, or unknown function
- **THEN** the response SHALL be HTTP 200 with XML body `<error code="..." description="..."/>`

#### Scenario: SABnzbd protocol error returns HTTP 200

- **WHEN** a SABnzbd request has an invalid mode, missing parameter, or operation failure
- **THEN** the response SHALL be HTTP 200 with JSON body `{"status": false, "error": "..."}`
