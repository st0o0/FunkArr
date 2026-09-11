<h1 align="center">FunkArr</h1>

<p align="center">
  German-language public broadcaster Mediathek integration for the *arr ecosystem
</p>

<p align="center">
  <a href="https://github.com/st0o0/funkarr/releases"><img src="https://img.shields.io/github/v/release/st0o0/funkarr" alt="Release" /></a>
  <a href="https://github.com/st0o0/funkarr/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue" alt="License" /></a>
  <img src="https://img.shields.io/badge/.NET-10.0-512bd4" alt=".NET 10" />
  <a href="https://ghcr.io/st0o0/funkarr"><img src="https://img.shields.io/docker/pulls/st0o0/funkarr" alt="Docker Pulls" /></a>
</p>

---

Searches ARD, ZDF, ORF, SRF, and other German-language public broadcaster Mediatheken via [MediathekViewWeb](https://mediathekviewweb.de/), downloads video and subtitles, remuxes to MKV with FFmpeg, and exposes standard APIs so Sonarr, Radarr, and Prowlarr treat it like a Usenet indexer and download client.

No Usenet account needed. No torrents. Just direct downloads from public media libraries.

## Features

- **Newznab indexer API** - add FunkArr in Prowlarr or directly in Sonarr/Radarr as an indexer
- **SABnzbd download client API** - add it as a SABnzbd download client in Sonarr/Radarr
- **Community rulesets** - map messy Mediathek titles to structured season/episode format, auto-synced from GitHub
- **RuleSet builder** - create and test rulesets with a visual editor and debugger in the web UI
- **Metadata resolution** - resolves series and movies via TMDB and TVDB for accurate matching
- **Match intelligence** - tracks which mappings worked so results improve over time
- **Subtitle handling** - downloads or extracts subtitles from HLS streams, converts to SRT
- **Web UI** - download queue, history, ruleset management, and setup health checks
- **Single container** - runs on any Docker host, SQLite by default

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

Community rulesets sync automatically from GitHub. You can also create custom rulesets in the web UI (Rulesets section), which includes a debugger to test rules against live Mediathek data.

## Configuration

All configuration is via environment variables. Defaults work out of the box - the only required setting is `FunkArr__Download__Path` if you want downloads written to a mounted volume.

| Variable | Default | Description |
|----------|---------|-------------|
| `FunkArr__ApiKey` | `funkarr-default-api-key` | API key for Prowlarr/Sonarr/Radarr |
| `FunkArr__DataPath` | `data` | Base path for database, rulesets, temp files |
| **Downloads** | | |
| `FunkArr__Download__Path` | `data/downloads` | Root download directory (contains `incomplete/` and `complete/`) |
| `FunkArr__Download__ConcurrentDownloads` | `3` | Max parallel downloads |
| `FunkArr__Download__Categories__0__Name` | — | Category name (e.g. `tv`) - map Sonarr/Radarr categories to subdirectories |
| `FunkArr__Download__Categories__0__Dir` | — | Subdirectory for this category (e.g. `tv`) |
| **RuleSets** | | |
| `FunkArr__RuleSet__Repository` | `st0o0/funkarr` | GitHub repo for community rulesets |
| `FunkArr__RuleSet__Version` | `latest` | Pin ruleset version or `latest` |
| `FunkArr__RuleSet__RefreshEnabled` | `true` | Auto-sync rulesets from GitHub |
| **Metadata** | | |
| `FunkArr__Tmdb__ApiKey` | _(empty)_ | TMDB API key for movie/series resolution |
| `FunkArr__Tvdb__ApiKey` | _(empty)_ | TVDB API key for episode guide resolution |
| **Scoring** | | |
| `FunkArr__Scoring__PoolSize` | `4` | Parallel scoring workers |
| **Match History** | | |
| `FunkArr__MatchHistory__MaxSnapshots` | `100` | Max match history snapshots to retain |
| `FunkArr__MatchHistory__MaxAgeDays` | `30` | Days before old snapshots are pruned |
| `FunkArr__MatchHistory__SnapshotInterval` | `20` | Interval between snapshots |
| **PostgreSQL** | | |
| `FunkArr__Postgres__Host` | _(empty)_ | PostgreSQL host - set to switch from SQLite to Postgres |
| `FunkArr__Postgres__Port` | `5432` | PostgreSQL port |
| `FunkArr__Postgres__User` | _(empty)_ | PostgreSQL user |
| `FunkArr__Postgres__Password` | _(empty)_ | PostgreSQL password |
| `FunkArr__Postgres__Database` | `funkarr` | PostgreSQL database name |

See [docker-compose.example.yml](docker-compose.example.yml) for a copy-paste ready template with all options.

## Build & Test

All commands run from the repo root:

```powershell
dotnet build src/FunkArr.slnx
```

Tests use xUnit v3 on Microsoft Testing Platform - run with `dotnet run`, not `dotnet test`:

```powershell
dotnet run --project src/FunkArr.Search.Tests/FunkArr.Search.Tests.csproj
dotnet run --project src/FunkArr.Download.Tests/FunkArr.Download.Tests.csproj
dotnet run --project src/FunkArr.RuleSet.Tests/FunkArr.RuleSet.Tests.csproj
dotnet run --project src/FunkArr.MatchMagic.Tests/FunkArr.MatchMagic.Tests.csproj
dotnet run --project src/FunkArr.MetadataResolver.Tests/FunkArr.MetadataResolver.Tests.csproj
dotnet run --project src/FunkArr.Api.Tests/FunkArr.Api.Tests.csproj
dotnet run --project src/FunkArr.ArrApi.Tests/FunkArr.ArrApi.Tests.csproj
dotnet run --project src/FunkArr.Architecture.Tests/FunkArr.Architecture.Tests.csproj
```

Format check (CI enforces this):

```powershell
dotnet format src/FunkArr.slnx --verify-no-changes
```

## Alternatives

Other projects in this space:

|                | FunkArr           | MediathekArr        | RundfunkArr          |
|----------------|-------------------|---------------------|----------------------|
| Stack          | .NET / Akka.NET   | .NET                | Node.js / Next.js    |
| Status         | Active            | Beta                | Dormant (since 2024) |
| Sonarr         | Yes               | Yes                 | Yes                  |
| Radarr         | Yes               | Limited             | Yes                  |
| ORF/SRF        | Yes               | Yes                 | No                   |
| Persistence    | SQLite / PostgreSQL | SQLite              | SQLite (Prisma)      |

- [MediathekArr](https://github.com/PCJones/MediathekArr) by PCJones
- [RundfunkArr](https://github.com/rundfunkarr/rundfunkarr)

## License

[MIT](LICENSE)
