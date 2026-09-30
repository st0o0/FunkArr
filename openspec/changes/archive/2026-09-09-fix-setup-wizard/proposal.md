## Why

The Setup Guide shows placeholder values (`<funkarr-host>:<port>`) for connection details instead of pre-filling FunkArr's actual address. Additionally, the Sonarr/Radarr configuration page instructs users to set URL Base to `/download/api`, but Sonarr appends `/api` itself - the correct value is `/download`. This caused a connection test failure during E2E testing.

## What Changes

- Pre-fill the actual hostname and port in the Prowlarr/Sonarr/Radarr config steps based on the current request's `Host` header
- Fix the URL Base value from `/download/api` to `/download` for Sonarr and Radarr SABnzbd config
- Similarly fix Prowlarr's API Path display if needed (verify Prowlarr's behavior matches)

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `setup-guide-ui`: Pre-fill connection details with actual host/port from the health check response, fix URL Base values for SABnzbd download client configuration

## Impact

- **FunkArr.UI**: `views/Setup.vue` - update `sonarrConfig()`, `radarrConfig()`, and `prowlarrConfig()` functions to show correct URL values and pre-fill host/port from health check data
- No backend changes needed - the health check already returns `setupConnectionInfo.defaultPort`
