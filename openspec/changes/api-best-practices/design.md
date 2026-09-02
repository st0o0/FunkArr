## Context

The internal API returns JSON with PascalCase property names (C# record defaults), enums as integers, no OpenAPI spec, and bare status codes for errors. The ArrApi layer is unaffected — it uses explicit `[JsonPropertyName]` for SABnzbd wire format and XML for Newznab.

## Goals / Non-Goals

**Goals:**
- camelCase JSON + string enums on all JSON responses
- OpenAPI document at `/openapi/v1.json` with Scalar reference UI at `/scalar`
- TypedResults for compile-time response metadata on internal API endpoints
- Structured error bodies (Problem Details) on internal API

**Non-Goals:**
- Changing ArrApi (Newznab/SABnzbd) serialization or endpoints
- Adding authentication to the internal API
- Generating TypeScript clients from OpenAPI (future work)

## Decisions

### Global JSON options via ConfigureHttpJsonOptions

Configure `JsonNamingPolicy.CamelCase` and `JsonStringEnumConverter` globally in `ServiceSetupContainer`. This affects all `Results.Json()` and `TypedResults.Ok()` calls. ArrApi SABnzbd models are safe because they use explicit `[JsonPropertyName]` attributes which take precedence over the naming policy. Newznab uses XML serialization, not JSON.

### Scalar.AspNetCore for API reference

Use `Scalar.AspNetCore` (v2.17.x) instead of Swagger UI. It's lighter, better-looking, and actively maintained. Integration is two lines: `AddOpenApi()` in services, `MapScalarApiReference()` + `MapOpenApi()` in app pipeline.

Scalar UI is served at `/scalar`, OpenAPI document at `/openapi/v1.json`.

### TypedResults on internal API only

Switch `RuleSetApiEndpoints` from `Results.Ok()` / `Results.StatusCode()` to `TypedResults.Ok()` / `TypedResults.Problem()` etc. This gives ASP.NET enough type information to generate accurate OpenAPI schemas.

ArrApi endpoints stay on `Results` — they return custom `IResult` (XML content, file bytes) that TypedResults doesn't cover, and their OpenAPI schema doesn't matter.

### Problem Details for errors

Replace bare `Results.StatusCode(504)` with `TypedResults.Problem(statusCode: 504, title: "Gateway Timeout", detail: "...")`. This follows RFC 9457 and gives the frontend structured error information.

## Risks / Trade-offs

- **Breaking frontend** — camelCase changes every property name in API responses. → Acceptable per project convention (0.x, breaking changes OK). Frontend doesn't exist yet in meaningful form.
- **Scalar in production** — Scalar UI is exposed at `/scalar`. For a local/Docker service this is fine; if the service ever goes public, restrict to Development environment. → Low risk for current deployment model.
