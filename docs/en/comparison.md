# Comparison with Alternatives

Three projects solve the same problem: getting content from German-language public broadcaster Mediatheken into Sonarr and Radarr. All three expose a Newznab indexer API and a SABnzbd download client API so the *arr apps treat them like any other Usenet source.

## Quick Comparison

|                        | FunkArr              | MediathekArr           | RundfunkArr             |
|------------------------|----------------------|------------------------|-------------------------|
| **Stack**              | .NET 10 / Akka.NET   | .NET (C#)              | TypeScript / Next.js    |
| **Status**             | Active               | Beta (last release Feb 2025) | Active (v1.3.0, Sep 2026) |
| **Mediatheken**        | ARD, ZDF, ORF, SRF + more | ARD, ZDF, ORF, SRF (beta) | ARD, ZDF, ORF, SRF     |
| **Sonarr**             | Yes                  | Yes                    | Yes                     |
| **Radarr**             | Yes                  | Limited (few movies)   | Yes                     |
| **Rulesets**           | Community + custom   | Built-in matching only | JSON rulesets (PR-based)|
| **Metadata**           | TMDB + TVDB          | TVDB + UmlautAdaptarr  | TVDB + TMDB             |
| **Download**           | FFmpeg (HLS + direct)| Direct HTTP            | Direct HTTP + yt-dlp    |
| **Output**             | MKV (remux, no re-encode) | MKV               | MKV (optional FFmpeg)   |
| **Subtitles**          | SRT from HLS or separate download | Yes       | Yes                     |
| **Database**           | SQLite or PostgreSQL | SQLite                 | SQLite (Prisma)         |
| **Web UI**             | Yes (Vue.js)         | Yes (setup wizard)     | Yes (Next.js)           |
| **Docker**             | Single container     | x86 + ARM64            | Multi-arch (amd64, arm64) |
| **Port**               | 6969                 | 5007                   | 6767                    |

## MediathekArr

[MediathekArr](https://github.com/PCJones/MediathekArr) by PCJones is the most popular project in this space (370+ stars). It uses a .NET backend with a multi-component architecture (MediathekArr, MediathekArrLib, MediathekArrServer).

### Strengths

- Largest community with active Discord and Telegram channels
- UmlautAdaptarr companion tool that handles German umlaut variations in titles across the entire *arr ecosystem
- Three-tier match confidence system (CERTAIN / UNCERTAIN / NO.MATCH) that shows how confident each result is
- Auto-configuration wizard that can set up indexers and download clients in Sonarr/Prowlarr automatically

### Differences from FunkArr

- **No community ruleset system.** Title matching uses built-in logic with three confidence tiers. Works well for common shows but can't be extended by users for niche content.
- **Radarr movie support is limited.** Interactive search finds some movies, but automated grabs are unreliable.
- **ORF and SRF support** was added in beta.12 (February 2025) via M3U download but is less mature than ARD/ZDF.
- **Requires UmlautAdaptarr** as a separate companion service for proper German title resolution. FunkArr handles this natively.
- **Still in beta** after 2+ years. The latest release (beta.12) included security fixes for command injection and path traversal vulnerabilities.
- **No TMDB integration.** Relies on TVDB only (with 12h caching for failed queries).
- **No PostgreSQL option.** The database is not configurable.
- **No match history or scoring.** Each search starts fresh without learning from past results.

## RundfunkArr

[RundfunkArr](https://github.com/rundfunkarr/rundfunkarr) is a TypeScript/Next.js project that takes a different architectural approach.

### Strengths

- Uses yt-dlp for HLS stream resolution, which handles SRF and ORF streams well
- Optional proxy support for region-restricted content (useful for SRF/ORF access outside Switzerland/Austria)
- Clean Next.js App Router UI with direct Mediathek search, download queue, and guided setup wizard
- Active development with regular releases, nightly Docker builds, and zero open issues
- PUID/PGID support in Docker for proper file permissions
- Hierarchical metadata lookup: local shows.json, then TVDB, then TMDB

### Differences from FunkArr

- **Rulesets in a single file.** Stored as `rulesets.json` with regex-based episode/season extraction. Adding new rules requires a PR to the repo rather than a standalone release cycle or web UI editor.
- **Metadata resolution** checks a local `shows.json` first, then falls back to TVDB/TMDB. This works well for known shows but requires manual updates for new ones.
- **SRF and ORF streams** are resolved at download time via yt-dlp, not at search time. This means search results may include items that fail to download if the stream URL has expired.
- **No scoring or match intelligence.** Results are based on the latest ruleset match without historical weighting.
- **Node.js runtime** has higher baseline memory usage compared to .NET.
- Was originally also called "MediathekArr" but renamed to avoid confusion with PCJones' project.

## When to Use Which

- **FunkArr** if you want community-driven rulesets with a visual editor, match scoring that improves over time, PostgreSQL support for larger setups, or need reliable ORF/SRF coverage.
- **MediathekArr** if you primarily watch ARD/ZDF content and want the largest community for support.
- **RundfunkArr** if you prefer a Node.js stack, need proxy support for region-restricted content, or want yt-dlp's broader format support.
