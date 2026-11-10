# FunkArr E2E Test Plan

Complete browser + API test script covering every clickable element and interaction.

> **Note**: This file is a test specification - do not check off items here. Write test
> results to a separate results file so this plan stays reusable across runs.

**Prerequisites**: `docker compose -f docker-compose.dev.yml up -d --build`, wait for FunkArr on port 6969.  
**Arr services**: Sonarr (8989), Radarr (7878), Prowlarr (9696) - all with API key `funkarr-dev-api-key-01`.

**Credentials & Endpoints**:
- API Key for all services: `funkarr-dev-api-key-01`
- Newznab API: `GET http://localhost:6969/index/api?t=<type>&apikey=funkarr-dev-api-key-01`
- SABnzbd API: `GET http://localhost:6969/download/api?mode=<mode>&apikey=funkarr-dev-api-key-01`
- Internal API: `http://localhost:6969/api/...` (no API key)
- Docker-internal FunkArr hostname: `funkarr` (for Arr service URLs: `http://funkarr:6969`)

---

## Test Scenarios & Setup Recipes

Every test must be executed - no skips allowed. The execution order (bottom of this file)
sets up each scenario deterministically before the tests that need it.

| Scenario | Setup recipe | Required for |
|---|---|---|
| **Clean slate** | Fresh volumes, no data | 0.x, 14.x (health), initial dashboard, 30.x (feed empty) |
| **Sonarr ready** | Tatort added (tvdbId 83214), root folder `/shared/tv` | 17.x |
| **Radarr ready** | Movie added (e.g. Schachnovelle tmdbId 1740919), root folder `/shared/movies` | 26.x |
| **Services configured** | Setup wizard configures Prowlarr; FunkArr setup API creates indexers directly in Sonarr/Radarr (Prowlarr does not auto-sync without user-configured application mappings) | 17.x, 26.x |
| **Local ruleset** | `POST /api/rulesets` with local-only ruleset | 6.9-6.11, 7.9-7.12, 10.x, 11.x |
| **Merged ruleset** | Edit Tatort via UI editor, save -> creates local overlay | 7.8, 7.10-7.11, 10.1-10.2, 11.x |
| **Paused pipeline** | `POST /api/downloads/queue/pause` before triggering searches | 3.x, 4.x, 25.x, 28.x |
| **Queued downloads** | Pause pipeline, trigger 3+ episode searches -> items queue | 3.7, 4.2-4.4, 25.3-25.6, 28.6-28.9 |
| **Active download** | Resume pipeline -> downloads become active | 3.3-3.6, 18.1 |
| **Failed download** | Upload NZB with invalid URL via SABnzbd addfile | 5.6-5.8, 18.4-18.5, 21.14 |
| **Pagination data** | Trigger searches to generate scoring entries (count non-deterministic) | 5.10, 12.5-12.6 |
| **Post-search** | Completed downloads + scoring history exist | 1.5-1.8, 5.x, 12.x, 13.x, 19.x, 30.x (feed populated) |
| **Feed populated** | Feed ingest has run and published entries | 30.x, 31.x, 33.x |

### Setup Recipe: Paused pipeline with queued items

Pause the download pipeline first, then trigger searches. Downloads queue instead of
starting immediately.

```powershell
$apiKey = "funkarr-dev-api-key-01"

# Pause pipeline
Invoke-RestMethod "http://localhost:6969/api/downloads/queue/pause" -Method Post

# Trigger 3 episode searches (items queue since pipeline is paused)
@(1403, 1404, 1405) | ForEach-Object {
    $body = "{`"name`":`"EpisodeSearch`",`"episodeIds`":[$_]}"
    Invoke-RestMethod "http://localhost:8989/api/v3/command?apikey=$apiKey" -Method Post `
        -ContentType "application/json" -Body $body
    Start-Sleep -Seconds 5  # wait for scoring + grab
}

# Now test queue operations (4.x, 25.x, 28.x) while items are queued
# Then resume to test active downloads (3.4-3.7):
# Invoke-RestMethod "http://localhost:6969/api/downloads/queue/resume" -Method Post
```

### Setup Recipe: Failed download via NZB upload

Upload a crafted NZB with an unreachable URL. The download worker will fail when it
tries to connect.

```powershell
$nzbXml = @'
<?xml version="1.0" encoding="utf-8"?>
<nzb xmlns="http://www.newzbin.com/DTD/2003/nzb">
  <head>
    <meta type="title">E2E-Failed-Download-Test</meta>
    <meta type="X-FunkArr-Url">http://192.0.2.1/nonexistent-video.mp4</meta>
    <meta type="X-FunkArr-Channel">TEST</meta>
    <meta type="X-FunkArr-Duration">60</meta>
    <meta type="X-FunkArr-Size">1000</meta>
    <meta type="X-FunkArr-Category">show</meta>
  </head>
  <file post_id="1">
    <groups><group>a.b.mediathek</group></groups>
    <segments><segment number="1">test@news.example.com</segment></segments>
  </file>
</nzb>
'@

# Write NZB to temp file and upload via multipart form
$tempNzb = [System.IO.Path]::GetTempFileName() + ".nzb"
$nzbXml | Set-Content $tempNzb -Encoding UTF8

# Upload via curl - API key MUST be a query parameter, not a form field
curl -s -X POST "http://localhost:6969/download/api?apikey=funkarr-dev-api-key-01&mode=addfile&cat=show" `
    -F "name=@$tempNzb"

Remove-Item $tempNzb
# Wait for download to fail (unreachable URL times out)
Start-Sleep -Seconds 30
```

### Setup Recipe: Radarr movie search

```powershell
$apiKey = "funkarr-dev-api-key-01"

# Add Schachnovelle via TMDB ID lookup (must match the community ruleset's tmdbId=1740919,
# imdbId=tt9781494). A title search may return a different TMDB entry with the same name.
$lookup = Invoke-RestMethod "http://localhost:7878/api/v3/movie/lookup/tmdb?tmdbId=1740919&apikey=$apiKey"
$movie = $lookup | ConvertTo-Json -Depth 10 | ConvertFrom-Json
$movie | Add-Member -NotePropertyName qualityProfileId -NotePropertyValue 1 -Force
$movie | Add-Member -NotePropertyName rootFolderPath -NotePropertyValue "/shared/movies" -Force
$movie | Add-Member -NotePropertyName monitored -NotePropertyValue $true -Force
$movie | Add-Member -NotePropertyName addOptions -NotePropertyValue @{searchForMovie=$false} -Force
Invoke-RestMethod "http://localhost:7878/api/v3/movie?apikey=$apiKey" -Method Post `
    -ContentType "application/json" -Body ($movie | ConvertTo-Json -Depth 10)

# Get movie ID
$movies = Invoke-RestMethod "http://localhost:7878/api/v3/movie?apikey=$apiKey"
$movieId = ($movies | Where-Object { $_.tmdbId -eq 1740919 }).id

# Trigger movie search
$searchBody = "{`"name`":`"MoviesSearch`",`"movieIds`":[$movieId]}"
Invoke-RestMethod "http://localhost:7878/api/v3/command?apikey=$apiKey" -Method Post `
    -ContentType "application/json" -Body $searchBody
```

### Setup Recipe: Generate scoring history entries

Trigger Sonarr searches to create scoring history entries. Note: the
MediathekViewWeb query queue is bounded, so rapid-fire searches will saturate it
and many will fail silently (no scoring entry recorded). Space searches 5+ seconds
apart and expect ~50% success rate. The exact entry count is non-deterministic.

```powershell
$apiKey = "funkarr-dev-api-key-01"

# Get episode IDs spread across seasons 2020-2026 (1 per season)
$episodes = Invoke-RestMethod "http://localhost:8989/api/v3/episode?seriesId=1&apikey=$apiKey"
$targets = $episodes `
    | Where-Object { $_.seasonNumber -ge 2015 -and $_.seasonNumber -le 2026 } `
    | Group-Object seasonNumber `
    | ForEach-Object { $_.Group | Select-Object -First 1 } `
    | Select-Object -First 12

# Trigger searches with generous spacing to avoid queue saturation
foreach ($ep in $targets) {
    $body = "{`"name`":`"EpisodeSearch`",`"episodeIds`":[$($ep.id)]}"
    Invoke-RestMethod "http://localhost:8989/api/v3/command?apikey=$apiKey" -Method Post `
        -ContentType "application/json" -Body $body
    Start-Sleep -Seconds 5
}

# Verify: expect 5-12 entries (queue saturation limits throughput)
Start-Sleep -Seconds 15
$scoring = Invoke-RestMethod "http://localhost:6969/api/rulesets/tatort/history?limit=1"
Write-Host "Tatort scoring entries: $($scoring.totalCount)"
```

### Setup Recipe: Generate history pagination data (>25 history entries)

Upload 20 NZB files with unreachable URLs via SABnzbd addfile. These fail within
seconds, producing history entries. Split across categories (show + movie) to enable
category filter testing (5.3). Combined with entries from real Sonarr grabs, this
exceeds the history pageSize of 25.

```powershell
$apiKey = "funkarr-dev-api-key-01"

# Upload 10 NZBs with category=show and 10 with category=movie
foreach ($i in 1..20) {
    $cat = if ($i -le 10) { "show" } else { "movie" }
    $title = "E2E-Pagination-$cat-$i"
    $nzb = "<?xml version=`"1.0`" encoding=`"utf-8`"?><nzb xmlns=`"http://www.newzbin.com/DTD/2003/nzb`"><head><meta type=`"title`">$title</meta><meta type=`"X-FunkArr-Url`">http://192.0.2.1/pagination-$i.mp4</meta><meta type=`"X-FunkArr-Channel`">TEST</meta><meta type=`"X-FunkArr-Duration`">60</meta><meta type=`"X-FunkArr-Size`">1000</meta><meta type=`"X-FunkArr-Category`">$cat</meta></head><file post_id=`"1`"><groups><group>a.b.mediathek</group></groups><segments><segment number=`"1`">pagination-$i@e2e</segment></segments></file></nzb>"
    $tempNzb = Join-Path $env:TEMP "e2e-pagination-$i.nzb"
    $nzb | Set-Content $tempNzb -Encoding UTF8
    curl -s -X POST "http://localhost:6969/download/api?apikey=$apiKey&mode=addfile&cat=$cat" -F "name=@$tempNzb" | Out-Null
    Remove-Item $tempNzb
}

# Wait for all NZBs to fail (poll until history has enough entries)
$maxRetries = 30
for ($i = 0; $i -lt $maxRetries; $i++) {
    $hist = Invoke-RestMethod "http://localhost:6969/download/api?mode=history&apikey=$apiKey"
    $count = $hist.history.slots.Count
    if ($count -ge 26) { Write-Host "History has $count entries (>= 26)"; break }
    Write-Host "History: $count entries, waiting..."
    Start-Sleep -Seconds 5
}
```

### Setup Recipe: SSE reconnect test

Test from the browser using JavaScript to close and reopen the EventSource:

```javascript
// Close all EventSource connections to simulate disconnect
performance.getEntriesByType('resource')
  .filter(r => r.name.includes('/stream'))
  .forEach(() => { /* EventSource will auto-reconnect */ });

// Or use the browser devtools Network panel to throttle/block the stream URL
// then unblock - the composable's onerror handler fires and EventSource reconnects
```

Alternatively, pause and resume the FunkArr container briefly:
```powershell
docker pause funkarr; Start-Sleep -Seconds 3; docker unpause funkarr
```

### Setup Wizard Notes

The setup wizard URL fields show placeholders (e.g. `http://sonarr:8989`) but the value is
empty - the user must type both URL and API key for the create buttons to enable. When testing
with Chrome automation, use the `type` action (physical keyboard) after clicking the field.
`nativeInputValueSetter` or `form_input` may not trigger Vue's v-model reliably.

Docker networking: Prowlarr/Sonarr/Radarr must reach FunkArr at `http://funkarr:6969` (Docker
internal DNS). The "Indexer erstellen" / "Download-Client erstellen" buttons make a test
connection - this only works when all containers are on the same Docker network. From `localhost`
(the browser), use the FunkArr setup API (`POST /api/setup/{service}/{resource}`) as a fallback.

**Prowlarr indexer sync**: The setup wizard creates the FunkArr indexer in Prowlarr, but does
NOT configure Prowlarr application mappings (`/api/v1/applications`). Without those mappings,
Prowlarr does not sync the indexer to Sonarr/Radarr. This is expected — configuring Prowlarr
apps is the user's responsibility. For E2E testing, create indexers directly in Sonarr/Radarr
via `POST /api/setup/sonarr/indexer` and `POST /api/setup/radarr/indexer` after the wizard.

### API Ruleset Roundtrip Pitfalls

When reading a ruleset via `GET /api/rulesets/{id}` and writing it back via `PUT`, watch for
these traps:

1. **Regex pattern escaping**: The API returns regex patterns as unescaped strings (e.g.
   `\s*` with a single backslash). When embedding these in a JSON body for PUT/POST, each
   backslash must be JSON-escaped to `\\`. PowerShell here-strings (`@'...'@`) pass content
   literally, so `\\s*` in a here-string becomes JSON `\\s*` which decodes to the string `\s*`
   (correct). But if you double-escape to `\\\\s*`, the API receives `\\s*` (literal backslash+s),
   which breaks the regex. **Always verify**: after a PUT, `GET /api/rulesets/{id}/raw` and
   compare the `pattern` fields against the community source file.

2. **Dropped fields on roundtrip**: The detail endpoint (`GET /api/rulesets/{id}`) returns a
   read-model with fields like `identity.topic`, `source`, `defaultConfidence`. The write
   endpoint (`PUT /api/rulesets/{id}`) expects a flat body with `topic`, `media`, `rules`,
   `aliases`, `enrichment`. If you GET the detail and naively map it back, you will lose:
   - **Filters** on rules (the detail response nests them differently)
   - **Confidence** at the ruleset level vs rule level
   - **Enrichment runtime/year** config (only title+airdate are commonly shown)

   To safely roundtrip, either use the **raw** endpoint (`GET /api/rulesets/{id}/raw`) which
   returns the on-disk JSON format, or make minimal changes (e.g. only add an alias) rather
   than replacing the entire ruleset.

3. **Enum values**: The API uses **numeric** enum values (strategy: 4 = TitleIncludes,
   titleRule type: 1 = Static, field: 1 = Title; Unknown=0 is rejected). The on-disk format uses **string** enum
   values (strategy: "itemTitleIncludes", type: "static", field: "title"). Don't mix them.

4. **Rule ID pattern**: Rule IDs must match `^[a-z][a-z0-9-]{2,}$` (lowercase, 3+ chars,
   start with letter). Short IDs like "r1" will fail schema validation.

---

## 0. Clean Slate - Reset from Previous Run

Run this before a fresh E2E test to wipe all state from a previous run.

### Full reset (nuclear - removes ALL data)

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

### Re-add series to Sonarr after reset

Add Tatort (for search tests) and In aller Freundschaft (for RSS sync tests — must not
be searched manually so RSS sync can grab fresh episodes).

```powershell
$apiKey = "funkarr-dev-api-key-01"

# Register root folder first (required on fresh volumes)
Invoke-RestMethod "http://localhost:8989/api/v3/rootfolder?apikey=$apiKey" -Method Post -ContentType "application/json" -Body '{"path":"/shared/tv"}'

# Create /shared/tv and /shared/movies dirs, writable by all containers
docker exec funkarr mkdir -p /shared/tv /shared/movies
docker exec -u root funkarr chmod 777 /shared/tv /shared/movies

# Add Tatort (for manual search tests)
$body = '{"title":"Tatort","tvdbId":83214,"qualityProfileId":1,"rootFolderPath":"/shared/tv","monitored":true,"seasonFolder":false,"addOptions":{"searchForMissingEpisodes":false}}'
Invoke-RestMethod "http://localhost:8989/api/v3/series?apikey=$apiKey" -Method Post -ContentType "application/json" -Body $body

# Add In aller Freundschaft (for RSS sync test — do NOT trigger manual searches)
$body = '{"title":"In aller Freundschaft","tvdbId":147521,"qualityProfileId":1,"rootFolderPath":"/shared/tv","monitored":true,"seasonFolder":false,"addOptions":{"searchForMissingEpisodes":false}}'
Invoke-RestMethod "http://localhost:8989/api/v3/series?apikey=$apiKey" -Method Post -ContentType "application/json" -Body $body
```

---

## 1. Dashboard (Übersicht)

**Route**: `/`

- [ ] **1.1** Stat cards visible: System (Alles OK), Letzte Downloads, Speicher (used/total + bar), Regelwerke (count + version)
- [ ] **1.2** System stat card click navigates to `/setup`
- [ ] **1.3** Recent Downloads stat card click navigates to `/activity/history`
- [ ] **1.4** Regelwerke stat card click navigates to `/rulesets`
- [ ] **1.5** Download status bar shows active/waiting counts
- [ ] **1.6** "Anzeigen" link in status bar navigates to `/activity`
- [ ] **1.7** Letzte Aktivität shows entries with status indicator (green=completed, orange=downloading, red=failed), title, episode info, size, relative timestamp
- [ ] **1.8** "Alle anzeigen" link navigates to `/activity/history`
- [ ] **1.9** Version number shown bottom-left (v0.0.0-dev or current)

---

## 2. Sidebar & Global Navigation

- [ ] **2.1** FunkArr logo click navigates to `/`
- [ ] **2.2** Sidebar nav: Übersicht → `/`, Aktivität → `/activity`, Regelwerke → `/rulesets`, Feed → `/feed`
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

## 3. Activity - Queue

**Route**: `/activity`

Unified view with filter chips: All, Active, Queued, Completed, Failed. Each chip shows
a count badge. The "All" filter is the default.

- [ ] **3.1** Filter chips visible: All, Active, Queued, Completed, Failed — All is default, each shows a count
- [ ] **3.2** Empty state: "No downloads" message when idle
- [ ] **3.3** During download: shows title, episode, quality badge (1080p), channel tag, size tag, duration tag, SUB badge (if subtitles)
- [ ] **3.4** Progress bar with %, download speed (KB/s), ETA
- [ ] **3.5** Global speed indicator in page header
- [ ] **3.6** Cancel button (X) on active download → toast "Download abgebrochen"
- [ ] **3.7** Click "Active" filter chip → shows only active (downloading) items

---

## 4. Activity - Queue Details

- [ ] **4.1** Search field visible and filters queue items
- [ ] **4.2** Badge count on Queue tab label when items present
- [ ] **4.3** Shows queued items with title, episode, quality, size, type (show/movie)
- [ ] **4.4** X button removes item from queue → toast "Download abgebrochen"

---

## 5. Activity - History

**Route**: `/activity` → click Completed or Failed filter chip

- [ ] **5.1** Click "Completed" filter chip → shows only completed downloads; click "Failed" → shows only failed
- [ ] **5.2** Table with columns: Titel, Qualität (badge), Größe, Dauer, Status, Abgeschlossen
- [ ] **5.3** Category filter dropdown ("Alle Kategorien") → filters by download category
- [ ] **5.4** Search field filters history entries
- [ ] **5.5** Completed status: green dot + "Completed"
- [ ] **5.6** Failed status: red dot + "Failed"
- [ ] **5.7** Failed item: expandable `<details>` shows full error message
- [ ] **5.8** Retry button (only on Failed items) → toast "Wiederholung gestartet", item re-queued
- [ ] **5.9** X button deletes history entry → toast "Eintrag gelöscht"
- [ ] **5.10** Pagination: "Zurück" / "Weiter" buttons when totalPages > 1

---

## 6. RuleSets - List

**Route**: `/rulesets`

- [ ] **6.1** Shows all rulesets with title, aliases, rule count, IMDB/TVDB IDs
- [ ] **6.2** Source badges: community (orange), merged (white)
- [ ] **6.3** Scoring stats visible for rulesets with history (Trefferquote, enrichment %, last scored)
- [ ] **6.4** Search input: type "tatort" → filters to matching rulesets
- [ ] **6.5** Clear search → all rulesets shown again
- [ ] **6.6** Type filter: click "Serien" → only series, count updates
- [ ] **6.7** Type filter: click "Filme" → only movies, count updates *(0 if no movie rulesets exist)*
- [ ] **6.8** Type filter: click "Alle" → reset *(default state - test after using another filter)*
- [ ] **6.9** Source filter: click "Community" → filtered count "X Regelwerke von Y"
- [ ] **6.10** Source filter: click "Lokal" → only merged/local rulesets
- [ ] **6.11** Source filter: click "Alle Quellen" → reset
- [ ] **6.12** Sort dropdown: "Nach Name sortieren" → alphabetical by topic
- [ ] **6.13** Sort dropdown: "Nach ID sortieren" → alphabetical by ruleset ID
- [ ] **6.14** "+ Neu" link navigates to `/rulesets/new`
- [ ] **6.15** Click on ruleset row navigates to `/rulesets/{id}`

---

## 7. RuleSet - Detail

**Route**: `/rulesets/{id}`

- [ ] **7.1** Breadcrumb links: "Regelwerke" → `/rulesets`
- [ ] **7.2** Identity section: Regelwerk-ID, Aliase, TVDB, IMDB, TMDB, Quelle (badge + timestamp)
- [ ] **7.3** Enrichment section: Enabled badge, method tags (title/airdate/runtime/year), thresholds
- [ ] **7.4** Matching rules: click rule header → expand/collapse rule details
- [ ] **7.5** Expanded rule shows: strategy, title parts, regex patterns, filters
- [ ] **7.6** "Scoring-Verlauf" button navigates to `/rulesets/{id}/history`
- [ ] **7.7** "Bearbeiten" button navigates to `/rulesets/{id}/edit`
- [ ] **7.8** "Für Community exportieren" button → toast "Erfolgreich exportiert" or validation error
- [ ] **7.9** "Lokal löschen" button → shows confirmation panel
- [ ] **7.10** Delete confirmation: "Bestätigen" → deletes local overlay, reloads as community-only or navigates to list
- [ ] **7.11** Delete confirmation: "Abbrechen" → hides confirmation panel
- [ ] **7.12** Delete on local-only ruleset → navigates to `/rulesets`

---

## 8. RuleSet - Editor

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

## 9. RuleSet - Create New

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

## 10. RuleSet - Delete

- [ ] **10.1** "Lokal löschen" on merged ruleset → confirmation panel
- [ ] **10.2** Confirm → removes local override, detail reloads as community-only
- [ ] **10.3** "Lokal löschen" on local-only ruleset → confirmation panel
- [ ] **10.4** Confirm → deletes completely, navigates to `/rulesets`
- [ ] **10.5** Cancel → confirmation panel disappears, no changes
- [ ] **10.6** Ruleset disappears from list after deletion

---

## 11. RuleSet - Export

- [ ] **11.1** Click "Für Community exportieren" on detail page
- [ ] **11.2** Valid ruleset → toast "Erfolgreich exportiert"

---

## 12. Scoring History

**Route**: `/rulesets/{id}/history`

- [ ] **12.1** Breadcrumb links: "Regelwerke" → `/rulesets`, "{id}" → `/rulesets/{id}`
- [ ] **12.2** Total count header ("X Bewertungsdurchläufe insgesamt")
- [ ] **12.3** Table with: Quelle, Abfrage, Zeitpunkt, Kandidaten, Treffer
- [ ] **12.4** Click on table row → navigates to `/rulesets/{id}/history/{requestId}`
- [ ] **12.5** Pagination API: `GET /api/rulesets/{id}/history?limit=2` returns at most 2 snapshots with correct `totalCount`
- [ ] **12.6** Pagination API: `GET /api/rulesets/{id}/history?limit=2&offset=2` returns different snapshots than offset=0
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

## 14. Setup Wizard - System Health

**Route**: `/setup`

- [ ] **14.1** Step indicator shows: 1 Systemprüfung → 2 Dienste
- [ ] **14.2** Health checks all green: API-Schlüssel, MediathekViewWeb, Datenverzeichnis, Zielverzeichnis, Temporärverzeichnis, Indexer-API, Download-API, FFmpeg (with version)
- [ ] **14.3** Click "Erneut prüfen" → re-runs health checks
- [ ] **14.4** Click "Weiter" → advances to Dienste step

---

## 15. Setup Wizard - Services (Dienste)

**Route**: `/setup` → step 2

- [ ] **15.1** Checkboxes for Prowlarr, Sonarr, Radarr with descriptions
- [ ] **15.2** Check Prowlarr → step indicator adds Prowlarr step
- [ ] **15.3** Check Sonarr → step indicator adds Sonarr step
- [ ] **15.4** Check Radarr → step indicator adds Radarr step
- [ ] **15.5** Uncheck all → "Weiter" disabled (requires ≥1 service selected)
- [ ] **15.6** Click "Zurück" → returns to Systemprüfung
- [ ] **15.7** Click "Weiter" → advances to first selected service

---

## 16. Setup Wizard - Service Configuration

**Route**: `/setup` → per-service steps

### Prowlarr
- [ ] **16.1** URL field shows placeholder `http://prowlarr:9696` (value empty - must type)
- [ ] **16.2** API-Schlüssel password field - type API key (both URL + key required for buttons to enable)
- [ ] **16.3** FunkArr-URL optional field shows placeholder `http://funkarr:6969`
- [ ] **16.4** Click "Indexer erstellen" → creates Newznab indexer in Prowlarr
- [ ] **16.5** Success state shown after creation (button changes)
- [ ] **16.6** Duplicate creation → error "Should be unique" shown gracefully
- [ ] **16.7** Click "Manuell konfigurieren" → expands manual instructions
- [ ] **16.8** Click copy button on manual field → clipboard + toast "In Zwischenablage kopiert"

### Sonarr
- [ ] **16.9** URL placeholder `http://sonarr:8989` (must type value)
- [ ] **16.10** Type API key into password field (both URL + key required)
- [ ] **16.11** Click "Indexer erstellen" → creates Newznab indexer in Sonarr
- [ ] **16.12** Click "Download-Client erstellen" → creates SABnzbd client
- [ ] **16.13** Success / duplicate error for both

### Radarr
- [ ] **16.14** URL placeholder `http://radarr:7878` (must type value)
- [ ] **16.15** Type API key into password field
- [ ] **16.16** Click "Indexer erstellen" → creates Newznab indexer in Radarr. **Note**: Radarr's
  test query (`t=movie` with no criteria) may fail when no movie entries exist in the feed —
  the validation item is only returned when the feed is completely empty. If this happens,
  create the indexer via the Radarr API directly.
- [ ] **16.17** Click "Download-Client erstellen" → creates SABnzbd client
- [ ] **16.18** Success / duplicate error for both

### Navigation
- [ ] **16.19** "Zurück" on each step returns to previous
- [ ] **16.20** Click completed step in indicator → jumps to that step
- [ ] **16.21** Final "Weiter" → navigates to Dashboard (`/`)

---

## 17. Sonarr → FunkArr Search Flow (E2E with Import)

**API-driven flow testing the complete integration from search to Sonarr import.**

The test is only complete when Sonarr has imported the file — `downloadFolderImported`
in the Sonarr history. A download that stays in `importBlocked` is a failure (usually
a permissions issue on the shared volume).

**Permissions**: FunkArr's `DataFiles` sets 666 (files) and 777 (directories) on every
completed download automatically. No chmod on `/shared/downloads/complete/` is needed —
but Sonarr must be able to write to the import targets (`/shared/tv`, `/shared/movies`).
These are created with 777 in the Sonarr setup recipe above. **The critical requirement
is waiting for all downloads to complete** before triggering `ProcessMonitoredDownloads` —
running it while downloads are still in progress causes "Access denied" on files that
haven't been moved to `complete/` yet.

- [ ] **17.1** Trigger season search via Sonarr API (season search finds more matches than single-episode):
  ```
  POST http://localhost:8989/api/v3/command
  { "name": "SeasonSearch", "seriesId": <seriesId>, "seasonNumber": <n> }
  ```
- [ ] **17.2** FunkArr receives Newznab tvsearch request (check FunkArr logs for `tvsearch` query)
- [ ] **17.3** Sonarr reports "Season search completed. N reports downloaded." with N > 0
- [ ] **17.4** FunkArr download queue shows grabbed items (`GET /download/api?mode=queue`)
- [ ] **17.5** Wait for **all** downloads to complete: poll FunkArr history until the expected items appear as Completed/Failed (not Sonarr queue — FunkArr controls the download lifecycle)
- [ ] **17.6** Trigger `ProcessMonitoredDownloads` command (no chmod needed — `DataFiles` sets permissions)
- [ ] **17.7** Sonarr history shows `downloadFolderImported` events for the searched episodes
- [ ] **17.8** Sonarr episode files exist: `GET /api/v3/episodefile?seriesId=<id>` returns files with correct season/quality
- [ ] **17.9** MKV files on disk at `/shared/tv/<series>/<season>/`
✅ **PASS criteria**: Sonarr history has `downloadFolderImported` for at least 1 episode from the search

---

## 18. Download Lifecycle

- [ ] **18.1** Active download shows cancel button → click cancels, toast shown
- [ ] **18.2** Queued download shows cancel button → click removes from queue
- [ ] **18.3** Completed download in history → X button deletes entry
- [ ] **18.4** Failed download → "Wiederholen" button re-queues → toast "Wiederholung gestartet"
- [ ] **18.5** Retried download appears in queue/active again

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
- [ ] **20.1a** Caps tv-search declares `supportedParams="q,season,ep,tvdbid,imdbid,tmdbid"` (all six params)
- [ ] **20.2** TV search: `GET /index/api?t=tvsearch&tvdbid=83214&season=2026&apikey=<key>` → Newznab RSS with items, season/episode attributes (season-only search; specific episode may not be in Mediathek)
- [ ] **20.3** Pagination: offset=0 and offset=100 return different items, correct total
- [ ] **20.4** General search: `GET /index/api?t=search&q=tatort&apikey=<key>`
- [ ] **20.5** Movie search by IMDB: `GET /index/api?t=movie&imdbid=<id>&apikey=<key>`
- [ ] **20.6** Movie search by TMDB: `GET /index/api?t=movie&tmdbid=<id>&apikey=<key>`
- [ ] **20.7** Invalid API key → HTTP 200 with XML `<error code="100" description="Invalid API Key"/>` (not 403)
- [ ] **20.8** Missing API key → HTTP 200 with same XML error structure

---

## 21. SABnzbd API

### Protocol
- [ ] **21.1** `GET /download/api?mode=version&apikey=<key>` → version string
- [ ] **21.2** `GET /download/api?mode=get_config&apikey=<key>` → config JSON with `misc.history_retention_option: "all"`, `misc.history_retention_number: 0`, `misc.tv_categories` and `misc.movie_categories` populated from configured categories, `mb`/`mbleft` fields use decimal format (e.g. `"100.0"` not `"100"`)
- [ ] **21.3** `GET /download/api?mode=fullstatus&apikey=<key>` → status with speed, disk space
- [ ] **21.4** Invalid API key → HTTP 200 with `{"status":false,"error":"API Key Required"}` (not 403)
- [ ] **21.4a** Missing API key → same HTTP 200 error response

### Queue Operations
- [ ] **21.5** `GET /download/api?mode=queue&apikey=<key>` → queue listing, verify `mb`/`mbleft` have decimal format (e.g. `"100.0"`)
- [ ] **21.6** `POST /download/api` with `mode=addfile` + NZB multipart → adds to queue
- [ ] **21.7** `GET /download/api?mode=queue&name=delete&value=<id>&apikey=<key>` → removes item
- [ ] **21.8** `GET /download/api?mode=queue&name=priority&value=<id>&value2=1&apikey=<key>` → `{"status":false,"error":"Invalid queue command"}` (removed — use internal API)
- [ ] **21.9** `GET /download/api?mode=queue&name=switch&value=<id1>&value2=<id2>&apikey=<key>` → `{"status":false,"error":"Invalid queue command"}` (removed — use internal API)
- [ ] **21.10** `GET /download/api?mode=pause&apikey=<key>` → `{"status":false,"error":"Invalid mode"}` (removed — use internal API)
- [ ] **21.11** `GET /download/api?mode=resume&apikey=<key>` → `{"status":false,"error":"Invalid mode"}` (removed — use internal API)

### History
- [ ] **21.12** `GET /download/api?mode=history&apikey=<key>` → history listing
- [ ] **21.13** `GET /download/api?mode=history&name=delete&value=<id>&apikey=<key>` → deletes entry
- [ ] **21.14** `GET /download/api?mode=retry&value=<id>&apikey=<key>` → retries failed download

---

## 22. System API

- [ ] **22.1** `GET /api/system/version` → version info
- [ ] **22.2** `GET /api/system/setup` → health check results
- [ ] **22.3** `GET /api/system/storage` → disk usage
- [ ] **22.4** `GET /api/system/routes` → network route configuration (Direct default + Austria proxy)
- [ ] **22.5** `GET /api/system/variants` → variant policies (4 entries, default all Exclude)
- [ ] **22.6** `GET /api/system/logs` → recent log entries from ring buffer
- [ ] **22.7** `GET /api/system/logs/stream` → SSE connection opens, log entries stream in real-time
- [ ] **22.8** `GET /openapi/v1.json` → valid OpenAPI spec document with all /api endpoints (includes /api/feed routes)

---

## 23. RuleSet CRUD API

- [ ] **23.1** `GET /api/rulesets` → list all rulesets with stats
- [ ] **23.2** `GET /api/rulesets/{id}` → ruleset detail with enrichment config
- [ ] **23.3** `POST /api/rulesets` → create new ruleset
- [ ] **23.4** `PUT /api/rulesets/{id}` → update existing ruleset
- [ ] **23.5** `DELETE /api/rulesets/{id}` → delete ruleset
- [ ] **23.6** `GET /api/rulesets/{id}/raw` → raw ruleset JSON (on-disk format with string enums)
- [ ] **23.7** `GET /api/rulesets/{id}/export` → community export. **Note:** Tested in Step 13 (requires local overlay to exist). Skipped in Step 37 API smoke tests.
- [ ] **23.8** `POST /api/rulesets/test` with `{ defaultConfidence, rules: [{ id, strategy, ... }], candidates: [{ title, topic, channel, duration }] }` → returns `{ itemTraces: [{ candidate, matched, score, ruleTraces }] }`

---

## 24. Settings Page

**Route**: `/settings`

- [ ] **24.1** Page loads with all sections: Download-Einstellungen, Broadcast Variants, Netzwerkrouten, System, Logs
- [ ] **24.2** Download settings: shows concurrent download limit and schedule config
- [ ] **24.3** Network routes: configured routes listed (Direct, Austria proxy)
- [ ] **24.4** System info: version, storage usage
- [ ] **24.5** Log viewer: recent log entries visible
- [ ] **24.6** Log viewer: new log entries appear in real-time (SSE stream)

---

## 25. Download Queue Operations

- [ ] **25.1** Pause button → pauses all downloads, toast confirmation
- [ ] **25.2** Resume button → resumes paused pipeline, toast confirmation
- [ ] **25.3** Force-start on queued item → bypasses concurrency limit, starts immediately
- [ ] **25.4** Set priority on queue item via `{"priority": 4}` (integer enum: 2=Low, 3=Normal, 4=High; 0 and 1 rejected) → item moves to correct priority section
- [ ] **25.5** Move download to different position in queue
- [ ] **25.6** Swap two downloads' positions
- [ ] **25.7** Download detail page (`/activity/{id}`) loads with full download info

---

## 26. Radarr Search Flow (E2E)

**API-driven flow testing Radarr integration (movie search path).**

> **Execution note:** Run with empty queue before Sonarr searches (Steps 21a-21b).
> Pipeline must be active for download. Re-pause after completion (Step 21c).

- [ ] **26.1** Add a movie to Radarr (or use existing) with root folder `/shared/movies`
- [ ] **26.2** Trigger movie search via Radarr API or UI
- [ ] **26.3** FunkArr receives Newznab movie search request (check logs)
- [ ] **26.4** Scoring matches items, TMDB enrichment resolves metadata
- [ ] **26.5** Radarr receives results and can initiate download via SABnzbd API
- [ ] **26.6** Download appears in FunkArr Activity queue
- [ ] **26.7** Download completes, Radarr sees completed item

---

## 27. Mediathek Search API

- [ ] **27.1** `GET /api/mediathek/search?q=Tatort&limit=5` → JSON with `items` array and `totalResults` count
- [ ] **27.2** Channel filter: `?q=Tatort&channel=ARD&limit=5` → filtered items
- [ ] **27.3** Topic filter: `?topic=Tagesschau&limit=5` → topic-specific items
- [ ] **27.4** Pagination: `?q=Tatort&offset=0&limit=5` then `offset=5&limit=5` → different result sets
- [ ] **27.5** Duration filter: `?q=Tatort&durationMin=600&limit=5` → items with duration >= 600
- [ ] **27.6** Sort parameter: `?q=Tatort&sortBy=timestamp` → results (verify 200 OK, only `timestamp` is valid)

---

## 28. Download API (Internal)

- [ ] **28.1** `GET /api/downloads/history/stats` → completed/failed counts, bytes, avg time, success rate
- [ ] **28.2** `GET /api/downloads/history/categories` → distinct category list
- [ ] **28.3** `GET /api/downloads/settings` → concurrency limit, schedule config
- [ ] **28.4** `POST /api/downloads/queue/pause` → pauses pipeline (200)
- [ ] **28.5** `POST /api/downloads/queue/resume` → resumes pipeline (200)
- [ ] **28.6** `POST /api/downloads/queue/{id}/force-start` → bypasses limit
- [ ] **28.7** `POST /api/downloads/queue/{id}/move` → repositions item
- [ ] **28.8** `POST /api/downloads/queue/{id}/priority` with body `{"priority": 4}` (integer enum: 2=Low, 3=Normal, 4=High; 0 and 1 rejected with 400) → 200
- [ ] **28.9** `POST /api/downloads/queue/swap` → swaps two items

---

## 29. Broadcast Variants

### Settings UI

- [ ] **29.1** Navigate to `/settings` → "Broadcast variants" section visible
- [ ] **29.2** Shows four variant types: Audio Description, Sign Language, Plain Language, Original Version
- [ ] **29.3** Default policy for all variants: "Excluded" (muted label)
- [ ] **29.4** Hint text: config key `FunkArr__Variants__<Variant>=Allow or Exclude`

### API

- [ ] **29.5** `GET /api/system/variants` → JSON array with 4 entries, each has `variant` and `policy` fields
- [ ] **29.6** Default response: all variants have `policy: "Exclude"`

### Scoring Detail Integration

- [ ] **29.7** After a Sonarr search that includes variant entries: scoring detail shows orange "Excluded: {variant}" badge on excluded items
- [ ] **29.8** Non-variant items show no variant badge

---

## 30. RSS Feed Page (UI)

**Route**: `/feed`

### Empty State (before first ingest)

- [ ] **30.1** Page loads with header "RSS feed"
- [ ] **30.2** "Crawl now" button visible in header
- [ ] **30.3** Last ingest run section shows "Status: Never" when no crawl has run
- [ ] **30.4** Published releases section shows "No releases published yet."

> **Timing note (30.3-30.4):** The authoritative empty-state check runs in Step 0
> via API (`GET /api/feed` → `ingest.lastRunAt = null`, `total = 0`). The browser
> check here (Step 9) may see a populated feed if auto-crawl has already fired —
> this is expected and not a failure if Step 0 API checks passed.

### After Feed Ingest

- [ ] **30.5** Click "Crawl now" → button becomes disabled, toast "Feed crawl started"
- [ ] **30.6** After crawl completes: last ingest run section updates with status, last run time, new entries count, published count
- [ ] **30.7** "Without ruleset" count shows entries that couldn't be matched to a ruleset
- [ ] **30.8** Published releases section shows entries with: relative timestamp, title (truncated), S##E## (if present), channel, quality in px
- [ ] **30.9** Entry count shown in section header
- [ ] **30.10** Hint text explains Sonarr/Radarr pick up releases via RSS sync

### Error States

- [ ] **30.11** "Crawl now" disabled when ingest is already running
- [ ] **30.12** Gap warning shown if crawl couldn't cover the full window (e.g. after long downtime)
- [ ] **30.13** Consecutive failures count displayed when > 0

---

## 31. Feed API

- [ ] **31.1** `GET /api/feed` → JSON with `entries[]`, `total`, and `ingest` status object
- [ ] **31.2** `GET /api/feed?limit=5` → at most 5 entries returned
- [ ] **31.3** `GET /api/feed?limit=500` → max 500 entries (upper bound)
- [ ] **31.4** `POST /api/feed/crawl` → 202 Accepted (triggers immediate crawl)
- [ ] **31.5** `ingest` object contains: `enabled`, `running`, `lastRunAt`, `lastOutcome`, `lastNewItems`, `lastPublished`, `lastUnmatched`, `lastGap`, `consecutiveFailures`
- [ ] **31.6** After crawl: `entries[]` contains items with `stableId`, `firstSeenAt`, `mediaType`, `title`, `channel`, `topic`, `quality`, `season`, `episode`
- [ ] **31.7** Entries sorted by `firstSeenAt` descending (newest first)
- [ ] **31.8** Duplicate crawl trigger while running → still returns 202 (idempotent)

---

## 32. Newznab RSS Sync (Empty Query)

### Feed-backed RSS

- [ ] **32.1** `GET /index/api?t=tvsearch&apikey=<key>` (no q, no IDs) → returns feed entries filtered to TV (mediaType=Show)
- [ ] **32.2** `GET /index/api?t=movie&apikey=<key>` (no q, no IDs) → returns feed entries filtered to Movie
- [ ] **32.3** `GET /index/api?t=search&apikey=<key>` (no q, no IDs) → returns all feed entries (both TV + Movie)
- [ ] **32.4** Feed-backed RSS items have proper Newznab attributes: `pubDate` = `FirstSeenAt`, GUID = `StableId`
- [ ] **32.5** Feed-backed RSS respects `offset` and `limit` parameters for pagination

### Validation Fallback (empty feed)

> **⏱ Timing constraint:** These tests run in Step 0 (API-only, before auto-crawl
> at ~43-60s). If the feed is already populated when these tests run, the auto-crawl
> window was missed — report as FAIL with timing note.

- [ ] **32.6** With empty feed: `GET /index/api?t=tvsearch&apikey=<key>` → single validation item with title `FunkArr.Validation.S01E01.GERMAN.1080p.WEB.h264-FunkArr`
- [ ] **32.7** Validation item GUID: `funkarr-validation-tv`
- [ ] **32.8** With empty feed: `GET /index/api?t=movie&apikey=<key>` → validation item with title `FunkArr.Validation.2024.GERMAN.1080p.WEB.h264-FunkArr`
- [ ] **32.9** Validation item GUID: `funkarr-validation-movie`
- [ ] **32.10** Validation item size: ~1 GB
- [ ] **32.11** Validation item description contains "FunkArr validation entry"

### Output Caching

- [ ] **32.12** Two identical empty-query requests within 30 min return the same response (cached)
- [ ] **32.13** After new feed entries are published, cached response is invalidated (new request returns updated entries)

---

## 33. Pre-Released Entries

- [ ] **33.1** Trigger Sonarr search for an episode → scoring history shows candidates from both aired and pre-released Mediathek entries
- [ ] **33.2** Pre-released and regular entries are merged by video URL (no duplicates)
- [ ] **33.3** `GET /api/mediathek/search?q=<topic>&limit=10` → may include entries with future air dates

---

## 34. Evaluation Workers

- [ ] **34.1** After Sonarr search: scoring history entry has `source: "sonarr"` (search workers delegate to evaluation workers internally)
- [ ] **34.2** After feed crawl: scoring history entries have `source: "feed"`
- [ ] **34.3** Feed-sourced scoring entries show enrichment data (TVDB/TMDB IDs resolved)
- [ ] **34.4** `GET /api/rulesets/{id}/history` → entries from both "sonarr" and "feed" sources appear

---

## 35. Feed → Sonarr Import (E2E with Import)

**Tests the complete feed ingest → Newznab RSS → Sonarr RSS sync → download → import chain.**

This is the primary way Sonarr discovers new releases from German public broadcasters.
The test verifies that the FeedCrawlWorker's 6 Become phases produce valid Newznab
entries that Sonarr can grab and import end-to-end.

**Prerequisites**: Sonarr configured with FunkArr indexer (RSS enabled) and SABnzbd
download client. "In aller Freundschaft" (tvdbId=147521) added as a monitored series
but never manually searched — this ensures RSS sync can grab episodes that Sonarr
doesn't already have. Feed crawl completed with entries published.

- [ ] **35.1** Feed crawl completed: `GET /api/feed?limit=1` → `ingest.lastOutcome = "completed"`, `total > 0`
- [ ] **35.2** Newznab RSS returns feed entries: `GET /index/api?t=tvsearch&apikey=<key>&limit=5` → items with `tvdbid`, `season`, `episode` attributes
- [ ] **35.3** Trigger RSS sync in Sonarr:
  ```
  POST http://localhost:8989/api/v3/command
  { "name": "RssSync" }
  ```
- [ ] **35.4** Sonarr reports "RSS Sync Completed. Reports found: N, Reports grabbed: M" with M > 0
- [ ] **35.5** FunkArr download queue shows grabbed items: `GET /download/api?mode=queue&apikey=<key>` → slots present
- [ ] **35.6** Wait for downloads to complete: poll FunkArr SABnzbd queue until slots empty or all completed
- [ ] **35.7** Trigger Sonarr `ProcessMonitoredDownloads` (no chmod needed — `DataFiles` sets permissions)
- [ ] **35.8** Sonarr history shows `downloadFolderImported` events for RSS-grabbed episodes
- [ ] **35.9** Sonarr episode list: `GET /api/v3/episodefile?seriesId=<id>` → files with quality, size, path
- [ ] **35.10** MKV files on disk at `/shared/tv/<series>/<season>/` with correct naming
✅ **PASS criteria**: At least 1 episode grabbed via RSS sync reaches `downloadFolderImported` in Sonarr history

---

## 36. Error Handling & Edge Cases

### API Errors
- [ ] **36.1** `GET /api/nonexistent` → 404
- [ ] **36.2** `GET /api/rulesets/nonexistent-id` → 404, not 500
- [ ] **36.3** `POST /api/rulesets` with invalid JSON → 400 with validation details
- [ ] **36.4** `DELETE /api/rulesets/{community-id}` → error (community protected)
- [ ] **36.5** Newznab `GET /index/api?t=invalid&apikey=<key>` → HTTP 200 with XML `<error code="202" description="No such function"/>`
- [ ] **36.5a** SABnzbd `GET /download/api?mode=bogus&apikey=<key>` → HTTP 200 with `{"status":false,"error":"Invalid mode"}`
- [ ] **36.6** `GET /api/feed?limit=999` → clamped to max 500, no error

### URL Scheme Validation (SSRF prevention)
- [ ] **36.6a** Newznab `t=get` with crafted NZB payload containing `file:///etc/passwd` as URL → `t=get` returns error (payload rejected at decode)
- [ ] **36.6b** SABnzbd `addfile` with NZB containing `<meta type="X-FunkArr-Url">file:///etc/shadow</meta>` → addfile responds with error (URL empty after validation), item not queued

### Legacy Redirects
- [ ] **36.7** Navigate to `/queue` → redirects to `/activity`
- [ ] **36.8** Navigate to `/history` → redirects to `/activity`
- [ ] **36.9** Navigate to `/search` → redirects to `/rulesets`

### API Key Log Redaction
- [ ] **36.10a** Make a Newznab request with apikey → `docker logs funkarr` does NOT contain the raw API key value in any log line from `Microsoft.AspNetCore.HttpLogging`
- [ ] **36.10b** Make a SABnzbd request with apikey → same check: no raw API key in HttpLogging output
- [ ] **36.10c** Non-apikey query params (t, q, mode, tvdbid) ARE still logged with their values

### Resilience
- [ ] **36.10** Queue SSE: disconnect network briefly → stream reconnects, state recovers
- [ ] **36.11** Log SSE: disconnect → stream reconnects, new entries appear
- [ ] **36.12** Concurrent download limit: with limit=2, queue 3 downloads → only 2 active, 1 waiting
- [ ] **36.13** Feed crawl during MVW indexing (HTTP 503): crawl retries with backoff, no crash

---

## Test Execution Order

Every test must be PASS or FAIL. No skips. Execute in this order - each step sets up
the state required by subsequent steps.

0. **⏱ Empty-feed validation (API-only, <30s after port 6969 responds)**: Auto-crawl fires at ~43-60s after startup — these API checks MUST complete before then. No browser navigation, only API calls: Newznab validation fallback items (section 32.6-32.11), feed API empty state with `ingest.lastRunAt = null` (sections 30.3 API, 30.4 API, 31.1, 31.5)
1. **Startup**: Docker compose up, wait for health (sections 14, 22)
2. **Setup wizard**: Full walkthrough - health check, select services, configure Prowlarr/Sonarr/Radarr, copy buttons, manual expand, finish (sections 14-16)
2a. **Create indexers in Sonarr/Radarr**: Prowlarr does not auto-sync without application mappings. Create indexers directly: `POST /api/setup/sonarr/indexer` and `POST /api/setup/radarr/indexer` with `{"url":"http://sonarr:8989","apiKey":"<key>","funkarrUrl":"http://funkarr:6969"}` (and radarr:7878 respectively). Verify indexer appears in each service.
3. **Dashboard (clean)**: Verify stat cards, links, version on clean slate (section 1.1-1.4, 1.9)
4. **Sidebar**: Collapse/expand, language switch all 4 locales, navigation links incl. Feed (section 2)
5. **Settings page**: All sections visible, download config, routes, **broadcast variants**, log viewer (sections 24, 29.1-29.4)
6. **Variants API**: `GET /api/system/variants` returns 4 variants with Exclude policy (section 29.5-29.6)
7. **Legacy redirects**: /queue, /history, /search redirect correctly (section 36.7-36.9)
8. **Feed page (empty)**: Browser check — may show populated feed if auto-crawl fired after Step 0. If Step 0 API checks passed, a populated feed here is acceptable (section 30.1-30.4)
9. **Feed API (empty)**: Verify empty entries if not already tested in step 0 (section 31.1, 31.5)
10. **Create local ruleset**: Via UI (section 9) - creates state for source filters + delete tests
11. **Edit Tatort**: Via UI editor, add alias, save - creates merged ruleset (section 8).
    After saving, **validate the roundtrip**: `GET /api/rulesets/tatort/raw` and compare
    regex patterns against the community source to ensure no double-escaping occurred.
    If editing via API instead of UI, read "API Ruleset Roundtrip Pitfalls" above.
12. **Rulesets list**: Search, ALL filter combinations (type + source), both sort options (section 6)
13. **Ruleset detail + export**: Tatort (merged) - identity, enrichment, expand/collapse, **export first** (section 11), then delete overlay (sections 7, 10.1-10.2). Export requires the local overlay to exist.
14. **Delete local ruleset**: Delete the section 9 ruleset via confirmation flow (section 10.3-10.6)
15. **Trigger feed crawl**: `POST /api/feed/crawl` → 202, wait for completion (section 31.4)
16. **Feed page (populated)**: Crawl now button, ingest status, published entries visible (section 30.5-30.10)
17. **Feed API (populated)**: Entries with limit/pagination, ingest status fields (section 31.1-31.8)
18. **RSS sync with feed**: Newznab empty-query now returns feed entries (section 32.1-32.5)
19. **RSS output caching**: Verify cached responses and cache invalidation (section 32.12-32.13)
20. **Evaluation workers**: Feed-sourced scoring entries appear with `source: "feed"` (section 34.2-34.4)
21. **Pause pipeline**: `POST /api/downloads/queue/pause` - prevent downloads from starting
21a. **Radarr search (isolated)**: Add movie to Radarr, trigger movie search while queue is empty (section 26.1-26.3)
21b. **Radarr download**: Resume pipeline, wait for Radarr download to complete, verify import (section 26.4-26.7)
21c. **Re-pause pipeline**: `POST /api/downloads/queue/pause` — queue must be paused again before Sonarr searches
22. **Trigger 3+ Sonarr searches**: Different episodes while paused - items queue (section 17.1-17.6)
23. **Pre-released entries**: Scoring history shows both aired and pre-released candidates (section 33)
24. **Upload failed NZB**: Add NZB with invalid URL via SABnzbd addfile - queues bad download
25. **Queue tests**: Queue tab shows queued items, queue operations (sections 4, 25.3-25.6, 28.6-28.9)
26. **Resume pipeline**: `POST /api/downloads/queue/resume` - downloads start
27. **Active download tests**: Queue tab during download, cancel one, global speed (sections 3.3-3.6, 18.1)
28. **Wait for completions + failure**: Downloads finish, failed NZB produces failed entry
29. **Search → Import verification**: Wait for all downloads to complete (poll FunkArr history), then trigger Sonarr `ProcessMonitoredDownloads`, verify `downloadFolderImported` in Sonarr history, episode files exist (section 17.5-17.9). No chmod needed — `DataFiles` sets 666/777 on completed downloads.
30. **Feed → Import verification**: Trigger RSS sync in Sonarr — "In aller Freundschaft" (never manually searched) provides fresh episodes for RSS grabs. Wait for all downloads to complete, trigger `ProcessMonitoredDownloads`, verify `downloadFolderImported` in Sonarr history (section 35.1-35.10)
31. **Generate history pagination data**: Upload 20 NZBs with bad URLs (10x show, 10x movie) via SABnzbd addfile, poll until history has >= 26 entries
32. **History tests**: History tab, completed + failed status, retry, delete, category filter, pagination (sections 5, 18.3-18.5)
33. **Dashboard (post-search)**: Download status bar, Anzeigen link, recent activity (section 1.5-1.8)
34. **Generate scoring history entries**: Trigger 10+ Sonarr searches across seasons 2015-2026 (1 per season, 5s spacing to avoid queue saturation)
35. **Scoring history + variants**: List, API pagination (limit/offset), click through to detail, filter tabs, traces, variant badges (sections 12-13, 29.7-29.8)
36. **Scoring verification**: API check for enrichment + rule traces, both sonarr + feed sources (sections 19, 34.1)
37. **API smoke tests**: Newznab, SABnzbd, System, RuleSet CRUD **except 23.7** (already tested in step 13), Mediathek, Download, Feed APIs (sections 20-23, 27-28, 31)
38. **Error handling**: Invalid routes, bad IDs, malformed requests, feed limit clamping (section 36.1-36.6)
39. **SSE resilience**: Pause/unpause container, verify stream reconnects (section 36.10-36.11)
40. **Concurrency limit**: Queue 3 downloads with limit=2, verify 2 active + 1 waiting (section 36.12)
41. **Feed crawl resilience**: MVW 503 handling, backoff behavior (section 36.13)
