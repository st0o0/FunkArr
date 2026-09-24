# Comparison with Alternatives

Three projects solve the same problem: getting content from German-language public broadcaster Mediatheken into Sonarr and Radarr. All three query the [MediathekViewWeb](https://mediathekviewweb.de/) API as their data source and expose a Newznab indexer API and a SABnzbd download client API so the *arr apps treat them like any other Usenet source.

::: info Same Data Source
All three projects query the same MediathekViewWeb API. The available content (ARD, ZDF, ORF, SRF, etc.) is identical — the difference lies in how each project matches, scores and downloads it.
:::

## Quick Comparison

|                        | FunkArr              | MediathekArr           | RundfunkArr             |
|------------------------|----------------------|------------------------|-------------------------|
| **Stack**              | .NET 10 / Akka.NET   | .NET (C#)              | Node.js / Next.js       |
| **Status**             | Active               | Beta (last release Feb 2025) | Active (v1.3.0)   |
| **GitHub Stars**       | New project          | ~374                   | ~40                     |
| **Sonarr**             | Yes                  | Yes                    | Yes                     |
| **Radarr**             | Yes                  | Limited (WIP)          | Yes (since v1.1.0)      |
| **Rulesets**           | Community + custom, web UI editor | Built-in matching | Community rulesets, auto-update from GitHub |
| **Metadata**           | TMDB + TVDB (requires API keys) | TVDB only     | Local shows.json → TVDB → TMDB |
| **Download**           | FFmpeg (HLS + direct)| Direct HTTP            | Direct HTTP + yt-dlp    |
| **Output**             | MKV (remux, no re-encode) | MKV               | MKV (optional FFmpeg)   |
| **Subtitles**          | SRT from HLS or separate download | Yes       | Yes                     |
| **Database**           | SQLite or PostgreSQL | SQLite                 | SQLite (Prisma)         |
| **Web UI**             | Yes (Vue.js, with setup wizard) | Yes (with setup wizard)| Yes (Next.js, with setup wizard) |
| **Port**               | 6969                 | 5007                   | 6767                    |
| **Docker**             | Multi-arch (amd64, arm64, armv7) | Yes         | Multi-arch (amd64, arm64) |
| **Auto-config**        | Yes (creates indexer + download client in Prowlarr/Sonarr/Radarr) | Yes (setup wizard) | No |
| **Proxy support**      | No                   | No                     | Yes (for downloads + yt-dlp) |
| **Umlaut handling**    | Built-in             | Via UmlautAdaptarr (separate service) | Not specified |
| **Match history**      | Yes (diagnostics + stats) | No                | No                      |
| **PUID/PGID**          | Yes                  | Not documented         | Yes                     |
| **ORF/SRF**            | Via MediathekViewWeb | M3U download (beta.12) | HLS via yt-dlp (SRF needs SRG-SSR credentials) |

## MediathekArr

[MediathekArr](https://github.com/PCJones/MediathekArr) by PCJones is the most popular project in this space (~374 stars). It uses a .NET backend and integrates MediathekViewWeb, UmlautAdaptarr, and TheTVDB.

### Strengths

- **Largest community** with active Discord (UsenetDE server) and Telegram channels — best place to get help if you run into issues
- **[UmlautAdaptarr](https://github.com/PCJones/UmlautAdaptarr)** (~306 stars) — companion tool that intercepts and modifies search queries between *arr apps and indexers to fix German umlaut matching, title discovery, and release naming across Sonarr, Lidarr, and Readarr (Radarr support in progress)
- **Setup wizard** in the web interface that guides through initial configuration
- **Advanced filter and matching system** for TV shows, seasons and episodes

### Limitations

- No user-extensible ruleset system — title matching uses built-in logic. Works well for common shows but can't be extended by users for niche content.
- Radarr movie support is limited/WIP. The README states: "You can find a few movies via interactive search, but not a lot."
- ORF and SRF support was added in beta.12 (February 2025) via M3U download. Issue #77 requests disabling ORF by default due to geoblocking.
- Requires [UmlautAdaptarr](https://github.com/PCJones/UmlautAdaptarr) as a separate companion service for proper German title resolution.
- Still in beta — the README warns "use the beta image until 1.0 is released. Latest/Main is not working." Last release (beta.12) is from February 2025. V2 is being planned in issues.
- TVDB only, no TMDB integration.
- SQLite only, database not configurable.
- Downloads via direct HTTP only (no HLS/FFmpeg).

## RundfunkArr

[RundfunkArr](https://github.com/rundfunkarr/rundfunkarr) (~40 stars) is a Node.js/Next.js project. Originally also called "MediathekArr", it was renamed to avoid confusion.

### Strengths

- **yt-dlp integration** for HLS stream resolution — handles SRF and ORF streams and supports more edge-case formats than FFmpeg alone. Version pinned with checksum verification in Docker.
- **Proxy support** for yt-dlp and downloads (not metadata APIs) — useful for accessing geo-restricted content from abroad
- **Full Radarr support** since v1.1.0 with TMDB/IMDB ID support and intelligent title parsing
- Active development with regular releases, zero open issues, and a setup wizard
- PUID/PGID support and multi-arch Docker images (amd64, arm64)
- Multiple metadata sources: local shows.json → TVDB → TMDB
- Community rulesets with auto-update from GitHub (since v1.2.0)

### Limitations

- Rulesets are community-maintained via PRs to the repository — no web UI editor for creating or modifying rules.
- Metadata resolution checks a local `shows.json` first, which needs manual updates for new shows that aren't in TVDB/TMDB.
- SRF support requires credentials from the SRG-SSR developer portal.
- yt-dlp resolves video references at download time, not at search time — regional restrictions depend on proxy location and broadcaster availability.
- No scoring, match history or diagnostics.
- Smaller community (~40 stars, no Discord/Telegram).

## FunkArr — Honest Limitations

In fairness, FunkArr also has limitations:

- **No proxy support** — unlike RundfunkArr, there's no way to use a proxy for geo-restricted ORF/SRF content.
- **No yt-dlp** — uses FFmpeg only for HLS and direct downloads. If a stream format isn't supported by FFmpeg's native HLS demuxer, it fails. yt-dlp handles more edge cases.
- **Newest and smallest community** — MediathekArr has years of head start and the largest user base. If you need help, there's no Discord or Telegram channel yet.
- **TVDB and TMDB API keys required** for metadata enrichment — they're optional but needed for full functionality.
- **Match history is diagnostic, not adaptive** — the scoring engine executes rules deterministically. History records past runs and aggregates stats for debugging, but it doesn't learn or adjust weights based on past results.

## When to Use Which

- **MediathekArr** if you primarily watch ARD/ZDF content and want the largest community for support.
- **RundfunkArr** if you need proxy support for geo-restricted content (SRF/ORF from abroad), want full Radarr movie support, or prefer yt-dlp's broader format handling.
- **FunkArr** if you want community-driven rulesets with a visual web editor, match scoring diagnostics, PostgreSQL support, or TMDB metadata alongside TVDB.
