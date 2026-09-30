## MODIFIED Requirements

### Requirement: FuzzyTitleMatch uses Levenshtein similarity
The TitleMatch method SHALL compute a normalized Levenshtein similarity (0.0 to 1.0) between the candidate title (or constructedTitle if present) and each TVDB episode name. When a TVDB episode name contains ` - ` separators, the method SHALL additionally compare the candidate against each segment produced by splitting on ` - `. The match score for a TVDB episode SHALL be the maximum similarity across the full name and all segments. The match SHALL be accepted only if the best score meets or exceeds `EnrichmentConfig.Title.Threshold` (default 0.7).

#### Scenario: Exact title match
- **WHEN** candidate title is "Roomservice" and TVDB episode name is "Roomservice"
- **THEN** similarity SHALL be 1.0 and the match SHALL be accepted

#### Scenario: Composite TVDB title with matching segment
- **WHEN** candidate title is "König in Gelb" and TVDB episode name is "Lindholm - 33 - König in Gelb" and threshold is 0.7
- **THEN** the method SHALL split the TVDB name into segments ["Lindholm", "33", "König in Gelb"]
- **AND** compute similarity against each segment
- **AND** the score against "König in Gelb" segment SHALL be 1.0
- **AND** the match SHALL be accepted with similarity 1.0

#### Scenario: Composite TVDB title with no matching segment
- **WHEN** candidate title is "Bauernsterben" and TVDB episode name is "Lindholm - 33 - König in Gelb" and threshold is 0.7
- **THEN** similarity against all segments SHALL be below 0.7
- **AND** the match SHALL be rejected

#### Scenario: Non-composite TVDB title (no separator)
- **WHEN** TVDB episode name is "Roomservice" (no ` - ` separator)
- **THEN** the method SHALL compare against the full name only (split produces one segment equal to the full name)
- **AND** behavior SHALL be identical to the current implementation

#### Scenario: Close title match above threshold
- **WHEN** candidate title is "Sashimi Spezial" and TVDB episode name is "Odenthal - 83 - Sashimi Spezial" and threshold is 0.7
- **THEN** the resolver SHALL match against the "Sashimi Spezial" segment with similarity 1.0 and accept the match

#### Scenario: Title below threshold
- **WHEN** candidate title is "Die kleine Zeugin" and no TVDB episode name (or segment) has similarity >= the configured threshold
- **THEN** the match SHALL be rejected and the next method SHALL be tried

#### Scenario: Custom threshold from config
- **WHEN** EnrichmentConfig.Title.Threshold is 0.5
- **THEN** a title match with similarity 0.55 SHALL be accepted (would be rejected at default 0.7)

#### Scenario: ConstructedTitle compared against segments too
- **WHEN** an EpisodeCandidate has both Title="Tatort: Roomservice" and ConstructedTitle="Roomservice"
- **AND** TVDB episode name is "Borowski - 42 - Roomservice"
- **THEN** the method SHALL compare ConstructedTitle against each segment
- **AND** the score against "Roomservice" segment SHALL be 1.0

#### Scenario: Case-insensitive and umlaut-normalized comparison
- **WHEN** candidate title is "Koenige der Nacht" and TVDB episode name is "Ott & Grandjean - 11 - Könige der Nacht"
- **THEN** the comparison SHALL normalize umlauts and be case-insensitive, matching the "Könige der Nacht" segment

#### Scenario: Multiple TVDB episodes match - highest similarity wins
- **WHEN** two TVDB episodes have similarity >= threshold (including segment scores)
- **THEN** the episode with the highest similarity SHALL be selected

#### Scenario: Tiebreaker considers segment scores
- **WHEN** multiple TVDB episodes have the same best similarity after segment matching
- **THEN** the runtime tiebreaker SHALL apply as before

## ADDED Requirements

### Requirement: AirDate matching requires minimum title affinity
The AirdateMatch method SHALL only attempt matching for a candidate when the candidate's best title segment score against all TVDB episodes meets or exceeds `EnrichmentConfig.Airdate.MinTitleAffinity` (default 0.3). This prevents AirDate matching from producing false positives when the candidate title has no relation to any TVDB episode in the search scope.

#### Scenario: AirDate allowed when title has weak affinity
- **WHEN** candidate title is "König in Gelb" and best title segment score against TVDB episodes is 1.0
- **AND** MinTitleAffinity is 0.3
- **THEN** AirDate matching SHALL proceed normally

#### Scenario: AirDate blocked when title has no affinity
- **WHEN** candidate title is "Bauernsterben" and best title segment score against all TVDB episodes is 0.15
- **AND** MinTitleAffinity is 0.3
- **THEN** AirDate matching SHALL be skipped for this candidate
- **AND** the candidate SHALL remain unenriched (no episode assignment)

#### Scenario: Default MinTitleAffinity is 0.3
- **WHEN** EnrichmentConfig.Airdate.MinTitleAffinity is not specified
- **THEN** the default value SHALL be 0.3

#### Scenario: Custom MinTitleAffinity from config
- **WHEN** EnrichmentConfig.Airdate.MinTitleAffinity is 0.5
- **AND** candidate best title segment score is 0.4
- **THEN** AirDate matching SHALL be skipped for this candidate

#### Scenario: MinTitleAffinity of 0 disables the guard
- **WHEN** EnrichmentConfig.Airdate.MinTitleAffinity is 0.0
- **THEN** AirDate matching SHALL always proceed (preserving pre-change behavior)

### Requirement: Season fallback enrichment for unmatched candidates
When episode enrichment is requested with a specific season filter and some candidates remain unmatched after the first pass, the enrichment actor SHALL run a second pass for those candidates against all TVDB episodes (no season filter). Matches from the fallback pass SHALL have their confidence multiplied by 0.9 to prefer same-season matches.

#### Scenario: Rerun matched via season fallback
- **WHEN** candidate title is "Das Ende der Nacht" and the requested season is 2026
- **AND** no TVDB episode in season 2026 matches above threshold
- **AND** TVDB season 2025 has an episode "Schürk & Hölzer - 05 - Das Ende der Nacht"
- **THEN** the fallback pass SHALL match the candidate to S2025E5 with confidence × 0.9

#### Scenario: Same-season match not re-processed in fallback
- **WHEN** a candidate was already matched in the first pass (same-season)
- **THEN** it SHALL NOT be included in the fallback pass

#### Scenario: No season filter means no fallback needed
- **WHEN** enrichment is requested with Season=null
- **THEN** the first pass already searches all episodes and no fallback pass SHALL run

#### Scenario: All candidates matched in first pass
- **WHEN** all candidates are matched in the same-season pass
- **THEN** no fallback pass SHALL run

#### Scenario: Fallback confidence penalty applied
- **WHEN** the fallback pass produces a match with raw confidence 0.85
- **THEN** the reported confidence SHALL be 0.85 × 0.9 = 0.765

### Requirement: AirdateMatchConfig includes MinTitleAffinity
`AirdateMatchConfig` SHALL include a `MinTitleAffinity` field (float) representing the minimum best title segment score required before AirDate matching is attempted.

#### Scenario: AirdateMatchConfig record shape
- **WHEN** an `AirdateMatchConfig` is created
- **THEN** it SHALL contain `Tolerance` (int) and `MinTitleAffinity` (float)

#### Scenario: Default MinTitleAffinity in config
- **WHEN** a ruleset does not specify MinTitleAffinity
- **THEN** the default value SHALL be 0.3
