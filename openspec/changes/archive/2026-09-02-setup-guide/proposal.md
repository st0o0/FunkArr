## Why

Users deploying FunkArr need to configure Prowlarr (as indexer) and Sonarr/Radarr (as download client) with the correct URLs, API paths, and API keys. Getting these settings wrong is the most common setup failure, and FunkArr currently offers no way to verify its own readiness or guide users through the *arr configuration. A self-check dashboard and interactive setup guide reduce support friction and make first-run setup reliable.

## What Changes

- New health/self-check API endpoint (`GET /api/health/setup`) that verifies FunkArr's operational readiness: API key configured, MediathekViewWeb reachable, data/download directories writable, own Newznab and SABnzbd endpoints responding, FFmpeg available.
- Dashboard health widget on the Home view showing real-time status of all self-checks with pass/fail indicators and inline fix hints.
- Interactive setup guide at `/setup` route with stepper UI:
  1. Self-check (auto-runs, blocks on critical failures)
  2. Service selection (multi-choice: Prowlarr, Sonarr, Radarr)
  3. Per-service configuration walkthrough with exact field values (URL, API Path, API Key, categories) and copy-to-clipboard — matching the actual field layout of each *arr application.
- No external service credentials are stored or tested from FunkArr — users use the *arr apps' built-in test buttons.

## Capabilities

### New Capabilities
- `setup-health-check`: Backend self-check endpoint that verifies FunkArr operational readiness (API key, MediathekViewWeb connectivity, directory access, own API endpoints, FFmpeg).
- `setup-guide-ui`: Interactive stepper UI for verifying FunkArr health and walking users through Prowlarr/Sonarr/Radarr configuration with exact field values.

### Modified Capabilities
- `application-bootstrap`: New `MapSetupApi()` endpoint registration in ApplicationSetupContainer.

## Impact

- **FunkArr.Api**: New `SetupApiEndpoints.cs` and health check response model.
- **FunkArr (Host)**: Wire `MapSetupApi()` in ApplicationSetupContainer.
- **FunkArr.UI**: New `Setup.vue` view with stepper, updated `Home.vue` with health widget, new route `/setup`, new API client for health endpoint.
- **Dependencies**: None new — uses existing `HttpClient`, `IConfiguration`, `IOptions<FunkArrOptions>`, filesystem APIs.
