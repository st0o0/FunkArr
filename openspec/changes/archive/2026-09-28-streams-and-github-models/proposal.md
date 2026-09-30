## Why

Three fan-out-Ask patterns exist across the codebase with inconsistent approaches:
DownloadManager uses Akka.Streams `.Ask()` (bounded parallelism, clean completion),
while StatsCollector uses foreach+Ask+PipeTo (unbounded, no completion signal) and
RuleSetManager uses Task.WhenAll with async lambdas (unbounded, blocks the actor mailbox).
Meanwhile, RuleSetUpdater manually walks `JsonElement` for the GitHub releases API
instead of using typed models like every other external API client in the project.

## What Changes

- Add typed GitHub API models (`GitHubRelease`, `GitHubReleaseAsset`) and rewrite
  `FindRelease()` with `ReadFromJsonAsync` + LINQ instead of `JsonElement` traversal
- Fix raw `Directory.Exists` call in RuleSetUpdater to use `IDataFiles` abstraction
- Add `ScoringStatsQueryResult` wrapper so the history region's query response carries
  its `RuleSetId` (matching `WorkerStatusResult` which carries `DownloadId`)
- Rewrite `StatsCollector.HandleBackfillRuleSets` with Akka.Streams `.Ask()` operator
  (bounded parallelism, bulk completion, `ResumingDecider` for per-element errors)
- Rewrite `RuleSetManager.HandleQueryListWithStats` with the same Akka.Streams pattern
  (replaces `Task.WhenAll` + `ReceiveAsync` with `Receive` + stream + `PipeTo`)
- Convert `RuleSetManager` debounce timer from manual `ICancelable` + `Scheduler` to
  `IWithTimers` (matches RuleSetUpdater, removes manual cancel in PostStop)

## Capabilities

### New Capabilities

- `github-api-models`: Typed deserialization models for GitHub releases API, internal to RuleSet domain

### Modified Capabilities

- `stats-collector`: Backfill mechanism changes from individual PipeTo to Akka.Streams bulk completion
- `ruleset-updater`: FindRelease rewritten with typed models, Directory.Exists fix
- `ruleset-list-enrichment`: Stats fan-out changes from Task.WhenAll to Akka.Streams
- `ruleset-management`: RuleSetManager debounce timer converted to IWithTimers

## Impact

- **FunkArr.Messages**: New `ScoringStatsQueryResult` record in History namespace
- **FunkArr.History**: HistoryWorker response type change, StatsCollector rewrite
- **FunkArr.RuleSet**: RuleSetUpdater FindRelease rewrite, RuleSetManager streams rewrite,
  new GitHubModels.cs
- **Tests**: StatsCollector, HistoryWorker, and RuleSetManager tests update for new
  response type and stream-based flow
- No API contract changes, no persistence changes, no breaking changes
