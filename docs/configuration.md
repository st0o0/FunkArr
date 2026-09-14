# Configuration

All configuration is via environment variables. Defaults work out of the box — the only required setting is `FunkArr__Download__Path` if you want downloads written to a mounted volume.

## General

| Variable | Default | Description |
|----------|---------|-------------|
| `FunkArr__ApiKey` | `funkarr-default-api-key` | API key for Prowlarr/Sonarr/Radarr |
| `FunkArr__DataPath` | `data` | Base path for database, rulesets, temp files |

## Downloads

| Variable | Default | Description |
|----------|---------|-------------|
| `FunkArr__Download__Path` | `data/downloads` | Root download directory (contains `incomplete/` and `complete/`) |
| `FunkArr__Download__ConcurrentDownloads` | `3` | Max parallel downloads |
| `FunkArr__Download__Categories__0__Name` | — | Category name (e.g. `tv`) |
| `FunkArr__Download__Categories__0__Dir` | — | Subdirectory for this category |

## Rulesets

| Variable | Default | Description |
|----------|---------|-------------|
| `FunkArr__RuleSet__Repository` | `st0o0/funkarr` | GitHub repo for community rulesets |
| `FunkArr__RuleSet__Version` | `latest` | Pin ruleset version or `latest` |
| `FunkArr__RuleSet__RefreshEnabled` | `true` | Auto-sync rulesets from GitHub |

## Metadata

| Variable | Default | Description |
|----------|---------|-------------|
| `FunkArr__Tmdb__ApiKey` | _(empty)_ | TMDB API key for movie/series resolution |
| `FunkArr__Tvdb__ApiKey` | _(empty)_ | TVDB API key for episode guide resolution |

## Scoring

| Variable | Default | Description |
|----------|---------|-------------|
| `FunkArr__Scoring__PoolSize` | `4` | Parallel scoring workers |

## Match History

| Variable | Default | Description |
|----------|---------|-------------|
| `FunkArr__MatchHistory__MaxSnapshots` | `100` | Max match history snapshots to retain |
| `FunkArr__MatchHistory__MaxAgeDays` | `30` | Days before old snapshots are pruned |
| `FunkArr__MatchHistory__SnapshotInterval` | `20` | Interval between snapshots |

## PostgreSQL

Set `FunkArr__Postgres__Host` to switch from SQLite to PostgreSQL.

| Variable | Default | Description |
|----------|---------|-------------|
| `FunkArr__Postgres__Host` | _(empty)_ | PostgreSQL host |
| `FunkArr__Postgres__Port` | `5432` | PostgreSQL port |
| `FunkArr__Postgres__User` | _(empty)_ | PostgreSQL user |
| `FunkArr__Postgres__Password` | _(empty)_ | PostgreSQL password |
| `FunkArr__Postgres__Database` | `funkarr` | PostgreSQL database name |
