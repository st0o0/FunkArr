## ADDED Requirements

### Requirement: API-layer enrichment config models
The API layer SHALL expose enrichment config through request and response models that mirror the JSON schema structure: enabled (bool), methods (EnrichmentMethod[]), title (threshold float), airdate (tolerance int), runtime (tolerance float, mode RuntimeMode), year (tolerance int).

#### Scenario: Enrichment config in detail response
- **WHEN** GET /api/rulesets/{id} is called for a ruleset with enrichment config
- **THEN** the response includes an enrichment object with all config fields

#### Scenario: Enrichment config defaults in detail response
- **WHEN** GET /api/rulesets/{id} is called for a ruleset without enrichment config in JSON
- **THEN** the response includes an enrichment object with default values (enabled=true, methods=[Title,Airdate], title.threshold=0.7, airdate.tolerance=7, runtime.tolerance=0.35, runtime.mode=Tiebreaker, year.tolerance=1)

### Requirement: Enrichment config in create/update requests
The create and update API endpoints SHALL accept an optional enrichment object in the request body and serialize it to the ruleset JSON file on disk.

#### Scenario: Create ruleset with enrichment
- **WHEN** POST /api/rulesets/ includes enrichment config
- **THEN** the saved JSON file includes the enrichment object

#### Scenario: Update ruleset with enrichment
- **WHEN** PUT /api/rulesets/{id} includes enrichment config
- **THEN** the saved JSON file includes the updated enrichment object
