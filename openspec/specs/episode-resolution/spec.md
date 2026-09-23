## Purpose

Episode enrichment logic matching Mediathek items to TVDB episodes using multiple match methods in priority order.

## Requirements

### Requirement: Episode resolution applies strategies in priority order
The episode resolver SHALL attempt to enrich each EpisodeCandidate to a TVDB episode using match methods. RegexExtracted SHALL always run first unconditionally. After that, the resolver SHALL walk the `EnrichmentConfig.Methods` array in order, attempting each method until one produces a confident match. If `EnrichmentConfig.Enabled` is false, the resolver SHALL return an empty array. If no method produces a match, the item SHALL remain unenriched. Default method order is [Title, Airdate] which preserves current behavior.

#### Scenario: Regex-extracted season/episode passes through
- **WHEN** an EpisodeCandidate has ExistingSeason="2026" and ExistingEpisode="01"
- **THEN** the resolver SHALL return an EnrichedEpisode with Season="2026", Episode="01", Method=MatchMethod.RegexExtracted, Confidence=1.0 without querying TVDB

#### Scenario: Methods array controls fallback order
- **WHEN** EnrichmentConfig.Methods is [Airdate, Title]
- **THEN** AirdateMatch SHALL be attempted before TitleMatch

#### Scenario: Default method order preserves current behavior
- **WHEN** EnrichmentConfig.Methods is [Title, Airdate] (the default)
- **THEN** TitleMatch SHALL be attempted before AirdateMatch, matching current hardcoded behavior

#### Scenario: Single method configured
- **WHEN** EnrichmentConfig.Methods is [Airdate]
- **THEN** only AirdateMatch SHALL be attempted; TitleMatch SHALL not run

#### Scenario: Enrichment disabled
- **WHEN** EnrichmentConfig.Enabled is false
- **THEN** the resolver SHALL return an empty array without attempting any matching

#### Scenario: No method matches
- **WHEN** no match method produces a confident match for an EpisodeCandidate
- **THEN** the resolver SHALL not include that item in the enriched results

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

### Requirement: AirdateMatch compares dates with tolerance
The AirdateMatch method SHALL compare the candidate's AiredAt timestamp against TVDB episode aired dates. A match SHALL be accepted if the dates are within `EnrichmentConfig.Airdate.Tolerance` days (default 7). When multiple TVDB episodes fall within the tolerance window, the closest date SHALL be selected.

#### Scenario: Exact airdate match
- **WHEN** candidate AiredAt is 2026-03-01 and a TVDB episode aired on 2026-03-01
- **THEN** the match SHALL be accepted with Confidence=0.9

#### Scenario: Airdate within tolerance
- **WHEN** candidate AiredAt is 2026-03-03 and a TVDB episode aired on 2026-03-01 and tolerance is 7 days
- **THEN** the match SHALL be accepted with reduced Confidence (proportional to distance)

#### Scenario: Custom tolerance from config
- **WHEN** EnrichmentConfig.Airdate.Tolerance is 3 and candidate AiredAt is 5 days from the closest TVDB episode
- **THEN** the match SHALL be rejected (5 > 3)

#### Scenario: Airdate outside tolerance
- **WHEN** candidate AiredAt is 2026-08-30 and the closest TVDB episode aired on 2026-05-03 and tolerance is 7 days
- **THEN** the match SHALL be rejected

#### Scenario: No AiredAt on candidate
- **WHEN** candidate AiredAt is null
- **THEN** the AirdateMatch method SHALL skip this candidate

#### Scenario: Multiple TVDB episodes within tolerance
- **WHEN** two TVDB episodes fall within the tolerance window
- **THEN** the episode with the smallest date difference SHALL be selected

### Requirement: RuntimeWindow matches by duration
The RuntimeWindow method SHALL compare the candidate's Duration against TVDB episode runtimes. When `EnrichmentConfig.Runtime.Mode` is Tiebreaker (default), it SHALL only be used to break ties between title matches. When `EnrichmentConfig.Runtime.Mode` is Filter, it SHALL pre-filter TVDB episodes before title/airdate matching, removing episodes whose runtime differs by more than `EnrichmentConfig.Runtime.Tolerance` (default 0.35 = 35%).

#### Scenario: Tiebreaker mode (default)
- **WHEN** RuntimeMode is Tiebreaker and multiple TVDB episodes have similar title matches
- **THEN** the episode with matching runtime SHALL be preferred

#### Scenario: Filter mode pre-filters episodes
- **WHEN** RuntimeMode is Filter and Runtime.Tolerance is 0.35
- **THEN** TVDB episodes whose runtime differs from the candidate by more than 35% SHALL be excluded before title/airdate matching

#### Scenario: Duration within window
- **WHEN** candidate Duration is 5400 seconds (90min) and a TVDB episode runtime is 88 minutes
- **THEN** the candidate is within the 35% window and the runtime matches

#### Scenario: Duration outside window
- **WHEN** candidate Duration is 720 seconds (12min) and all TVDB episode runtimes are 88 minutes
- **THEN** the candidate does NOT match any episode by runtime

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

### Requirement: LevenshteinDistance namespace
The LevenshteinDistance utility SHALL reside in the `FunkArr.Enrichment` namespace.

#### Scenario: Namespace
- **WHEN** LevenshteinDistance is referenced
- **THEN** it SHALL be in the `FunkArr.Enrichment` namespace
