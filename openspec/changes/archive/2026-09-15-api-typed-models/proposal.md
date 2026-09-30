## Why

The internal API uses `JsonElement` and manual property-by-property parsing in several endpoints (create, update, test scoring), and returns anonymous `new { ... }` objects from mutation endpoints. This bypasses STJ model binding, produces no OpenAPI schema, and spreads ~240 lines of fragile hand-parsing across `RuleSetTestRequestParser`. Replacing these with typed request/response models improves OpenAPI documentation, catches deserialization issues at the framework level, and removes a class of silent-failure bugs.

## What Changes

- Replace `JsonElement body` parameters in create, update, and test endpoints with typed request records that STJ deserializes automatically.
- Remove `RuleSetTestRequestParser` — its manual JSON walking becomes unnecessary once a typed request model exists.
- Replace anonymous `new { success, error }` response objects in download mutation endpoints with a shared typed response record.
- Replace the anonymous `new { itemTraces }` test endpoint response with a typed model.
- Replace anonymous `new { error }` / `new { errors }` objects in ruleset validation/error responses with typed error models.
- Move inline `MediathekSearchResponse` / `MediathekSearchResult` records from the endpoint file into `Models/`.
- Update `.Produces<>()` declarations to reference actual types instead of `object`.

## Capabilities

### New Capabilities

- `api-request-models`: Typed request models for API endpoints that currently accept raw `JsonElement` — covers create/update ruleset and test scoring requests.
- `api-response-models`: Typed response models for mutation results and error responses that currently use anonymous objects.

### Modified Capabilities

- `ruleset-api`: The test request parsing requirement changes from manual `RuleSetTestRequestParser` to STJ-deserialized request model. Create/update endpoints change from `JsonElement` to typed models.

## Impact

- `FunkArr.Api/RuleSetApiEndpoints.cs` — create, update, and test handlers change parameter types
- `FunkArr.Api/RuleSetTestRequestParser.cs` — removed entirely
- `FunkArr.Api/DownloadsApiEndpoints.cs` — mutation responses use typed model
- `FunkArr.Api/MediathekApiEndpoints.cs` — inline records move to `Models/`
- `FunkArr.Api/Models/` — new request and response record files
- No message or domain changes — this is purely API layer
