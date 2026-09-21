# FunkArr E2E Test Plan

Complete browser + API test script covering every clickable element and interaction.

> **Note**: This file is a test specification — do not check off items here. Write test
> results to a separate results file so this plan stays reusable across runs.

**Prerequisites**: `docker compose -f docker-compose.dev.yml up -d --build`, wait for FunkArr on port 6969.  
**Arr services**: Sonarr (8989), Radarr (7878), Prowlarr (9696) — all with API key `funkarr-dev-api-key-01`.

---

## Test Scenarios & Prerequisites

Some tests require specific state. The execution order (bottom of this file) sets up these
scenarios in sequence — each phase builds on the previous one.

| Scenario | Setup | Required for |
|---|---|---|
| **Clean slate** | Fresh volumes, no data | 0.x, 14.x (health), initial dashboard |
| **Sonarr ready** | Tatort added (tvdbId 83214), root folder `/shared/tv` registered | 17.x (search flow) |
| **Services configured** | FunkArr indexer + download client in Sonarr (via setup wizard or API) | 17.x, download tests |
| **Post-search** | At least 1 Sonarr search triggered → scoring history + download exist | 3.x, 4.x, 5.x, 12.x, 13.x, 18.x, 19.x |
| **Local ruleset exists** | A local-only ruleset created (via UI or API) | 6.9-6.11 (source filters), 7.8 (export), 10.x (delete), 11.x |
| **Merged ruleset exists** | Community ruleset edited locally (PUT /api/rulesets/{id}) | 7.10-7.11, 10.1-10.2 |
| **Active download** | Download in progress (timing-sensitive — trigger search and test quickly) | 3.4-3.7, 18.1 |
| **Failed download** | Download that failed (403 or similar) | 5.6-5.8, 18.4-18.5 |
| **Multiple queue items** | 2+ downloads queued simultaneously | 3.8-3.9, 4.2 |

### Setup Wizard Notes

The setup wizard URL fields show placeholders (e.g. `http://sonarr:8989`) but the value is
empty — the user must type both URL and API key for the create buttons to enable. When testing
with Chrome automation, use the `type` action (physical keyboard) after clicking the field.
`nativeInputValueSetter` or `form_input` may not trigger Vue's v-model reliably.

Docker networking: Prowlarr/Sonarr/Radarr must reach FunkArr at `http://funkarr:6969` (Docker
internal DNS). The "Indexer erstellen" / "Download-Client erstellen" buttons make a test
connection — this only works when all containers are on the same Docker network. From `localhost`
(the browser), use the FunkArr setup API (`POST /api/setup/{service}/{resource}`) as a fallback.

---

## 0. Clean Slate — Reset from Previous Run

Run this before a fresh E2E test to wipe all state from a previous run.

### Full reset (nuclear — removes ALL data)

```powershell
# Stop everything
docker compose -f docker-compose.dev.yml down

# Remove all named volumes (FunkArr data, Arr configs, media)
docker volume rm rundfunkarr_funkarr-data rundfunkarr_media rundfunkarr_sonarr-config rundfunkarr_radarr-config rundfunkarr_prowlarr-config 2>$null

# Rebuild and start fresh
docker compose -f docker-compose.dev.yml up -d --build
```

### Selective reset

```powershell
# FunkArr only (scoring history, download history, rulesets, persistence)
docker compose -f docker-compose.dev.yml stop funkarr
docker volume rm rundfunkarr_funkarr-data
docker compose -f docker-compose.dev.yml up -d funkarr

# Arr services only (re-triggers setup wizard flow)
docker compose -f docker-compose.dev.yml stop sonarr radarr prowlarr
docker volume rm rundfunkarr_sonarr-config rundfunkarr_radarr-config rundfunkarr_prowlarr-config
docker compose -f docker-compose.dev.yml up -d sonarr radarr prowlarr

# Downloaded media only
docker compose -f docker-compose.dev.yml stop funkarr
docker volume rm rundfunkarr_media
docker compose -f docker-compose.dev.yml up -d
```

### Verification after reset

- [ ] **0.1** Dashboard shows: Letzte Downloads = 0, Speicher near-zero usage
- [ ] **0.2** Activity: all tabs empty (no active, no queue, no history)
- [ ] **0.3** Regelwerke: 46 community rulesets, 0 local (no merged/local badges)
- [ ] **0.4** Scoring history: empty for all rulesets
- [ ] **0.5** Setup wizard: Sonarr/Radarr/Prowlarr configs need re-creation
- [ ] **0.6** Sonarr at :8989 needs initial series setup (add Tatort)

### Re-add Tatort to Sonarr after reset

```powershell
$apiKey = "funkarr-dev-api-key-01"

# Register root folder first (required on fresh volumes)
Invoke-RestMethod "http://localhost:8989/api/v3/rootfolder?apikey=$apiKey" -Method Post -ContentType "application/json" -Body '{"path":"/shared/tv"}'

# Create /shared/tv dir if needed (funkarr container has write access)
docker exec funkarr mkdir -p /shared/tv

# Add Tatort directly (SkyHook may be unavailable)
$body = '{"title":"Tatort","tvdbId":83214,"qualityProfileId":1,"rootFolderPath":"/shared/tv","monitored":true,"seasonFolder":false,"addOptions":{"searchForMissingEpisodes":false}}'
Invoke-RestMethod "http://localhost:8989/api/v3/series?apikey=$apiKey" -Method Post -ContentType "application/json" -Body $body
```

---

## 1. Dashboard (Übersicht)

**Route**: `/`

- [ ] **1.1** Stat cards visible: System (Alles OK), Letzte Downloads, Speicher (used/total + bar), Regelwerke (count + version)
- [ ] **1.2** System stat card click navigates to `/setup`
- [ ] **1.3** Recent Downloads stat card click navigates to `/activity/history`
- [ ] **1.4** Regelwerke stat card click navigates to `/rulesets`
- [ ] **1.5** Download status bar shows active/waiting counts *(requires: post-search)*
- [ ] **1.6** "Anzeigen" link in status bar navigates to `/activity` *(requires: post-search)*
- [ ] **1.7** Letzte Aktivität shows entries with status indicator (green=completed, orange=downloading, red=failed), title, episode info, size, relative timestamp
- [ ] **1.8** "Alle anzeigen" link navigates to `/activity/history`
- [ ] **1.9** Version number shown bottom-left (v0.0.0-dev or current)

---

## 2. Sidebar & Global Navigation

- [ ] **2.1** FunkArr logo click navigates to `/`
- [ ] **2.2** Sidebar nav: Übersicht → `/`, Aktivität → `/activity`, Regelwerke → `/rulesets`
- [ ] **2.3** Einrichtung link → `/setup`
- [ ] **2.4** Sidebar collapse button → icons only mode
- [ ] **2.5** Sidebar expand button → full labels restored
- [ ] **2.6** Collapse state persists across navigation (localStorage)
- [ ] **2.7** Language switcher: select Deutsch → UI in German
- [ ] **2.8** Language switcher: select English → UI in English (Overview, Activity, RuleSets, Setup, Collapse)
- [ ] **2.9** Language switcher: select Österreichisch → dialect strings
- [ ] **2.10** Language switcher: select Schwizerdütsch → dialect strings
- [ ] **2.11** Language persists across navigation (localStorage)

---

## 3. Activity — Active Downloads (Aktiv tab)

**Route**: `/activity`

- [ ] **3.1** Tab buttons visible: Aktiv, Wartend, Verlauf — Aktiv is default
- [ ] **3.2** Empty state: "Keine aktiven Downloads" message when idle
- [ ] **3.3** Search field visible and filters active downloads
- [ ] **3.4** During download: shows title, episode, quality badge (1080p), channel tag, size tag, duration tag, SUB badge (if subtitles) *(requires: active download)*
- [ ] **3.5** Progress bar with %, download speed (KB/s), ETA *(requires: active download)*
- [ ] **3.6** Global speed indicator in page header *(requires: active download)*
- [ ] **3.7** Cancel button (X) on active download → toast "Download abgebrochen" *(requires: active download — timing-sensitive)*
- [ ] **3.8** Queue group cards expand/collapse on header click *(requires: multiple active downloads in same group)*

---

## 4. Activity — Queue (Wartend tab)

**Route**: `/activity` → click Wartend tab

- [ ] **4.1** Click Wartend tab → switches view
- [ ] **4.2** Badge count on tab label ("Wartend 1") *(requires: queued download)*
- [ ] **4.3** Shows queued items with title, episode, quality, size, type (show/movie) *(requires: queued download)*
- [ ] **4.4** X button removes item from queue → toast "Download abgebrochen" *(requires: queued download)*

---

## 5. Activity — History (Verlauf tab)

**Route**: `/activity` → click Verlauf tab

- [ ] **5.1** Click Verlauf tab → switches view
- [ ] **5.2** Table with columns: Titel, Qualität (badge), Größe, Dauer, Status, Abgeschlossen
- [ ] **5.3** Category filter dropdown ("Alle Kategorien") → filters by download category
- [ ] **5.4** Search field filters history entries
- [ ] **5.5** Completed status: green dot + "Completed"
- [ ] **5.6** Failed status: red dot + "Failed" *(requires: failed download)*
- [ ] **5.7** Failed item: expandable `<details>` shows full error message *(requires: failed download)*
- [ ] **5.8** Retry button (only on Failed items) → toast "Wiederholung gestartet", item re-queued *(requires: failed download)*
- [ ] **5.9** X button deletes history entry → toast "Eintrag gelöscht"
- [ ] **5.10** Pagination: "Zurück" / "Weiter" buttons when totalPages > 1 *(requires: >20 history entries)*

---

## 6. RuleSets — List

**Route**: `/rulesets`

- [ ] **6.1** Shows all rulesets with title, aliases, rule count, IMDB/TVDB IDs
- [ ] **6.2** Source badges: community (orange), merged (white)
- [ ] **6.3** Scoring stats visible for rulesets with history (Trefferquote, enrichment %, last scored) *(requires: post-search)*
- [ ] **6.4** Search input: type "tatort" → filters to matching rulesets
- [ ] **6.5** Clear search → all rulesets shown again
- [ ] **6.6** Type filter: click "Serien" → only series, count updates
- [ ] **6.7** Type filter: click "Filme" → only movies, count updates *(0 if no movie rulesets exist)*
- [ ] **6.8** Type filter: click "Alle" → reset *(default state — test after using another filter)*
- [ ] **6.9** Source filter: click "Community" → filtered count "X Regelwerke von Y" *(requires: local ruleset exists)*
- [ ] **6.10** Source filter: click "Lokal" → only merged/local rulesets *(requires: local ruleset exists)*
- [ ] **6.11** Source filter: click "Alle Quellen" → reset *(requires: local ruleset exists)*
- [ ] **6.12** Sort dropdown: "Nach Name sortieren" → alphabetical by topic
- [ ] **6.13** Sort dropdown: "Nach ID sortieren" → alphabetical by ruleset ID
- [ ] **6.14** "+ Neu" link navigates to `/rulesets/new`
- [ ] **6.15** Click on ruleset row navigates to `/rulesets/{id}`

---

## 7. RuleSet — Detail

**Route**: `/rulesets/{id}`

- [ ] **7.1** Breadcrumb links: "Regelwerke" → `/rulesets`
- [ ] **7.2** Identity section: Regelwerk-ID, Aliase, TVDB, IMDB, TMDB, Quelle (badge + timestamp)
- [ ] **7.3** Enrichment section: Enabled badge, method tags (title/airdate/runtime/year), thresholds
- [ ] **7.4** Matching rules: click rule header → expand/collapse rule details
- [ ] **7.5** Expanded rule shows: strategy, title parts, regex patterns, filters
- [ ] **7.6** "Scoring-Verlauf" button navigates to `/rulesets/{id}/history`
- [ ] **7.7** "Bearbeiten" button navigates to `/rulesets/{id}/edit`
- [ ] **7.8** "Für Community exportieren" button → toast "Erfolgreich exportiert" or validation error *(requires: merged ruleset)*
- [ ] **7.9** "Lokal löschen" button → shows confirmation panel *(requires: merged or local ruleset)*
- [ ] **7.10** Delete confirmation: "Bestätigen" → deletes local overlay, reloads as community-only or navigates to list *(requires: merged ruleset)*
- [ ] **7.11** Delete confirmation: "Abbrechen" → hides confirmation panel *(requires: merged or local ruleset)*
- [ ] **7.12** Delete on local-only ruleset → navigates to `/rulesets` *(requires: local-only ruleset)*

---

## 8. RuleSet — Editor

**Route**: `/rulesets/{id}/edit`

### Identity
- [ ] **8.1** Regelwerk-ID field readonly for existing rulesets
- [ ] **8.2** Thema field editable, changes reflected
- [ ] **8.3** Click "+ Alias hinzufügen" → adds empty alias input
- [ ] **8.4** Type text in alias field
- [ ] **8.5** Click X on alias → removes alias
- [ ] **8.6** Medientyp dropdown: switch between Serie/Film
- [ ] **8.7** Medienname field editable
- [ ] **8.8** TVDB-ID, IMDB-ID, TMDB-ID fields editable

### Enrichment
- [ ] **8.9** Click Enabled toggle → switches on/off
- [ ] **8.10** Check/uncheck Title method checkbox
- [ ] **8.11** Check/uncheck Airdate method checkbox
- [ ] **8.12** Check/uncheck Runtime method checkbox
- [ ] **8.13** Check/uncheck Year method checkbox
- [ ] **8.14** Edit Title Threshold field (0.0-1.0)
- [ ] **8.15** Edit Airdate Tolerance (days) field
- [ ] **8.16** Warning "Enrichment requires a TVDB ID" shown when enabled without TVDB ID

### Matching Rules
- [ ] **8.17** Click rule header → expand/collapse rule
- [ ] **8.18** Edit Regel-ID field
- [ ] **8.19** Edit Priorität field
- [ ] **8.20** Edit Konfidenz field
- [ ] **8.21** Change Strategie dropdown (Titel enthält / Staffel- & Episodennummer / etc.)
- [ ] **8.22** Season/Episode regex fields visible when strategy = season-episode
- [ ] **8.23** Titelregeln: change type dropdown (Statisch/Regex)
- [ ] **8.24** Titelregeln: change field dropdown (Titel/Topic)
- [ ] **8.25** Titelregeln: edit pattern/value
- [ ] **8.26** Titelregeln: edit group field
- [ ] **8.27** Click X on title rule → removes it
- [ ] **8.28** Click "+ Titelteil hinzufügen" → adds new title rule row
- [ ] **8.29** Filter: click "+ Hinzufügen" in "Alle erfüllt" → adds filter
- [ ] **8.30** Filter: click "+ Hinzufügen" in "Mind. eins" → adds filter
- [ ] **8.31** Filter: click "+ Hinzufügen" in "Keines" → adds filter
- [ ] **8.32** Filter: change field dropdown (Dauer/Qualität/etc.)
- [ ] **8.33** Filter: change operator dropdown (größer als/kleiner als/etc.)
- [ ] **8.34** Filter: edit value field
- [ ] **8.35** Filter: click X → removes filter
- [ ] **8.36** Click "+ Regel hinzufügen" → adds new empty rule
- [ ] **8.37** Click X on rule → removes entire rule

### Live Preview
- [ ] **8.38** Shows candidate count and match count ("Live-Vorschau X / Y Treffer")
- [ ] **8.39** Each result shows title, channel, duration, matched rule ID
- [ ] **8.40** Click "↻ Aktualisieren" → refreshes preview from Mediathek
- [ ] **8.41** Click "Vollständiger Test (N)" → runs full scoring with enrichment
- [ ] **8.42** After full test: results show enrichment data
- [ ] **8.43** Click "← Zurück zur Vorschau" → returns to live preview mode
- [ ] **8.44** Click on result item → expand/collapse trace details

### Save/Cancel
- [ ] **8.46** "Speichern" → saves changes, toast "Regelwerk gespeichert", navigates to detail
- [ ] **8.47** "Abbrechen" → discards changes, navigates back

---

## 9. RuleSet — Create New

**Route**: `/rulesets/new`

- [ ] **9.1** Regelwerk-ID field editable (placeholder "meine-sendung")
- [ ] **9.2** Thema field editable (placeholder "Sendungsname")
- [ ] **9.3** Default Konfidenz is 0.8
- [ ] **9.4** Enrichment enabled by default with Title+Airdate checked
- [ ] **9.5** Enrichment warning: "Enrichment requires a TVDB ID to resolve episodes"
- [ ] **9.6** Live preview shows "Thema eingeben, um Kandidaten zu laden" when empty
- [ ] **9.7** Enter Thema → live preview loads candidates from Mediathek
- [ ] **9.8** Add a matching rule, configure it
- [ ] **9.9** "Speichern" creates ruleset → toast, navigates to detail
- [ ] **9.10** Duplicate ID returns validation error

---

## 10. RuleSet — Delete

- [ ] **10.1** "Lokal löschen" on merged ruleset → confirmation panel *(requires: merged ruleset — PUT a community ruleset first)*
- [ ] **10.2** Confirm → removes local override, detail reloads as community-only *(requires: merged ruleset)*
- [ ] **10.3** "Lokal löschen" on local-only ruleset → confirmation panel
- [ ] **10.4** Confirm → deletes completely, navigates to `/rulesets`
- [ ] **10.5** Cancel → confirmation panel disappears, no changes
- [ ] **10.6** Ruleset disappears from list after deletion

---

## 11. RuleSet — Export

- [ ] **11.1** Click "Für Community exportieren" on detail page
- [ ] **11.2** Valid ruleset → toast "Erfolgreich exportiert" *(requires: merged ruleset)*

---

## 12. Scoring History

**Route**: `/rulesets/{id}/history`

- [ ] **12.1** Breadcrumb links: "Regelwerke" → `/rulesets`, "{id}" → `/rulesets/{id}`
- [ ] **12.2** Total count header ("X Bewertungsdurchläufe insgesamt")
- [ ] **12.3** Table with: Quelle, Abfrage, Zeitpunkt, Kandidaten, Treffer
- [ ] **12.4** Click on table row → navigates to `/rulesets/{id}/history/{requestId}`
- [ ] **12.5** Pagination: "Weiter" button loads next page *(requires: >20 scoring entries)*
- [ ] **12.6** Pagination: "Zurück" button loads previous page *(requires: >20 scoring entries)*
- [ ] **12.7** After a Sonarr search: only 1 entry per logical search (pagination cache)

---

## 13. Scoring Detail (Bewertungsdetail)

**Route**: `/rulesets/{id}/history/{requestId}`

- [ ] **13.1** Breadcrumb links: "Regelwerke", "{id}", "Scoring-Verlauf" → all navigate correctly
- [ ] **13.2** Header: Quelle (sonarr), Abfrage, Zeitpunkt
- [ ] **13.3** Filter tab: click "Alle (N)" → shows all items
- [ ] **13.4** Filter tab: click "Treffer (N)" → shows only matched items
- [ ] **13.5** Filter tab: click "Keine Treffer (N)" → shows only unmatched items
- [ ] **13.6** Matched items: green title, "Treffer" badge, score, matched rule ID
- [ ] **13.7** Unmatched items: gray title, "Kein Treffer" badge, score 0.00
- [ ] **13.8** Each item metadata: channel, topic, duration, quality
- [ ] **13.9** Click "▶ N Regel-Trace(s)" → expands rule traces
- [ ] **13.10** Expanded trace shows: rule ID, priority, outcome
- [ ] **13.11** Filter trace: pass/fail indicators (✓/✗), expected vs actual values, color coded
- [ ] **13.12** Identification trace: strategy, attempted, detail
- [ ] **13.13** Click expanded "▼ N Regel-Trace(s)" → collapses

---

## 14. Setup Wizard — System Health

**Route**: `/setup`

- [ ] **14.1** Step indicator shows: 1 Systemprüfung → 2 Dienste
- [ ] **14.2** Health checks all green: API-Schlüssel, MediathekViewWeb, Datenverzeichnis, Zielverzeichnis, Temporärverzeichnis, Indexer-API, Download-API, FFmpeg (with version)
- [ ] **14.3** Click "Erneut prüfen" → re-runs health checks
- [ ] **14.4** Click "Weiter" → advances to Dienste step

---

## 15. Setup Wizard — Services (Dienste)

**Route**: `/setup` → step 2

- [ ] **15.1** Checkboxes for Prowlarr, Sonarr, Radarr with descriptions
- [ ] **15.2** Check Prowlarr → step indicator adds Prowlarr step
- [ ] **15.3** Check Sonarr → step indicator adds Sonarr step
- [ ] **15.4** Check Radarr → step indicator adds Radarr step
- [ ] **15.5** Uncheck all → "Weiter" disabled (requires ≥1 service selected)
- [ ] **15.6** Click "Zurück" → returns to Systemprüfung
- [ ] **15.7** Click "Weiter" → advances to first selected service

---

## 16. Setup Wizard — Service Configuration

**Route**: `/setup` → per-service steps

### Prowlarr
- [ ] **16.1** URL field shows placeholder `http://prowlarr:9696` (value empty — must type)
- [ ] **16.2** API-Schlüssel password field — type API key (both URL + key required for buttons to enable)
- [ ] **16.3** FunkArr-URL optional field shows placeholder `http://funkarr:6969`
- [ ] **16.4** Click "Indexer erstellen" → creates Newznab indexer in Prowlarr *(requires: Docker networking)*
- [ ] **16.5** Success state shown after creation (button changes) *(requires: successful creation)*
- [ ] **16.6** Duplicate creation → error "Should be unique" shown gracefully *(requires: prior successful creation)*
- [ ] **16.7** Click "Manuell konfigurieren" → expands manual instructions
- [ ] **16.8** Click copy button on manual field → clipboard + toast "In Zwischenablage kopiert"

### Sonarr
- [ ] **16.9** URL placeholder `http://sonarr:8989` (must type value)
- [ ] **16.10** Type API key into password field (both URL + key required)
- [ ] **16.11** Click "Download-Client erstellen" → creates SABnzbd client *(requires: Docker networking)*
- [ ] **16.12** Success / duplicate error

### Radarr
- [ ] **16.13** URL placeholder `http://radarr:7878` (must type value)
- [ ] **16.14** Type API key into password field
- [ ] **16.15** Click "Download-Client erstellen" → creates SABnzbd client *(requires: Docker networking)*
- [ ] **16.16** Success / duplicate error

### Navigation
- [ ] **16.17** "Zurück" on each step returns to previous
- [ ] **16.18** Click completed step in indicator → jumps to that step
- [ ] **16.19** Final "Weiter" → navigates to Dashboard (`/`)

---

## 17. Sonarr → FunkArr Search Flow (E2E)

**API-driven flow testing the complete integration.**

- [ ] **17.1** Trigger episode search via Sonarr API:
  ```
  POST http://localhost:8989/api/v3/command
  { "name": "EpisodeSearch", "episodeIds": [<episodeId>] }
  ```
- [ ] **17.2** FunkArr receives Newznab tvsearch request (check logs)
- [ ] **17.3** Mediathek query returns results
- [ ] **17.4** Scoring matches items with correct rule
- [ ] **17.5** Enrichment resolves season/episode via TVDB
- [ ] **17.6** Only 1 scoring history entry created (pagination cache working)
- [ ] **17.7** Sonarr receives results and initiates download via SABnzbd API
- [ ] **17.8** Download appears in FunkArr Activity (Aktiv tab) with progress
- [ ] **17.9** Download completes, appears in Verlauf tab as Completed
- [ ] **17.10** Downloaded file accessible at `/shared/downloads/complete`

---

## 18. Download Lifecycle

- [ ] **18.1** Active download shows cancel button → click cancels, toast shown *(requires: active download — timing-sensitive)*
- [ ] **18.2** Queued download shows cancel button → click removes from queue *(requires: queued download)*
- [ ] **18.3** Completed download in history → X button deletes entry
- [ ] **18.4** Failed download → "Wiederholen" button re-queues → toast "Wiederholung gestartet" *(requires: failed download)*
- [ ] **18.5** Retried download appears in queue/active again *(requires: failed download)*

---

## 19. Scoring Detail (API verification)

- [ ] **19.1** `GET /api/rulesets/{id}/history?limit=5` returns snapshots with requestId, source, query, candidateCount, matchedCount
- [ ] **19.2** `GET /api/rulesets/{id}/history/{requestId}` returns itemTraces with:
  - candidateTitle, candidateChannel, candidateDuration, candidateQuality
  - matched, score, matchedRuleId
  - identification (season, episode, title)
  - ruleTraces (per-rule outcome, filterTrace, identificationTrace)
  - enrichmentTrace (method, confidence, enriched, resolvedSeason/Episode/Title)

---

## 20. Newznab API

- [ ] **20.1** Caps: `GET /index/api?t=caps&apikey=<key>` → XML with server, limits, categories
- [ ] **20.2** TV search: `GET /index/api?t=tvsearch&tvdbid=83214&season=2026&ep=18&apikey=<key>` → Newznab RSS with items, season/episode attributes
- [ ] **20.3** Pagination: offset=0 and offset=100 return different items, correct total
- [ ] **20.4** General search: `GET /index/api?t=search&q=tatort&apikey=<key>`
- [ ] **20.5** Movie search: `GET /index/api?t=movie&imdbid=<id>&apikey=<key>`
- [ ] **20.6** Invalid API key → 403 error XML

---

## 21. SABnzbd API

- [ ] **21.1** Queue endpoint returns download status
- [ ] **21.2** History endpoint returns completed downloads

---

## 22. System API

- [ ] **22.1** `GET /api/system/version` → version info
- [ ] **22.2** `GET /api/system/setup` → health check results
- [ ] **22.3** `GET /api/system/storage` → disk usage
- [ ] **22.4** `GET /api/system/cache` → enrichment cache stats

---

## 23. RuleSet CRUD API

- [ ] **23.1** `GET /api/rulesets` → list all rulesets with stats
- [ ] **23.2** `GET /api/rulesets/{id}` → ruleset detail with enrichment config
- [ ] **23.3** `POST /api/rulesets` → create new ruleset
- [ ] **23.4** `PUT /api/rulesets/{id}` → update existing ruleset
- [ ] **23.5** `DELETE /api/rulesets/{id}` → delete ruleset
- [ ] **23.6** `GET /api/rulesets/{id}/raw` → raw ruleset YAML
- [ ] **23.7** `GET /api/rulesets/{id}/export` → community export
- [ ] **23.8** `POST /api/rulesets/test` → ad-hoc scoring test with enrichment

---

## Test Execution Order

For a clean E2E run, execute in this order:

1. **Startup**: Docker compose up, wait for health (sections 14, 22)
2. **Setup wizard**: Full walkthrough — health check, select services, configure Prowlarr/Sonarr/Radarr, copy buttons, manual expand, finish (sections 14-16)
3. **Dashboard**: Verify stat cards, links, version (section 1)
4. **Sidebar**: Collapse/expand, language switch all 4 locales, navigation links (section 2)
5. **Rulesets list**: Search, all filter combinations, both sort options, click through (section 6)
6. **Ruleset detail**: View Tatort — identity, enrichment, expand/collapse rules, export (sections 7, 11)
7. **Ruleset editor**: Open editor — edit every field type, add/remove aliases, toggle enrichment, add/remove rules and filters, live preview + full test, save (section 8)
8. **Create ruleset**: Create test ruleset with all fields, verify in list (section 9)
9. **Delete ruleset**: Delete test ruleset via confirmation flow, verify gone (section 10)
10. **Sonarr search**: Trigger search, verify pipeline end-to-end (section 17)
11. **Activity**: Active tab during download, Queue tab, History tab after completion (sections 3-5)
12. **Download lifecycle**: Cancel, retry on failure, delete history (section 18)
13. **Scoring history**: Click through to detail, filter tabs, expand traces (sections 12-13)
14. **Scoring verification**: API check for enrichment traces (section 19)
15. **API smoke tests**: Newznab, SABnzbd, System, RuleSet CRUD APIs (sections 20-23)
