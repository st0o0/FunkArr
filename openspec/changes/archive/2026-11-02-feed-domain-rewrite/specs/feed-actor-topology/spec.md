## ADDED Requirements

### Requirement: FeedScheduler is a timer-only singleton
FeedScheduler SHALL be a non-persistent Cluster Singleton implementing `IWithTimers`. It SHALL be responsible only for timer management — starting, stopping, and firing crawl triggers. It SHALL resolve FeedCrawlManager via `Context.GetActor<IFeedCrawlManager>()` and send `StartFeedCrawl` on each tick. FeedScheduler SHALL read interval configuration from `IOptionsMonitor<FeedOptions>`.

#### Scenario: Timer fires and triggers crawl
- **WHEN** the configured crawl interval elapses
- **THEN** FeedScheduler SHALL send `StartFeedCrawl` to FeedCrawlManager

#### Scenario: Timer respects options changes
- **WHEN** FeedOptions.CrawlInterval changes at runtime
- **THEN** FeedScheduler SHALL reschedule its timer with the new interval

#### Scenario: Scheduler has no persistence
- **WHEN** FeedScheduler restarts
- **THEN** it SHALL start a fresh timer from the configured interval without recovering any prior state

### Requirement: FeedCrawlManager is a thin persistent orchestrator
FeedCrawlManager SHALL be a persistent Cluster Singleton responsible only for: cursor persistence (FeedCursorAdvanced, FeedRunFailed events), spawning and watching FeedCrawlWorker, handling CrawlResult, and answering status queries. It SHALL NOT contain timer logic, page fetching, evaluation dispatch, gating, or publishing.

#### Scenario: Manager spawns worker on StartFeedCrawl
- **WHEN** FeedCrawlManager receives StartFeedCrawl and no crawl is running
- **THEN** it SHALL spawn a FeedCrawlWorker child with a CrawlInput snapshot, set `_running = true`, and `Context.Watch` the child

#### Scenario: Manager rejects concurrent crawls
- **WHEN** FeedCrawlManager receives StartFeedCrawl while a crawl is running
- **THEN** it SHALL NOT spawn a second worker

#### Scenario: Manager handles CrawlResult
- **WHEN** FeedCrawlManager receives a CrawlResult from the worker
- **THEN** it SHALL persist the appropriate event (FeedCursorAdvanced or FeedRunFailed), update state, set `_running = false`, and set `_resultReceived = true`

#### Scenario: Manager detects unexpected worker death
- **WHEN** FeedCrawlManager receives Terminated for the worker without having received a CrawlResult
- **THEN** it SHALL treat this as a failed crawl and set `_running = false`

### Requirement: FeedCrawlWorker is a Become-phase child actor
FeedCrawlWorker SHALL be a non-persistent `ReceiveActor` child of FeedCrawlManager that executes exactly one crawl through six Become phases: Probing, LoadingRuleSets, Harvesting, Evaluating, Publishing, and Complete. It SHALL stop after sending CrawlResult to `Context.Parent`.

#### Scenario: Worker progresses through all phases on success
- **WHEN** a crawl encounters no errors
- **THEN** the worker SHALL transition Probing → LoadingRuleSets → Harvesting → Evaluating → Publishing → Complete, in that order

#### Scenario: Worker stops after any terminal phase
- **WHEN** the worker sends CrawlResult to its parent (from any phase)
- **THEN** it SHALL call `Context.Stop(Self)`

#### Scenario: Each phase handles only its own message types
- **WHEN** the worker is in the Harvesting phase
- **THEN** it SHALL only process PageResult messages, not probe or evaluation messages

### Requirement: Worker creates child pools
FeedCrawlWorker SHALL create two `SmallestMailboxPool` router pools as child actors in its constructor: a PageFetchPool of FeedPageActor instances (pool size from FeedOptions.PagePrefetch, default 3) and an EvalPool of FeedEvalActor instances (pool size from FeedOptions.MaxConcurrency).

#### Scenario: Pools are children of the worker
- **WHEN** the worker stops
- **THEN** both pools and their routees SHALL be stopped automatically by the actor hierarchy

#### Scenario: Pool sizes come from options
- **WHEN** FeedOptions.PagePrefetch is 3 and MaxConcurrency is 5
- **THEN** the PageFetchPool SHALL have 3 routees and the EvalPool SHALL have 5 routees

### Requirement: All actors use registry resolution
Every actor in the Feed crawl pipeline SHALL resolve dependencies via `Context.GetActor<T>()` or `Context.Parent`. No actor SHALL accept `IActorRef` as a constructor parameter. DI services (TimeProvider, IOptionsMonitor) SHALL be injected via `resolver.Props<T>()`.

#### Scenario: FeedCrawlWorker resolves MvwManager
- **WHEN** FeedCrawlWorker needs to probe MvwManager
- **THEN** it SHALL use `Context.GetActor<IMediathekManager>()`, not a constructor-injected IActorRef

#### Scenario: FeedPageActor resolves MvwManager
- **WHEN** FeedPageActor needs to fetch a page
- **THEN** it SHALL use `Context.GetActor<IMediathekManager>()`

### Requirement: All state records follow Pathfinder pattern
Every state record in the Feed crawl pipeline SHALL follow the Pathfinder pattern: a separate `<Name>State.cs` file with a `public static readonly Empty` field, a separate `<Name>StateExtensions` static class with all Apply/mutation methods as extension methods. State records SHALL hold only data, no methods.

#### Scenario: FeedCrawlWorkerState has Empty and extensions
- **WHEN** inspecting FeedCrawlWorkerState
- **THEN** it SHALL have a `public static readonly FeedCrawlWorkerState Empty` field and a `FeedCrawlWorkerStateExtensions` class with extension methods for phase transitions
