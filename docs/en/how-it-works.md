# How FunkArr Works

This page explains how FunkArr works internally: from receiving a search request through scoring and metadata enrichment to the finished download.

## Search Flow

When Sonarr or Radarr send a search request to FunkArr, it passes through several stages:

### 1. Newznab API Receives the Request

Sonarr/Radarr send a standard Newznab request to `/index/api`. FunkArr identifies the type from the parameter:

- `t=tvsearch` - TV search (from Sonarr), with optional TVDB ID, season and episode
- `t=movie` - Movie search (from Radarr), with optional IMDB ID or TMDB ID
- `t=search` - General search (from Prowlarr), category determines the type

The request is converted into an internal search command and forwarded to the SearchManager.

### 2. SearchManager Coordinates the Search

The SearchManager decides based on the category whether to search for TV, movies, or both:

- Category 5000-5999: TV only
- Category 2000-2999: movies only
- No category: both in parallel, results are merged

Each search has a 30-second timeout. If one part of a combined search fails, the results from the other part are still returned.

### 3. MediathekViewWeb Query

The MediathekViewWebManager sends the search query to the MediathekViewWeb API. This searches the Mediathek database (ARD, ZDF, ORF, SRF and other broadcasters).

Queries can filter by topic (show name), title and description. Each result contains:

- Channel, topic, title, description
- Video URLs in different qualities (HD, normal, low)
- Subtitle URL (if available)
- Duration, size, air date

FunkArr limits concurrent queries to MediathekViewWeb to 3 to avoid overloading the API. Additional requests are automatically queued.

### 4. Ruleset Matching

If a TVDB ID or IMDB ID is included in the request, FunkArr loads the matching ruleset from the RuleSet store. The ruleset determines how Mediathek titles are converted into structured season/episode formats.

If no ruleset is available or no ID is provided, the Mediathek results are passed directly to scoring.

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

Scoring runs in parallel across a pool of worker actors. The pool size is configurable via `FunkArr__Scoring__PoolSize` (default: 4).

### 6. Metadata Enrichment

After scoring, matches are enriched with external metadata (see the [Metadata Enrichment](#metadata-enrichment) section).

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

The DownloadManager maintains a persistent queue with these capabilities:

- **Concurrent downloads:** Up to 3 downloads run simultaneously by default (configurable via `FunkArr__Download__ConcurrentDownloads`).
- **Priorities:** Each download has a priority. Higher-priority downloads are processed first.
- **Pause/Resume:** The entire queue can be paused and resumed.
- **Force-start:** Individual downloads can be started immediately, even if the maximum concurrent downloads limit is already reached.
- **Reorder:** Downloads can be moved to a specific queue position or swapped with each other.
- **Delete:** Queued or active downloads can be removed from the queue.
- **Retry:** Failed downloads can be re-queued for another attempt.

The queue is persistent - it survives container restarts. Active downloads are automatically re-dispatched after a restart.

### Download Schedule

FunkArr supports optional download time windows. When configured, downloads only start during the defined windows. Outside the windows, they remain in the queue. New downloads start automatically when the next window begins.

### FFmpeg Remux

Each download is processed by a DownloadWorker:

1. **Subtitle preparation:** If a subtitle URL is available, the subtitle file is downloaded. FunkArr auto-detects the format:
   - **TTML/XML** - converted to SRT (via a built-in TTML-to-SRT converter)
   - **WebVTT** - used directly as a VTT file
   - **SRT** - used directly

2. **Video download and remux:** FFmpeg downloads the video (direct URL or HLS stream) and packages it as an MKV container. When a network route with a proxy is configured for the channel, both the subtitle download and FFmpeg are routed through the configured HTTP proxy (see [Configuration - Network Routes](/en/configuration#network-routes)):
   - Video and audio codecs are copied (no re-encoding)
   - Subtitles are embedded as an SRT track with German language tag (`language=deu`)
   - Progress is reported live (downloaded bytes, time position, speed)

3. **Completion:** The finished file is moved from the `incomplete/` directory to the `complete/` directory, organized by category subdirectory (e.g. `complete/tv/`).

### Download History

Every completed download (successful or failed) is recorded in the download history. The history stores title, category, file size, status, error message (on failure), download duration and completion timestamp. You can remove individual history entries.

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

Every search request evaluated against a ruleset is recorded in that ruleset's scoring history. Each snapshot records:

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
| `FunkArr__MatchHistory__MaxSnapshots` | `100` | Maximum number of stored snapshots per ruleset |
| `FunkArr__MatchHistory__MaxAgeDays` | `30` | Snapshots older than this many days are removed |
| `FunkArr__MatchHistory__SnapshotInterval` | `20` | Interval between snapshot persistences |

## Metadata Enrichment

FunkArr can enrich search results with external metadata to determine season and episode numbers when the ruleset alone is not sufficient.

### TVDB (TV Shows)

For TV shows, FunkArr loads the episode list from TVDB and tries to match Mediathek entries to the correct episodes. Two methods are tried in order:

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
