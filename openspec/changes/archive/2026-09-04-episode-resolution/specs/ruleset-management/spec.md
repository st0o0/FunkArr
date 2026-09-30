## ADDED Requirements

### Requirement: RuleSetMerger parses resolution config from JSON
The RuleSetMerger SHALL parse an optional `"resolution"` block from RuleSet JSON files. The block SHALL contain: `"strategy"` (string, default "fuzzy"), `"threshold"` (float, default 0.7), `"airdateTolerance"` (int, default 7). The parsed values SHALL be used to construct a `ResolutionConfig` record included in the `MatchingConfig`.

#### Scenario: JSON with resolution block
- **WHEN** a RuleSet JSON contains `"resolution": {"strategy": "strict", "threshold": 0.95, "airdateTolerance": 3}`
- **THEN** the resulting MatchingConfig SHALL have Resolution=ResolutionConfig(Strategy="strict", Threshold=0.95, AirdateTolerance=3)

#### Scenario: JSON without resolution block
- **WHEN** a RuleSet JSON has no `"resolution"` property
- **THEN** the resulting MatchingConfig SHALL have Resolution=null

#### Scenario: Partial resolution block
- **WHEN** a RuleSet JSON has `"resolution": {"strategy": "strict"}`
- **THEN** the resulting MatchingConfig SHALL have Resolution=ResolutionConfig(Strategy="strict", Threshold=0.7, AirdateTolerance=7) with defaults for missing fields

### Requirement: Resolution config merges during community/local overlay
When merging community and local RuleSet JSON files, the resolution config SHALL follow the same merge semantics as other fields: local overrides community. If the local file specifies a resolution block, it SHALL replace the community resolution block entirely.

#### Scenario: Community has resolution, local does not
- **WHEN** community JSON has `"resolution": {"strategy": "fuzzy"}` and local JSON has no resolution block
- **THEN** the merged MatchingConfig SHALL use the community resolution config

#### Scenario: Local overrides community resolution
- **WHEN** community JSON has `"resolution": {"strategy": "fuzzy"}` and local JSON has `"resolution": {"strategy": "strict"}`
- **THEN** the merged MatchingConfig SHALL use the local resolution config (strategy="strict")

#### Scenario: Standalone local with resolution
- **WHEN** local JSON has `standalone: true` and `"resolution": {"strategy": "none"}`
- **THEN** the merged MatchingConfig SHALL use the local resolution config only

### Requirement: RuleSetWorker includes resolution config in MatchingConfig
The RuleSetWorker SHALL pass the parsed ResolutionConfig from RuleSetMerger.Build through to the MatchingConfig sent to MatchMagicManager.

#### Scenario: MatchingConfig carries resolution
- **WHEN** RuleSetMerger.Build produces a MatchingConfig with Resolution set
- **THEN** the MatchingConfig sent to MatchMagicManager SHALL include the same Resolution value

#### Scenario: MatchingConfig without resolution
- **WHEN** RuleSetMerger.Build produces a MatchingConfig with Resolution=null
- **THEN** the MatchingConfig sent to MatchMagicManager SHALL have Resolution=null
