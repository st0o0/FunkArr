# Configuration

All configuration is via environment variables using the `FunkArr__` prefix. The double underscore (`__`) separates nested sections - this is standard ASP.NET Core configuration binding.

Defaults work out of the box for local development. For production, you typically only need to set `FunkArr__ApiKey` and `FunkArr__Download__Path`.

## General

| Variable | Default | Description |
|----------|---------|-------------|
| `FunkArr__ApiKey` | `funkarr-default-api-key` | API key for Prowlarr/Sonarr/Radarr |
| `FunkArr__DataPath` | `data` | Base path for database, rulesets, temp files |

### API Key

The API key authenticates all requests from Prowlarr, Sonarr, and Radarr. Both the indexer API (`/index/api`) and download client API (`/download/api`) require this key. Use the same value when configuring FunkArr in your *arr apps.

::: warning
Change the default API key in production. Anyone with the key can search and download through your FunkArr instance.
:::

### Data Path

The data path is the root directory for all persistent state. FunkArr creates the following structure inside it:

```
data/
  funkarr.db            # SQLite database (when not using PostgreSQL)
  rulesets/
    community/          # Auto-synced community rulesets
    local/              # Your custom rulesets
    version.txt         # Currently installed community ruleset version
  temp/                 # Temporary files during download/remux
```

## Downloads

| Variable | Default | Description |
|----------|---------|-------------|
| `FunkArr__Download__Path` | `data/downloads` | Root download directory |
| `FunkArr__Download__ConcurrentDownloads` | `3` | Max parallel downloads |

### Download Path

The download path contains two subdirectories that FunkArr manages automatically:

- **`incomplete/`** - active downloads and remux operations in progress
- **`complete/`** - finished downloads, organized by category subdirectories

Sonarr and Radarr monitor the `complete/` directory for finished files. Make sure this path is accessible to both FunkArr and your *arr apps (typically via a shared Docker volume mount).

### Concurrent Downloads

Controls how many videos are downloaded and remuxed simultaneously. Each download uses one FFmpeg process. Increase on fast connections with available CPU; decrease if you see timeouts or resource issues.

### Categories

Categories map Sonarr/Radarr download categories to subdirectories inside `complete/`. Configure one category per *arr app type:

```
FunkArr__Download__Categories__0__Name=tv
FunkArr__Download__Categories__0__Dir=tv
FunkArr__Download__Categories__1__Name=movies
FunkArr__Download__Categories__1__Dir=movies
```

| Variable | Default | Description |
|----------|---------|-------------|
| `FunkArr__Download__Categories__N__Name` | - | Category name as configured in Sonarr/Radarr |
| `FunkArr__Download__Categories__N__Dir` | - | Subdirectory inside `complete/` for this category |

When Sonarr sends a download request with category `tv`, the finished file ends up in `complete/tv/`. The `N` in the variable name is a zero-based index - use `0`, `1`, `2`, etc. for each category.

### Download Schedule

Optional time windows during which downloads are allowed to start. Outside these windows, downloads remain in the queue and start automatically when the next window opens. Without configured time slots, downloads run around the clock.

| Variable | Default | Description |
|----------|---------|-------------|
| `FunkArr__Download__DownloadSchedule__N__Start` | - | Start time of the window (format: `HH:mm`) |
| `FunkArr__Download__DownloadSchedule__N__End` | - | End time of the window (format: `HH:mm`) |

The `N` is a zero-based index. Time windows can span midnight (e.g. `23:00` to `02:00`). Multiple windows are supported.

```
FunkArr__Download__DownloadSchedule__0__Start=23:00
FunkArr__Download__DownloadSchedule__0__End=06:00
```

## Network Routes

FunkArr can reach different Mediatheken via different network paths. This is necessary to access geo-restricted content from ORF (Austria) or SRF (Switzerland). FunkArr only supports HTTP proxies - VPN infrastructure (e.g. WireGuard + AirVPN) runs outside of FunkArr in Docker.

### Route Definitions

Named network paths with an optional HTTP proxy:

| Variable | Default | Description |
|----------|---------|-------------|
| `FunkArr__Routes__Definitions__N__Name` | `Direct` | Route name |
| `FunkArr__Routes__Definitions__N__Proxy` | _(empty)_ | HTTP proxy URI (empty = direct connection) |
| `FunkArr__Routes__Default` | `Direct` | Default route for unmapped channels |

### Channel Mapping

Glob patterns map Mediathek channels to a route. The first matching pattern wins. Channels without a match use the default route.

| Variable | Default | Description |
|----------|---------|-------------|
| `FunkArr__Routes__ChannelRoutes__N__Pattern` | - | Glob pattern for the channel name (e.g. `ORF*`) |
| `FunkArr__Routes__ChannelRoutes__N__Route` | - | Name of the assigned route |

Patterns use simple glob matching (`*` for any characters). MediathekViewWeb returns these channel names: `ARD`, `ARD-alpha`, `BR`, `ZDF`, `ZDFneo`, `ZDFinfo`, `3Sat`, `ARTE.DE`, `ORF`, `SRF`, `Funk.net`, `KiKA`, `DW`, and more.

Example configuration:

```
FunkArr__Routes__Definitions__0__Name=Direct
FunkArr__Routes__Definitions__1__Name=Austria
FunkArr__Routes__Definitions__1__Proxy=http://tinyproxy-at:8888
FunkArr__Routes__Definitions__2__Name=Switzerland
FunkArr__Routes__Definitions__2__Proxy=http://tinyproxy-ch:8888

FunkArr__Routes__ChannelRoutes__0__Pattern=ORF*
FunkArr__Routes__ChannelRoutes__0__Route=Austria
FunkArr__Routes__ChannelRoutes__1__Pattern=SRF*
FunkArr__Routes__ChannelRoutes__1__Route=Switzerland

FunkArr__Routes__Default=Direct
```

With this configuration, ORF content is downloaded through an Austrian proxy, SRF content through a Swiss proxy, and all other channels (ARD, ZDF, 3Sat, ...) connect directly.

::: warning
If a proxy is unreachable, the download fails - there is no automatic fallback to a direct connection. This is intentional: a silent fallback would bypass the geo-restriction you explicitly configured.
:::

The route applies to the entire download pipeline: both subtitle downloads and FFmpeg use the configured proxy.

### Docker Compose with Proxy Infrastructure

A complete example with FunkArr, HTTP proxies, and WireGuard VPN gateways:

```yaml
services:
  funkarr:
    image: ghcr.io/st0o0/funkarr:latest
    restart: unless-stopped
    ports:
      - "8080:6969"
    volumes:
      - funkarr-data:/app/data
      - /path/to/media:/media
    environment:
      - FunkArr__Download__Path=/media/downloads
      # Route definitions
      - FunkArr__Routes__Definitions__0__Name=Direct
      - FunkArr__Routes__Definitions__1__Name=Austria
      - FunkArr__Routes__Definitions__1__Proxy=http://tinyproxy-at:8888
      - FunkArr__Routes__Definitions__2__Name=Switzerland
      - FunkArr__Routes__Definitions__2__Proxy=http://tinyproxy-ch:8888
      # Channel mapping
      - FunkArr__Routes__ChannelRoutes__0__Pattern=ORF*
      - FunkArr__Routes__ChannelRoutes__0__Route=Austria
      - FunkArr__Routes__ChannelRoutes__1__Pattern=SRF*
      - FunkArr__Routes__ChannelRoutes__1__Route=Switzerland
      - FunkArr__Routes__Default=Direct

  # Austria: HTTP proxy in front of WireGuard gateway
  tinyproxy-at:
    image: monokal/tinyproxy
    restart: unless-stopped
    network_mode: "service:wireguard-at"

  wireguard-at:
    image: lscr.io/linuxserver/wireguard
    restart: unless-stopped
    cap_add:
      - NET_ADMIN
    volumes:
      - ./wireguard/at:/config
    environment:
      - PUID=1000
      - PGID=1000

  # Switzerland: HTTP proxy in front of WireGuard gateway
  tinyproxy-ch:
    image: monokal/tinyproxy
    restart: unless-stopped
    network_mode: "service:wireguard-ch"

  wireguard-ch:
    image: lscr.io/linuxserver/wireguard
    restart: unless-stopped
    cap_add:
      - NET_ADMIN
    volumes:
      - ./wireguard/ch:/config
    environment:
      - PUID=1000
      - PGID=1000

volumes:
  funkarr-data:
```

::: tip
The proxy containers (`tinyproxy-at`, `tinyproxy-ch`) use `network_mode: "service:wireguard-*"`, so all their network traffic flows through the respective WireGuard tunnel. FunkArr reaches them via the internal Docker network - the proxies don't need to be publicly accessible.
:::

## Rulesets

| Variable | Default | Description |
|----------|---------|-------------|
| `FunkArr__RuleSet__Repository` | `st0o0/funkarr` | GitHub repository for community rulesets |
| `FunkArr__RuleSet__Version` | `latest` | Ruleset version to use |
| `FunkArr__RuleSet__RefreshEnabled` | `true` | Enable automatic ruleset updates |

### Repository

The GitHub repository where community rulesets are published as release assets. Only change this if you're running a fork with your own ruleset releases.

### Version

Set to `latest` to always use the newest community ruleset release. Pin to a specific version tag (e.g. `rulesets-v0.2.0`) to prevent automatic updates - useful if a new release breaks a mapping you depend on.

### Refresh

When enabled, FunkArr checks for new ruleset releases every 30 minutes and automatically downloads updates. Disable this if you pin a specific version or run in an air-gapped environment.

## Metadata

| Variable | Default | Description |
|----------|---------|-------------|
| `FunkArr__Tmdb__ApiKey` | _(empty)_ | TMDB API key |
| `FunkArr__Tvdb__ApiKey` | _(empty)_ | TVDB API key |

### TMDB

A [TMDB](https://www.themoviedb.org/) API key enables metadata resolution for movies and series. FunkArr uses it to match Mediathek entries to the correct TMDB title, improving accuracy for Radarr and Sonarr.

Get a free API key at [themoviedb.org/settings/api](https://www.themoviedb.org/settings/api).

### TVDB

A [TVDB](https://thetvdb.com/) API key enables episode guide resolution. FunkArr uses TVDB data to map airdate-based Mediathek entries to season/episode numbers when the ruleset uses an airdate strategy.

Get an API key at [thetvdb.com/api-information](https://thetvdb.com/api-information).

::: tip
Both keys are optional but recommended. Without them, FunkArr relies solely on ruleset patterns for matching, which works but produces fewer results.
:::

## Scoring

| Variable | Default | Description |
|----------|---------|-------------|
| `FunkArr__Scoring__PoolSize` | `4` | Parallel scoring workers |

The scoring pool processes Mediathek search results against rulesets in parallel. Each worker evaluates one ruleset match at a time. Increase for faster search responses on multi-core systems; the default of 4 works well for most setups.

## Match History

| Variable | Default | Description |
|----------|---------|-------------|
| `FunkArr__ScoringHistory__MaxSnapshots` | `100` | Max match history snapshots to retain |
| `FunkArr__ScoringHistory__MaxAgeDays` | `30` | Days before old snapshots are pruned |
| `FunkArr__ScoringHistory__SnapshotInterval` | `20` | Interval between snapshots |

Match history tracks which ruleset mappings produced successful downloads over time. This data feeds back into scoring - rules that historically produced correct matches get a confidence boost.

- **MaxSnapshots** - limits storage for match history. Higher values give more historical data for scoring but use more disk.
- **MaxAgeDays** - removes snapshots older than this many days. Mediathek content changes regularly, so old match data becomes less relevant.
- **SnapshotInterval** - controls how often new snapshots are taken. Lower values capture more granular data but increase database writes.

## Arr API

| Variable | Default | Description |
|----------|---------|-------------|
| `FunkArr__ArrApi__SearchTimeoutSeconds` | `30` | Timeout in seconds for Newznab search requests |
| `FunkArr__ArrApi__DownloadTimeoutSeconds` | `10` | Timeout in seconds for SABnzbd download requests |
| `FunkArr__ArrApi__SearchCacheTtlSeconds` | `60` | Duration in seconds that search results are cached |

These timeouts apply to the external API endpoints (`/index/api` and `/download/api`) that Prowlarr, Sonarr and Radarr call. The search cache prevents a pagination follow-up request from re-triggering the full search.

## PostgreSQL

By default, FunkArr uses SQLite with the database file at `{DataPath}/funkarr.db`. Set `FunkArr__Postgres__Host` to switch to PostgreSQL.

| Variable | Default | Description |
|----------|---------|-------------|
| `FunkArr__Postgres__Host` | _(empty)_ | PostgreSQL host - set to enable PostgreSQL |
| `FunkArr__Postgres__Port` | `5432` | PostgreSQL port |
| `FunkArr__Postgres__User` | _(empty)_ | PostgreSQL user |
| `FunkArr__Postgres__Password` | _(empty)_ | PostgreSQL password |
| `FunkArr__Postgres__Database` | `funkarr` | PostgreSQL database name |

::: info
When `Host` is empty or not set, FunkArr uses SQLite. This is the recommended default for single-instance setups. Use PostgreSQL when you need external database management or plan to scale.
:::

## Logging

FunkArr uses [Serilog](https://serilog.net/) for structured logging. Log levels can be tuned per namespace:

| Variable | Default | Description |
|----------|---------|-------------|
| `Serilog__MinimumLevel__Default` | `Information` | Global minimum log level |
| `Serilog__MinimumLevel__Override__Akka` | `Warning` | Akka.NET actor system log level |
| `Serilog__MinimumLevel__Override__Microsoft.AspNetCore` | `Warning` | ASP.NET Core request log level |

Valid levels: `Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Fatal`.

Set the default to `Debug` for troubleshooting. The Akka and ASP.NET overrides are set to `Warning` by default to reduce noise from the actor system and HTTP pipeline.

## Networking

FunkArr listens on port **6969** inside the container. Map it to any host port in your Docker Compose:

```yaml
ports:
  - "8080:6969"   # Access via http://localhost:8080
```

## FFmpeg

FFmpeg must be available on `PATH` - it is included in the official Docker image. FunkArr uses FFmpeg to:

- Download video streams (including HLS `.m3u8`)
- Remux to MKV (copies video/audio codecs without re-encoding)
- Embed subtitles as SRT tracks with German language tag

When a [network route](#network-routes) with a proxy is configured for the channel, FFmpeg is automatically started with the corresponding HTTP proxy (`-http_proxy`). There are no separate configuration options for FFmpeg behavior. The setup health check (`/api/system/setup`) verifies FFmpeg is available.

## Health Checks

FunkArr exposes three health endpoints, none of which are configurable:

| Endpoint | Purpose |
|----------|---------|
| `/healthz` | Full health check (database, actor system) - returns 200 or 503 |
| `/alive` | Simple liveness probe - always returns 200 |
| `/api/system/setup` | Setup validation - checks API key, directories, FFmpeg, API connectivity |

Use `/healthz` for container orchestration health probes and `/api/system/setup` in the web UI to verify your configuration.
