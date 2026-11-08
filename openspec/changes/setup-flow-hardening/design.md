## Context

The setup wizard provisions FunkArr as an indexer and download client in external Arr services (Prowlarr, Sonarr, Radarr). Five POST endpoints in `SetupApiEndpoints.cs` use `ArrSetupClient` to send payloads. Each endpoint computes a "self-URL" (how Arr services reach FunkArr) from either the request body or the current HTTP request context.

Current issues:
- URI construction drops Arr sub-paths (`new Uri(base, "/absolute")` replaces the path)
- SABnzbd download client payload lacks `useSsl` field
- Malformed self-URL crashes or produces garbage silently
- Docker users get `localhost` as self-URL (unreachable from other containers)
- No config option for self-URL; no guidance about Prowlarr indexer syncing
- TVDB/TMDB API keys unchecked in health endpoint

## Goals / Non-Goals

**Goals:**
- Fix all URI and payload construction bugs
- Make self-URL configurable and surface it in health check response
- Validate self-URL before use across all setup endpoints
- Guide users on Prowlarr topology (indexer sync)
- Warn about missing enrichment API keys

**Non-Goals:**
- Remote path mapping detection (#7 — deferred, not automatable)
- Sub-path deployment of FunkArr itself (urlBase stays `/download`)
- Auto-detection of Docker network names
- Enrichment key setup wizard (keys are env vars, not runtime-configurable)

## Decisions

### 1. URI join: string concatenation over `new Uri(base, relative)`

**Decision:** Replace `new Uri(baseUri, apiPath)` with `new Uri(baseUri.ToString().TrimEnd('/') + apiPath)`.

**Why:** The `Uri` constructor treats a path starting with `/` as absolute, replacing the entire base path. All `apiPath` values are hardcoded strings (`"/api/v1/indexer"`, `"/api/v3/indexer"`, `"/api/v3/downloadclient"`), so string concat is safe and preserves any sub-path on the Arr base URL.

**Alternative considered:** Making apiPath relative (no leading `/`) and ensuring baseUri always ends with `/`. Rejected because it requires coordinating two conventions across callers.

### 2. Self-URL resolution chain

**Decision:** Three-tier resolution: per-request `FunkArrUrl` > `FunkArrOptions.BaseUrl` > `ctx.Request.Scheme://ctx.Request.Host`.

**Why:** Per-request override stays for flexibility. Config-level `BaseUrl` covers the common Docker case (set once via env var `FunkArr__BaseUrl`). Request fallback remains as last resort for single-machine setups.

The health check `connectionInfo` exposes the resolved `baseUrl` so the frontend can pre-fill the field.

**Alternative considered:** Dropping the request fallback entirely (require config). Rejected — works fine for non-Docker single-machine setups where browser perspective is correct.

### 3. Self-URL validation: once per endpoint, before payload creation

**Decision:** Extract a `private static` helper `ResolveSelfUrl(CreateArrResourceRequest, FunkArrOptions, HttpContext)` that returns `(Uri? selfUri, ArrResourceResponse? error)`. Each endpoint calls it first and returns the error if non-null.

**Why:** All 5 endpoints duplicate the same logic. Validation before payload creation prevents both the `UriFormatException` in `SabnzbdDownloadClientPayload.Create` and silent garbage in indexer payloads. Returning `ArrResourceResponse` with `Success=false` keeps the envelope contract.

Payload factory signatures change from `string funkArrUrl` to `Uri selfUri` — callers pass the validated URI.

### 4. useSsl derived from self-URL scheme

**Decision:** Add `new ArrField("useSsl", selfUri.Scheme == "https")` to `SabnzbdDownloadClientPayload`.

**Why:** Sonarr/Radarr SABnzbd settings need `useSsl` to connect over HTTPS. Deriving from the scheme is the correct signal — if FunkArr's self-URL is HTTPS, the download client should use SSL.

### 5. TVDB/TMDB checks as warn-level

**Decision:** Add to the setup health check, status `warn` when key is empty, `ok` when set. Not `fail` — enrichment is optional.

**Why:** Users who don't set these keys get no enrichment data, but the system still functions. A warning surfaces the issue without blocking the setup flow.

### 6. Prowlarr topology: conditional UI, not backend change

**Decision:** Frontend-only change. When Prowlarr is selected in step 2, Sonarr/Radarr steps hide "Create Indexer" and show an info callout. The backend endpoints remain available.

**Why:** The backend doesn't know the user's topology intent. The UI is the right place to guide behavior. Keeping endpoints available means advanced users can still call them directly.

## Risks / Trade-offs

- **String concat URI join** — Less "correct" than `UriBuilder`, but simpler and all inputs are controlled. Risk: a baseUrl ending with a path like `/sonarr` (no trailing slash) + `/api/v3/indexer` produces `/sonarr/api/v3/indexer` which is correct. A baseUrl WITH trailing slash `/sonarr/` produces `/sonarr//api/v3/indexer` — double slash. → Mitigation: `TrimEnd('/')` on baseUri string handles this.

- **BaseUrl config empty by default** — Docker users must set it manually. → Mitigation: health check warn when BaseUrl is empty and request came from a different host than the container hostname? Too complex. Just document it clearly in the Docker Compose example and setup UI hint text.

- **Prowlarr UI logic** — Hiding indexer buttons based on step 2 selection. If user changes step 2 after step 3+, state resets. → Mitigation: existing stepper already resets forward steps on back-navigation. No new risk.
