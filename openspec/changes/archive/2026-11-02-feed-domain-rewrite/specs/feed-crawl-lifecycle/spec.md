## MODIFIED Requirements

### Requirement: FeedCrawler is a non-persistent child actor
FeedCrawlManager SHALL spawn a `FeedCrawlWorker` child actor for each crawl run. FeedCrawlWorker SHALL be a plain `ReceiveActor` (non-persistent) that executes exactly one crawl through six Become phases (Probing, LoadingRuleSets, Harvesting, Evaluating, Publishing, Complete) and then stops. FeedCrawlManager SHALL NOT contain crawl execution logic — only cursor persistence, spawning FeedCrawlWorker, and handling the result. Timer scheduling SHALL be handled by the separate FeedScheduler actor.

#### Scenario: Manager spawns worker on StartFeedCrawl
- **WHEN** FeedCrawlManager receives StartFeedCrawl and no crawl is running
- **THEN** it SHALL spawn a FeedCrawlWorker child actor with a CrawlInput snapshot, set `_running = true`, and `Context.Watch` the child

#### Scenario: Manager does not spawn concurrent crawlers
- **WHEN** FeedCrawlManager receives StartFeedCrawl while a crawl is already running
- **THEN** it SHALL NOT spawn a second FeedCrawlWorker

#### Scenario: Worker stops after reporting result
- **WHEN** FeedCrawlWorker has sent a CrawlResult to its parent
- **THEN** it SHALL call `Context.Stop(Self)` to terminate

### Requirement: Become-based probe phase
FeedCrawlWorker SHALL use `Become` to separate phases. The Probing phase SHALL resolve the list timestamp via Ask to MvwManager with PipeTo. On success it SHALL Become(LoadingRuleSets). On unchanged list it SHALL send CrawlResult.ForUnchanged and stop. On failure it SHALL send CrawlResult.ForFailed and stop.

#### Scenario: Probe asks MediathekManager for list timestamp
- **WHEN** FeedCrawlWorker starts in the Probing phase
- **THEN** it SHALL Ask MediathekManager for the current list timestamp using PipeTo with a failure handler

#### Scenario: Probe fetches registered rulesets
- **WHEN** the list timestamp probe succeeds and the timestamp is newer than the cursor
- **THEN** FeedCrawlWorker SHALL Become(LoadingRuleSets) and Ask RuleSetResolver for registered rulesets

#### Scenario: Probe detects unchanged list
- **WHEN** the probe response shows listTimestamp <= cursor
- **THEN** FeedCrawlWorker SHALL send `CrawlResult.ForUnchanged` to its parent and stop

#### Scenario: Probe failure
- **WHEN** the probe Ask times out or returns an error response
- **THEN** FeedCrawlWorker SHALL send `CrawlResult.ForFailed` to its parent and stop

### Requirement: Crawling phase replaced by Harvesting, Evaluating, and Publishing phases
After LoadingRuleSets completes, FeedCrawlWorker SHALL NOT materialize an Akka.Streams pipeline. Instead it SHALL Become(Harvesting) and use actor pool messaging for page fetching, then Become(Evaluating) for evaluation dispatch via scatter-gather, then Become(Publishing) for batch publishing to FeedStoreManager.

#### Scenario: No stream materialization
- **WHEN** inspecting FeedCrawlWorker
- **THEN** it SHALL NOT reference Source, Flow, Sink, IMaterializer, or any Akka.Streams types

#### Scenario: Phase progression after rulesets loaded
- **WHEN** rulesets are loaded successfully
- **THEN** the worker SHALL Become(Harvesting), not Become(Crawling)

### Requirement: FeedCrawlManager watches crawler lifecycle
FeedCrawlManager SHALL use DeathWatch (`Context.Watch`) on the spawned FeedCrawlWorker to detect unexpected termination.

#### Scenario: Worker terminates without sending result
- **WHEN** FeedCrawlManager receives a `Terminated` message for the worker without having received a CrawlResult
- **THEN** it SHALL treat this as a failed crawl, set `_running = false`

#### Scenario: Normal termination after result
- **WHEN** FeedCrawlManager receives a CrawlResult followed by Terminated
- **THEN** it SHALL process the CrawlResult normally and ignore the subsequent Terminated message

### Requirement: FeedCrawlManager persistence behavior unchanged
FeedCrawlManager SHALL continue to persist FeedCursorAdvanced and FeedRunFailed events, manage the cursor, handle consecutive failure logic, and snapshot exactly as it does today.

#### Scenario: Completed crawl persists cursor
- **WHEN** FeedCrawlManager receives a CrawlResult with status Completed
- **THEN** it SHALL persist FeedCursorAdvanced with the result's fields and update state

#### Scenario: Status query still answered by manager
- **WHEN** FeedCrawlManager receives QueryFeedCrawlStatus
- **THEN** it SHALL respond with the current state, including whether a worker is running

### Requirement: FeedCrawlManager has no timer logic
Timer scheduling SHALL be handled exclusively by FeedScheduler. FeedCrawlManager SHALL NOT implement `IWithTimers`, SHALL NOT schedule timers, and SHALL NOT contain tick-handling logic.

#### Scenario: Manager does not schedule timers
- **WHEN** inspecting FeedCrawlManager
- **THEN** it SHALL NOT reference `IWithTimers`, `Timers.StartSingleTimer`, or any timer-related APIs
