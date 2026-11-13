# Aktoren

Die ganze Arbeit in FunkArr passiert in Aktoren. Es gibt vier Arten, wie sie gehostet werden:

| Art | Lebensdauer | Name |
|---|---|---|
| Cluster-Singleton | eine Instanz, solange der Knoten läuft | `*Manager` |
| Shard-Region-Entity | eine Instanz pro ID, gestartet von der ersten Nachricht, passiviert bei Leerlauf | `*Worker` |
| Local Entity Region | wie eine Shard-Region, aber lokal auf dem Knoten | `*Worker` |
| Kind-Aktor oder Pool | gehört einem Elternaktor | `*Actor` |

Shard-Region- und Local-Region-Entities nutzen dasselbe Routing- und Passivierungsprotokoll, eine Entity kann also ohne Codeänderung zwischen beiden wechseln.

## Topologie

```
SINGLETONS
├─ MediathekViewWebManager                            [Search]
├─ SearchManager                                      [Search]
├─ RuleSetResolver, RuleSetManager, RuleSetUpdater    [RuleSet]
├─ DownloadManager (persistent)                       [Download]
├─ DownloadScheduler                                  [Download]
├─ DownloadHistoryManager (persistent)                [Download]
├─ ScoringManager ── ScoringActor pool                [Scoring]
├─ EnrichmentManager ── TVDB / TMDB actor pools       [Enrichment]
├─ StatsCollector                                     [History]
├─ FeedCrawlManager (persistent) ── FeedHarvestActor  [Feed]
└─ FeedStoreManager (persistent)                      [Feed]

SHARD REGIONS
├─ TvSearchWorker, MovieSearchWorker                  [Search]
├─ TvEvaluationWorker, MovieEvaluationWorker          [Search]
├─ DownloadWorker (persistent)                        [Download]
├─ RuleSetWorker                                      [RuleSet]
└─ HistoryWorker (persistent)                         [History]

LOCAL ENTITY REGIONS
└─ TestScoringWorker                                  [Scoring]
```

| Domäne | Aufgabe |
|---|---|
| Search | MediathekViewWeb abfragen, Einträge zu Releases bewerten (für Suche und Feed) |
| Feed | neue Mediathek-Einträge crawlen, den RSS-Feed speichern |
| RuleSet | Regelwerke laden, zusammenführen und auflösen, Community-Regelwerke von GitHub synchronisieren |
| Scoring | Einträge gegen Regelwerk-Regeln prüfen, Testläufe aus dem Regelwerk-Debugger ausführen |
| Enrichment | Staffel-, Episoden- und Filmdaten aus TVDB und TMDB auflösen |
| History | Bewertungsverlauf und Statistiken |
| Download | Warteschlange, Zeitplan, FFmpeg-Remux, Download-Verlauf |

Jede Domäne hat eine eigene Ansicht im Architekturmodell. Du öffnest sie über den View-Browser auf der Seite [Architektur](./architecture): Singletons, Region-Entities und Kind-Aktoren sind dort unterschiedlich dargestellt, persistente Aktoren tragen ein Datenbank-Symbol.

## Persistenz

Persistente Aktoren schreiben Events ins Akka.Persistence-Journal (standardmäßig SQLite, optional PostgreSQL) und stellen ihren Zustand nach einem Neustart wieder her. Deshalb übersteht die Download-Warteschlange einen Container-Neustart. Alle anderen Aktoren bauen ihren Zustand aus Konfiguration, Dateien oder anderen Aktoren neu auf.

## Adressierung

Aktoren werden nie als Konstruktorparameter übergeben. Sie werden zur Laufzeit aufgelöst: `Context.GetActor<T>()` in Aktoren, `IActorRegistry.GetAsync<T>()` in API-Endpunkten. Nachrichten an Region-Entities tragen ein typisiertes ID-Interface (`IWithDownloadId`, `IWithRuleSetId`, …), das sie zur richtigen Entity leitet.

Wie man einen Aktor schreibt (Zustandsklasse, `Persist`, `DeferAsync`, Snapshots, Passivierung), steht auf anvil: [Aktor-Design](https://anvil.st0o0.net/akka/actor-design/), [Zustand](https://anvil.st0o0.net/akka/state-management/), [Persistenz](https://anvil.st0o0.net/akka/persistence/), [Cluster](https://anvil.st0o0.net/akka/cluster/).
