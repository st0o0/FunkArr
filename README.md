# FunkArr

[![Release](https://img.shields.io/github/v/release/st0o0/funkarr)](https://github.com/st0o0/funkarr/releases)
[![License](https://img.shields.io/github/license/st0o0/funkarr)](LICENSE)
[![Docker Pulls](https://img.shields.io/docker/pulls/st0o0/funkarr)](https://ghcr.io/st0o0/funkarr)

Searches German public broadcaster Mediatheken (ARD, ZDF, etc.) for TV shows
and movies, downloads them, and remuxes everything into MKV with proper
metadata. Plugs into Sonarr, Radarr, and Prowlarr as if it were a Usenet
indexer and download client.

No Usenet account needed. No torrents. Just direct downloads from public
media libraries.


## How it works

FunkArr exposes two APIs:

- A Newznab-compatible indexer API (add it in Prowlarr or directly in Sonarr/Radarr)
- A SABnzbd-compatible download client API (add it as a SABnzbd client in Sonarr/Radarr)

When Sonarr or Radarr sends a search request, FunkArr queries MediathekViewWeb,
matches results using rulesets, and returns them as if they were Usenet releases.
When a download is triggered, FunkArr grabs the video and subtitles via HTTP and
remuxes them into MKV using FFmpeg.

### Matching

Mediathek titles don't follow any standard naming convention. "Tatort", "TATORT",
"Tatort: Freddy tanzt", "Tatort - Freddy tanzt" can all be the same show.
FunkArr uses community-driven rulesets to map these messy titles to the structured
season/episode format that Sonarr and Radarr expect. Rulesets are pulled from a
GitHub repository and refreshed automatically. A built-in match ledger tracks
which mappings worked so results improve over time.

### Quality probing

Most Mediathek APIs don't report video quality. FunkArr probes the actual media
files using URL pattern analysis, HTTP HEAD requests, and container metadata
parsing to determine real resolution and bitrate. Results are cached so repeated
searches stay fast. This lets Sonarr and Radarr make proper quality-based
decisions instead of guessing.


## Quick start

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
      - FunkArr__Download__DownloadPath=/media/downloads

volumes:
  funkarr-data:
```

See [docker-compose.example.yml](docker-compose.example.yml) for all
configuration options including PostgreSQL, quality probing, rulesets,
and path mapping.


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
