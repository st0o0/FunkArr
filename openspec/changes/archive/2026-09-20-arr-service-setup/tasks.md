## 1. Backend Models & Endpoint Scaffold

- [x] 1.1 Add request model `CreateArrResourceRequest(string Url, string ApiKey)` and response model `CreateArrResourceResponse(bool Success, string? Error)` in `FunkArr.Api/Models/`
- [x] 1.2 Create `SetupArrEndpoints` static class with `MapSetupArrApi` extension method, register 5 POST routes under `/api/setup/`, wire into `Program.cs`

## 2. Arr Service Payload Construction

- [x] 2.1 Implement Prowlarr indexer payload builder — Newznab indexer JSON for `POST /api/v1/indexer` with name, base URL, API path, API key, categories 5000+2000
- [x] 2.2 Implement Sonarr/Radarr indexer payload builder — Newznab indexer JSON for `POST /api/v3/indexer` with name, base URL, API key, service-specific categories (TV: 5030+5040, Movies: 2030+2040)
- [x] 2.3 Implement Sonarr/Radarr download client payload builder — SABnzbd JSON for `POST /api/v3/downloadclient` with name, host, port, URL base `/download`, API key, category (`tv`/`movies`)

## 3. Backend Proxy Logic

- [x] 3.1 Implement shared proxy method: validate URL, create HttpClient, set `X-Api-Key` header, POST payload, handle success/error/timeout (10s), return `CreateArrResourceResponse`
- [x] 3.2 Wire each endpoint to its payload builder + proxy call, derive FunkArr self-URL from `HttpContext.Request`

## 4. Frontend API Client

- [x] 4.1 Add `setupArr.ts` in `src/FunkArr.UI/src/api/` with typed functions for all 5 endpoints (`createProwlarrIndexer`, `createSonarrIndexer`, `createSonarrDownloadClient`, `createRadarrIndexer`, `createRadarrDownloadClient`)

## 5. Setup Wizard UI Changes

- [x] 5.1 Add URL and API Key input fields to step 3 service config, with per-service placeholders
- [x] 5.2 Add "Create Indexer" button (all services) and "Create Download Client" button (Sonarr/Radarr) with loading/success/error states
- [x] 5.3 Collapse existing copy-paste config table under "Configure manually" toggle, collapsed by default
- [x] 5.4 Add i18n keys for new UI strings (button labels, placeholders, error messages, toggle label)

## 6. Validation & Polish

- [x] 6.1 Test full flow in dev compose: create indexer in Prowlarr, create indexer + download client in Sonarr and Radarr
- [x] 6.2 Run `dotnet format` and verify build
