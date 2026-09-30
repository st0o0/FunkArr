## Why

MediathekArr (PHP-turned-C#) integrates German public broadcaster media libraries (ARD, ZDF, etc.) into the *arr ecosystem (Prowlarr, Sonarr, Radarr) but suffers from duplicated code, no fault tolerance on downloads, weak metadata matching, and no subtitle support. FunkArr is a clean-slate rewrite that establishes a production-grade foundation with proper supervision, durability, and extensibility.

## What Changes

- New Newznab-compatible REST API (Indexer) that searches MediathekViewWeb and returns results as RSS/XML to Prowlarr/Sonarr/Radarr
- New SABnzbd-compatible REST API (Download Client) that accepts download requests, fetches video/subtitle streams via HTTP, and muxes to MKV
- Actor-based download queue with supervision, retry, and configurable concurrency (default 3)
- Persistent download queue state via SQLite (survives restarts)
- FFmpeg-based remuxing pipeline (video + subtitles → MKV, stream-copy, no re-encoding)
- Structured logging with Serilog
- Multi-arch Docker image (linux-x64, linux-arm64) with CI/CD via GitHub Actions and release-please

## Capabilities

### New Capabilities

- `newznab-indexer`: Newznab XML/RSS API surface — caps, tvsearch, movie search, fake NZB generation. Translates *arr search requests into MediathekViewWeb queries and returns formatted results.
- `sabnzbd-download-client`: SABnzbd JSON API surface — version, config, queue, history, addfile. Accepts fake NZBs, extracts real download URLs, manages download lifecycle.
- `mediathek-search`: MediathekViewWeb API client with rate limiting and result caching. Includes the matching pipeline (title normalization, date matching, S##E## patterns, runtime filtering).
- `download-pipeline`: Actor-supervised download queue with concurrent HTTP downloads, retry on failure, and persistent state via Akka.Persistence + SQLite.
- `muxing-pipeline`: FFmpeg process management for remuxing downloaded video + subtitle streams into MKV containers with correct language metadata.
- `project-infrastructure`: Solution structure (.NET 10, FunkArr naming), central package management, Servus AppBuilder startup, Serilog logging, Docker build, CI/CD, release-please versioning.

### Modified Capabilities

(none — greenfield project)

## Impact

- **APIs**: Two HTTP API surfaces that must be protocol-compatible with Prowlarr (Newznab) and Sonarr/Radarr (SABnzbd)
- **External dependencies**: MediathekViewWeb API (search), broadcaster CDN servers (downloads), TVDB (show metadata lookup)
- **Runtime dependencies**: FFmpeg binary must be available in the container
- **Infrastructure**: Docker image on GHCR, GitHub Actions CI/CD pipeline
- **Packages**: Akka.NET (Hosting, Persistence.Sql, Streams, Logger.Serilog), Servus, Servus.Akka, Serilog stack, Microsoft.Data.Sqlite
