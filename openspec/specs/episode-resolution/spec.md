## Purpose

Episode enrichment logic matching Mediathek items to TVDB episodes using multiple match methods in priority order.

## Requirements

### Requirement: Episode resolution applies strategies in priority order
The episode resolver SHALL attempt to enrich each EpisodeCandidate to a TVDB episode using match methods in the following priority order: RegexExtracted, TitleMatch, AirdateMatch, RuntimeWindow. The first method that produces a confident match SHALL win. If no method produces a match, the item SHALL remain unenriched. The resolver SHALL use default threshold (0.7) and airdate tolerance (7 days) as internal constants — no external config parameter.

#### Scenario: Regex-extracted season/episode passes through
- **WHEN** an EpisodeCandidate has ExistingSeason="2026" and ExistingEpisode="01"
- **THEN** the resolver SHALL return an EnrichedEpisode with Season="2026", Episode="01", Method=MatchMethod.RegexExtracted, Confidence=1.0 without querying TVDB

#### Scenario: Title match resolves an episode
- **WHEN** an EpisodeCandidate has Title="Roomservice" and TVDB episode "Roomservice" exists for the series
- **THEN** the resolver SHALL return an EnrichedEpisode with the TVDB season/episode numbers, Method=MatchMethod.TitleMatch, and Confidence equal to the similarity score

#### Scenario: Airdate match resolves an episode
- **WHEN** an EpisodeCandidate has AiredAt=2026-03-01 and a TVDB episode aired on 2026-03-01
- **THEN** the resolver SHALL return an EnrichedEpisode with that episode's season/episode numbers, Method=MatchMethod.AirdateMatch, Confidence=0.9

#### Scenario: No method matches
- **WHEN** no match method produces a confident match for an EpisodeCandidate
- **THEN** the resolver SHALL not include that item in the enriched results

#### Scenario: Method priority order
- **WHEN** both TitleMatch and AirdateMatch would produce a result for the same candidate
- **THEN** TitleMatch SHALL take priority (it runs first)

### Requirement: FuzzyTitleMatch uses Levenshtein similarity
The TitleMatch method SHALL compute a normalized Levenshtein similarity (0.0 to 1.0) between the candidate title (or constructedTitle if present) and each TVDB episode name. The match SHALL be accepted only if the similarity meets or exceeds the threshold (0.7).

#### Scenario: Exact title match
- **WHEN** candidate title is "Roomservice" and TVDB episode name is "Roomservice"
- **THEN** similarity SHALL be 1.0 and the match SHALL be accepted

#### Scenario: Close title match above threshold
- **WHEN** candidate title is "Sashimi Spezial" and TVDB episode name is "Odenthal - 83 - Sashimi Spezial" and threshold is 0.7
- **THEN** the resolver SHALL compute similarity considering substring containment and accept the match if similarity >= 0.7

#### Scenario: Title below threshold
- **WHEN** candidate title is "Die kleine Zeugin" and no TVDB episode name has similarity >= 0.7
- **THEN** the match SHALL be rejected and the next method SHALL be tried

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
The AirdateMatch method SHALL compare the candidate's AiredAt timestamp against TVDB episode aired dates. A match SHALL be accepted if the dates are within tolerance (7 days). When multiple TVDB episodes fall within the tolerance window, the closest date SHALL be selected.

#### Scenario: Exact airdate match
- **WHEN** candidate AiredAt is 2026-03-01 and a TVDB episode aired on 2026-03-01
- **THEN** the match SHALL be accepted with Confidence=0.9

#### Scenario: Airdate within tolerance
- **WHEN** candidate AiredAt is 2026-03-03 and a TVDB episode aired on 2026-03-01 and tolerance is 7 days
- **THEN** the match SHALL be accepted with reduced Confidence (proportional to distance)

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
The RuntimeWindow method SHALL compare the candidate's Duration against TVDB episode runtimes. A match SHALL be accepted if the candidate duration is within +/-35% of a TVDB episode runtime. This method SHALL only be used as a tiebreaker when combined with other partial signals.

#### Scenario: Duration within 35% window
- **WHEN** candidate Duration is 5400 seconds (90min) and a TVDB episode runtime is 88 minutes
- **THEN** the candidate is within the +/-35% window (57-119 min) and the runtime matches

#### Scenario: Duration outside window
- **WHEN** candidate Duration is 720 seconds (12min) and all TVDB episode runtimes are 88 minutes
- **THEN** the candidate does NOT match any episode by runtime

#### Scenario: Runtime used as tiebreaker
- **WHEN** multiple TVDB episodes have similar title matches and one has a matching runtime
- **THEN** the episode with the matching runtime SHALL be preferred

### Requirement: LevenshteinDistance namespace
The LevenshteinDistance utility SHALL reside in the `FunkArr.Enrichment` namespace.

#### Scenario: Namespace
- **WHEN** LevenshteinDistance is referenced
- **THEN** it SHALL be in the `FunkArr.Enrichment` namespace
