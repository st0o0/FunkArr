# API Validation

## Purpose

DataAnnotation-based input validation for all API request models, with automatic enforcement via ValidationEndpointFilter (Minimal APIs) and [ApiController] (controllers), plus SSRF protection on setup endpoints.

## Requirements

### Requirement: DataAnnotation validation on all request models
All request records across FunkArr.Api and FunkArr.ArrApi SHALL use `System.ComponentModel.DataAnnotations` attributes to declare input constraints. Every non-optional property SHALL have `[Required]`. Numeric ranges SHALL use `[Range]`. URL fields SHALL use `[Url]` or a custom safety attribute. String fields with format constraints SHALL use `[RegularExpression]` or `[StringLength]`.

#### Scenario: Required field missing from JSON body
- **WHEN** a POST/PUT request omits a required field (e.g. `Topic` in `CreateRuleSetRequest`)
- **THEN** the API returns 400 with a validation error before the handler executes

#### Scenario: Numeric field out of range
- **WHEN** a request includes a numeric value outside its annotated range (e.g. `Confidence` > 1.0)
- **THEN** the API returns 400 with a validation error identifying the field and constraint

#### Scenario: Optional field omitted
- **WHEN** a request omits an optional nullable field
- **THEN** the request binds successfully with the field as null

### Requirement: ValidationEndpointFilter for Minimal APIs
FunkArr.Api SHALL register a `ValidationEndpointFilter` on all endpoint groups. The filter SHALL validate models bound via `[AsParameters]` and JSON body parameters using their DataAnnotation attributes. On validation failure, the filter SHALL return 400 with a `ValidationProblemDetails` response before the handler executes.

#### Scenario: Invalid AsParameters model on GET endpoint
- **WHEN** a GET request to `/api/mediathek/search` provides `limit=-5`
- **THEN** the ValidationEndpointFilter returns 400 with validation errors
- **THEN** the endpoint handler does not execute

#### Scenario: Invalid JSON body on POST endpoint
- **WHEN** a POST request to `/api/rulesets/` sends a body with empty `RuleSetId`
- **THEN** the ValidationEndpointFilter returns 400 with validation errors

#### Scenario: Valid request passes through
- **WHEN** a request passes all DataAnnotation validation
- **THEN** the filter invokes the next handler normally

### Requirement: ArrApi controller validation via ApiController
FunkArr.ArrApi controllers with `[ApiController]` SHALL automatically validate request models via DataAnnotations. The `[ApiController]` attribute's built-in model validation SHALL return 400 for invalid models without explicit `ModelState.IsValid` checks in handler code.

#### Scenario: ArrApi request with invalid model state
- **WHEN** a Newznab or SABnzbd request fails DataAnnotation validation
- **THEN** ASP.NET returns 400 automatically via `[ApiController]` behavior

### Requirement: SafeUrl validation attribute
A custom `[SafeUrl]` validation attribute SHALL validate that a URL uses http or https scheme and does not resolve to a loopback, link-local, or private IP range (127.0.0.0/8, 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16, ::1, fe80::/10). This attribute SHALL be applied to `CreateArrResourceRequest.Url`.

#### Scenario: Loopback URL rejected
- **WHEN** `CreateArrResourceRequest.Url` is `http://127.0.0.1:8080/api`
- **THEN** validation fails with "URL must not target a private or loopback address"

#### Scenario: Private network URL rejected
- **WHEN** `CreateArrResourceRequest.Url` is `http://192.168.1.100:7878/api`
- **THEN** validation fails with "URL must not target a private or loopback address"

#### Scenario: Public URL accepted
- **WHEN** `CreateArrResourceRequest.Url` is `http://sonarr.example.com:8989/api`
- **THEN** validation passes
