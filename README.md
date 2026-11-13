# funkarr

[![Release](https://img.shields.io/github/v/release/st0o0/funkarr?sort=semver&style=flat-square)](https://github.com/st0o0/funkarr/releases)
[![GHCR](https://img.shields.io/badge/ghcr.io-st0o0%2Ffunkarr-2496ED?style=flat-square&logo=docker&logoColor=white)](https://github.com/st0o0/funkarr/pkgs/container/funkarr)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg?style=flat-square)](LICENSE)

German-language public broadcaster Mediathek integration for the *arr ecosystem. Searches ARD, ZDF, ORF, SRF, and other Mediatheken via [MediathekViewWeb](https://mediathekviewweb.de/), downloads video and subtitles, remuxes to MKV with FFmpeg, and exposes standard APIs so Sonarr, Radarr, and Prowlarr treat it like a Usenet indexer and download client.

No Usenet account needed. No torrents. Just direct downloads from public media libraries.

## Features

- **Newznab indexer API** - add FunkArr in Prowlarr or directly in Sonarr/Radarr as an indexer
- **SABnzbd download client API** - add it as a SABnzbd download client in Sonarr/Radarr
- **Community rulesets** - map messy Mediathek titles to structured season/episode format, auto-synced from GitHub ([browse catalog](https://funkarr.st0o0.net/en/rulesets/catalog))
- **RuleSet builder** - create and test rulesets with a visual editor and debugger in the web UI
- **Metadata resolution** - resolves series and movies via TMDB and TVDB for accurate matching
- **Match intelligence** - tracks which mappings worked so results improve over time
- **Subtitle handling** - downloads or extracts subtitles from HLS streams, converts to SRT
- **Network routing and proxy** - route channel-specific traffic through VPN or proxy for geo-restricted content (ORF, SRF, etc.)
- **Download scheduling** - define time windows for downloads to run, pause automatically outside schedule
- **Health checks** - real health checks for FFmpeg, MediathekViewWeb connectivity, and directory write access
- **Observability** - Prometheus `/metrics` endpoint with download, search, scoring, enrichment, ruleset, and external API health metrics
- **Web UI** - download queue with detail view, activity and scoring history, ruleset management, settings with log viewer, and setup health checks
- **Single container** - runs on any Docker host, SQLite by default, PUID/PGID support

## Quick Start

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
      - FunkArr__ApiKey=your-api-key-here
      - FunkArr__Download__Path=/media/downloads
      - FunkArr__Download__Categories__0__Name=tv
      - FunkArr__Download__Categories__0__Dir=tv
      - FunkArr__Download__Categories__1__Name=movies
      - FunkArr__Download__Categories__1__Dir=movies

volumes:
  funkarr-data:
```

```bash
docker compose up -d
```

The web UI is available at `http://localhost:8080`.

## Setup in Prowlarr / Sonarr / Radarr

### Indexer (Prowlarr or Sonarr/Radarr)

1. Add a new indexer of type **Newznab**
2. URL: `http://funkarr:6969/index/api`
3. API Key: the value you set for `FunkArr__ApiKey`
4. Test and save

### Download Client (Sonarr / Radarr)

1. Add a new download client of type **SABnzbd**
2. Host: `funkarr`, Port: `6969`
3. URL Base: `/download/api`
4. API Key: the value you set for `FunkArr__ApiKey`
5. Test and save

## Rulesets

Mediathek titles are messy - "Tatort" episodes might appear as "Tatort: Der letzte Schrei" with no season or episode number. Rulesets map these titles to structured season/episode format so Sonarr can match them.

Community rulesets sync automatically from GitHub and cover many popular shows and movies out of the box ([catalog](https://funkarr.st0o0.net/en/rulesets/catalog)). You can also create custom rulesets in the web UI (Rulesets section), which includes a debugger to test rules against live Mediathek data.

### Confidence Levels

Each ruleset carries a confidence score (0.0 - 1.0) that reflects how reliably its rules can identify episodes. The score flows through to Sonarr/Radarr as match quality and affects which results are prioritized.

| Strategy | Confidence | Rationale |
|----------|-----------|-----------|
| Season + episode regex | 0.95 | Direct numeric extraction from structured titles |
| Season/episode + title fallback | 0.90 | Reliable primary match, title fallback for edge cases |
| Absolute episode number | 0.90 | Consistent numbering in long-running series |
| Airdate extraction | 0.85 | Date parsing reliable, but airdate matching depends on guide data |
| Exact title match | 0.85 | Precise but fragile, so any title variation breaks it |
| Title substring match (shows) | 0.80 | Flexible but can produce false positives |
| Title substring match (movies) | 0.80 | Static title matching against broadcast listings |

## Configuration

All configuration is via environment variables (`FunkArr__…`). Defaults work out of the box - the only required setting is `FunkArr__Download__Path` if you want downloads written to a mounted volume.

See [docker-compose.example.yml](docker-compose.example.yml) for a copy-paste ready template with all options. For the full configuration reference including network routes, logging, and health checks, see the [documentation site](https://funkarr.st0o0.net/en/configuration).

## Image

- **Registry:** `ghcr.io/st0o0/funkarr`
- **Tags:** semantic version (`X.Y.Z`), `latest`
- **Architectures:** `linux/amd64`

## Development

Want to help? Start with [Contributing](https://funkarr.st0o0.net/en/develop/) ([Deutsch](https://funkarr.st0o0.net/develop/)): architecture, local development with the dev stack, building and testing, rulesets and releases. Code conventions are on [anvil](https://anvil.st0o0.net/).

## Alternatives

Other projects in this space:

|                | FunkArr           | MediathekArr        | RundfunkArr          |
|----------------|-------------------|---------------------|----------------------|
| Stack          | .NET / Akka.NET   | .NET                | Node.js / Next.js    |
| Status         | 0.x               | Beta                | Dormant (since 2024) |
| Sonarr         | Yes               | Yes                 | Yes                  |
| Radarr         | Yes               | Limited             | Yes                  |
| ORF/SRF        | Yes               | Yes                 | No                   |
| Persistence    | SQLite / PostgreSQL | SQLite              | SQLite (Prisma)      |

- [MediathekArr](https://github.com/PCJones/MediathekArr) by PCJones
- [RundfunkArr](https://github.com/rundfunkarr/rundfunkarr)

## License

[MIT](LICENSE)
