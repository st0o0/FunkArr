## MODIFIED Requirements

### Requirement: RegisterRuleSet carries EnrichmentConfig
The `RegisterRuleSet` message SHALL include an optional `EnrichmentConfig? Enrichment` parameter (default null). `RuleSetWorker` SHALL extract the `EnrichmentConfig` from `RuleSetMerger.ExtractIdentity` and pass it to `RegisterRuleSet`.

#### Scenario: RegisterRuleSet with enrichment config
- **WHEN** a ruleset JSON has an `"enrichment"` section
- **THEN** `RegisterRuleSet` SHALL carry the parsed `EnrichmentConfig`

#### Scenario: RegisterRuleSet without enrichment config
- **WHEN** a ruleset JSON has no `"enrichment"` section
- **THEN** `RegisterRuleSet` SHALL carry the default `EnrichmentConfig` (Enabled=true, Methods=[Title, Airdate], etc.)

### Requirement: RuleSetResolverState stores EnrichmentConfig per ruleSetId
`RuleSetResolverState` SHALL maintain an `ImmutableDictionary<string, EnrichmentConfig>` keyed by ruleSetId. When a `RegisterRuleSet` is applied, the enrichment config SHALL be stored. When a `DeregisterRuleSet` is applied, it SHALL be removed.

#### Scenario: Store enrichment config on register
- **WHEN** `Apply(RegisterRuleSet)` is called with an `EnrichmentConfig`
- **THEN** the config SHALL be retrievable by ruleSetId

#### Scenario: Remove enrichment config on deregister
- **WHEN** `Apply(DeregisterRuleSet)` is called
- **THEN** the enrichment config for that ruleSetId SHALL be removed

### Requirement: RuleSetResolved includes EnrichmentConfig
The `RuleSetResolved` response SHALL include an `EnrichmentConfig? Enrichment` field. When the resolver resolves a ruleset, it SHALL include the stored `EnrichmentConfig` in the response.

#### Scenario: Resolve returns enrichment config
- **WHEN** a `ResolveRuleSet` is resolved to a ruleSetId that has an enrichment config
- **THEN** the `RuleSetResolved` response SHALL include the stored `EnrichmentConfig`

#### Scenario: Resolve without enrichment config
- **WHEN** a `ResolveRuleSet` is resolved to a ruleSetId with no enrichment config stored
- **THEN** the `RuleSetResolved.Enrichment` SHALL be null
