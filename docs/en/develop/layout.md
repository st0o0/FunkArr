# Repository layout

```
src/                      .NET solution (FunkArr.slnx) and the web UI
data/community/           community rulesets (JSON) and their schema
docs/                     this site (VitePress) and the architecture model
openspec/                 specs and change proposals
.github/                  workflows, issue forms, PR template
.claude/                  agent skills and commands
docker-compose.dev.yml    dev stack with Sonarr, Radarr and Prowlarr
Dockerfile                production image (UI build, .NET publish, FFmpeg)
AGENTS.md, CLAUDE.md      instructions for AI agents
```

## Projects in `src/`

| Project | Content |
|---|---|
| `FunkArr` | host: `Program.cs`, setup containers, configuration |
| `FunkArr.Api` | internal REST API for the web UI (`/api`) |
| `FunkArr.ArrApi` | Newznab (`/index/api`) and SABnzbd (`/download/api`) adapter |
| `FunkArr.Search` | MediathekViewWeb queries, release evaluation |
| `FunkArr.Download` | download queue, FFmpeg, subtitles, muxing |
| `FunkArr.RuleSet` | ruleset registry, GitHub sync |
| `FunkArr.Scoring` | match scoring, ruleset test runs |
| `FunkArr.History` | scoring history and statistics |
| `FunkArr.Enrichment` | TMDB and TVDB metadata |
| `FunkArr.Feed` | RSS crawl and feed store |
| `FunkArr.Core` | shared types, Akka and Terp references |
| `FunkArr.Messages` | commands, queries, responses for all domains |
| `FunkArr.Persistence` | persisted events and state records |
| `FunkArr.UI` | Vue web UI, see [Web UI](./web-ui) |

## Tests

| Project | Covers |
|---|---|
| `FunkArr.<Domain>.Tests` | actors and state of one domain |
| `FunkArr.Api.Tests`, `FunkArr.ArrApi.Tests` | the HTTP surfaces |
| `FunkArr.Persistence.Tests` | serialized shape of persisted records (snapshots) |
| `FunkArr.IntegrationTests` | the host in process: HTTP to real actors |
| `FunkArr.Architecture.Tests` | reference rules (ArchUnitNET) |
| `FunkArr.Tests.Shared` | shared helpers such as `TestDataFiles` and `TestOptionsMonitor<T>` |

Shared build settings are in `src/Directory.Build.props` (warnings as errors), package versions in `src/Directory.Packages.props`, and the SDK version in `src/global.json`.
