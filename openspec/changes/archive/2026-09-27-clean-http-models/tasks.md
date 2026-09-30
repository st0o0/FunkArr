## 1. ArrSetupClient rename and response models

- [x] 1.1 Rename `ArrApiClient` to `ArrSetupClient` (class, file, constructor)
- [x] 1.2 Update DI registration in `ArrApiSetupContainer.cs` to `AddHttpClient<ArrSetupClient>`
- [x] 1.3 Update `SetupArrEndpoints.cs` parameter type from `ArrApiClient` to `ArrSetupClient`
- [x] 1.4 Update test server registrations in `FunkArrTestServer.cs` (both Api.Tests and ArrApi.Tests)
- [x] 1.5 Add response model records: `ArrResourceResponse`, `ArrProviderMessage`, `ArrValidationError` in `FunkArr.Api/Models/`
- [x] 1.6 Replace `CreateArrResourceResponse(bool, string?)` with `ArrResourceResponse` in `PostResourceAsync` return type
- [x] 1.7 Rewrite `PostResourceAsync` to deserialize success responses (extract `id`, `name`, `message`) and error responses (validation array + single-object error)
- [x] 1.8 Update `SetupArrEndpoints.cs` to pass through the richer `ArrResourceResponse`

## 2. ArrField cleanup

- [x] 2.1 Replace `ArrField<T>` with non-generic `ArrField(string Name, object Value)` in `ArrPayloads.cs`
- [x] 2.2 Update all three payload records (`ProwlarrIndexerPayload`, `SonarrRadarrIndexerPayload`, `SabnzbdDownloadClientPayload`) to use `ArrField[]` instead of `ArrField<object>[]`

## 3. ValidationErrorResponse in FunkArr.Api

- [x] 3.1 Add `ValidationErrorResponse(IReadOnlyList<string> Errors)` record in `FunkArr.Api/Models/`
- [x] 3.2 Replace `new { errors = failed.Errors }` with `new ValidationErrorResponse(failed.Errors)` at RuleSetApiEndpoints lines 193, 208, 255
- [x] 3.3 Add `.Produces<ValidationErrorResponse>(422)` to affected endpoint metadata

## 4. SabnzbdController typed responses

- [x] 4.1 Add `SabnzbdVersionResponse(string Version)` and `SabnzbdErrorResponse(bool Status, string Error)` records in `FunkArr.ArrApi/Sabnzbd/Models/`
- [x] 4.2 Replace `new { version = ... }` with `new SabnzbdVersionResponse(SabnzbdConstants.Version)` in SabnzbdController
- [x] 4.3 Replace all `new { status = false, error = "..." }` with `new SabnzbdErrorResponse(false, "...")` in SabnzbdController (HandleGet and HandlePost)
- [x] 4.4 Update `MapResult` error branch to use `new SabnzbdErrorResponse(false, err.Message)` instead of anonymous type

## 5. SSE JsonSerializerOptions cache

- [x] 5.1 Extract inline `new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }` to a `private static readonly` field in `SystemApiEndpoints`

## 6. Home.vue routing fix

- [x] 6.1 Change `router-link to="/activity/history"` to `router-link to="/activity?tab=history"` in Home.vue (both the stats card and View All link)
- [x] 6.2 Update Activity.vue to read `route.query.tab` on mount and set `activeTab` to `'history'` when the query param is present

## 7. Verify

- [x] 7.1 Run `dotnet build src/FunkArr.slnx` and fix any compilation errors
- [x] 7.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 7.3 Run all test projects
