## MODIFIED Requirements

### Requirement: RuleSetWorker registers with RuleSetResolver

After loading, the RuleSetWorker SHALL send a registration message to the RuleSetResolver containing the ruleSetId, topic, aliases, and media IDs (tvdbId, imdbId, tmdbId) extracted from the ruleset JSON.

#### Scenario: Registration with aliases and media IDs

- **WHEN** a RuleSetWorker loads a ruleset with `topic: "Tatort"`, `aliases: ["Tatort - Münster"]`, and `media: { tvdbId: 83214, imdbId: "tt0806910", tmdbId: 2116 }`
- **THEN** it sends `RegisterRuleSet("tatort", "Tatort", ["Tatort - Münster"], TvdbId: 83214, ImdbId: "tt0806910", TmdbId: 2116)` to the RuleSetResolver

#### Scenario: Registration without aliases

- **WHEN** a RuleSetWorker loads a ruleset with `topic: "Schloss Einstein"` and empty aliases and no media block
- **THEN** it sends `RegisterRuleSet("schloss-einstein", "Schloss Einstein", [], TvdbId: null, ImdbId: null, TmdbId: null)` to the RuleSetResolver

#### Scenario: Registration with partial media IDs

- **WHEN** a RuleSetWorker loads a ruleset with `media: { tvdbId: 83214 }` (no imdbId or tmdbId)
- **THEN** it sends `RegisterRuleSet` with `TvdbId: 83214, ImdbId: null, TmdbId: null`

### Requirement: RuleSetResolver resolves topic/alias to ruleSetId

The RuleSetResolver (Singleton) SHALL maintain an in-memory index of topic/alias → ruleSetId mappings and an ID index of media IDs → (ruleSetId, topic). It SHALL respond to lookup queries using topic/alias first, then falling back to ID-based resolution.

#### Scenario: Resolve by exact topic

- **WHEN** a `ResolveRuleSet("Tatort")` query is received
- **THEN** the resolver responds with `RuleSetResolved("tatort", "Tatort")`

#### Scenario: Resolve by alias

- **WHEN** a `ResolveRuleSet("Tatort - Münster")` query is received
- **THEN** the resolver responds with `RuleSetResolved("tatort", "Tatort")`

#### Scenario: Resolve unknown topic

- **WHEN** a `ResolveRuleSet("Unknown Show")` query is received with no IDs
- **THEN** the resolver responds with `RuleSetNotFound("Unknown Show")`

#### Scenario: Registration updates overwrite

- **WHEN** a RuleSetWorker re-sends RegisterRuleSet with updated aliases and media IDs
- **THEN** the resolver replaces the previous registration for that ruleSetId including all ID mappings

## ADDED Requirements

### Requirement: RuleSetMerger extracts media IDs from ruleset JSON

The `RuleSetMerger.ExtractIdentity` method SHALL return media IDs (tvdbId, imdbId, tmdbId) alongside topic and aliases. The `RawRuleSet` SHALL include a `Media` property for deserializing the JSON `media` block.

#### Scenario: Community ruleset with media block

- **WHEN** a community JSON has `"media": { "tvdbId": 83214, "imdbId": "tt0806910", "tmdbId": 2116 }`
- **THEN** `ExtractIdentity` SHALL return TvdbId=83214, ImdbId="tt0806910", TmdbId=2116

#### Scenario: Local ruleset overrides media IDs

- **WHEN** community JSON has `"media": { "tvdbId": 83214 }` and local JSON has `"media": { "tvdbId": 99999 }`
- **THEN** `ExtractIdentity` SHALL return TvdbId=99999 (local overrides community)

#### Scenario: No media block

- **WHEN** a ruleset JSON has no `media` property
- **THEN** `ExtractIdentity` SHALL return null for all media IDs

#### Scenario: Standalone local with media

- **WHEN** a local JSON has `standalone: true` and `"media": { "imdbId": "tt1234567" }`
- **THEN** `ExtractIdentity` SHALL return ImdbId="tt1234567" from the local-only ruleset
