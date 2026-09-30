## Context

FunkArr authenticates all `/api/*` and `/download/*` requests via `ApiKeyMiddleware`, which compares a query-parameter `apikey` against `FunkArrOptions.ApiKey`. The Setup Wizard writes configuration (including the API key) to `data/config.json` via `PUT /api/v1/config`. This creates a chicken-and-egg bug: the wizard can't save the API key because the endpoint requires the key that hasn't been set yet.

The API key exists solely because Sonarr/Radarr/Prowlarr require one as a mandatory field when adding indexers and download clients. FunkArr runs in a Docker network and doesn't need its own authentication layer. Similarly, FunkArr stores Prowlarr and ArrInstance API keys in config.json but never uses them during normal operation — only for setup validation checks.

The current `SettingsView` references endpoints that don't exist (`POST /api/v1/config/regenerate-key`) and uses wrong URL patterns.

## Goals / Non-Goals

**Goals:**
- Remove all authentication enforcement — no middleware, no auth checks
- Remove runtime config persistence (`ConfigFileWriter`, `config.json`, config endpoints)
- Remove storage of external app credentials (`Prowlarr`, `ArrInstances` from options)
- Provide a default API key in `appsettings.json` so FunkArr works out of the box
- Convert Setup Wizard to a read-only guide with self-checks and copy-paste instructions
- Convert Settings page to a read-only status dashboard
- Keep setup validation and test endpoints functional with per-request credentials

**Non-Goals:**
- Adding a new authentication mechanism
- Changing the Newznab or SABnzbd API surfaces (they still accept `apikey` as a pass-through, just don't enforce it)
- Changing how `appsettings.json` or environment variables configure the app
- Modifying the download pipeline, search, or muxing systems

## Decisions

### 1. Remove ApiKeyMiddleware entirely

**Choice:** Delete the middleware and its registration. All endpoints become publicly accessible.

**Why not keep it with a bypass?** The key only exists for Sonarr/Radarr compatibility. Adding bypass logic (skip when unconfigured, skip for setup routes) just adds complexity for a feature that isn't needed. FunkArr runs in a Docker network where the only callers are Sonarr/Radarr and the user's browser on the same LAN.

**Alternative considered:** Keep middleware but auto-skip when `ApiKey` is empty. Rejected because it still leaves dead code paths and the `apikey` query parameter plumbing throughout the frontend.

### 2. Default API key in appsettings.json

**Choice:** Set `"ApiKey": "funkarr-default-api-key"` in `appsettings.json`. Users can override via `FunkArr__ApiKey` env var if they want a custom key.

**Why a default?** Sonarr/Radarr require a non-empty API key field. A default means zero-config setup — users just copy the key shown in the wizard into their Arr apps.

**Why not generate one at startup?** A generated key changes on every container recreation, breaking existing Arr connections. A stable default is more predictable for Docker deployments.

### 3. Remove config.json runtime persistence

**Choice:** Delete `ConfigFileWriter`, remove `config.json` from the configuration builder in `Program.cs`, remove `PUT /api/v1/config` and `GET /api/v1/config` endpoints.

**Why?** All settings that `config.json` stored can be set via environment variables in `docker-compose.yml`. Runtime config mutation is a source of confusion (which value wins? what happens on restart?). Environment-variable-only config is the standard pattern for containerized services.

### 4. Remove Prowlarr and ArrInstances from FunkArrOptions

**Choice:** Remove the `Prowlarr` property (`ArrConnection`) and `ArrInstances` property (`List<ArrInstanceConnection>`) from `FunkArrOptions`. Remove `ArrConnection`, `ArrInstanceConnection` types if unused elsewhere.

**Why?** FunkArr never connects to these services during normal operation. The only use was setup validation, which already accepts credentials per-request via `ValidationRequest`.

### 5. Wizard becomes a read-only guide

**Choice:** The wizard flow becomes:
1. **Self-Check** — calls `GET /api/v1/setup/status` to verify FunkArr's own health (API key present, FFmpeg found, paths writable, Mediathek reachable). Shows fix guidance with env var names for any failures.
2. **Integration Guide** — shows step-by-step instructions for adding FunkArr as indexer/download-client in Prowlarr/Sonarr/Radarr. Displays the configured API key with a copy button. No mode selection needed.
3. **Verify (optional)** — user can enter Arr URLs and keys temporarily to run `POST /api/v1/setup/validate`. Keys live only in component state, never persisted.

**Why remove mode selection?** The instructions for "with Prowlarr" and "without Prowlarr" can be shown together or toggled with a simple UI control. No need for a branching wizard flow.

### 6. Settings page becomes read-only status

**Choice:** Settings shows system info (FFmpeg version, paths, API key for copying, persistence info) and optionally runs validation checks. No save button, no config editing.

**Why?** With no `PUT /api/v1/config` endpoint, there's nothing to save to. The page becomes purely informational. Users who need to change settings edit their `docker-compose.yml` and restart.

### 7. Frontend removes API key plumbing

**Choice:** Remove `getApiKey()`/`setApiKey()` from the API client. Remove the `apikey` query parameter from all frontend API calls. Remove the router guard that redirects to `/setup` when no key is in localStorage. Remove `funkarr-apikey` from localStorage entirely.

**Why?** With no middleware enforcing auth, the frontend doesn't need to send a key. The router guard for setup detection can be replaced by checking the setup status endpoint on app load.

### 8. Setup status endpoint adaptation

**Choice:** `GET /api/v1/setup/status` removes Prowlarr and ArrInstance connectivity checks (since those credentials are no longer stored). It reports only self-checks: API key configured, FFmpeg found, paths writable, Mediathek reachable.

**Why?** Without stored credentials, the status endpoint can't check external services. The `POST /api/v1/setup/validate` endpoint handles that when the user provides credentials temporarily.

## Risks / Trade-offs

- **[No authentication]** → Users who expose FunkArr's port to the internet have no protection. This is acceptable: FunkArr is a local-network service like other *arr tools, and users who expose ports are expected to use a reverse proxy with auth. Documented in README.
- **[Default API key is public]** → Anyone who reads the source or docs knows the default key. This is fine since there's no auth enforcement — the key is just a token Sonarr/Radarr need to fill a required field.
- **[No runtime config changes]** → Users must restart the container after changing env vars. This is standard for Docker services and matches how Sonarr/Radarr handle their own config. Trade-off: slightly less convenient than a UI editor, but much simpler and more predictable.
- **[Breaking change for existing users]** → Users who have a `config.json` will need to migrate settings to env vars. Mitigation: the app should still read `config.json` if present but log a deprecation warning, or we document the migration path. Given FunkArr is pre-release, a clean break is acceptable.
