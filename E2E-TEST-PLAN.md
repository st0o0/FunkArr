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
| **Clean slate** | Fresh volumes, no data | 0.x, 14.x (health), initial dashboard |
| **Sonarr ready** | Tatort added (tvdbId 83214), root folder `/shared/tv` | 17.x |
| **Radarr ready** | Movie added (e.g. Schachnovelle tmdbId 1740919), root folder `/shared/movies` | 26.x |
| **Services configured** | Setup wizard or API configures Prowlarr/Sonarr/Radarr | 17.x, 26.x |
| **Local ruleset** | `POST /api/rulesets` with local-only ruleset | 6.9-6.11, 7.9-7.12, 10.x, 11.x |
| **Merged ruleset** | Edit Tatort via UI editor, save -> creates local overlay | 7.8, 7.10-7.11, 10.1-10.2, 11.x |
| **Paused pipeline** | `POST /api/downloads/pause` before triggering searches | 3.x, 4.x, 25.x, 28.x |
| **Queued downloads** | Pause pipeline, trigger 3+ episode searches -> items queue | 3.7, 4.2-4.4, 25.3-25.6, 28.6-28.9 |
| **Active download** | Resume pipeline -> downloads become active | 3.3-3.6, 18.1 |
| **Failed download** | Upload NZB with invalid URL via SABnzbd addfile | 5.6-5.8, 18.4-18.5, 21.14 |
| **Pagination data** | Trigger searches to generate scoring entries (count non-deterministic) | 5.10, 12.5-12.6 |
| **Post-search** | Completed downloads + scoring history exist | 1.5-1.8, 5.x, 12.x, 13.x, 19.x |

### Setup Recipe: Paused pipeline with queued items

Pause the download pipeline first, then trigger searches. Downloads queue instead of
starting immediately.

```powershell
$apiKey = "funkarr-dev-api-key-01"

# Pause pipeline
Invoke-RestMethod "http://localhost:6969/api/downloads/pause" -Method Post

# Trigger 3 episode searches (items queue since pipeline is paused)
@(1403, 1404, 1405) | ForEach-Object {
    $body = "{`"name`":`"EpisodeSearch`",`"episodeIds`":[$_]}"
    Invoke-RestMethod "http://localhost:8989/api/v3/command?apikey=$apiKey" -Method Post `
        -ContentType "application/json" -Body $body
    Start-Sleep -Seconds 5  # wait for scoring + grab
}

# Now test queue operations (4.x, 25.x, 28.x) while items are queued
# Then resume to test active downloads (3.4-3.7):
# Invoke-RestMethod "http://localhost:6969/api/downloads/resume" -Method Post
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

# Add Schachnovelle (2021 German film)
# Note: Radarr looks up movies via TMDB. Use Radarr's lookup to find the correct tmdbId
# since TMDB IDs can change over time.
$lookup = Invoke-RestMethod "http://localhost:7878/api/v3/movie/lookup?term=schachnovelle&apikey=$apiKey"
$movie = $lookup | Where-Object { $_.year -eq 2021 } | Select-Object -First 1
$body = $movie | ConvertTo-Json -Depth 5
# Set required fields
$movieObj = $movie | Select-Object *
$movieObj.qualityProfileId = 1
$movieObj.rootFolderPath = "/shared/movies"
$movieObj.monitored = $true
$movieObj.addOptions = @{ searchForMovie = $false }
Invoke-RestMethod "http://localhost:7878/api/v3/movie?apikey=$apiKey" -Method Post `
    -ContentType "application/json" -Body ($movieObj | ConvertTo-Json -Depth 5)

# Get movie ID
$movies = Invoke-RestMethod "http://localhost:7878/api/v3/movie?apikey=$apiKey"
$movieId = ($movies | Where-Object { $_.title -match "Schachnovelle" }).id

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

3. **Enum values**: The API uses **numeric** enum values (strategy: 3 = TitleIncludes,
   titleRule type: 0 = Static, field: 0 = Title). The on-disk format uses **string** enum
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
- [ ] **1.5** Download status bar shows active/waiting counts
- [ ] **1.6** "Anzeigen" link in status bar navigates to `/activity`
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

## 3. Activity - Queue (Queue tab)

**Route**: `/activity`

The Queue tab is the default tab and shows both active downloads and queued items.

- [ ] **3.1** Tab buttons visible: Queue, History - Queue is default
- [ ] **3.2** Empty state: "No active downloads" message when idle
- [ ] **3.3** During download: shows title, episode, quality badge (1080p), channel tag, size tag, duration tag, SUB badge (if subtitles)
- [ ] **3.4** Progress bar with %, download speed (KB/s), ETA
- [ ] **3.5** Global speed indicator in page header
- [ ] **3.6** Cancel button (X) on active download → toast "Download abgebrochen"
- [ ] **3.7** Queue group cards expand/collapse on header click

---

## 4. Activity - Queue Details

- [ ] **4.1** Search field visible and filters queue items
- [ ] **4.2** Badge count on Queue tab label when items present
- [ ] **4.3** Shows queued items with title, episode, quality, size, type (show/movie)
- [ ] **4.4** X button removes item from queue → toast "Download abgebrochen"

---

## 5. Activity - History (History tab)

**Route**: `/activity` → click History tab

- [ ] **5.1** Click History tab → switches view
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
- [ ] **16.16** Click "Indexer erstellen" → creates Newznab indexer in Radarr
- [ ] **16.17** Click "Download-Client erstellen" → creates SABnzbd client
- [ ] **16.18** Success / duplicate error for both

### Navigation
- [ ] **16.19** "Zurück" on each step returns to previous
- [ ] **16.20** Click completed step in indicator → jumps to that step
- [ ] **16.21** Final "Weiter" → navigates to Dashboard (`/`)

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
- [ ] **20.2** TV search: `GET /index/api?t=tvsearch&tvdbid=83214&season=2026&ep=18&apikey=<key>` → Newznab RSS with items, season/episode attributes
- [ ] **20.3** Pagination: offset=0 and offset=100 return different items, correct total
- [ ] **20.4** General search: `GET /index/api?t=search&q=tatort&apikey=<key>`
- [ ] **20.5** Movie search by IMDB: `GET /index/api?t=movie&imdbid=<id>&apikey=<key>`
- [ ] **20.6** Movie search by TMDB: `GET /index/api?t=movie&tmdbid=<id>&apikey=<key>`
- [ ] **20.7** Invalid API key → 403 error XML

---

## 21. SABnzbd API

### Protocol
- [ ] **21.1** `GET /download/api?mode=version&apikey=<key>` → version string
- [ ] **21.2** `GET /download/api?mode=get_config&apikey=<key>` → config JSON
- [ ] **21.3** `GET /download/api?mode=fullstatus&apikey=<key>` → status with speed, disk space
- [ ] **21.4** Invalid/missing API key → error response

### Queue Operations
- [ ] **21.5** `GET /download/api?mode=queue&apikey=<key>` → queue listing
- [ ] **21.6** `POST /download/api` with `mode=addfile` + NZB multipart → adds to queue
- [ ] **21.7** `GET /download/api?mode=queue&name=delete&value=<id>&apikey=<key>` → removes item
- [ ] **21.8** `GET /download/api?mode=queue&name=priority&value=<id>&value2=1&apikey=<key>` → changes priority
- [ ] **21.9** `GET /download/api?mode=queue&name=switch&value=<id1>&value2=<id2>&apikey=<key>` → swaps items
- [ ] **21.10** `GET /download/api?mode=pause&apikey=<key>` → pauses queue
- [ ] **21.11** `GET /download/api?mode=resume&apikey=<key>` → resumes queue

### History
- [ ] **21.12** `GET /download/api?mode=history&apikey=<key>` → history listing
- [ ] **21.13** `GET /download/api?mode=history&name=delete&value=<id>&apikey=<key>` → deletes entry
- [ ] **21.14** `GET /download/api?mode=retry&value=<id>&apikey=<key>` → retries failed download

---

## 22. System API

- [ ] **22.1** `GET /api/system/version` → version info
- [ ] **22.2** `GET /api/system/setup` → health check results
- [ ] **22.3** `GET /api/system/storage` → disk usage
- [ ] **22.4** `GET /api/system/cache` → enrichment cache stats (TVDB/TMDB counts + oldest entry)
- [ ] **22.5** `GET /api/system/routes` → network route configuration (Direct default + Austria proxy)
- [ ] **22.6** `GET /api/system/logs` → recent log entries from ring buffer
- [ ] **22.7** `GET /api/system/logs/stream` → SSE connection opens, log entries stream in real-time
- [ ] **22.8** `GET /openapi/v1.json` → valid OpenAPI spec document with all /api endpoints

---

## 23. RuleSet CRUD API

- [ ] **23.1** `GET /api/rulesets` → list all rulesets with stats
- [ ] **23.2** `GET /api/rulesets/{id}` → ruleset detail with enrichment config
- [ ] **23.3** `POST /api/rulesets` → create new ruleset
- [ ] **23.4** `PUT /api/rulesets/{id}` → update existing ruleset
- [ ] **23.5** `DELETE /api/rulesets/{id}` → delete ruleset
- [ ] **23.6** `GET /api/rulesets/{id}/raw` → raw ruleset JSON (on-disk format with string enums)
- [ ] **23.7** `GET /api/rulesets/{id}/export` → community export
- [ ] **23.8** `POST /api/rulesets/test` with `{ defaultConfidence, rules: [{ id, strategy, ... }], candidates: [{ title, topic, channel, duration }] }` → returns `{ itemTraces: [{ candidate, matched, score, ruleTraces }] }`

---

## 24. Settings Page

**Route**: `/settings`

- [ ] **24.1** Page loads with all sections: Download-Einstellungen, Metadaten-Cache, Netzwerkrouten, System
- [ ] **24.2** Download settings: shows concurrent download limit and schedule config
- [ ] **24.3** Cache stats: TVDB and TMDB entry counts displayed
- [ ] **24.4** Network routes: configured routes listed (Direct, Austria proxy)
- [ ] **24.5** System info: version, storage usage
- [ ] **24.6** Log viewer: recent log entries visible
- [ ] **24.7** Log viewer: new log entries appear in real-time (SSE stream)

---

## 25. Download Queue Operations

- [ ] **25.1** Pause button → pauses all downloads, toast confirmation
- [ ] **25.2** Resume button → resumes paused pipeline, toast confirmation
- [ ] **25.3** Force-start on queued item → bypasses concurrency limit, starts immediately
- [ ] **25.4** Set priority High/Normal/Low on queue item → item moves to correct priority section
- [ ] **25.5** Move download to different position in queue
- [ ] **25.6** Swap two downloads' positions
- [ ] **25.7** Download detail page (`/activity/{id}`) loads with full download info

---

## 26. Radarr Search Flow (E2E)

**API-driven flow testing Radarr integration (movie search path).**

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
- [ ] **27.5** Duration filter: `?q=Tatort&minDuration=600&limit=5` → items with duration >= 600
- [ ] **27.6** Sort parameter: `?q=Tatort&sortBy=timestamp` → results (verify 200 OK, only `timestamp` is valid)

---

## 28. Download API (Internal)

- [ ] **28.1** `GET /api/downloads/history/stats` → completed/failed counts, bytes, avg time, success rate
- [ ] **28.2** `GET /api/downloads/history/categories` → distinct category list
- [ ] **28.3** `GET /api/downloads/settings` → concurrency limit, schedule config
- [ ] **28.4** `POST /api/downloads/pause` → pauses pipeline (200)
- [ ] **28.5** `POST /api/downloads/resume` → resumes pipeline (200)
- [ ] **28.6** `POST /api/downloads/queue/{id}/force-start` → bypasses limit
- [ ] **28.7** `POST /api/downloads/queue/{id}/move` → repositions item
- [ ] **28.8** `POST /api/downloads/queue/{id}/priority` → changes priority
- [ ] **28.9** `POST /api/downloads/queue/swap` → swaps two items

---

## 29. Error Handling & Edge Cases

### API Errors
- [ ] **29.1** `GET /api/nonexistent` → 404
- [ ] **29.2** `GET /api/rulesets/nonexistent-id` → 404, not 500
- [ ] **29.3** `POST /api/rulesets` with invalid JSON → 400 with validation details
- [ ] **29.4** `DELETE /api/rulesets/{community-id}` → error (community protected)
- [ ] **29.5** Newznab `GET /index/api?t=invalid&apikey=<key>` → proper error response

### Legacy Redirects
- [ ] **29.6** Navigate to `/queue` → redirects to `/activity`
- [ ] **29.7** Navigate to `/history` → redirects to `/activity`
- [ ] **29.8** Navigate to `/search` → redirects to `/rulesets`

### Resilience
- [ ] **29.9** Queue SSE: disconnect network briefly → stream reconnects, state recovers
- [ ] **29.10** Log SSE: disconnect → stream reconnects, new entries appear
- [ ] **29.11** Concurrent download limit: with limit=2, queue 3 downloads → only 2 active, 1 waiting

---

## Test Execution Order

Every test must be PASS or FAIL. No skips. Execute in this order - each step sets up
the state required by subsequent steps.

1. **Startup**: Docker compose up, wait for health (sections 14, 22)
2. **Setup wizard**: Full walkthrough - health check, select services, configure Prowlarr/Sonarr/Radarr, copy buttons, manual expand, finish (sections 14-16)
3. **Dashboard (clean)**: Verify stat cards, links, version on clean slate (section 1.1-1.4, 1.9)
4. **Sidebar**: Collapse/expand, language switch all 4 locales, navigation links (section 2)
5. **Settings page**: All sections visible, download config, cache stats, routes, log viewer (section 24)
6. **Legacy redirects**: /queue, /history, /search redirect correctly (section 29.6-29.8)
7. **Create local ruleset**: Via UI (section 9) - creates state for source filters + delete tests
8. **Edit Tatort**: Via UI editor, add alias, save - creates merged ruleset (section 8).
   After saving, **validate the roundtrip**: `GET /api/rulesets/tatort/raw` and compare
   regex patterns against the community source to ensure no double-escaping occurred.
   If editing via API instead of UI, read "API Ruleset Roundtrip Pitfalls" above.
9. **Rulesets list**: Search, ALL filter combinations (type + source), both sort options (section 6)
10. **Ruleset detail + export**: Tatort (merged) - identity, enrichment, expand/collapse, **export first** (section 11), then delete overlay (sections 7, 10.1-10.2). Export requires the local overlay to exist.
11. **Delete local ruleset**: Delete the section 9 ruleset via confirmation flow (section 10.3-10.6)
12. **Pause pipeline**: `POST /api/downloads/pause` - prevent downloads from starting
13. **Trigger 3+ Sonarr searches**: Different episodes while paused - items queue (section 17.1-17.6)
14. **Upload failed NZB**: Add NZB with invalid URL via SABnzbd addfile - queues bad download
15. **Queue tests**: Queue tab shows queued items, queue operations (sections 4, 25.3-25.6, 28.6-28.9)
16. **Resume pipeline**: `POST /api/downloads/resume` - downloads start
17. **Active download tests**: Queue tab during download, cancel one, global speed (sections 3.3-3.6, 18.1)
18. **Wait for completions + failure**: Downloads finish, failed NZB produces failed entry
19. **Generate history pagination data**: Upload 20 NZBs with bad URLs (10x show, 10x movie) via SABnzbd addfile, poll until history has >= 26 entries
20. **History tests**: History tab, completed + failed status, retry, delete, category filter, pagination (sections 5, 18.3-18.5)
21. **Dashboard (post-search)**: Download status bar, Anzeigen link, recent activity (section 1.5-1.8)
22. **Generate scoring history entries**: Trigger 10+ Sonarr searches across seasons 2015-2026 (1 per season, 5s spacing to avoid queue saturation)
23. **Scoring history**: List, API pagination (limit/offset), click through to detail, filter tabs, traces (sections 12-13)
24. **Scoring verification**: API check for enrichment + rule traces (section 19)
25. **Radarr search**: Add movie to Radarr, trigger search (section 26)
26. **API smoke tests**: Newznab, SABnzbd, System, RuleSet CRUD, Mediathek, Download APIs (sections 20-23, 27-28)
27. **Error handling**: Invalid routes, bad IDs, malformed requests (section 29.1-29.5)
28. **SSE resilience**: Pause/unpause container, verify stream reconnects (section 29.9-29.10)
29. **Concurrency limit**: Queue 3 downloads with limit=2, verify 2 active + 1 waiting (section 29.11)
