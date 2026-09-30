## 1. Remove Authentication Layer

- [x] 1.1 Delete `ApiKeyMiddleware.cs` and remove its registration from `FunkArrApplicationSetup`
- [x] 1.2 Remove `apikey` query parameter handling from frontend API client (`client.ts`) — remove `getApiKey()`, `setApiKey()`, and the `apikey` append logic
- [x] 1.3 Remove `funkarr-apikey` localStorage usage and the router guard in `router.ts` that redirects to `/setup`

## 2. Remove Config Persistence

- [x] 2.1 Delete `ConfigFileWriter.cs`
- [x] 2.2 Remove `config.json` loading from `Program.cs` (the `AddJsonFile(configJsonPath, ...)` call)
- [x] 2.3 Remove `PUT /api/v1/config` and `GET /api/v1/config` endpoints from `SetupController`
- [x] 2.4 Remove config-related response models (`ConfigResponse`, `ProwlarrConfig`, `ArrInstanceConfig`) from `SetupResponses.cs`

## 3. Simplify Options Structure

- [x] 3.1 Remove `Prowlarr` and `ArrInstances` properties from `FunkArrOptions`
- [x] 3.2 Keep `ArrConnection.cs` — still used by `SetupValidationModels` and `ArrRegistrationChecker`
- [x] 3.3 Set default API key to `"funkarr-default-api-key"` in `appsettings.json`
- [x] 3.4 `FunkArrOptionsValidator` unchanged — already validates non-empty ApiKey, default passes
- [x] 3.5 Remove `ConfigFileWriter` from `FunkArrServiceSetup` DI registration

## 4. Adapt Setup Status Endpoint

- [x] 4.1 Simplify `GET /api/v1/setup/status` in `SetupController` to remove Prowlarr and ArrInstance connectivity checks — keep only self-checks (API key, FFmpeg, paths, Mediathek)
- [x] 4.2 Update `StatusResponse` model to remove `ProwlarrStatus` and `ArrInstanceStatus` fields
- [x] 4.3 Update `SetupController` constructor to remove `IOptions<FunkArrOptions>` injection for Prowlarr/ArrInstances (keep only what's needed for self-checks)

## 5. Adapt Setup Validation

- [x] 5.1 Verified — `SetupValidationService` already uses per-request credentials from `ValidationRequest`, not from options
- [x] 5.2 Verified — `ArrConnection` and `ArrInstanceConnection` remain in `Configuration/ArrConnection.cs`, referenced by `SetupValidationModels`

## 6. Rework Setup Wizard UI

- [x] 6.1 Rewrite `SetupWizard.vue` as a read-only guide: self-check step (from `GET /api/v1/setup/status`), integration instructions with copy buttons for API key and URL, optional connection testing with per-request credentials
- [x] 6.2 Remove API key generation step — display the configured key (from status endpoint) with copy button instead
- [x] 6.3 Remove "Finish Setup" config save logic — the final action navigates to the dashboard without calling any config endpoint

## 7. Rework Settings View

- [x] 7.1 Rewrite `SettingsView.vue` as a read-only status dashboard: show system info from `GET /api/v1/setup/status`, display API key with copy button, link to Setup Guide (`/setup`)
- [x] 7.2 Remove save button, config editing, and `PUT /api/v1/config` calls
- [x] 7.3 Remove API key regenerate functionality and the non-existent `POST /api/v1/config/regenerate-key` call

## 8. Update Configuration Defaults

- [x] 8.1 Update `appsettings.json` with `"ApiKey": "funkarr-default-api-key"`
- [x] 8.2 Update `docker-compose.example.yml` — ApiKey is now optional with default

## 9. Tests and Cleanup

- [x] 9.1 Removed `ConfigFileWriterTests.cs`, removed 4 auth-expecting tests from integration tests, updated remaining tests to not use `apikey` param
- [x] 9.2 `FunkArrOptionsValidatorTests` unchanged — already correct (no Prowlarr/ArrInstances refs, validates non-empty key)
- [x] 9.3 Build passes, all 414 tests pass with 0 failures
