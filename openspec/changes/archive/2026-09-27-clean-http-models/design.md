## Context

FunkArr has three HTTP surfaces: the internal API (`FunkArr.Api`), the Arr adapter APIs (`FunkArr.ArrApi`), and outbound HTTP clients (Enrichment, Search, Setup). The Enrichment and Search clients already use typed request/response records. The setup client (`ArrApiClient`) and the Arr adapter controller (`SabnzbdController`) use anonymous types and untyped `object`, making responses undocumentable and losing compile-time safety. Additionally, the Home page links to `/activity/history` which incorrectly matches the `/activity/:id` route.

## Goals / Non-Goals

**Goals:**
- Every HTTP response body is a named record type, documentable via OpenAPI
- `ArrSetupClient` surfaces the created resource ID and any Arr-side warnings
- `SabnzbdResult` carries typed data instead of `object`
- Home page "Recent Downloads" link navigates to the Activity history tab
- SSE endpoint reuses cached `JsonSerializerOptions`

**Non-Goals:**
- Full Sonarr/Radarr/Prowlarr API client (only setup provisioning)
- Changing Newznab XML response handling (already typed)
- Refactoring the SSE streaming approach itself
- Adding new API endpoints or changing existing endpoint paths

## Decisions

### D1: Rename ArrApiClient to ArrSetupClient

The client is used exclusively by `SetupArrEndpoints` for one-time resource provisioning. `ArrSetupClient` communicates this scope directly. The rename touches the class, DI registration in `ArrApiSetupContainer`, and test server registrations.

**Alternative**: `ArrProvisioningClient` -- rejected as unnecessarily long for a class used in one place.

### D2: Drop the generic from ArrField

`ArrField<T>` is always instantiated as `ArrField<object>`. The generic parameter provides zero type safety. Replace with a non-generic `sealed record ArrField(string Name, object Value)`. The JSON serializer handles the runtime types (`string`, `int`, `int[]`) via polymorphic serialization, which already works today.

**Alternative**: Strongly type each field value -- rejected because Arr API field schemas vary per implementation and FunkArr only constructs fixed payloads. The `object` is serialization-only, never deserialized.

### D3: Model Arr API responses as typed records

Sonarr, Radarr, and Prowlarr share a common response shape for resource creation. Model only the fields FunkArr needs:

```
ArrResourceResponse
  int? Id
  string? Name
  ArrProviderMessage? Message    // warning/info from the Arr app
  ArrValidationError[]? Errors   // validation failures

ArrProviderMessage
  string Message
  string Type                    // "info", "warning", "error"

ArrValidationError
  string PropertyName
  string ErrorMessage
  bool IsWarning
```

Replace `CreateArrResourceResponse(bool, string?)` with `ArrResourceResponse`. The `PostResourceAsync` method deserializes success responses to extract `Id`/`Name`/`Message`, and error responses to extract structured validation errors. The `bool Success` is derived from whether `Id` is present.

Place these records in `FunkArr.Api/Models/` alongside existing models.

### D4: Typed SabnzbdResult with generic data

Replace `SabnzbdResult.Ok(object Data)` with `SabnzbdResult.Ok<T>(T Data)` or individual typed result records per response shape. Since the controller always knows which response type it expects, a generic `SabnzbdResult<T>` approach works cleanly. However, the `MapResult` method in the controller needs a single return type.

**Chosen approach**: Keep `SabnzbdResult` as the abstract base but replace `Ok(object Data)` with `Ok(object Data)` staying as-is for the controller's `MapResult`, while introducing typed records for the individual response shapes that get wrapped. The anonymous types (`new { version = ... }`, `new { status = false, error = "..." }`) become named records:

```
SabnzbdVersionResponse(string Version)
SabnzbdErrorResponse(bool Status, string Error)  // Status always false
```

The `SabnzbdResult.Ok` keeps `object Data` because the controller's `MapResult` returns `ObjectResult` which is already untyped. The improvement is that the data flowing into `Ok(...)` is now a named type, not an anonymous one.

### D5: ValidationErrorResponse for FunkArr.Api

Add `sealed record ValidationErrorResponse(IReadOnlyList<string> Errors)` in `FunkArr.Api/Models/`. Replace three sites in `RuleSetApiEndpoints` where `new { errors = failed.Errors }` is used. Add `.ProducesValidationProblem()` or `.Produces<ValidationErrorResponse>(422)` to endpoint metadata.

### D6: Fix Home.vue link via query parameter

Change `/activity/history` to `/activity?tab=history`. Activity.vue reads `route.query.tab` on mount and sets `activeTab` accordingly. This avoids route conflicts with `/activity/:id` and follows the convention that tab selection is view state, not a distinct route.

**Alternative**: Add a dedicated `/activity/history` route before `/activity/:id` -- rejected because it would need to render the same Activity.vue component with the same props, adding routing complexity for no benefit.

### D7: Cache SSE JsonSerializerOptions

Extract the inline `new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }` into a `private static readonly` field on `SystemApiEndpoints`. This avoids allocating a new options instance per SSE event. The existing `_jsonOptions` pattern used in other files (TvdbClient, TmdbClient, ArrApiClient) is the established convention.

## Risks / Trade-offs

- **[ArrResourceResponse deserialization may encounter unknown fields]** -- Mitigated by using `JsonSerializerOptions` with default handling (ignores unknown properties). We only model the fields we need.
- **[SabnzbdResult.Ok still uses `object`]** -- Accepted trade-off. The controller's `MapResult` returns `ObjectResult` which erases the type anyway. The win is that callers construct named types instead of anonymous ones.
- **[Query parameter `?tab=history` not preserved on back-navigation]** -- Vue Router preserves query params in history. No issue expected.
