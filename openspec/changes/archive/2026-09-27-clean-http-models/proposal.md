## Why

Several HTTP boundaries use anonymous types (`new { }`) or untyped `object` instead of proper records, breaking OpenAPI documentation and compile-time safety. The `ArrApiClient` has a misleading name (it only does setup provisioning) and discards useful response data from Sonarr/Radarr/Prowlarr. A routing bug in the UI sends "Recent Downloads" links to a non-existent download detail page instead of the Activity history tab.

## What Changes

- Rename `ArrApiClient` to `ArrSetupClient` across class, DI registration, and all references
- Replace `ArrField<object>` pseudo-generic with non-generic `ArrField` using `object` values (the generic parameter is never used meaningfully)
- Model Arr API success responses (`id`, `name`, `message`) and error responses (validation array, single-object error) as typed records
- Replace `CreateArrResourceResponse(bool, string?)` with a richer response surfacing the created resource ID and any warnings from the Arr app
- Replace anonymous types in `RuleSetApiEndpoints` validation errors (3 sites) with a `ValidationErrorResponse` record
- Replace anonymous types in `SabnzbdController` (version response, error responses) with typed records
- Replace `SabnzbdResult.Ok(object Data)` with a typed variant
- Fix Home.vue `/activity/history` links to use query parameter `/activity?tab=history`
- Update Activity.vue to read `tab` query param for initial tab selection
- Cache `JsonSerializerOptions` in SystemApiEndpoints SSE endpoint instead of allocating per event

## Capabilities

### New Capabilities

- `arr-setup-client`: Typed HTTP client for provisioning indexers and download clients in Sonarr/Radarr/Prowlarr during setup, with modeled request payloads and response types

### Modified Capabilities

- `api-response-models`: Add `ValidationErrorResponse` record for validation failure responses
- `arrapi-service-results`: Replace `SabnzbdResult.Ok(object)` with typed data, add typed error/version response records
- `sabnzbd-download-api`: Replace anonymous types in controller with typed response records
- `dashboard-stats`: Fix routing for "Recent Downloads" link on Home page

## Impact

- **FunkArr.Api**: `ArrApiClient.cs` renamed, `Models/ArrPayloads.cs` updated, `Models/CreateArrResourceRequest.cs` updated, `SetupArrEndpoints.cs` updated, `RuleSetApiEndpoints.cs` updated, `SystemApiEndpoints.cs` SSE options cached
- **FunkArr.ArrApi**: `SabnzbdController.cs` updated, `SabnzbdResult.cs` updated, new typed response records
- **FunkArr.Api.Tests / FunkArr.ArrApi.Tests**: Update references to renamed client
- **FunkArr.UI**: `Home.vue` link targets, `Activity.vue` tab initialization, `main.ts` route unchanged
- **FunkArr (Host)**: `ArrApiSetupContainer.cs` DI registration updated
