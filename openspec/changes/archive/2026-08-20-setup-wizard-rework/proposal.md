## Why

The ApiKeyMiddleware creates a chicken-and-egg bug: the Setup Wizard needs to call `PUT /api/v1/config` to save the API key, but that endpoint is protected by the middleware which requires the key to already be set. The API key only exists because Sonarr/Radarr/Prowlarr require one as a mandatory field for indexers and download clients — FunkArr itself doesn't need authentication (it runs in a Docker network). Additionally, storing Prowlarr/ArrInstance API keys in `config.json` is unnecessary since FunkArr never uses them during normal operation.

## What Changes

- **BREAKING**: Remove `ApiKeyMiddleware` — no more request authentication on any endpoint
- **BREAKING**: Remove `ConfigFileWriter` and runtime `config.json` persistence — all configuration via environment variables or `appsettings.json`
- **BREAKING**: Remove `PUT /api/v1/config` and `GET /api/v1/config` endpoints
- **BREAKING**: Remove `Prowlarr` and `ArrInstances` from `FunkArrOptions` — FunkArr does not store external app credentials
- Set a default API key in `appsettings.json` so FunkArr works out of the box (users copy it into Sonarr/Radarr)
- Rework Setup Wizard from config-writer to read-only guide: self-health checks, copy-paste instructions for Arr integration, and temporary connection verification (keys sent per-request, never persisted)
- Rework Settings page from config-editor to read-only status dashboard

## Capabilities

### New Capabilities

(none — this is a simplification, not new functionality)

### Modified Capabilities

- `api-key-middleware`: Remove entirely — no auth enforcement
- `config-api`: Remove entirely — no runtime config persistence
- `options-structure`: Remove `Prowlarr`, `ArrInstances` from `FunkArrOptions`; add default API key
- `setup-wizard`: Convert from config-writing wizard to read-only guide with self-checks and copy-paste instructions
- `setup-validation`: Adapt to work without stored credentials; validation requests provide keys per-request
- `settings-view`: Convert from config-editor to read-only status dashboard

## Impact

- **Backend**: Remove `ApiKeyMiddleware.cs`, `ConfigFileWriter.cs`, config endpoints from `SetupController`. Remove `config.json` loading from `Program.cs`. Simplify `FunkArrOptions`.
- **Frontend**: Rewrite `SetupWizard.vue` (guide flow instead of config flow). Rewrite `SettingsView.vue` (read-only). Remove `setApiKey`/`getApiKey` from API client. Remove router guard that redirects to `/setup` when no key in localStorage.
- **API**: All endpoints become unauthenticated. Setup/test endpoints unchanged (already use per-request keys). Config endpoints removed.
- **Docker**: No more `FunkArr__ApiKey` env var required. Default key works out of the box.
- **Breaking**: Users who rely on API key authentication for exposed instances lose that protection.
