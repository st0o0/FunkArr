## 1. Add Step 0: Time-critical empty-feed tests

- [x] 1.1 Insert new Step 0 block in the "Test Execution Order" section (line ~998), before existing Step 1. Step 0 runs API-only checks within 30s of container health: tests 32.6-32.11, 30.3 (API), 30.4 (API), 31.1, 31.5. Include a timing annotation: `⏱ MUST complete within 30s of container responding on port 6969 — auto-crawl fires at ~43-60s`.
- [x] 1.2 In section 30 ("RSS Feed Page"), split tests 30.3 and 30.4 into API check (Step 0) and browser check (Step 9). Add a note under 30.3-30.4: "API-verified in Step 0. Browser check in Step 9 may see a populated feed if auto-crawl has fired — this is expected, not a failure, if Step 0 passed."
- [x] 1.3 In section 32 ("Newznab RSS Sync"), add a note above 32.6: "⏱ These tests run in Step 0 (API-only, before auto-crawl). If the feed is already populated when these tests run, the auto-crawl window was missed — report as FAIL with timing note."
- [x] 1.4 Renumber existing Steps 1-43 to Steps 1-43 (no shift needed — Step 0 is inserted before Step 1). Update Step 2's text to remove the "Run IMMEDIATELY" urgency note and reference to the 60s mark, since Step 0 now handles this. Change Step 2 to: "Setup wizard: Full walkthrough (sections 14-16)". Move the health/system API checks (sections 14, 22) to Step 1 only.
- [x] 1.5 Update Step 9 (feed page empty) to note: "Browser check — may show populated feed if auto-crawl fired after Step 0. If Step 0 API checks passed, a populated feed here is acceptable."

## 2. Fix export state dependency

- [x] 2.1 In the "Test Execution Order" section, update Step 39 (API smoke tests) to read: "API smoke tests: Newznab, SABnzbd, System, RuleSet CRUD **except 23.7** (already tested in step 14), Mediathek, Download, Feed APIs (sections 20-23, 27-28, 31)"
- [x] 2.2 In section 23 ("RuleSet CRUD API"), add a note on test 23.7: "**Note:** Tested in Step 14 (requires local overlay). Skipped in Step 39 API smoke tests."

## 3. Document priority body format

- [x] 3.1 In section 28 ("Download API Internal"), update test 28.8 from `POST /api/downloads/queue/{id}/priority` → changes priority` to: `` `POST /api/downloads/queue/{id}/priority` with body `{"priority": 1}` (integer enum: 0=Normal, 1=High, 2=Low) → 200 ``
- [x] 3.2 In section 25 ("Download Queue Operations"), update test 25.4 to clarify: "Set priority on queue item via `{"priority": 1}` (integer enum: 0=Normal, 1=High, 2=Low)"

## 4. Isolate Radarr search window

- [x] 4.1 Remove Step 38 ("Radarr search: Add movie to Radarr, trigger search") from its current position in the execution order.
- [x] 4.2 Insert Radarr flow between current Step 22 (pause pipeline) and Step 23 (Sonarr searches). New sequence: Step 22 pauses pipeline → Step 22a adds movie to Radarr and triggers search (26.1-26.3) → Step 22b resumes pipeline, waits for Radarr download completion, verifies import (26.4-26.7) → Step 22c re-pauses pipeline → Step 23 triggers Sonarr searches (unchanged).
- [x] 4.3 Add a setup note in section 26 ("Radarr Search Flow"): "**Execution note:** Run with empty queue before Sonarr searches. Pipeline must be active for download. Re-pause after completion."
- [x] 4.4 Renumber Steps 23-43 to account for the inserted 22a-22c sub-steps. Keep sub-step numbering (22a, 22b, 22c) to minimize renumbering impact on the rest of the plan.

## 5. Verify consistency

- [x] 5.1 Read through the complete updated "Test Execution Order" section and verify: every test ID (0.x through 36.x) appears exactly once, no test is orphaned, all step cross-references are correct.
- [x] 5.2 Verify the "Test Scenarios & Setup Recipes" table at the top of the plan still matches the updated execution order — specifically that "Clean slate" still maps to the right steps and "Feed populated" references are consistent.
