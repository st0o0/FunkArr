## MODIFIED Requirements

### Requirement: Detail endpoint returns enrichment config
The GET /api/rulesets/{id} endpoint SHALL include enrichment config in the RuleSetDetail response.

#### Scenario: Detail with enrichment config
- **WHEN** GET /api/rulesets/{id} is called
- **THEN** the response includes an enrichment object with the ruleset's resolved enrichment config (merged from community + local, or defaults)

### Requirement: Create endpoint accepts enrichment config
The POST /api/rulesets/ endpoint SHALL accept an optional enrichment object in CreateRuleSetRequest and serialize it to disk.

#### Scenario: Create with enrichment
- **WHEN** POST /api/rulesets/ includes enrichment config
- **THEN** the enrichment object is serialized to the JSON file

### Requirement: Update endpoint accepts enrichment config
The PUT /api/rulesets/{id} endpoint SHALL accept an optional enrichment object in UpdateRuleSetRequest and serialize it to disk.

#### Scenario: Update with enrichment
- **WHEN** PUT /api/rulesets/{id} includes enrichment config
- **THEN** the enrichment object is serialized to the JSON file

### Requirement: Test endpoint accepts enrichment config and identity
The POST /api/rulesets/test endpoint SHALL accept optional enrichment config and identity fields (tvdbId, tmdbId, imdbId, mediaType) alongside existing scoring config.

#### Scenario: Test with enrichment
- **WHEN** POST /api/rulesets/test includes enrichment config and tvdbId
- **THEN** enrichment runs after scoring and results include EnrichmentTrace

#### Scenario: Test without enrichment
- **WHEN** POST /api/rulesets/test omits enrichment config
- **THEN** behavior is unchanged from current (scoring only)
