## ADDED Requirements

### Requirement: HistoryState Apply and ProcessCommand tests
`HistoryState` Apply and ProcessCommand methods SHALL have tests covering single events, multiple events, and command-to-event mapping.

#### Scenario: Apply single event adds entry
- **WHEN** `Apply` is called with one `HistoryRecorded` event
- **THEN** state contains one entry with matching fields

#### Scenario: Apply multiple events accumulates
- **WHEN** `Apply` is called with multiple events
- **THEN** state contains all entries

#### Scenario: ProcessCommand returns event matching command
- **WHEN** `ProcessCommand` is called with a `RecordHistory` command
- **THEN** it returns a new state and an event with fields matching the command

### Requirement: HistoryState ComputeStats tests
`ComputeStats` SHALL be tested for empty state, single entry, multiple entries, zero-candidate entries, and correct rate calculations.

#### Scenario: Empty list returns null rates
- **WHEN** `ComputeStats` is called on empty state
- **THEN** rates are null, total runs is 0

#### Scenario: Rates calculated correctly
- **WHEN** `ComputeStats` is called with entries having known candidate/match/enriched counts
- **THEN** match rate and enrichment rate are correctly averaged

#### Scenario: Zero candidates excluded from rate
- **WHEN** an entry has `CandidateCount = 0`
- **THEN** it is excluded from match rate calculation

### Requirement: HistoryState Trim tests
`Trim` SHALL be tested for under-limit (no-op), exceeds max count, exceeds max age, both limits, all expired, and empty state.

#### Scenario: Under limits returns same state
- **WHEN** state is within both count and age limits
- **THEN** `Trim` returns the state unchanged

#### Scenario: Exceeds max count keeps newest
- **WHEN** state exceeds the max snapshot count
- **THEN** `Trim` keeps only the newest entries up to the limit

#### Scenario: Old entries removed by age
- **WHEN** entries are older than the max retention period
- **THEN** `Trim` removes them

### Requirement: HistoryState Query tests
`QueryHistory` and `QueryDetail` SHALL be tested for pagination, ordering, empty results, and unknown request IDs.

#### Scenario: QueryHistory returns newest first with pagination
- **WHEN** `QueryHistory` is called with offset and limit
- **THEN** results are ordered newest first, respecting pagination parameters

#### Scenario: QueryDetail returns matching entry
- **WHEN** `QueryDetail` is called with an existing request ID
- **THEN** it returns the full detail result

#### Scenario: QueryDetail unknown ID returns failed
- **WHEN** `QueryDetail` is called with a non-existent request ID
- **THEN** it returns a failed response

### Requirement: StatsCollectorState tests
`StatsCollectorState` SHALL have tests for Apply, GetSnapshot/FromSnapshot roundtrip, and empty state.

#### Scenario: Apply UpdateStats adds entry
- **WHEN** `Apply(UpdateStats)` is called
- **THEN** state contains the stats entry for that ruleset

#### Scenario: Apply RemoveStats removes entry
- **WHEN** `Apply(RemoveStats)` is called for an existing ruleset
- **THEN** the entry is removed

#### Scenario: Snapshot roundtrip preserves entries
- **WHEN** `GetSnapshot()` and `FromSnapshot()` are called in sequence
- **THEN** all entries are preserved

### Requirement: HistoryWorker actor tests
`HistoryWorker` SHALL have actor TestKit tests for command handling, query handling, and snapshot saving.

#### Scenario: RecordHistory persists and notifies StatsCollector
- **WHEN** `HistoryWorker` receives `RecordHistory`
- **THEN** it persists the event and sends stats update to `StatsCollector`

#### Scenario: Query handlers return correct responses
- **WHEN** `HistoryWorker` receives `QueryScoringHistory`, `QueryScoringDetail`, or `QueryScoringStats`
- **THEN** it responds with the correct result from state

### Requirement: StatsCollector actor tests
`StatsCollector` SHALL have actor TestKit tests for stats aggregation, removal, and query handling.

#### Scenario: UpdateStats updates and QueryAllStats returns
- **WHEN** `StatsCollector` receives `UpdateStats` followed by `QueryAllStats`
- **THEN** it returns results including the updated stats

#### Scenario: RemoveStats removes entry
- **WHEN** `StatsCollector` receives `RemoveStats`
- **THEN** the entry is removed from subsequent queries
