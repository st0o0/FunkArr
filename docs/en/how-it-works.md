# How FunkArr Works

This page explains how FunkArr works internally: from receiving a search request through scoring and metadata enrichment to the finished download.

## Architecture Overview

FunkArr is built on Akka.NET. Work is split across a few long-lived singleton actors (managers) and short-lived sharded entities (workers). The domains communicate only through messages.

| Domain | Actors | Responsibility |
|--------|--------|----------------|
| Search | `SearchManager`, `MediathekViewWebManager` (singletons), `TvSearchWorker`, `MovieSearchWorker` (sharded, one per search request) | Orchestrates a search and queries MediathekViewWeb |
| RuleSet | `RuleSetResolver`, `RuleSetManager`, `RuleSetUpdater` (singletons), `RuleSetWorker` (sharded, one per ruleset) | Loads, merges and resolves rulesets, syncs community rulesets |
| Scoring | `ScoringManager` (singleton) with a pool of `ScoringActor` | Evaluates Mediathek entries against ruleset rules |
| Enrichment | `EnrichmentManager` (singleton) with TVDB and TMDB actor pools | Resolves season/episode and movie data via external APIs |
| History | `StatsCollector` (singleton), `HistoryWorker` (sharded, persistent, one per ruleset) | Scoring history and statistics |
| Download | `DownloadManager`, `DownloadScheduler`, `DownloadHistoryManager` (singletons), `DownloadWorker` (sharded, persistent, one per download) | Queue, schedule, FFmpeg remux, download history |

The HTTP layer (`/index/api` for Newznab, `/download/api` for SABnzbd) is a thin adapter that only translates requests into messages for these actors.

## Search Flow

When Sonarr or Radarr send a search request to FunkArr, it passes through several stages:

### 1. Newznab API Receives the Request

Sonarr/Radarr send a standard Newznab request to `/index/api`. FunkArr identifies the type from the parameter:

- `t=tvsearch` - TV search (from Sonarr), with optional TVDB ID, season and episode
- `t=movie` - Movie search (from Radarr), with optional IMDB ID or TMDB ID
- `t=search` - General search (from Prowlarr), category determines the type
- `t=caps` - Capabilities of the indexer
- `t=get` - Fetches the NZB for a search result

The request is checked against the API key, converted into an internal search command and forwarded to the SearchManager. The full result list of a search is cached for 60 seconds (`SearchCacheTtlSeconds`), so pagination and the season/episode filter are applied to the cached list without re-running the search.

### 2. SearchManager Coordinates the Search

The SearchManager (cluster singleton) creates a search ID and hands the work to a sharded search worker (one `TvSearchWorker` or `MovieSearchWorker` entity per search request). Explicit `t=tvsearch` / `t=movie` requests go straight to the matching worker. For `t=search`, the category decides:

- Category 5000-5999: TV only
- Category 2000-2999: movies only
- No category: both in parallel, results are merged

Each search has a 30-second timeout (the controller itself times out after 45 seconds). If one part of a combined search fails or times out, the results from the other part are still returned.

The search worker runs the following steps as a small state machine (resolve ruleset, query, score, enrich) and then replies to the SearchManager. Idle workers are passivated after 30 seconds.

### 3. MediathekViewWeb Query

:::tip MediathekViewWeb
[MediathekViewWeb](https://mediathekviewweb.de/#everywhere=true) is a free community project that makes the media libraries of German-language public broadcasters searchable. FunkArr uses their API as the data source for all search queries.
:::

The MediathekViewWebManager (cluster singleton) sends the search query to the MediathekViewWeb API. This searches the Mediathek database (ARD, ZDF, ORF, SRF and other broadcasters).

Queries filter by topic (show name), are sorted by date (newest first), exclude future entries and ignore entries shorter than 5 minutes. API responses are cached for 5 minutes. Each result contains:

- Channel, topic, title, description
- Video URLs in different qualities (HD, normal, low)
- Subtitle URL (if available)
- Duration, size, air date

FunkArr limits concurrent queries to MediathekViewWeb to 3 to avoid overloading the API. Additional requests wait in a bounded queue (64 entries). If the queue is full, the search worker retries twice (after 0.5 s and 1.5 s) before the search fails.

### 4. Ruleset Matching

The `RuleSetResolver` (singleton) knows all loaded rulesets and finds the matching one by topic or alias, or by TVDB, IMDB or TMDB ID. The ruleset determines how Mediathek titles are converted into structured season/episode formats.

The order depends on the request:

- **With TVDB/IMDB ID:** the ruleset is resolved first. Its topic is then used for the Mediathek query.
- **Without ID:** the Mediathek query runs first with the search term as the topic. Afterwards the ruleset is resolved from the topic of the first result.

If no ruleset is found, the Mediathek results are returned without scoring.

Rulesets are managed separately from search:

- `RuleSetManager` scans the community and local ruleset directories, watches them for changes (changes are debounced for 2 seconds) and loads each ruleset into a sharded `RuleSetWorker`.
- `RuleSetWorker` merges the community and local version of a ruleset, validates it, registers it with the `RuleSetResolver` and sends its matching configuration to the `ScoringManager`.
- `RuleSetUpdater` checks the GitHub releases of the community ruleset repository every 30 minutes (if `RefreshEnabled`), downloads the `rulesets.zip` of a new release and triggers a re-scan.

### 5. Scoring

The scoring engine evaluates each Mediathek entry against the ruleset's rules. Three steps are performed for each rule (rules are sorted by priority, the first match wins):

**Filter check:** Each rule can define filters that an entry must satisfy. Filters can check various fields (title, topic, channel, duration, description) with operators like equals, contains, regex, greater/less than. Filters can be combined with `all` (all must pass), `any` (one must pass) and `not` (none may pass).

**Identification:** If an entry passes the filters, the rule tries to identify the season and episode. There are five strategies:

- `SeasonAndEpisodeNumber` - extracts season and episode via regex from the title
- `AbsoluteEpisodeNumber` - extracts an absolute episode number via regex
- `TitleExact` - reconstructs the expected title from parts and checks for an exact match
- `TitleIncludes` - checks if a constructed title is contained in the Mediathek title (with umlaut normalization)
- `AirdateExtraction` - extracts a German date from the title (e.g. "15.03.2026" or "15. Maerz 2026")

**Score assignment:** Each match receives a confidence value (0.0-1.0). This value comes from the rule itself or the ruleset's default confidence.

The `ScoringManager` holds the matching configuration of every ruleset and distributes the work to a pool of `ScoringActor` workers (smallest-mailbox routing). The pool size is configurable via `FunkArr__Scoring__PoolSize` (default: 4). Regex evaluation has a 100 ms timeout per match to protect against runaway patterns. If no matching configuration exists for a ruleset, the entries are returned unscored.

### 6. Metadata Enrichment

After scoring, matches are enriched with external metadata (see the [Metadata Enrichment](#metadata-enrichment) section). This step is optional: if enrichment is not configured for the ruleset or fails, the scored results are returned as they are. Afterwards the search worker records the run in the scoring history of the ruleset (see [Scoring History](#scoring-history)).

### 7. Newznab Response

Results are returned as a Newznab RSS feed. Each entry contains:

- Title formatted as "ShowName S01E05 EpisodeTitle 1080p"
- Estimated file size
- Category (TV or movie, with quality tier)
- Metadata attributes (TVDB ID, IMDB ID, TMDB ID, season, episode)
- NZB download link (contains the video URL as payload)

Search results are cached so that follow-up requests with pagination don't re-trigger the entire search.

## Download Pipeline

When Sonarr or Radarr start a download, they send a SABnzbd request to `/download/api`. FunkArr emulates a SABnzbd download client.

### Queue Management

The `DownloadManager` (persistent cluster singleton) maintains the queue and dispatches downloads to the sharded, persistent `DownloadWorker` entities (one per download). When a worker finishes or fails for good, it frees its slot, records the result in the `DownloadHistoryManager` and the manager dispatches the next entry. The queue supports:

- **Concurrent downloads:** Up to 3 downloads run simultaneously by default (configurable via `FunkArr__Download__ConcurrentDownloads`).
- **Priorities:** Each download has a priority. Higher-priority downloads are processed first.
- **Pause/Resume:** The entire queue can be paused and resumed.
- **Force-start:** Individual downloads can be started immediately, even if the maximum concurrent downloads limit is already reached.
- **Reorder:** Downloads can be moved to a specific queue position or swapped with each other.
- **Delete:** Queued or active downloads can be removed from the queue.
- **Retry:** Failed downloads can be re-queued for another attempt.

The queue is persistent - it survives container restarts. Active downloads are automatically re-dispatched after a restart.

### Download Schedule

FunkArr supports optional download time windows. The `DownloadScheduler` (singleton) evaluates the configured windows and tells the DownloadManager when downloads are enabled or disabled. Outside the windows, downloads remain in the queue. New downloads start automatically when the next window begins. Windows may span midnight, and changes to the configuration are applied without a restart.

### FFmpeg Remux

Each download is processed by its own DownloadWorker, whose state (initialized, downloading, completed, failed) is persisted:

1. **Subtitle preparation:** If a subtitle URL is available, the subtitle file is downloaded. FunkArr auto-detects the format:
   - **TTML/XML** - converted to SRT (via a built-in TTML-to-SRT converter)
   - **WebVTT** - used directly as a VTT file
   - **SRT** - used directly

2. **Video download and remux:** FFmpeg downloads the video (direct URL or HLS stream) and packages it as an MKV container. When a network route with a proxy is configured for the channel, both the subtitle download and FFmpeg are routed through the configured HTTP proxy (see [Configuration - Network Routes](/en/configuration#network-routes)):
   - Video and audio codecs are copied (no re-encoding)
   - Subtitles are embedded as an SRT track with German language tag (`language=deu`)
   - Progress is reported live (downloaded bytes, time position, speed)

3. **Completion:** The finished file is moved from the `incomplete/` directory to the `complete/` directory, organized by category subdirectory (e.g. `complete/tv/`).

4. **Retry on failure:** If retries are enabled (`RetryEnabled`, off by default), transient failures are retried up to `MaxRetries` times (default: 3). The delay doubles with each attempt, starting at 30 seconds (`RetryBackoffBase`) and capped at 5 minutes. Permanent failures and exhausted retries end up in the download history as failed.

### Download History

Every completed download (successful or failed) is recorded by the `DownloadHistoryManager` (persistent singleton). At most 1000 entries are kept (`MaxHistoryRecords`), the oldest are trimmed. The history stores title, category, file size, status, error message (on failure), download duration and completion timestamp. You can remove individual history entries.

### Live Progress

The download queue can be queried via the API. For each active download, the following is shown:

- Currently downloaded bytes
- Current time position in the video
- Download speed
- Total duration and size

## Scoring System

The scoring system (also called "Match Intelligence") consists of two components: the scoring engine and the scoring history.

### Scoring Engine

The scoring engine evaluates Mediathek entries against ruleset rules. The process is described in the [Scoring](#5-scoring) section above.

Importantly, every evaluation produces a complete trace. The trace documents for each entry and rule:

- Which filters were checked and whether they passed
- Which fields were compared with which values
- Which identification strategy was used
- Why an identification failed (e.g. "regex did not match")
- Which rule ultimately matched and with what confidence value

These traces are viewable in the web UI and help when debugging rulesets.

### Scoring History

Every search request evaluated against a ruleset is recorded in that ruleset's scoring history. The search worker sends the result to a sharded, persistent `HistoryWorker` (one per ruleset), which stores the snapshots and trims them by count and age. After each recording it reports updated statistics to the `StatsCollector` (singleton), which keeps the statistics of all rulesets in memory for the UI and rebuilds them from the history workers at startup. Each snapshot records:

- Search source (Sonarr, Radarr, Prowlarr or test)
- Search query
- Timestamp
- Number of candidates, matches and enriched results
- Complete item traces

From these snapshots, FunkArr computes per-ruleset statistics:

- **Match rate:** proportion of Mediathek entries that were recognized by a rule
- **Enrichment rate:** proportion of recognized entries that were successfully enriched with metadata
- **Last run:** timestamp of the most recent evaluation

Configuration options for the history:

| Variable | Default | Description |
|----------|---------|-------------|
| `FunkArr__ScoringHistory__MaxSnapshots` | `100` | Maximum number of stored snapshots per ruleset |
| `FunkArr__ScoringHistory__MaxAgeDays` | `30` | Snapshots older than this many days are removed |
| `FunkArr__ScoringHistory__SnapshotInterval` | `20` | Interval between snapshot persistences |

## Metadata Enrichment

FunkArr can enrich search results with external metadata to determine season and episode numbers when the ruleset alone is not sufficient.

### TVDB (TV Shows)

The `EnrichmentManager` (singleton) forwards requests to two actor pools (two workers each): one for TVDB, one for TMDB. For TV shows, FunkArr loads the episode list from TVDB and tries to match Mediathek entries to the correct episodes. If the search specified a season, the match is first limited to that season; entries without a match are then retried against all episodes with a 10% confidence penalty. Two methods are tried in order:

**Title matching:** Compares the Mediathek title with TVDB episode names using Levenshtein distance (similarity measurement). The default threshold is 0.7 (70% similarity). When multiple episodes tie, runtime is used as a tiebreaker.

**Airdate matching:** If the Mediathek entry has an air date, the TVDB episode with the closest matching date is found. Default tolerance: 7 days. This matching is only performed if the best title score is at least 0.3 (a safety net against completely wrong matches).

TVDB episode lists are cached. Shows with upcoming episodes are cached for 2 days, completed shows for 7 days.

Configuration: `FunkArr__Tvdb__ApiKey`

### TMDB (Movies)

For movies, FunkArr queries TMDB to resolve movie data (title, release year, IMDB ID). Alternative titles are also considered. The matching uses:

- **Title similarity** via Levenshtein distance (threshold: 0.5)
- **Year validation** with a default tolerance of 1 year

TMDB movie data is cached for 30 days.

Configuration: `FunkArr__Tmdb__ApiKey`

### Without API Keys

If no API keys are configured, FunkArr relies exclusively on ruleset patterns for matching. This works but produces fewer and less accurate results.

## Video Quality

Mediathek entries offer videos in up to three quality tiers. FunkArr creates a separate search result entry for each available tier:

| Quality | Source | Estimated Bitrate | Example 60 min |
|---------|--------|-------------------|----------------|
| 1080p (HD) | `url_video_hd` | ~6.5 Mbit/s | ~490 MB |
| 720p (Normal) | `url_video` | ~3.3 Mbit/s | ~250 MB |
| 480p (Low) | `url_video_low` | ~0.8 Mbit/s | ~60 MB |

Not every Mediathek entry has all three qualities. If only a normal URL is available, only a 720p result is created.

The estimated file size is calculated from the video duration and the typical bitrate per quality tier. If the Mediathek entry reports an actual size, that is used instead.

Sonarr and Radarr see the quality in the Newznab category and title (e.g. "Tatort S01E05 Der letzte Schrei 1080p"). This means normal quality profiles and preferences apply.
