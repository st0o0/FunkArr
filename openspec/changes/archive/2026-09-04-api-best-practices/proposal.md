## Why

The internal API (`/api`) lacks standard .NET API conventions: JSON serializes as PascalCase instead of camelCase, enums serialize as integers, there's no OpenAPI spec, endpoints use untyped `Results` instead of `TypedResults`, and errors return bare status codes without a body. These gaps make frontend integration harder and prevent tooling-based contract generation.

## What Changes

- Configure global `camelCase` JSON serialization with `JsonStringEnumConverter` for the internal API
- Add OpenAPI document generation via `AddOpenApi()` / `MapOpenApi()` with Scalar API reference UI
- Switch internal API endpoints from `Results` to `TypedResults` for OpenAPI metadata inference
- Add Problem Details error responses on the internal API (504 gateway timeout, 404 not found)
- Add `Scalar.AspNetCore` package

**Not changing:**
- ArrApi (Newznab/SABnzbd) — external protocol adapters with fixed wire formats, already use explicit `[JsonPropertyName]` attributes. These stay as-is.

## Capabilities

### New Capabilities

- `api-json-conventions`: Global JSON serialization configuration (camelCase, string enums) for the internal API
- `api-openapi`: OpenAPI document generation and Scalar API reference UI

### Modified Capabilities

- `ruleset-api`: Endpoints switch to `TypedResults` and return Problem Details for errors

## Impact

- `FunkArr` (host) — `ServiceSetupContainer` adds JSON options + OpenAPI; `ApplicationSetupContainer` maps OpenAPI + Scalar endpoints
- `FunkArr.Api` — `RuleSetApiEndpoints` switches to `TypedResults`, adds Problem Details
- `Directory.Packages.props` — adds `Scalar.AspNetCore` package version
- `FunkArr.csproj` — adds `Scalar.AspNetCore` package reference
- **BREAKING**: JSON responses change from PascalCase to camelCase. Enums change from integer to string. Frontend code consuming the internal API must be updated.
