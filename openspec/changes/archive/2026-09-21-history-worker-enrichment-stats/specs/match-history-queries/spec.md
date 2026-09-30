## MODIFIED Requirements

### Requirement: Stats queries go through StatsCollector
The list endpoint SHALL query StatsCollector with a single QueryAllStats ask instead of N individual QueryScoringStats asks to the history region.

#### Scenario: List endpoint gets all stats in one ask
- **WHEN** GET /api/rulesets/ builds the list response
- **THEN** it asks StatsCollector once with QueryAllStats
- **THEN** it receives AllStatsResult with stats for all rulesets
- **THEN** it maps stats into the list entries

#### Scenario: Missing stats for a ruleset
- **WHEN** StatsCollector has no entry for a ruleSetId
- **THEN** the list entry shows null lastRun and null matchRate (same as current behavior)

### Requirement: History detail queries remain direct
Individual history queries (QueryHistory, QueryDetail) SHALL continue to go directly to the HistoryRegion, as these are targeted at a specific ruleset and don't cause fan-out.

#### Scenario: History detail query
- **WHEN** GET /api/rulesets/{id}/history/{requestId} is called
- **THEN** it asks HistoryRegion directly for that ruleSetId
- **THEN** the response includes ItemTraces with EnrichmentTrace data
