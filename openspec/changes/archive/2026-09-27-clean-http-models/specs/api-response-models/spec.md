## MODIFIED Requirements

### Requirement: Error response model
The system SHALL use typed record models for all error responses. `ErrorResponse(string Message)` SHALL be used for single-error responses. `ValidationErrorResponse(IReadOnlyList<string> Errors)` SHALL be used for validation failure responses. All endpoints returning validation errors SHALL use `Results.UnprocessableEntity(new ValidationErrorResponse(...))` instead of anonymous types.

#### Scenario: Single error response
- **WHEN** an endpoint returns a single error message
- **THEN** it SHALL use `Results.Ok/BadRequest(new ErrorResponse(message))` with the named record type

#### Scenario: Validation error response
- **WHEN** a create, update, or export operation fails validation
- **THEN** the endpoint SHALL return `Results.UnprocessableEntity(new ValidationErrorResponse(errors))` using the named record type, never an anonymous `new { errors = ... }`

#### Scenario: OpenAPI documents validation response
- **WHEN** the OpenAPI schema is generated
- **THEN** endpoints returning validation errors SHALL declare `.Produces<ValidationErrorResponse>(422)`
