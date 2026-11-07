## Context

Arr apps validate a new indexer with `t=caps` and an empty-criteria RSS probe that must return at least one item. FunkArr answers that probe from the feed store (`QueryFeed` → `FeedResult(Entries, Total, FilteredTotal)`) and falls back to `ValidationRss` when it has nothing. `FeedStoreManagerState.Query` sets `Total` to the size of the whole store and `FilteredTotal` to the size after the media-type/channel/quality/search filters. `NewznabSearchService.FeedOrValidation` branches on `Total > 0` and reports `Total`, so a store with entries of the other media type yields an empty page. See proposal.md for the verified failure.

Existing tests always reply `FeedResult(entries, n, n)` or `([], 0, 0)`, so `Total == FilteredTotal` in every test.

## Goals / Non-Goals

**Goals:**
- Radarr, Sonarr and Prowlarr setup passes for every feed state.
- The state that triggers the bug (entries of only some media types) is covered by hermetic tests.

**Non-Goals:**
- No separate Arr-compat test project and no real Arr apps in the automated test run. The behavior of the Arr apps is verified manually (below), the FunkArr side is pinned by hermetic tests.
- No schema-driven payloads for the setup flow: all six tested Arr versions accepted the current hand-built payloads and ignored the superfluous fields.
- No change to default download categories: `appsettings.json` already seeds `tv` and `movies`.
- No change to the URL FunkArr hands to the Arr app during setup; that is a separate topic.

## Decisions

**D1: Decide on the filtered result.** `FeedOrValidation` uses `FilteredTotal` for the feed-vs-validation branch and for the reported `total`. `FeedResult` and the feed API stay unchanged (the UI already shows both numbers). Alternative: change `FeedResult.Total` to mean the filtered total. Rejected: the feed API and UI expose both values on purpose.

**D2: Keep the `Cacheable` semantics.** The validation item is returned with `Cacheable` when the feed answered, also for the new case "feed answered but nothing matches". A movie probe may therefore keep seeing the validation item for up to the cache TTL (5 minutes) after the first movie entry arrives; harmless because the validation item is a valid release.

**D3: Validation item stays as is.** `GetNzb` keeps rejecting the validation item's id on purpose (`NzbServiceTests.GetNzb_rejects_validation_item_ids`), so an accidental grab creates no download job. No Arr app was observed fetching the link, so there is no reason to change it.

**D4: Tests in existing projects.** A `FunkArr.IntegrationTests` Theory sends the probes through HTTP with a feed probe replying realistic `(Total, FilteredTotal)` pairs; `FunkArr.ArrApi.Tests` covers `FeedOrValidation` directly; `FunkArr.Feed.Tests` asserts that `Total` counts all media types while `FilteredTotal` honors the filter, because the Newznab side relies on that distinction. Alternative: Testcontainers or dev-compose tests against real Arr apps. Rejected by decision: too much infrastructure for a bug the hermetic matrix pins down.

## Risks / Trade-offs

- [Arr behavior can change with new versions and nothing automated notices] → Manual verification below, repeatable with throwaway containers; revisit if Arr regressions show up again.
- [The simulated feed answer could drift from the real store semantics] → The `Feed.Tests` contract assertion fails when `Total`/`FilteredTotal` semantics change.
- [`Total` in the Newznab response changes for existing consumers] → It now matches the items returned, which is what Newznab clients expect.

## Manual verification (evidence and repeat recipe)

Verified on 2026-11-06 with a FunkArr image built from `main`, real feed crawl (about 2,500 TV entries, no movies) and throwaway Arr containers (`ghcr.io/home-operations/*`, config on tmpfs, API key via `*__AUTH__APIKEY`), creating the indexer through each app's `POST /api/{v1,v3}/indexer` without `forceSave`:

| App | Versions | Result before the fix |
|---|---|---|
| Sonarr | 4.0.16.2942, 5.0.0.2785, 5.0.0.2941 | pass |
| Radarr | 5.21.0.9792, 6.4.4.10685, 7.0.1.10859 | fail (empty movie probe) |
| Prowlarr | 1.33.1.4997, 2.7.0.5723, 3.0.0.5737 | pass |

Download-client creation passed in all Sonarr and Radarr versions.

Repeated on 2026-11-06 with the fix, same feed state (2,484 TV entries, no movies): Radarr 5.21.0.9792, 6.4.4.10685 and 7.0.1.10859 now pass, as do Sonarr 5.0.0.2941 and Prowlarr 3.0.0.5737 (indexer and, for Sonarr/Radarr, download client). The movie probe returns the validation item with `total=1`; the TV probe still returns the feed entries with `total=2484`.
