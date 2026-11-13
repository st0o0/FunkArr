# Request flows

How a search, a download, the RSS crawl and the ruleset sync move through the actors. What each step does for the user (timeouts, scoring strategies, FFmpeg options) is in [How FunkArr works](/en/how-it-works).

## Search

Sonarr sends `GET /index/api?t=tvsearch&tvdbid=…&season=1&ep=5`.

<ArchitectureView view="search-flow" height="860px" />

- **Search and feed share the evaluation workers.** The RSS crawl uses the same `Tv/MovieEvaluationWorker`, so a change to scoring or enrichment affects both.
- **Search workers are request-scoped.** One entity per search ID, passivated after it answers. State that must outlive a request doesn't belong there.
- **The controller does no work.** Which worker, which ruleset and which timeouts is decided by actors, not by `NewznabController`.

## Download

Sonarr grabs a release and uploads its NZB with `POST /download/api?mode=addfile`. The NZB carries the video URL.

<ArchitectureView view="download-flow" height="860px" />

Sonarr then polls `mode=queue` and `mode=history`, answered from `DownloadManager` and `DownloadHistoryManager`.

- **Two journals.** The queue lives in `DownloadManager`'s journal, the progress of one download in its `DownloadWorker`'s journal. After a restart the manager re-dispatches active downloads and each worker recovers its own state.
- **Side effects after persistence.** The worker starts FFmpeg only after the matching event is persisted.

## RSS crawl and sync

`FeedCrawlManager` starts a crawl on a timer. Its `FeedHarvestActor` child pages through new Mediathek entries in an Akka.Streams pipeline and sends them through the same evaluation workers as a search. Sonarr's RSS sync (a search without query or ID) is answered from `FeedStoreManager`.

<ArchitectureView view="rss-sync" />

The ruleset sync from GitHub is on the [Rulesets](./rulesets) page.
