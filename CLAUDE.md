# CLAUDE.md

## Project

FunkArr - German-language public broadcaster media library integration for the *arr
ecosystem. A .NET service (Docker container) on Akka.NET: searches ARD/ZDF/ORF/SRF/etc.
Mediatheken via MediathekViewWeb API, downloads video + subtitles, remuxes to
MKV via FFmpeg, and exposes Newznab-compatible indexer API (for Sonarr/Radarr/
Prowlarr) and SABnzbd-compatible download client API.

## Solution structure

Multi-project solution with domain isolation:

```
src/
  FunkArr.slnx
  FunkArr/                        # Host: Program.cs, Startup, DI, Config
  FunkArr.Core/                   # Common types, Akka/Servus refs
  FunkArr.Api/                    # Internal REST API (JSON, OpenAPI-first)
  FunkArr.ArrApi/                 # Newznab + SABnzbd adapter (thin translation)
  FunkArr.Search/                 # MediathekViewWeb query + fetch
  FunkArr.Download/               # Download pipeline, FFmpeg, subtitles, muxing
  FunkArr.RuleSet/                # Ruleset registry, models, GitHub sync, generator
  FunkArr.MatchMagic/             # Match scoring, stats, diagnostics
  FunkArr.MetadataResolver/       # TMDB + TVDB metadata resolution
  FunkArr.Messages/               # Commands, queries, responses (all domains)
  FunkArr.Persistence/            # DTOs (extend-only, versioned)
  FunkArr.UI/                     # Vue.js frontend (Vite + Tailwind)
  FunkArr.Search.Tests/
  FunkArr.Download.Tests/
  FunkArr.RuleSet.Tests/
  FunkArr.MatchMagic.Tests/
  FunkArr.MetadataResolver.Tests/
  FunkArr.Api.Tests/
  FunkArr.ArrApi.Tests/
  FunkArr.Tests.Shared/           # Shared test infrastructure
```

## Build & test

All commands run from repo root (never cd into src/):

```powershell
dotnet build src/FunkArr.slnx
dotnet format src/FunkArr.slnx --verify-no-changes
```

Tests are xUnit v3 on Microsoft.Testing.Platform - `dotnet run`, **not** `dotnet test`:

```powershell
dotnet run --project src/FunkArr.Search.Tests/FunkArr.Search.Tests.csproj
dotnet run --project src/FunkArr.Download.Tests/FunkArr.Download.Tests.csproj
```

Run the service via `docker-compose.dev.yml`, never `dotnet run`.

## Workflow

Changes go through OpenSpec: `/opsx:explore` to think - `/opsx:propose` to create
a change (proposal/design/specs/tasks) - `/opsx:apply` to implement - `/opsx:archive`.

## Skill routing

- Actors & supervision: `sepp:actor-pattern-library`, `sepp:resilience-patterns`
- Messages & pipeline design: `sepp:message-driven-designer`
- Domain modeling: `sepp:domain-modeling-patterns`
- Servus patterns: `servus-skills:servus-*` (startup, actors, etc.)

## References

- MediathekViewWeb API: https://mediathekviewweb.de/
- Newznab API spec: https://newznab.readthedocs.io/
- SABnzbd API spec: https://sabnzbd.org/wiki/advanced/api
