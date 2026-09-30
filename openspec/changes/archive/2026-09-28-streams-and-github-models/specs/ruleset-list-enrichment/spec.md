## MODIFIED Requirements

### Requirement: RegisteredRuleSetEntry includes MediaName
RegisteredRuleSetEntry SHALL include an optional MediaName field alongside the existing
RuleSetId, Topic, Aliases, Ids, and MediaType fields. The stats fan-out for the list
endpoint SHALL use an Akka.Streams pipeline with the `.Ask()` operator instead of
`Task.WhenAll` with async lambdas.

#### Scenario: Stats enrichment via Akka.Streams
- **WHEN** `QueryRuleSetListWithStats` is received by RuleSetManager
- **THEN** RuleSetManager SHALL create an Akka.Streams pipeline using `Source.From`
  over the summary keys
- **THEN** it SHALL fan out `QueryScoringStats` to the history region via the `.Ask()`
  operator with parallelism 4 and `ResumingDecider`
- **THEN** on stream completion, it SHALL build the response from summaries + stats
  and reply to Sender via `PipeTo`

#### Scenario: Individual stats query failure
- **WHEN** a single HistoryWorker Ask times out during stats enrichment
- **THEN** `ResumingDecider` SHALL skip that entry
- **THEN** the corresponding ruleset SHALL appear in the result with null stats

#### Scenario: Complete stats enrichment failure
- **WHEN** the entire stats stream fails
- **THEN** RuleSetManager SHALL respond with the ruleset list without any stats
  (null LastRun, null MatchRate for all entries)

#### Scenario: Handler is synchronous with PipeTo
- **WHEN** `QueryRuleSetListWithStats` is handled
- **THEN** the handler SHALL be registered with `Receive` (not `ReceiveAsync`)
- **THEN** the stream result SHALL be delivered to `Sender` via `PipeTo`
