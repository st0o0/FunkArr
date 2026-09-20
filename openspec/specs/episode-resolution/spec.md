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
The TitleMatch method SHALL compute a normalized Levenshtein similarity (0.0 to 1.0) between the candidate title (or constructedTitle if present) and each TVDB episode name. The match SHALL be accepted only if the similarity meets or exceeds `EnrichmentConfig.Title.Threshold` (default 0.7).

#### Scenario: Exact title match
- **WHEN** candidate title is "Roomservice" and TVDB episode name is "Roomservice"
- **THEN** similarity SHALL be 1.0 and the match SHALL be accepted

#### Scenario: Close title match above threshold
- **WHEN** candidate title is "Sashimi Spezial" and TVDB episode name is "Odenthal - 83 - Sashimi Spezial" and threshold is 0.7
- **THEN** the resolver SHALL compute similarity considering substring containment and accept the match if similarity >= 0.7

#### Scenario: Title below threshold
- **WHEN** candidate title is "Die kleine Zeugin" and no TVDB episode name has similarity >= the configured threshold
- **THEN** the match SHALL be rejected and the next method SHALL be tried

#### Scenario: Custom threshold from config
- **WHEN** EnrichmentConfig.Title.Threshold is 0.5
- **THEN** a title match with similarity 0.55 SHALL be accepted (would be rejected at default 0.7)

#### Scenario: ConstructedTitle takes precedence over Title
- **WHEN** an EpisodeCandidate has both Title="Tatort: Roomservice" and ConstructedTitle="Roomservice"
- **THEN** the title match SHALL use ConstructedTitle for comparison

#### Scenario: Case-insensitive and umlaut-normalized comparison
- **WHEN** candidate title is "Koenige der Nacht" and TVDB episode name is "Könige der Nacht"
- **THEN** the comparison SHALL normalize umlauts and be case-insensitive

#### Scenario: Multiple TVDB episodes match — highest similarity wins
- **WHEN** two TVDB episodes have similarity >= threshold
- **THEN** the episode with the highest similarity SHALL be selected

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

### Requirement: LevenshteinDistance namespace
The LevenshteinDistance utility SHALL reside in the `FunkArr.Enrichment` namespace.

#### Scenario: Namespace
- **WHEN** LevenshteinDistance is referenced
- **THEN** it SHALL be in the `FunkArr.Enrichment` namespace
