# Repository-Aufbau

```
src/                      .NET-Solution (FunkArr.slnx) und die Web-Oberfläche
data/community/           Community-Regelwerke (JSON) und ihr Schema
docs/                     diese Seite (VitePress) und das Architekturmodell
openspec/                 Specs und Änderungsvorschläge
.github/                  Workflows, Issue-Formulare, PR-Vorlage
.claude/                  Agent-Skills und -Commands
docker-compose.dev.yml    Dev-Stack mit Sonarr, Radarr und Prowlarr
Dockerfile                Produktiv-Image (UI-Build, .NET-Publish, FFmpeg)
AGENTS.md, CLAUDE.md      Anweisungen für KI-Agenten
```

## Projekte in `src/`

| Projekt | Inhalt |
|---|---|
| `FunkArr` | Host: `Program.cs`, Setup-Container, Konfiguration |
| `FunkArr.Api` | interne REST-API für die Web-Oberfläche (`/api`) |
| `FunkArr.ArrApi` | Adapter für Newznab (`/index/api`) und SABnzbd (`/download/api`) |
| `FunkArr.Search` | MediathekViewWeb-Abfragen, Release-Bewertung |
| `FunkArr.Download` | Download-Warteschlange, FFmpeg, Untertitel, Muxing |
| `FunkArr.RuleSet` | Regelwerk-Registry, GitHub-Sync |
| `FunkArr.Scoring` | Match-Bewertung, Regelwerk-Testläufe |
| `FunkArr.History` | Bewertungsverlauf und Statistiken |
| `FunkArr.Enrichment` | TMDB- und TVDB-Metadaten |
| `FunkArr.Feed` | RSS-Crawl und Feed-Speicher |
| `FunkArr.Core` | gemeinsame Typen, Akka- und Terp-Referenzen |
| `FunkArr.Messages` | Commands, Queries, Antworten aller Domänen |
| `FunkArr.Persistence` | persistierte Events und Zustandsdatensätze |
| `FunkArr.UI` | Vue-Oberfläche, siehe [Web-Oberfläche](./web-ui) |

## Tests

| Projekt | Deckt ab |
|---|---|
| `FunkArr.<Domain>.Tests` | Aktoren und Zustand einer Domäne |
| `FunkArr.Api.Tests`, `FunkArr.ArrApi.Tests` | die HTTP-Schnittstellen |
| `FunkArr.Persistence.Tests` | serialisierte Form persistierter Datensätze (Snapshots) |
| `FunkArr.IntegrationTests` | der Host im Prozess: HTTP bis zu echten Aktoren |
| `FunkArr.Architecture.Tests` | Referenzregeln (ArchUnitNET) |
| `FunkArr.Tests.Shared` | gemeinsame Helfer wie `TestDataFiles` und `TestOptionsMonitor<T>` |

Gemeinsame Build-Einstellungen stehen in `src/Directory.Build.props` (Warnungen als Fehler), Paketversionen in `src/Directory.Packages.props` und die SDK-Version in `src/global.json`.
