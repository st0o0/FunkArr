# Configuration

All configuration is via environment variables using the `FunkArr__` prefix. The double underscore (`__`) separates nested sections — this is standard ASP.NET Core configuration binding.

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

- **`incomplete/`** — active downloads and remux operations in progress
- **`complete/`** — finished downloads, organized by category subdirectories

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
| `FunkArr__Download__Categories__N__Name` | — | Category name as configured in Sonarr/Radarr |
| `FunkArr__Download__Categories__N__Dir` | — | Subdirectory inside `complete/` for this category |

When Sonarr sends a download request with category `tv`, the finished file ends up in `complete/tv/`. The `N` in the variable name is a zero-based index — use `0`, `1`, `2`, etc. for each category.

## Rulesets

| Variable | Default | Description |
|----------|---------|-------------|
| `FunkArr__RuleSet__Repository` | `st0o0/funkarr` | GitHub repository for community rulesets |
| `FunkArr__RuleSet__Version` | `latest` | Ruleset version to use |
| `FunkArr__RuleSet__RefreshEnabled` | `true` | Enable automatic ruleset updates |

### Repository

The GitHub repository where community rulesets are published as release assets. Only change this if you're running a fork with your own ruleset releases.

### Version

Set to `latest` to always use the newest community ruleset release. Pin to a specific version tag (e.g. `rulesets-v0.2.0`) to prevent automatic updates — useful if a new release breaks a mapping you depend on.

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
| `FunkArr__MatchHistory__MaxSnapshots` | `100` | Max match history snapshots to retain |
| `FunkArr__MatchHistory__MaxAgeDays` | `30` | Days before old snapshots are pruned |
| `FunkArr__MatchHistory__SnapshotInterval` | `20` | Interval between snapshots |

Match history tracks which ruleset mappings produced successful downloads over time. This data feeds back into scoring — rules that historically produced correct matches get a confidence boost.

- **MaxSnapshots** — limits storage for match history. Higher values give more historical data for scoring but use more disk.
- **MaxAgeDays** — removes snapshots older than this many days. Mediathek content changes regularly, so old match data becomes less relevant.
- **SnapshotInterval** — controls how often new snapshots are taken. Lower values capture more granular data but increase database writes.

## PostgreSQL

By default, FunkArr uses SQLite with the database file at `{DataPath}/funkarr.db`. Set `FunkArr__Postgres__Host` to switch to PostgreSQL.

| Variable | Default | Description |
|----------|---------|-------------|
| `FunkArr__Postgres__Host` | _(empty)_ | PostgreSQL host — set to enable PostgreSQL |
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

FFmpeg must be available on `PATH` — it is included in the official Docker image. FunkArr uses FFmpeg to:

- Download video streams (including HLS `.m3u8`)
- Remux to MKV (copies video/audio codecs without re-encoding)
- Embed subtitles as SRT tracks with German language tag

There are no configuration options for FFmpeg behavior. The setup health check (`/api/system/setup`) verifies FFmpeg is available.

## Health Checks

FunkArr exposes three health endpoints, none of which are configurable:

| Endpoint | Purpose |
|----------|---------|
| `/healthz` | Full health check (database, actor system) — returns 200 or 503 |
| `/alive` | Simple liveness probe — always returns 200 |
| `/api/system/setup` | Setup validation — checks API key, directories, FFmpeg, API connectivity |

Use `/healthz` for container orchestration health probes and `/api/system/setup` in the web UI to verify your configuration.
