# Architecture

FunkArr is one .NET 10 service built on [Akka.NET](https://getakka.net/). It searches the public broadcaster Mediatheken through [MediathekViewWeb](https://mediathekviewweb.de/), downloads video and subtitles, remuxes them to MKV with FFmpeg, and offers a Newznab indexer API and a SABnzbd download client API so that Sonarr, Radarr and Prowlarr can use it. [How FunkArr works](/en/how-it-works) explains the same from a user's point of view.

The diagrams are interactive: zoom, click an element for details, and use the view browser (top left) to open any other view, including one view per domain.

## Context

<ArchitectureView view="index" height="480px" />

## Containers

Inside the FunkArr container are the web UI, the two HTTP adapters, the seven domains, the persistence journal and FFmpeg.

<ArchitectureView view="containers" height="620px" />

## Reference direction

```
Host ─▶ Api / ArrApi ─▶ domains ─▶ Core ─▶ Messages / Persistence
```

- **Domains never reference each other.** Search, Download, RuleSet, Scoring, History, Enrichment and Feed only talk through messages from `FunkArr.Messages`, sent to actors that are resolved at runtime.
- **Core bundles the framework.** `FunkArr.Core` references Messages and Persistence and declares the Akka and Terp packages. Domains reference only Core.
- **ArrApi is a thin adapter** from the Newznab and SABnzbd protocols to domain messages.

`FunkArr.Architecture.Tests` enforces this with ArchUnitNET, so breaking the direction fails CI. Background: [solution structure](https://anvil.st0o0.net/getting-started/solution-structure/) and [architecture tests](https://anvil.st0o0.net/testing/architecture-tests/) on anvil.

## Three type worlds

The same concept usually exists three times, with explicit mapping between them:

| World | Project | Purpose |
|---|---|---|
| Messages | `FunkArr.Messages` | what actors send each other; in memory only |
| Persistence | `FunkArr.Persistence` | what goes into the journal; extend-only, a stored contract |
| API | `FunkArr.Api`, `FunkArr.ArrApi` | what clients see over HTTP |

Messages ↔ Persistence mapping lives in each domain project, API ↔ Messages mapping in `FunkArr.Api/Extensions/`.

## Startup

`Program.cs` applies one Terp setup container per concern (`src/FunkArr/Configuration/`): `builder.Setups().Add<T>()` before `Build()`, `app.Setups().Add<T>()` after. Logging and telemetry come first, `ApplicationSetupContainer` last. `AkkaSetupContainer` defines the actor system.

## The architecture model

The diagrams come from a [LikeC4](https://likec4.dev/) model in `docs/architecture/`:

| File | Content |
|---|---|
| `spec.c4` | element kinds (singleton, shard, local region, child), tags, relationship kinds |
| `model.c4` | people, external systems, containers, actors, relationships |
| `views.c4` | context, containers, one view per domain |
| `dynamic.c4` | `search-flow`, `download-flow`, `rss-sync`, `ruleset-sync` |

- Preview: `pnpm --dir docs dev` updates the diagrams as you save; `pnpm --dir docs exec likec4 serve architecture` opens the standalone viewer.
- Validate: `pnpm --dir docs exec likec4 validate architecture`. CI runs this on pull requests that touch the model.
- Embed: `<ArchitectureView view="search-flow" variant="sequence" />` (`height` is optional).

When you add, remove or rename an actor or a domain, update `model.c4`, the [Actors](./actors) page and `AGENTS.md` in the same pull request.
