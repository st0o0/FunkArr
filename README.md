<h1 align="center">FunkArr</h1>

<p align="center">
  German public broadcaster Mediathek integration for the *arr ecosystem
</p>

<p align="center">
  <a href="https://github.com/st0o0/funkarr/releases"><img src="https://img.shields.io/github/v/release/st0o0/funkarr" alt="Release" /></a>
  <a href="https://github.com/st0o0/funkarr/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue" alt="License" /></a>
  <img src="https://img.shields.io/badge/.NET-10.0-512bd4" alt=".NET 10" />
  <a href="https://ghcr.io/st0o0/funkarr"><img src="https://img.shields.io/docker/pulls/st0o0/funkarr" alt="Docker Pulls" /></a>
</p>

---

A .NET service (Docker container) built on [Akka.NET](https://getakka.net/) that searches ARD, ZDF, and other German public broadcaster Mediatheken via [MediathekViewWeb](https://mediathekviewweb.de/), downloads video and subtitles, and remuxes everything into MKV using FFmpeg. Plugs into Sonarr, Radarr, and Prowlarr as if it were a Usenet indexer and download client.

No Usenet account needed. No torrents. Just direct downloads from public media libraries.

## Features

- **Newznab indexer API** — add FunkArr in Prowlarr or directly in Sonarr/Radarr as an indexer
- **SABnzbd download client API** — add it as a SABnzbd client in Sonarr/Radarr for downloads
- **Community-driven rulesets** — map messy Mediathek titles to structured season/episode format, auto-refreshed from GitHub
- **Quality probing** — determines real resolution and bitrate via URL pattern analysis, HTTP HEAD, and container metadata parsing
- **Match intelligence** — tracks which mappings worked so results improve over time
- **Movie search** — resolves IMDB IDs via TMDB, searches with original and translated titles
- **Subtitle handling** — downloads or extracts subtitles from HLS streams, converts to SRT
- **Single container** — runs on any Docker host with SQLite persistence by default, optional PostgreSQL

## Quick Start

```yaml
# docker-compose.yml
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
      - FunkArr__Download__DownloadPath=/media/downloads

volumes:
  funkarr-data:
```

```bash
docker compose up -d
```

See [docker-compose.example.yml](docker-compose.example.yml) for all configuration options including PostgreSQL, quality probing, rulesets, and path mapping.

## Build & Test

All commands run from `src/`:

```powershell
dotnet build FunkArr.slnx
dotnet run --project FunkArr.Tests/FunkArr.Tests.csproj   # xUnit v3 via MTP
```

## Alternatives

There are other projects in this space. Pick what fits your setup.

|                | FunkArr           | MediathekArr        | RundfunkArr          |
|----------------|-------------------|---------------------|----------------------|
| Stack          | .NET / Akka.NET   | .NET                | Node.js / Next.js    |
| Status         | Active            | Beta                | Dormant (since 2024) |
| Sonarr         | Yes               | Yes                 | Yes                  |
| Radarr         | Yes               | Limited             | Yes                  |
| Quality probe  | Yes               | No                  | No                   |
| ORF/SRF        | No                | Yes                 | No                   |
| Persistence    | SQLite / Postgres | SQLite              | SQLite (Prisma)      |

- [MediathekArr](https://github.com/PCJones/MediathekArr) by PCJones
- [RundfunkArr](https://github.com/rundfunkarr/rundfunkarr)

## License

[MIT](LICENSE)
