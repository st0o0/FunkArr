---
description: "Run the full E2E test plan against the dev stack. Handles reset, startup, and complete verification. Use when the user wants to verify all features, run E2E tests, or do a final verification pass."
---

# E2E Verification

Execute the complete E2E test plan from `E2E-TEST-PLAN.md` against the dev stack.

**Every test must be PASS or FAIL. No SKIP allowed.** If a test requires specific state,
create that state before running the test. The execution order in the test plan is
designed so each step builds the state for subsequent steps.

## How to run

1. Read `E2E-TEST-PLAN.md` fully before starting
2. Execute Phase 0 (environment setup) below
3. Follow the **Test Execution Order** at the bottom of the plan (steps 1-29)
4. Use the plan's **Setup Recipes** for creating each state
5. At Phase 5 (step 26+), spawn parallel subagents as described below
6. Write results to `E2E-TEST-RESULTS-<DATE>.md`

**NEVER skip a test.** If it needs state, create that state first.

## Architecture

The main agent (Opus) handles all sequential/browser work (Phases 0-4), then spawns
3 Haiku subagents for independent API test groups while doing its own concurrent work.

```
Main Agent (Opus):
  Phase 0: Docker setup, health, Sonarr/Radarr config
  Phase 1: Browser - Setup Wizard, Dashboard, Sidebar, Settings, Redirects
  Phase 2: Browser - Ruleset CRUD (Create, Edit, List, Detail, Export, Delete)
  Phase 3: Downloads - Pause, Queue ops, Resume, Active, Wait, History
  Phase 4: Scoring history + Dashboard post-search + Pagination + Radarr

  --> All state is now built. Collect IDs needed by subagents, then spawn:

  Agent "api-readonly" (Haiku):   Newznab + System + Mediathek + read-only errors
  Agent "api-downloads" (Haiku):  SABnzbd + Download API + concurrency limit
  Agent "api-rulesets" (Haiku):   RuleSet CRUD API + ruleset error handling

  Main agent concurrently:        SSE resilience + Radarr search flow + Scoring API

  --> Collect subagent results, validate edge cases, write results file
```

### Subagent isolation

- **api-readonly**: Sections 20, 22, 27, 29.1-29.2, 29.5. Pure GET requests.
- **api-downloads**: Sections 21, 28, 29.11. Mutates download state. Runs its own
  pause/add/test/resume sequence internally.
- **api-rulesets**: Sections 23, 29.3-29.4. Mutates ruleset state.
- **Main agent concurrent**: Sections 19, 26, 29.9-29.10. SSE tests need docker
  pause/unpause. Radarr flow needs Sonarr/Radarr state.

### Subagent result validation

Haiku subagents return raw PASS/FAIL results. Before writing to the results file,
the main agent (Opus) must:
1. Parse each subagent's result table
2. Sanity-check any FAIL results - if the failure description seems like a
   misinterpretation (e.g. "field missing" when the field has a different name),
   re-run that specific test from the main agent to confirm
3. Only mark confirmed failures as FAIL in the final results

## Phase 0: Environment Setup (Always Clean Start)

Every E2E run starts from a clean slate. Always tear down and rebuild.

```powershell
# Tear down + remove volumes
docker compose -f docker-compose.dev.yml down -v

# Build and start
docker compose -f docker-compose.dev.yml up -d --build
```

Poll until all services respond (can take 30-60s):

```powershell
$maxRetries = 30
for ($i = 0; $i -lt $maxRetries; $i++) {
    try {
        $r = Invoke-RestMethod "http://localhost:6969/api/system/version" -TimeoutSec 3
        if ($r) { Write-Host "FunkArr ready"; break }
    } catch {}
    Start-Sleep -Seconds 5
}
Invoke-RestMethod "http://localhost:8989/api/v3/system/status?apikey=funkarr-dev-api-key-01" | Select-Object version
Invoke-RestMethod "http://localhost:7878/api/v3/system/status?apikey=funkarr-dev-api-key-01" | Select-Object version
Invoke-RestMethod "http://localhost:9696/api/v1/system/status?apikey=funkarr-dev-api-key-01" | Select-Object version
```

Then run the Sonarr and Radarr setup recipes from `E2E-TEST-PLAN.md` section 0
("Re-add Tatort to Sonarr after reset" and "Setup Recipe: Radarr movie search").

All containers must be running: funkarr (6969), sonarr (8989), radarr (7878),
prowlarr (9696). Only proceed to Phase 1 when all services respond.

## Test Methods

### curl tests

Use `Invoke-RestMethod` or `curl`. Check status code, response structure, plausible values.

### Browser tests

Use claude-in-chrome browser tools:
- `tabs_context_mcp` first to see current tabs
- `tabs_create_mcp` to open new tabs
- `navigate` to go to URLs
- `read_page` to verify page content
- `computer` for clicks, typing, interactions

Key UI details:
- Vue.js SPA, English as default language
- Setup wizard URL fields have placeholders but values are empty - must type actual values
- Docker networking: Arr services reach FunkArr at `http://funkarr:6969`
- Setup wizard Sonarr/Radarr steps each have TWO buttons: "Create Indexer" AND
  "Create Download Client". Click BOTH for each service.
- Activity page has 2 tabs: Queue (default, shows active + queued) and History

## Phase 5: Parallel API Verification

After Phase 4, all state is built. Collect dynamic IDs needed by subagents, then
launch all 3 Agent calls in a **single message** so they run concurrently.
Use `model: "haiku"` for all subagents.

### Step 1: Collect state for subagent prompts

```powershell
# Get a failed download ID for SABnzbd retry test
$history = Invoke-RestMethod "http://localhost:6969/download/api?mode=history&apikey=funkarr-dev-api-key-01"
$failedId = ($history.history.slots | Where-Object { $_.status -eq "Failed" } | Select-Object -First 1).nzo_id

# Get a community-only ruleset ID for delete-protection test
$rulesets = Invoke-RestMethod "http://localhost:6969/api/rulesets"
$communityId = ($rulesets.rulesets | Where-Object { $_.sourceType -eq 0 } | Select-Object -First 1).ruleSetId
```

### Step 2: Spawn 3 subagents + do own concurrent work

Substitute `<FAILED_ID>` and `<COMMUNITY_ID>` in the prompt templates below
with the actual IDs collected in Step 1.

#### Agent "api-readonly" prompt template

```
You are testing a REST API. Run each test, report PASS or FAIL with a one-line note.
Use PowerShell Invoke-RestMethod or Invoke-WebRequest for all requests.

Base URL: http://localhost:6969
API Key: funkarr-dev-api-key-01

## Tests

### Section 20: Newznab API
| # | Test | Method |
|---|------|--------|
| 20.1 | Caps | GET /index/api?t=caps&apikey=<key> -> XML response with <server>, <limits>, <categories> |
| 20.2 | TV search | GET /index/api?t=tvsearch&tvdbid=83214&season=2026&ep=18&apikey=<key> -> RSS XML with <item> elements, newznab:attr for season/episode |
| 20.3 | Pagination | GET with offset=0 then offset=100 -> different items or offset acknowledged |
| 20.4 | General search | GET /index/api?t=search&q=tatort&apikey=<key> -> RSS with items |
| 20.5 | Movie search IMDB | GET /index/api?t=movie&imdbid=tt9781494&apikey=<key> -> RSS |
| 20.6 | Movie search TMDB | GET /index/api?t=movie&tmdbid=455&apikey=<key> -> RSS (use tmdbId from a known community movie ruleset) |
| 20.7 | Invalid API key | GET /index/api?t=caps&apikey=wrong -> 403 or error XML |

### Section 22: System API
| # | Test | Method |
|---|------|--------|
| 22.1 | Version | GET /api/system/version -> JSON with version field |
| 22.2 | Setup | GET /api/system/setup -> JSON with health check results |
| 22.3 | Storage | GET /api/system/storage -> JSON with disk usage |
| 22.4 | Cache | GET /api/system/cache -> JSON with TVDB/TMDB counts |
| 22.5 | Routes | GET /api/system/routes -> JSON with network routes |
| 22.6 | Logs | GET /api/system/logs -> JSON array of log entries |
| 22.7 | Log stream | GET /api/system/logs/stream -> SSE connection opens (status 200, content-type text/event-stream). Just verify the connection opens, don't wait for events. |
| 22.8 | OpenAPI | GET /openapi/v1.json -> valid JSON with openapi field and paths |

### Section 27: Mediathek Search API
| # | Test | Method |
|---|------|--------|
| 27.1 | Basic search | GET /api/mediathek/search?q=Tatort&limit=5 -> JSON with items array containing results |
| 27.2 | Channel filter | GET /api/mediathek/search?q=Tatort&channel=ARD -> filtered results |
| 27.3 | Topic filter | GET /api/mediathek/search?topic=Tagesschau -> results |
| 27.4 | Pagination | GET /api/mediathek/search?q=Tatort&offset=0&size=5 then offset=5 -> different results |
| 27.5 | Duration filter | GET /api/mediathek/search?q=Tatort&minDuration=600 -> results with duration >= 600 |
| 27.6 | Sort | GET /api/mediathek/search?q=Tatort&sortBy=timestamp -> results (verify 200 OK, only "timestamp" is valid) |

### Section 29: Error handling (read-only subset)
| # | Test | Method |
|---|------|--------|
| 29.1 | Unknown route | GET /api/nonexistent -> 404 |
| 29.2 | Unknown ruleset | GET /api/rulesets/nonexistent-id-xyz -> 404 (not 500) |
| 29.5 | Invalid Newznab type | GET /index/api?t=invalid&apikey=<key> -> error XML or 4xx |

## Output format

Return ONLY a markdown table, no other text:

| # | Test | Status | Notes |
|---|------|--------|-------|
| 20.1 | Caps | PASS | XML with server element |
| ... | ... | ... | ... |
```

#### Agent "api-downloads" prompt template

```
You are testing a REST API's download management. Tests MUST run in the exact order
listed because they modify shared state. Use PowerShell Invoke-RestMethod or
Invoke-WebRequest and curl for multipart uploads.

Base URL: http://localhost:6969
API Key: funkarr-dev-api-key-01
Failed download ID from history: <FAILED_ID>  (substitute actual ID)

## Test sequence

### Phase A: Protocol tests (read-only, safe to run first)
| # | Test | Method |
|---|------|--------|
| 21.1 | Version | GET /download/api?mode=version&apikey=<key> -> version string |
| 21.2 | Config | GET /download/api?mode=get_config&apikey=<key> -> config JSON |
| 21.3 | Full status | GET /download/api?mode=fullstatus&apikey=<key> -> status with speed, diskspace |
| 21.4 | Bad API key | GET /download/api?mode=version&apikey=wrong -> error |
| 21.5 | Queue listing | GET /download/api?mode=queue&apikey=<key> -> queue JSON |
| 21.12 | History listing | GET /download/api?mode=history&apikey=<key> -> history JSON with slots |

### Phase B: Internal Download API (read-only)
| # | Test | Method |
|---|------|--------|
| 28.1 | History stats | GET /api/downloads/history/stats -> completed/failed counts |
| 28.2 | Categories | GET /api/downloads/history/categories -> category list |
| 28.3 | Settings | GET /api/downloads/settings -> concurrency, schedule |

### Phase C: Pause + add items for queue operation tests
1. Pause pipeline: POST /api/downloads/pause (28.4)
2. Upload 3 NZB files via SABnzbd addfile for queue testing. Use this XML template
   for each (change N to 1, 2, 3):

<?xml version="1.0" encoding="utf-8"?>
<nzb xmlns="http://www.newzbin.com/DTD/2003/nzb">
  <head>
    <meta type="title">E2E-Queue-Test-N</meta>
    <meta type="X-FunkArr-Url">http://192.0.2.1/test-N.mp4</meta>
    <meta type="X-FunkArr-Channel">TEST</meta>
    <meta type="X-FunkArr-Duration">60</meta>
    <meta type="X-FunkArr-Size">1000</meta>
    <meta type="X-FunkArr-Category">show</meta>
  </head>
  <file post_id="1">
    <groups><group>a.b.mediathek</group></groups>
    <segments><segment number="1">test-N@e2e</segment></segments>
  </file>
</nzb>

Upload each by writing XML to a temp file and using:
curl -s -X POST "http://localhost:6969/download/api?apikey=funkarr-dev-api-key-01&mode=addfile&cat=show" -F "name=@<file>"
**Important**: API key MUST be a query parameter, not a form field.

| # | Test | Method |
|---|------|--------|
| 21.6 | Addfile | Upload NZB -> returns status: true or nzo_ids |

3. Get queue to find item IDs for subsequent tests.

### Phase D: SABnzbd queue operations (with items in queue)
| # | Test | Method |
|---|------|--------|
| 21.8 | Priority | GET /download/api?mode=queue&name=priority&value=<id>&value2=1&apikey=<key> -> priority changed |
| 21.9 | Swap | GET /download/api?mode=queue&name=switch&value=<id1>&value2=<id2>&apikey=<key> -> swapped |
| 21.7 | Delete | GET /download/api?mode=queue&name=delete&value=<id>&apikey=<key> -> removed |

### Phase E: Internal queue operations (with remaining items)
| # | Test | Method |
|---|------|--------|
| 28.6 | Force-start | POST /api/downloads/queue/<id>/force-start -> 200 |
| 28.7 | Move | POST /api/downloads/queue/<id>/move with {"position":0} -> 200 |
| 28.8 | Priority | POST /api/downloads/queue/<id>/priority with {"priority":"High"} -> 200 |
| 28.9 | Swap | POST /api/downloads/queue/swap with {"id1":"<id1>","id2":"<id2>"} -> 200 (both items must have same priority for swap to work) |

### Phase F: Resume + concurrency test
| # | Test | Method |
|---|------|--------|
| 28.5 | Resume | POST /api/downloads/resume -> 200 |
| 29.11 | Concurrency | After resume, GET /download/api?mode=queue&apikey=<key> -> at most 2 items with status "Downloading", rest "Queued" or waiting |

### Phase G: History operations
| # | Test | Method |
|---|------|--------|
| 21.14 | Retry | GET /download/api?mode=retry&value=<FAILED_ID>&apikey=<key> -> success |
| 21.13 | Delete history | GET /download/api?mode=history&name=delete&value=<any_id>&apikey=<key> -> removed |

### Phase H: Pause/Resume via SABnzbd
| # | Test | Method |
|---|------|--------|
| 21.10 | Pause | GET /download/api?mode=pause&apikey=<key> -> paused |
| 21.11 | Resume | GET /download/api?mode=resume&apikey=<key> -> resumed |

## Output format

Return ONLY a markdown table, no other text:

| # | Test | Status | Notes |
|---|------|--------|-------|
| 21.1 | Version | PASS | returned "4.3.1" |
| ... | ... | ... | ... |
```

#### Agent "api-rulesets" prompt template

```
You are testing a REST API's ruleset CRUD operations. Tests MUST run in the exact
order listed because they modify shared state. Use PowerShell Invoke-RestMethod or
Invoke-WebRequest.

Base URL: http://localhost:6969
Community ruleset ID (protected, cannot delete): <COMMUNITY_ID>  (substitute actual)

IMPORTANT: The API uses NUMERIC enum values, not strings. When creating/updating rulesets:
- strategy: 3 = TitleIncludes
- titleRules type: 0 = Static, 1 = Regex
- titleRules field: 0 = Title, 1 = Topic
- Rule IDs must match ^[a-z][a-z0-9-]{2,}$ (lowercase, 3+ chars, kebab-case)

## Test sequence

### Phase A: Read operations
| # | Test | Method |
|---|------|--------|
| 23.1 | List all | GET /api/rulesets -> JSON with rulesets array, each has ruleSetId, topic, sourceType |
| 23.2 | Get detail | GET /api/rulesets/tatort -> JSON with ruleSetId, identity.topic, enrichment, rules |
| 23.6 | Raw JSON | GET /api/rulesets/tatort/raw -> raw JSON ruleset file content |
| 23.7 | Export | GET /api/rulesets/tatort/export -> export data JSON |

### Phase B: Create + Update + Delete lifecycle
| # | Test | Method |
|---|------|--------|
| 23.3 | Create | POST /api/rulesets with JSON body: {"ruleSetId":"e2e-api-test","topic":"API Test Ruleset","media":{"name":"API Test","type":"show"},"rules":[{"id":"rule-api","strategy":3,"priority":1,"confidence":0.8,"titleRules":[{"type":0,"field":0,"value":"test"}]}]} -> 201 with {"ruleSetId":"e2e-api-test"} |
| 23.4 | Update | PUT /api/rulesets/e2e-api-test with body: {"topic":"API Test Updated","media":{"name":"API Test Updated","type":"show"},"rules":[{"id":"rule-api","strategy":3,"priority":1,"confidence":0.8,"titleRules":[{"type":0,"field":0,"value":"test updated"}]}]} -> 200 |
| 23.5 | Delete | DELETE /api/rulesets/e2e-api-test -> 200 |

### Phase C: Test endpoint
| # | Test | Method |
|---|------|--------|
| 23.8 | Test scoring | POST /api/rulesets/test with {"defaultConfidence":0.8,"rules":[{"id":"rule-test","strategy":3,"priority":1,"confidence":0.8,"titleRules":[{"type":0,"field":0,"value":"Tatort"}]}],"candidates":[{"title":"Tatort - Mord am See","topic":"Tatort","channel":"ARD","duration":5400}]} -> JSON with itemTraces array |

### Phase D: Error handling
| # | Test | Method |
|---|------|--------|
| 29.3 | Invalid JSON | Use Invoke-WebRequest -SkipHttpErrorCheck: POST /api/rulesets with body "not json" and Content-Type application/json -> expect 400 status |
| 29.4 | Delete community | Use Invoke-WebRequest -SkipHttpErrorCheck: DELETE /api/rulesets/<COMMUNITY_ID> -> expect error response (not 204). Community rulesets cannot be deleted. |

## Output format

Return ONLY a markdown table, no other text:

| # | Test | Status | Notes |
|---|------|--------|-------|
| 23.1 | List all | PASS | 99 rulesets returned |
| ... | ... | ... | ... |
```

### Step 3: Main agent concurrent work

While subagents run, the main agent executes these tests itself:

**Scoring API verification (Section 19)**:
- `GET /api/rulesets/{id}/history?limit=5` - verify response structure
- `GET /api/rulesets/{id}/history/{requestId}` - verify itemTraces, enrichmentTrace

**Radarr search flow (Section 26)**:
- Trigger movie search via Radarr API
- Verify FunkArr receives request, scoring works, download queues

**SSE resilience (Section 29.9-29.10)**:
- Open Settings page in browser (log stream)
- `docker pause funkarr; Start-Sleep -Seconds 3; docker unpause funkarr`
- Verify stream reconnects

### Step 4: Collect results and write file

Wait for all 3 subagents to complete. Parse their result tables. For any FAIL
results from Haiku agents, verify the failure is genuine:
- Re-run the specific failing request from the main agent
- If Haiku misinterpreted the response, correct to PASS with a note
- If the failure is confirmed, keep as FAIL

## Results file

Write results to `E2E-TEST-RESULTS-<DATE>.md` in the repo root:

```markdown
# E2E Test Results - <DATE>

**Stack**: FunkArr v<version>, Sonarr, Radarr, Prowlarr
**Duration**: ~X minutes
**Agents**: Main (Opus) + 3 API subagents (Haiku)

## Summary

| Status | Count |
|--------|-------|
| PASS   | X     |
| FAIL   | Y     |
| Total  | N     |

## Results by Section

### 1. Dashboard
| # | Test | Status | Notes |
|---|------|--------|-------|
| 1.1 | Stat cards visible | PASS | |
```

Mark each test:
- **PASS** - works as described
- **FAIL** - broken, with exact error (HTTP status, error message, what happened vs expected)

**No SKIP column in the summary.** Every test must be either PASS or FAIL.

## Important

- Don't modify E2E-TEST-PLAN.md - it's the reusable spec
- Write results to a separate file
- Follow the execution order exactly - it creates state for later tests
- If a test fails because state setup failed, mark both the setup failure and the dependent test as FAIL
- Subagent prompts contain templates with `<PLACEHOLDER>` values - substitute actual IDs before spawning
- Launch all 3 subagents in ONE message (single response with 3 Agent tool calls) for true parallelism
