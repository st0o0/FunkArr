## Why

The Setup wizard currently displays static config tables that users must manually copy-paste into each arr service's UI (Prowlarr, Sonarr, Radarr). This is tedious and error-prone — wrong values, missed fields, and unnecessary context-switching between browser tabs. One-click creation via the arr services' own APIs removes this friction entirely.

## What Changes

- Add URL + API key input fields per selected arr service in the Setup wizard (step 3)
- Add "Create Indexer" and "Create Download Client" buttons that call arr service APIs through FunkArr's backend
- Add backend proxy endpoints (`POST /api/setup/{service}/{resource}`) that forward creation requests to the arr service APIs
- Collapse the existing manual copy-paste table under a "Configure manually" toggle as fallback
- Credentials are ephemeral — used only for the creation call, not persisted

## Capabilities

### New Capabilities

- `arr-service-proxy`: Backend endpoints that accept arr service URL + API key, construct the correct payload, and proxy POST requests to Prowlarr/Sonarr/Radarr APIs to create indexers and download clients

### Modified Capabilities

- `setup-guide-ui`: Step 3 changes from static copy-paste tables to URL/API key inputs with Create buttons and status feedback; existing manual config becomes a collapsed fallback

## Impact

- **FunkArr.Api**: New `SetupArrEndpoints` with 5 POST routes under `/api/setup/`
- **FunkArr.Api/Models**: Request/response models for arr service proxy calls
- **FunkArr.UI**: Reworked Setup.vue step 3 with input fields, buttons, loading/success/error states
- **FunkArr.UI/api**: New `setupArr.ts` API client for the proxy endpoints
- **Dependencies**: `HttpClient` usage in FunkArr.Api (no new NuGet packages needed)
- **No breaking changes**: existing manual setup flow preserved as fallback
