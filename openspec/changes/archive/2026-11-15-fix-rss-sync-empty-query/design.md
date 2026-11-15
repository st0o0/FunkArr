# Design

## Context

`NewznabSearchService.BuildCommand` turns every `tvsearch`/`movie`/`search` request into a `SearchCommand`, even when `q` and all media IDs are missing. Downstream:

- `TvSearchWorkerState.TryGetMediathekQuery` sends a topic-less MediathekViewWeb query, then `TryGetRuleSetRequest` resolves the ruleset from `Sources[0].Topic` and `TryGetScoringRequest` scores all items against it.
- `MovieSearchWorker` rejects a search without query and IDs with `SearchMovieFailed`, which the controller maps to Newznab error XML.

The *arr apps send exactly these requests for RSS sync and for the indexer test (`HttpIndexerBase.TestConnection` fetches the first RSS page and fails when it contains no releases). See proposal.md – Why.

## Goals / Non-Goals

**Goals:**
- Requests without search criteria never reach the Search domain.
- The *arr indexer test keeps passing, including FunkArr's auto-configuration of Sonarr, Radarr and Prowlarr.
- The response is deterministic and cannot lead to a download.

**Non-Goals:**
- Serving real new releases via RSS (#204).
- Changing `TvSearchWorker` / `MovieSearchWorker` behavior for topic-less commands. After this change, ArrApi no longer sends them; other callers (e.g. the internal Mediathek API) are out of scope and keep their current behavior.

## Decisions

### Detect RSS-sync requests in ArrApi, not in Search

The check lives in `NewznabSearchService` before `BuildCommand`. "No `q` and no ID means RSS sync" is Newznab protocol knowledge, which belongs in the thin adapter. Search keeps receiving only real searches.

*Alternative:* guard in `SearchManager` or the workers. Rejected: it would still need a protocol-specific answer (the validation item), and Search should not know about *arr indexer tests.

### Answer with one synthetic validation item instead of an empty result

An empty result breaks the *arr indexer test (see Context). One synthetic item satisfies the test and is harmless during RSS sync:

- **Title:** `FunkArr.Validation.S01E01.GERMAN.1080p.WEB.h264-FunkArr` (TV), `FunkArr.Validation.2024.GERMAN.1080p.WEB.h264-FunkArr` (Movies). It parses cleanly, but does not map to any real series or movie, so Sonarr/Radarr ignore it.
- **GUID:** `funkarr-validation-tv` / `funkarr-validation-movie`. It is stable, so Sonarr's "last seen release" tracking stops paging immediately on the next sync.
- **Link:** `t=get&id=funkarr-validation-<type>`. This ID is not valid base64, so the existing `NzbService.GetNzb` returns error 201. No new code path is needed to make a grab fail.
- **pubDate:** request time via `TimeProvider`, so retention rules in the *arr apps never reject it as too old.
- **Size and enclosure:** a fixed positive size and an `application/x-nzb` enclosure, so the item looks like any other FunkArr release. The *arr apps only drop items without a title or download URL (`HttpIndexerBase.IsValidRelease`), so both are set as well.

*Alternative:* an empty result. Rejected: indexer test fails.
*Alternative:* keep the live search but fix the ruleset resolution. Rejected: it is the job of #204, and it would still run a full MVW query, scoring and enrichment on every RSS sync.

Prior art: RundfunkArr returns a comparable validation item when its RSS result is empty.

### Category selection

- `tvsearch` → TV HD (5040).
- `movie` → Movies HD (2040).
- `search` → Movies only if every parsable entry of the comma-separated `cat` list is a movie category, otherwise TV.

The current `ParseInt(req.Cat)` cannot read lists like `5030,5040`, so category parsing for this path splits on commas. The existing search path is left unchanged.

### Paging

`offset = 0` returns the item, `offset ≥ 1` returns no items. Both report `total="1"`, so a pager never loops.

### Building the RSS

A small static builder in `FunkArr.ArrApi/Newznab` creates the `Rss` model directly, reusing the existing `Rss`/`Item`/`NewznabAttribute` types and `NewznabXmlResult`. `ToRss` is not reused because it derives GUID and link from a `SearchResultItem` payload, which is exactly what the validation item must avoid.

`NewznabSearchService` gets `TimeProvider` injected. It is already registered in DI and used by `NewznabController`.

## Risks / Trade-offs

- [A user has a series or movie literally named "Validation" or "FunkArr Validation"] → Sonarr could try to grab the item. The `get` link fails with error 201, so nothing is downloaded. Sonarr marks the release as failed, which is acceptable.
- [RSS sync no longer returns any real releases] → This is the intended interim state. Today's results are wrong or errors. New episodes are found via automatic and interactive search until #204.
- [Prowlarr sends `t=search` without `q` for its test] → It is covered by the same detection and gets a validation item in a matching category.

## Migration Plan

No data or configuration migration. Rollback is reverting the commit. Existing *arr indexer configurations keep working, because the test request still returns a result.
