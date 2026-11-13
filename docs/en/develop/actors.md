# Actors

All work in FunkArr happens in actors. They come in four hosting shapes:

| Shape | Lifetime | Name |
|---|---|---|
| Cluster singleton | one instance for the node's lifetime | `*Manager` |
| Shard region entity | one instance per ID, started by the first message, passivated when idle | `*Worker` |
| Local entity region | like a shard region, but local to the node | `*Worker` |
| Child actor or pool | owned by a parent | `*Actor` |

Shard region and local region entities use the same routing and passivation protocol, so an entity can move between them without code changes.

## Topology

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

| Domain | Responsibility |
|---|---|
| Search | query MediathekViewWeb, evaluate entries into releases (for search and feed) |
| Feed | crawl new Mediathek entries, store the RSS feed |
| RuleSet | load, merge and resolve rulesets, sync community rulesets from GitHub |
| Scoring | match entries against ruleset rules, run tests from the ruleset debugger |
| Enrichment | resolve season, episode and movie data from TVDB and TMDB |
| History | scoring history and statistics |
| Download | queue, schedule, FFmpeg remux, download history |

Each domain has its own view in the architecture model. Open it with the view browser on the [Architecture](./architecture) page: singletons, region entities and children are drawn differently there, and persistent actors carry a database icon.

## Persistence

Persistent actors write events to the Akka.Persistence journal (SQLite by default, PostgreSQL optional) and recover on restart. That is why the download queue survives a container restart. All other actors rebuild their state from configuration, files or other actors.

## Addressing

Actors are never passed in as constructor arguments. They are resolved at runtime: `Context.GetActor<T>()` inside actors, `IActorRegistry.GetAsync<T>()` in API endpoints. Messages for region entities carry a typed ID interface (`IWithDownloadId`, `IWithRuleSetId`, …) that routes them to the right entity.

How to write an actor (state class, `Persist`, `DeferAsync`, snapshots, passivation) is on anvil: [actor design](https://anvil.st0o0.net/akka/actor-design/), [state management](https://anvil.st0o0.net/akka/state-management/), [persistence](https://anvil.st0o0.net/akka/persistence/), [cluster](https://anvil.st0o0.net/akka/cluster/).
