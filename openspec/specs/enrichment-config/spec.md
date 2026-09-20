# Enrichment Config

## Purpose

Enrichment configuration model — enums, config records, JSON schema section, and merge logic in RuleSetMerger. Controls per-ruleset enrichment behavior: method ordering, thresholds, tolerances, and enable/disable.

## Requirements

### Requirement: EnrichmentMethod enum
The system SHALL define an `EnrichmentMethod` enum in `FunkArr.Messages.Enrichment` with values: Title, Airdate. The enum SHALL have `[JsonConverter(typeof(JsonStringEnumConverter<EnrichmentMethod>))]` with `[JsonStringEnumMemberName]` for camelCase JSON serialization (`"title"`, `"airdate"`).

#### Scenario: Deserialize method from JSON
- **WHEN** a ruleset JSON contains `"methods": ["title", "airdate"]`
- **THEN** it SHALL deserialize to `[EnrichmentMethod.Title, EnrichmentMethod.Airdate]`

#### Scenario: Deserialize single method
- **WHEN** a ruleset JSON contains `"methods": ["airdate"]`
- **THEN** it SHALL deserialize to `[EnrichmentMethod.Airdate]`

### Requirement: RuntimeMode enum
The system SHALL define a `RuntimeMode` enum in `FunkArr.Messages.Enrichment` with values: Tiebreaker, Filter. The enum SHALL have `[JsonConverter(typeof(JsonStringEnumConverter<RuntimeMode>))]` with `[JsonStringEnumMemberName]` for camelCase JSON serialization (`"tiebreaker"`, `"filter"`).

#### Scenario: Deserialize runtime mode from JSON
- **WHEN** a ruleset JSON contains `"runtime": { "mode": "filter" }`
- **THEN** it SHALL deserialize to `RuntimeMode.Filter`

### Requirement: EnrichmentConfig record hierarchy
The system SHALL define the following sealed records in `FunkArr.Messages.Enrichment`:

- `EnrichmentConfig(bool Enabled, EnrichmentMethod[] Methods, TitleMatchConfig Title, AirdateMatchConfig Airdate, RuntimeMatchConfig Runtime, YearMatchConfig Year)` — all fields non-nullable
- `TitleMatchConfig(float Threshold)` — Levenshtein similarity threshold (0.0–1.0)
- `AirdateMatchConfig(int Tolerance)` — days ± for date proximity matching
- `RuntimeMatchConfig(float Tolerance, RuntimeMode Mode)` — fraction of runtime for matching, and whether runtime is used as tiebreaker or pre-filter
- `YearMatchConfig(int Tolerance)` — years ± for movie release year matching

#### Scenario: All fields are non-nullable
- **WHEN** an `EnrichmentConfig` is constructed
- **THEN** all properties SHALL be non-nullable — no null checks required downstream

#### Scenario: Default config matches current hardcoded values
- **WHEN** no enrichment section is present in a ruleset JSON
- **THEN** the system SHALL produce an `EnrichmentConfig` with Enabled=true, Methods=[Title, Airdate], Title.Threshold=0.7, Airdate.Tolerance=7, Runtime.Tolerance=0.35, Runtime.Mode=Tiebreaker, Year.Tolerance=1

### Requirement: RuleSetMerger parses and merges enrichment section
`RuleSetMerger` SHALL parse an optional `"enrichment"` section from ruleset JSON. All fields within the section SHALL be optional. When merging community and local rulesets, local enrichment fields SHALL override community fields (same pattern as existing confidence/media merge). `RuleSetMerger` SHALL expose a `BuildEnrichmentConfig(RawEnrichment?)` method that applies defaults for any missing fields and returns a fully populated `EnrichmentConfig`.

#### Scenario: Ruleset without enrichment section
- **WHEN** a ruleset JSON has no `"enrichment"` key
- **THEN** `BuildEnrichmentConfig(null)` SHALL return the default `EnrichmentConfig`

#### Scenario: Partial enrichment section
- **WHEN** a ruleset JSON has `"enrichment": { "methods": ["airdate"] }`
- **THEN** `BuildEnrichmentConfig` SHALL return an `EnrichmentConfig` with Methods=[Airdate] and all other fields at their defaults

#### Scenario: Local overrides community enrichment
- **WHEN** community ruleset has `"enrichment": { "title": { "threshold": 0.7 } }` and local has `"enrichment": { "title": { "threshold": 0.5 } }`
- **THEN** the merged enrichment SHALL have Title.Threshold=0.5

#### Scenario: Disabled enrichment
- **WHEN** a ruleset JSON has `"enrichment": { "enabled": false }`
- **THEN** `BuildEnrichmentConfig` SHALL return an `EnrichmentConfig` with Enabled=false

### Requirement: ExtractIdentity returns EnrichmentConfig
`RuleSetMerger.ExtractIdentity` SHALL include `EnrichmentConfig` in its return value alongside Topic, Aliases, TvdbId, ImdbId, TmdbId, MediaName, and MediaType.

#### Scenario: Identity includes enrichment config
- **WHEN** `ExtractIdentity` is called on a ruleset with an enrichment section
- **THEN** the returned tuple SHALL include the parsed `EnrichmentConfig`

#### Scenario: Identity without enrichment section
- **WHEN** `ExtractIdentity` is called on a ruleset without an enrichment section
- **THEN** the returned `EnrichmentConfig` SHALL be the default config
