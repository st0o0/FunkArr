## Context

The E2E test plan (`E2E-TEST-PLAN.md`) is executed by Claude agents (main Opus + Haiku API subagents) against a fresh Docker Compose stack. The plan has 200 tests across 36 sections with a strict execution order. Across 6 runs, ~7 tests fail every time due to four documented plan issues — not code bugs.

The auto-crawl timer in FeedCrawlManager fires ~43-60s after container startup. The current execution order puts health checks and setup wizard (Steps 1-2) before the empty-feed tests, but those steps take 30-60s — exceeding the window.

## Goals / Non-Goals

**Goals:**
- Eliminate all recurring false failures caused by test ordering, state dependencies, and missing format documentation
- Keep the test plan as a single self-contained document (no external config changes)
- Maintain the principle "every test must be PASS or FAIL, no skips"

**Non-Goals:**
- Adding a config option to disable auto-crawl (would require code changes)
- Automating the test plan execution (it's agent-driven)
- Changing any application code or API behavior

## Decisions

### 1. New Step 0: Time-critical empty-feed tests (API-only)

Insert a new Step 0 before all existing steps. This step runs **only API calls** (no browser navigation) to beat the auto-crawl timer:

```
Step 0 — WITHIN 30s OF CONTAINER HEALTH (<30s absolute)
  API calls only, no browser:
  ├─ 32.6-32.11: Validation fallback items (Newznab empty-query)
  ├─ 30.3: Feed API ingest status = "never"  (GET /api/feed)
  ├─ 30.4: Feed API entries = empty          (GET /api/feed)
  └─ 31.1+31.5: Feed API structure           (GET /api/feed)
```

**Why API-only:** Browser navigation (open tab, wait for render, read page) takes 5-15s per interaction. API calls take <1s each. Six API calls fit comfortably in 10s.

**Why not move health checks after:** Health checks verify the container is ready. The empty-feed API calls implicitly confirm the container is healthy (they'd fail with connection errors otherwise). We can run them as soon as the container responds on port 6969.

**30.3 and 30.4 split:** The browser-based feed page check moves to existing Step 9 (feed page empty). The API-based check (which is what actually matters for the race) stays in Step 0. The test plan will note: "30.3-30.4 API verified in Step 0; browser verification in Step 9 if auto-crawl hasn't fired yet."

### 2. Export exclusion from API smoke tests

Step 14 already tests export (section 11) before deleting the overlay. Step 39 (API smoke tests) re-runs section 23 including 23.7 — but by then the overlay is gone.

**Fix:** Add a note to Step 39: `Section 23 EXCEPT 23.7 (already tested in step 14)`.

**Why not create a fresh overlay:** Extra complexity, risk of interfering with subsequent tests, and the export is already fully tested at Step 14.

### 3. Explicit body format for 28.8

Change the test description to include the exact request body. The current description is ambiguous — agents guess string values instead of integer enums.

```markdown
- [ ] **28.8** `POST /api/downloads/queue/{id}/priority` with body
  `{"priority": 1}` (integer enum: 0=Normal, 1=High, 2=Low) → 200
```

### 4. Radarr search isolation

Move Radarr search (section 26) from Step 38 (after 75+ Sonarr grabs fill the queue) to a new position between pipeline pause (Step 22) and Sonarr searches (Step 23). Sequence:

```
Step 22: Pause pipeline
Step 22a: Add movie to Radarr, trigger search (26.1-26.3)
Step 22b: Resume, wait for Radarr download to complete (26.4-26.7)
Step 22c: Pause pipeline again
Step 23: Trigger Sonarr searches (existing)
```

**Why before Sonarr:** Radarr movies produce 1-3 downloads vs Sonarr's 50-75+. A single movie download completes in <60s. This avoids queue competition entirely.

**Why pause-resume-pause:** The Radarr download needs the pipeline active, but we want the pipeline paused again before Sonarr searches queue items for queue operation tests.

## Risks / Trade-offs

- **[Risk] Step 0 might still lose the race if container startup is slow** → The 30s budget has ~15-20s margin vs the 43-60s auto-crawl window. If startup consistently exceeds 40s, we'd need the code-level fix (disable auto-crawl via config). Current data shows agents reach Step 2 in 30-60s including browser work — API-only should be well under 30s.
- **[Risk] Radarr pause-resume-pause adds test complexity** → Minimal — it's three API calls. The alternative (Radarr at end, untestable) is worse.
- **[Trade-off] 30.3-30.4 browser check becomes best-effort** → The API check in Step 0 is the authoritative test. The browser check in Step 9 may see a populated feed if auto-crawl has fired. The plan will note this is expected, not a failure.
