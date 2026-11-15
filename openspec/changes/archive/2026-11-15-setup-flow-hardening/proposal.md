## Why

The setup wizard breaks for non-trivial deployments. Sub-path Arr services (reverse proxy with `/sonarr/` prefix) lose their base path due to a URI construction bug. HTTPS deployments fail silently because the SABnzbd payload lacks `useSsl`. Docker users get `localhost` as FunkArr's self-URL, which is unreachable from other containers. Malformed self-URLs crash the SABnzbd endpoint or silently produce garbage indexer URLs. The Prowlarr topology isn't explained, leading users to create duplicate indexers. TVDB/TMDB API keys aren't checked, so enrichment fails without explanation.

## What Changes

- Fix `new Uri(baseUri, apiPath)` in `ArrSetupClient` to preserve the Arr service's base path when it has a sub-path prefix
- Add `useSsl` field to `SabnzbdDownloadClientPayload` derived from the self-URL scheme
- Validate the resolved self-URL before passing it to payload factories; return a clear error instead of throwing or producing garbage
- Add `FunkArrOptions.BaseUrl` config property with resolution chain: per-request override > config > request fallback
- Expose `baseUrl` in the health check `connectionInfo` so the frontend can pre-fill it
- Extract the duplicated self-URL resolution + validation logic from all 5 endpoints into a shared helper
- Add `warn`-level health checks for TVDB and TMDB API key configuration
- Frontend: pre-fill FunkArr URL field from `connectionInfo.baseUrl` instead of `window.location`
- Frontend: when Prowlarr is selected, hide "Create Indexer" on Sonarr/Radarr steps and show topology info callout
- Frontend: fix dead code where Prowlarr's `downloadClient` handler points to the indexer function

## Capabilities

### New Capabilities

- `setup-self-url-resolution`: Self-URL resolution chain (per-request > config > request fallback) with validation, `FunkArrOptions.BaseUrl` config property, and health check `connectionInfo.baseUrl` exposure

### Modified Capabilities

- `arr-service-proxy`: Fix URI construction for sub-path Arr services; add `useSsl` to SABnzbd payload; validate self-URL before payload creation; extract shared self-URL helper
- `arr-setup-client`: Fix sub-path URI join in `PostResourceAsync`
- `setup-health-check`: Add `tvdbApiKey` and `tmdbApiKey` warn-level checks; expose `baseUrl` in `connectionInfo`
- `setup-guide-ui`: Pre-fill FunkArr URL from health check; Prowlarr topology guidance (hide indexer buttons on Sonarr/Radarr, show info callout); fix Prowlarr dead code

## Impact

- **FunkArr.Api**: `ArrSetupClient.cs`, `SetupApiEndpoints.cs`, `ArrPayloads.cs`, `SystemApiEndpoints.cs`, `SetupHealthCheck.cs` model
- **FunkArr.Core**: `FunkArrOptions.cs` (new `BaseUrl` property)
- **FunkArr.Api.Tests**: New/updated tests for sub-path URI, self-URL validation, useSsl, BaseUrl resolution, TVDB/TMDB checks
- **FunkArr.UI**: `Setup.vue`, `setup.ts` API client
- **Config**: `appsettings.json` (new `FunkArr__BaseUrl` entry)
