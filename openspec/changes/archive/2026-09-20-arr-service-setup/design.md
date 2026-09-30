## Context

The Setup wizard (step 3) currently shows static config tables with copy-paste values for each arr service. Users must manually open Prowlarr/Sonarr/Radarr, navigate to the right settings page, and enter each field by hand. The arr services all expose REST APIs that accept indexer and download client creation via POST.

FunkArr.Api already has `SystemApiEndpoints` with `IHttpClientFactory` usage for health checks (self-test, MediathekViewWeb). The same pattern extends naturally to outbound arr service calls.

## Goals / Non-Goals

**Goals:**

- One-click creation of indexers and download clients in Prowlarr, Sonarr, and Radarr from the FunkArr Setup wizard
- Backend proxy pattern: UI sends arr service URL + API key to FunkArr, which constructs the correct payload and POSTs to the arr service
- Keep the manual copy-paste flow as a collapsed fallback
- Ephemeral credentials — no persistence, no new config models

**Non-Goals:**

- Persisting arr service connection details (future concern)
- Update-or-check-if-exists logic — create-only, surface errors if already configured
- Managing arr service state beyond initial setup (no delete, no list, no sync)
- Prowlarr sync to Sonarr/Radarr (Prowlarr handles that natively)

## Decisions

### 1. New endpoint group in FunkArr.Api, not a new project

The proxy endpoints are thin HTTP-to-HTTP forwarders with no domain logic, no actors, no persistence. They fit in `FunkArr.Api` as a new `SetupArrEndpoints` static class alongside `SystemApiEndpoints`. No new project needed.

**Alternative considered:** Putting this in a dedicated service/actor. Rejected because there's no state, no lifecycle, no concurrency concern — just request-scoped HTTP calls.

### 2. Single request model per service type

Two request models: `CreateArrIndexerRequest` and `CreateArrDownloadClientRequest`, both containing `url` and `apiKey`. The endpoint route determines which arr service and resource type (e.g., `/api/setup/sonarr/indexer`). FunkArr constructs the arr-specific payload internally — the UI doesn't need to know Prowlarr uses API v1 while Sonarr/Radarr use v3.

**Alternative considered:** A generic proxy that forwards arbitrary payloads. Rejected because FunkArr knows exactly what payload each service needs (the same values currently shown in the copy-paste table), so the UI just provides credentials.

### 3. Use IHttpClientFactory with named clients

Each arr service call creates a short-lived `HttpClient` via `IHttpClientFactory` (already registered in the project). No named/typed clients needed — the URL comes from the request body. The arr service API key goes in the `X-Api-Key` header per arr API convention.

### 4. FunkArr self-URL from HttpContext

FunkArr needs to tell the arr service where to reach it (the indexer/download client URL). This is derived from `HttpContext.Request` (same approach as `SystemApiEndpoints` health check), producing `http://<host>:<port>`. The UI already knows this value and could send it, but deriving server-side avoids inconsistencies.

### 5. Five endpoints, flat routing

```
POST /api/setup/prowlarr/indexer
POST /api/setup/sonarr/indexer
POST /api/setup/sonarr/download-client
POST /api/setup/radarr/indexer
POST /api/setup/radarr/download-client
```

Each endpoint builds the service-specific JSON payload and POSTs to the arr service. Response is a simple success/error envelope — the UI doesn't need the full arr API response, just whether it worked and what went wrong if not.

### 6. UI: input fields replace static table, manual fallback collapsed

Step 3 per service changes from a static config table to:
- URL input field (text)
- API Key input field (password-style)
- "Create Indexer" button (all three services)
- "Create Download Client" button (Sonarr/Radarr only)
- Status feedback per button (idle → loading → success/error)
- Collapsed "Configure manually" section with the existing copy-paste table

## Risks / Trade-offs

- **[Arr service unreachable]** → Surface the HTTP error message to the user. The manual fallback is always available.
- **[Already configured]** → Arr services return 400/409 when a duplicate indexer/download client exists. Surface this clearly ("FunkArr indexer already exists in Sonarr").
- **[API version drift]** → Prowlarr v1, Sonarr/Radarr v3 are stable and well-established. Breaking changes are unlikely but possible. If an arr service changes its API, only the payload construction in `SetupArrEndpoints` needs updating.
- **[CORS not an issue]** → Backend proxy pattern means the browser never calls arr services directly. No CORS configuration needed.
