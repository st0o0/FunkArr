## 1. Config Model & Persistence

- [x] 1.1 Add `ArrConnection` record (Url, ApiKey) and `ArrInstanceConnection` record (Name, Type, Url, ApiKey) with `ArrType` enum (Sonarr, Radarr) to `FunkArr.Configuration`
- [x] 1.2 Add `Prowlarr` and `ArrInstances` properties to `FunkArrOptions`
- [x] 1.3 Implement `data/config.json` loading in `Program.cs` — add `AddJsonFile` for `data/config.json` with `optional: true, reloadOnChange: true`, layered after `appsettings.json`
- [x] 1.4 Implement `ConfigFileWriter` service that reads/merges/writes `data/config.json` (create if absent, merge partial updates, never touch `appsettings.json`)

## 2. Backend API — Config & Setup Endpoints

- [x] 2.1 Create `SetupEndpoints.cs` with endpoint group `/api/setup` authenticated via apikey filter
- [x] 2.2 Implement `GET /api/setup/status` — aggregate checks: configured state, arr connections, FFmpeg, paths, Mediathek, ruleset count
- [x] 2.3 Implement `POST /api/setup/test-prowlarr` — HTTP call to Prowlarr `/api/v1/health` with provided URL and API key
- [x] 2.4 Implement `POST /api/setup/test-arr` — HTTP call to Sonarr/Radarr `/api/v3/system/status` with provided URL, API key, and type
- [x] 2.5 Implement `POST /api/setup/test-paths` — verify write access to download and temp directories
- [x] 2.6 Implement `POST /api/setup/test-ffmpeg` — run `ffmpeg -version`, parse version string
- [x] 2.7 Implement `POST /api/setup/test-mediathek` — ping MediathekViewWeb API
- [x] 2.8 Implement `GET /api/config` — return current FunkArrOptions with arr API keys masked
- [x] 2.9 Implement `PUT /api/config` — accept partial config, write via ConfigFileWriter
- [x] 2.10 Register setup endpoints in `FunkArrApplicationSetup`

## 3. Backend API — Queue Endpoints

- [x] 3.1 Create `QueueEndpoints.cs` with endpoint group `/api/queue` and `/api/history` authenticated via apikey filter
- [x] 3.2 Implement `GET /api/queue` — ask DownloadQueueActor for GetQueue, return flat JSON array (nzoId, title, status, progressPercent, downloadedBytes, totalBytes, enqueuedAt)
- [x] 3.3 Implement `GET /api/history` — ask DownloadQueueActor for GetHistory, return flat JSON array with path mapping applied
- [x] 3.4 Register queue endpoints in `FunkArrApplicationSetup`

## 4. Backend API — Ruleset Endpoints

- [x] 4.1 Add new messages to `RuleSetRegistryActor`: `GetAllRulesets`, `GetRuleSet(topic)`, `SaveLocalRuleSet(RuleSetFile)`, `DeleteLocalRuleSet(topic)`, `TestRules(topic, tvdbId?, rules[])`
- [x] 4.2 Implement `GetAllRulesets` handler — iterate `_byTopic`, return list with topic, source, rule count, media reference
- [x] 4.3 Implement `GetRuleSet` handler — lookup by topic, return full RuleSetFile or not-found
- [x] 4.4 Implement `SaveLocalRuleSet` handler — write to local directory via RuleSetFileWriter, reload all from disk
- [x] 4.5 Implement `DeleteLocalRuleSet` handler — delete from local directory, reload all from disk
- [x] 4.6 Implement `TestRules` handler — search Mediathek via MediathekClient, optionally fetch TVDB episodes, run EvaluateRulesWithTraces, return traces
- [x] 4.7 Create `RulesetEndpoints.cs` with endpoint group `/api/rulesets` authenticated via apikey filter
- [x] 4.8 Implement `GET /api/rulesets` — ask actor for GetAllRulesets, merge match stats from MatchLedger
- [x] 4.9 Implement `GET /api/rulesets/:topic` — ask actor for GetRuleSet
- [x] 4.10 Implement `PUT /api/rulesets/:topic` — deserialize body, ask actor for SaveLocalRuleSet
- [x] 4.11 Implement `DELETE /api/rulesets/:topic` — ask actor for DeleteLocalRuleSet
- [x] 4.12 Implement `POST /api/rulesets/test` — ask actor for TestRules
- [x] 4.13 Implement `POST /api/rulesets/reload` — send ReloadLocal to actor
- [x] 4.14 Register ruleset endpoints in `FunkArrApplicationSetup`

## 5. Static File Serving & SPA Fallback

- [x] 5.1 Add `UseStaticFiles()` to `FunkArrApplicationSetup` for serving `wwwroot/`
- [x] 5.2 Add SPA fallback — `MapFallbackToFile("index.html")` for non-API routes
- [x] 5.3 Add `wwwroot/` to `.gitignore`

## 6. UI Project Setup

- [x] 6.1 Scaffold `src/FunkArr.UI/` with `npm create vue@latest` — Vue 3, TypeScript, Vue Router
- [x] 6.2 Install and configure Tailwind CSS v4
- [x] 6.3 Configure `vite.config.ts` — output to `../FunkArr/wwwroot/`, dev proxy for `/api/*` and `/download/*` to `http://localhost:5000`
- [x] 6.4 Configure Vue Router with hash mode and route definitions for all views
- [x] 6.5 Create `src/api/client.ts` — fetch wrapper that appends `apikey` from localStorage
- [x] 6.6 Create `src/composables/usePolling.ts` — interval-based polling with visibility pause

## 7. UI — App Shell & Layout

- [x] 7.1 Create `App.vue` with tab navigation bar (Queue, History, Rulesets, Matches, Settings) and `<router-view>`
- [x] 7.2 Apply base Tailwind styles — system font stack, neutral color palette, single accent color
- [x] 7.3 Implement first-run detection — check localStorage for API key, redirect to setup wizard if absent

## 8. UI — Setup Wizard

- [x] 8.1 Create `SetupWizard.vue` with step navigation (back/next), step indicator
- [x] 8.2 Implement API key step — generate random key or manual input, copy button
- [x] 8.3 Implement mode selection step — "With Prowlarr" / "Without Prowlarr" cards
- [x] 8.4 Implement Prowlarr connection step — URL/API key inputs, test button, indexer instructions with copy
- [x] 8.5 Implement arr instances step — dynamic list of Sonarr/Radarr connections, add/remove/test each, download client instructions (context-dependent on Prowlarr mode)
- [x] 8.6 Implement paths step — download path, temp path, concurrent downloads, path mapping inputs with backend validation
- [x] 8.7 Implement verification step — call `/api/setup/status`, display check results, finish button
- [x] 8.8 Implement save — write all config via `PUT /api/config`, store API key in localStorage, redirect to dashboard

## 9. UI — Queue & History Views

- [x] 9.1 Create `QueueView.vue` — fetch `/api/queue`, render download cards with progress bars, use polling composable
- [x] 9.2 Create `DownloadCard.vue` — title, status badge, progress bar (for downloading), bytes info
- [x] 9.3 Create `HistoryView.vue` — fetch `/api/history`, render completed/failed items with timestamps and error messages

## 10. UI — Rulesets Views

- [x] 10.1 Create `RulesetsView.vue` — fetch `/api/rulesets`, render searchable table with topic, source, rule count, match rate, warning/edit indicators
- [x] 10.2 Create `RulesetDetail.vue` — fetch `/api/rulesets/:topic`, render media reference, rules with filters/title rules/hit counts, unmatched items with failure reasons
- [x] 10.3 Create `RuleCard.vue` — display a single rule: priority, strategy, filters tree, title rules
- [x] 10.4 Create `RulesetEditor.vue` — form for editing/creating rulesets: topic, media reference, mode (replace/merge), rule list with add/remove
- [x] 10.5 Create `FilterEditor.vue` — visual builder for FilterGroup: All/Any/Not sections, add/remove filters, field/op/value dropdowns
- [x] 10.6 Create `TitleRuleEditor.vue` — inputs for regex (field, pattern, capture group) and static (value) title rules
- [x] 10.7 Create `MatchTestPanel.vue` — test button, call `POST /api/rulesets/test`, display matched/filtered/unmatched traces with details
- [x] 10.8 Wire up save (PUT) and delete (DELETE) operations in the editor

## 11. UI — Match Intelligence Views

- [x] 11.1 Create `MatchesView.vue` with sub-navigation: Recent, Topics, Unmatched
- [x] 11.2 Implement recent matches list — fetch `/api/matches/recent`, render expandable match records with traces
- [x] 11.3 Implement topic stats — fetch `/api/matches/topics`, render table sorted by match rate, highlight low performers
- [x] 11.4 Implement unmatched explorer — fetch `/api/matches/unmatched`, render grouped by topic with failure details, link to ruleset detail

## 12. UI — Settings View

- [x] 12.1 Create `SettingsView.vue` — fetch `/api/config` and `/api/setup/status`, render form with current values and connection status indicators
- [x] 12.2 Implement API key section — masked display, copy button, regenerate button
- [x] 12.3 Implement connection section — Prowlarr, arr instances with live status indicators and test buttons
- [x] 12.4 Implement paths/downloads section — editable fields with save
- [x] 12.5 Implement system info section — FFmpeg version, ruleset count, database path
- [x] 12.6 Add "Re-run Setup Wizard" button

## 13. Docker & CI

- [x] 13.1 Update Dockerfile with Node.js build stage — `npm ci && npm run build` in `src/FunkArr.UI/`, copy output to `src/FunkArr/wwwroot/`
- [x] 13.2 Update GitHub Actions workflow to include Node.js setup and UI build step before .NET build
- [x] 13.3 Verify multi-arch Docker build (amd64/arm64) still works with the Node stage

## 14. Testing

- [x] 14.1 Add integration tests for config API endpoints (GET/PUT config, setup status)
- [x] 14.2 Add integration tests for queue API endpoints (GET queue, GET history)
- [x] 14.3 Add integration tests for ruleset API endpoints (list, get, save, delete, test, reload)
- [x] 14.4 Add unit tests for ConfigFileWriter (merge logic, file creation)
- [x] 14.5 Add unit tests for new RuleSetRegistryActor messages (list, get, save, delete)
