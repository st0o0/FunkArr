## 1. Health Check API

- [x] 1.1 Create `SetupHealthCheck` response model in `FunkArr.Api/Models/` — record with `Checks` (dictionary of check results) and `ConnectionInfo` (indexer/download API paths, default port)
- [x] 1.2 Create `SetupApiEndpoints.cs` in `FunkArr.Api` with `MapSetupApi()` extension method mapping `GET /api/health/setup`
- [x] 1.3 Implement API key check — compare against default, return masked + cleartext value
- [x] 1.4 Implement MediathekViewWeb connectivity check — HTTP HEAD with 3s timeout via `IHttpClientFactory`
- [x] 1.5 Implement data directory and download directory write checks — create/delete temp file
- [x] 1.6 Implement indexer API self-test — internal HTTP GET to own `/index/api?t=caps&apikey=<key>`
- [x] 1.7 Implement download API self-test — internal HTTP GET to own `/download/api?mode=version&apikey=<key>`
- [x] 1.8 Implement FFmpeg availability check — run `ffmpeg -version` process, report warn if missing
- [x] 1.9 Run all checks concurrently with `Task.WhenAll`, assemble response

## 2. Host Wiring

- [x] 2.1 Register `IHttpClientFactory` in `FunkArrServiceSetup` if not already present
- [x] 2.2 Add `app.MapSetupApi()` call in `FunkArrApplicationSetup` (before SPA fallback, after static files)

## 3. Health Check Tests

- [x] 3.1 Create `SetupHealthCheckTests.cs` in `FunkArr.Api.Tests` — test API key check (custom vs default)
- [x] 3.2 Test directory check logic (writable vs non-writable paths)
- [x] 3.3 Test response structure (all checks present, connection info correct)

## 4. Frontend — Health Widget

- [x] 4.1 Create `api/setup.ts` — fetch wrapper for `GET /api/health/setup`
- [x] 4.2 Create `HealthWidget.vue` component — status indicators (green/yellow/red), auto-refresh 30s, link to `/setup`
- [x] 4.3 Integrate `HealthWidget` into `Home.vue` dashboard

## 5. Frontend — Setup Guide

- [x] 5.1 Create `Setup.vue` with stepper shell — step indicators, back/next/done navigation, step tracking state
- [x] 5.2 Implement step 1: self-check — auto-run health check, show results, block on failures, re-check button
- [x] 5.3 Implement step 2: service selection — checkboxes for Prowlarr/Sonarr/Radarr with descriptions, require at least one
- [x] 5.4 Implement Prowlarr config step — field table (Name, URL placeholder, API Path `/index/api`, API Key, Categories), copy-to-clipboard buttons
- [x] 5.5 Implement Sonarr config step — field table (Name, Host placeholder, Port placeholder, URL Base `/download/api`, API Key, Category `tv`), copy-to-clipboard
- [x] 5.6 Implement Radarr config step — field table (same as Sonarr but Category `movies`), copy-to-clipboard
- [x] 5.7 Add `/setup` route to `main.ts` router and "Setup" link to `AppLayout.vue` header nav

## 6. Verify

- [x] 6.1 Run `dotnet build FunkArr.slnx` and `dotnet format --verify-no-changes`
- [x] 6.2 Run all test projects
- [x] 6.3 Start dev server, verify dashboard health widget and setup guide flow in browser
