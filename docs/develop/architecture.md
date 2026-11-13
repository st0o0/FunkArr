# Architektur

FunkArr ist ein einzelner .NET-10-Dienst auf [Akka.NET](https://getakka.net/). Er durchsucht die Mediatheken der öffentlich-rechtlichen Sender über [MediathekViewWeb](https://mediathekviewweb.de/), lädt Video und Untertitel herunter, remuxt sie mit FFmpeg zu MKV und bietet eine Newznab-Indexer-API und eine SABnzbd-Downloadclient-API, damit Sonarr, Radarr und Prowlarr ihn nutzen können. [So funktioniert FunkArr](/how-it-works) erklärt dasselbe aus Sicht der Nutzer.

Die Diagramme sind interaktiv: Zoomen, ein Element für Details anklicken, und über den View-Browser (oben links) jede andere Ansicht öffnen, auch eine pro Domäne.

## Kontext

<ArchitectureView view="index" height="480px" />

## Container

Im FunkArr-Container laufen die Web-Oberfläche, die beiden HTTP-Adapter, die sieben Domänen, das Persistenz-Journal und FFmpeg.

<ArchitectureView view="containers" height="620px" />

## Referenzrichtung

```
Host ─▶ Api / ArrApi ─▶ Domänen ─▶ Core ─▶ Messages / Persistence
```

- **Domänen referenzieren sich nie gegenseitig.** Search, Download, RuleSet, Scoring, History, Enrichment und Feed sprechen nur über Nachrichten aus `FunkArr.Messages` miteinander, die an zur Laufzeit aufgelöste Aktoren gehen.
- **Core bündelt das Framework.** `FunkArr.Core` referenziert Messages und Persistence und bringt die Akka- und Terp-Pakete mit. Domänen referenzieren nur Core.
- **ArrApi ist ein dünner Adapter** von den Protokollen Newznab und SABnzbd auf Domänen-Nachrichten.

`FunkArr.Architecture.Tests` prüft das mit ArchUnitNET; eine falsche Referenz lässt die CI fehlschlagen. Hintergrund: [Solution-Struktur](https://anvil.st0o0.net/getting-started/solution-structure/) und [Architekturtests](https://anvil.st0o0.net/testing/architecture-tests/) auf anvil.

## Drei Typwelten

Dasselbe Konzept gibt es meist dreimal, mit explizitem Mapping dazwischen:

| Welt | Projekt | Zweck |
|---|---|---|
| Messages | `FunkArr.Messages` | was Aktoren sich schicken; nur im Speicher |
| Persistence | `FunkArr.Persistence` | was ins Journal geschrieben wird; nur erweiterbar, ein gespeicherter Vertrag |
| API | `FunkArr.Api`, `FunkArr.ArrApi` | was Clients über HTTP sehen |

Das Mapping Messages ↔ Persistence liegt im jeweiligen Domänenprojekt, API ↔ Messages in `FunkArr.Api/Extensions/`.

## Start

`Program.cs` wendet pro Thema einen Terp-Setup-Container an (`src/FunkArr/Configuration/`): `builder.Setups().Add<T>()` vor `Build()`, `app.Setups().Add<T>()` danach. Logging und Telemetrie kommen zuerst, `ApplicationSetupContainer` zuletzt. `AkkaSetupContainer` definiert das Aktorsystem.

## Das Architekturmodell

Die Diagramme stammen aus einem [LikeC4](https://likec4.dev/)-Modell in `docs/architecture/`:

| Datei | Inhalt |
|---|---|
| `spec.c4` | Elementarten (Singleton, Shard, Local Region, Child), Tags, Beziehungsarten |
| `model.c4` | Personen, externe Systeme, Container, Aktoren, Beziehungen |
| `views.c4` | Kontext, Container, eine Ansicht pro Domäne |
| `dynamic.c4` | `search-flow`, `download-flow`, `rss-sync`, `ruleset-sync` |

- Vorschau: `pnpm --dir docs dev` aktualisiert die Diagramme beim Speichern; `pnpm --dir docs exec likec4 serve architecture` öffnet den eigenständigen Viewer.
- Prüfen: `pnpm --dir docs exec likec4 validate architecture`. Die CI führt das bei Pull Requests aus, die das Modell ändern.
- Einbetten: `<ArchitectureView view="search-flow" variant="sequence" />` (`height` ist optional).

Wenn du einen Aktor oder eine Domäne hinzufügst, entfernst oder umbenennst, pass `model.c4`, die Seite [Aktoren](./actors) und `AGENTS.md` im selben Pull Request an.
