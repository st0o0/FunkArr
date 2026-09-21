# history-domain

## Purpose

Defines the FunkArr.History domain project: project structure, actor locations, message namespace, deletion of legacy ScoringHistoryWorker, test project, and AkkaSetupContainer registration.

## Requirements

### Requirement: History domain project exists

The system SHALL have a `FunkArr.History` project containing all history and stats aggregation actors. It SHALL reference `FunkArr.Core`, `FunkArr.Messages`, and `FunkArr.Persistence`. It SHALL NOT reference any other domain project.

#### Scenario: Project references
- **WHEN** `FunkArr.History.csproj` is examined
- **THEN** it SHALL reference `FunkArr.Core`, `FunkArr.Messages`, and `FunkArr.Persistence`
- **AND** it SHALL NOT reference `FunkArr.Scoring`, `FunkArr.Search`, `FunkArr.RuleSet`, or `FunkArr.Download`

### Requirement: HistoryWorker lives in History domain

The `HistoryWorker` sharded entity actor SHALL be defined in `FunkArr.History/HistoryWorker.cs`. Its state SHALL be defined in `FunkArr.History/HistoryState.cs`.

#### Scenario: HistoryWorker location
- **WHEN** the `HistoryWorker` class is located
- **THEN** it SHALL be in namespace `FunkArr.History` in file `FunkArr.History/HistoryWorker.cs`

#### Scenario: HistoryWorker not in Scoring
- **WHEN** the `FunkArr.Scoring` project is examined
- **THEN** it SHALL NOT contain `HistoryWorker.cs` or `HistoryState.cs`

### Requirement: StatsCollector lives in History domain

The `StatsCollector` singleton actor SHALL be defined in `FunkArr.History/StatsCollector.cs`. Its state SHALL be defined in `FunkArr.History/StatsCollectorState.cs`.

#### Scenario: StatsCollector location
- **WHEN** the `StatsCollector` class is located
- **THEN** it SHALL be in namespace `FunkArr.History` in file `FunkArr.History/StatsCollector.cs`

### Requirement: History messages in dedicated namespace

History messages SHALL live in `FunkArr.Messages/History/` under namespace `FunkArr.Messages.History`. They SHALL NOT remain under `FunkArr.Messages.Scoring.History`.

#### Scenario: Message namespace
- **WHEN** `RecordHistory`, `QueryScoringStats`, `QueryScoringHistory`, `StatsUpdated`, `QueryAllStats`, `AllStatsSnapshot`, and `RemoveStats` are located
- **THEN** they SHALL be in namespace `FunkArr.Messages.History`

### Requirement: Actor keys remain in FunkArr.Core

`IHistoryRegion` and `IStatsCollector` actor key interfaces SHALL remain in `FunkArr.Core/ActorKeys.cs`.

#### Scenario: Actor keys location
- **WHEN** `IHistoryRegion` and `IStatsCollector` are located
- **THEN** they SHALL be in `FunkArr.Core/ActorKeys.cs`

### Requirement: ScoringHistoryWorker is deleted

The legacy `ScoringHistoryWorker` and `ScoringHistoryState` SHALL be removed from the codebase, along with their test files `ScoringHistoryWorkerTests.cs` and `ScoringHistoryStateTests.cs`.

#### Scenario: No ScoringHistoryWorker in codebase
- **WHEN** the solution is searched for `ScoringHistoryWorker`
- **THEN** no results SHALL be found

### Requirement: History test project exists

The system SHALL have a `FunkArr.History.Tests` project containing tests for `HistoryWorker`, `HistoryState`, `StatsCollector`, and `StatsCollectorState`.

#### Scenario: Test project structure
- **WHEN** `FunkArr.History.Tests` is examined
- **THEN** it SHALL contain tests previously in `FunkArr.Scoring.Tests` for history and stats actors

### Requirement: AkkaSetupContainer registers History actors

The `AkkaSetupContainer` SHALL register `HistoryWorker` shard region and `StatsCollector` singleton using types from `FunkArr.History`, not `FunkArr.Scoring`.

#### Scenario: History actor registration
- **WHEN** `AkkaSetupContainer.BuildSystem` is examined
- **THEN** it SHALL use `FunkArr.History.HistoryWorker` and `FunkArr.History.StatsCollector` in the shard/singleton registrations
