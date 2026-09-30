## MODIFIED Requirements

### Requirement: Match trace emission
The matching engine SHALL produce a MatchTrace for every Mediathek result item evaluated, recording the outcome (matched/filtered/unmatched) and the evaluation path through rules and filters. Matched items SHALL carry the full `MatchedEpisodeInfo` (including TvdbEpisodeInfo with season, episode number, episode name, and air date) through the response to callers.

#### Scenario: Matched item produces trace
- **WHEN** item "Tatort - Der letzte Schrei" matches via rule #1
- **THEN** the trace SHALL record outcome=Matched, ruleIndex=0, strategy, confidence, and the TVDB episode info

#### Scenario: Filtered item produces trace
- **WHEN** item "Tatort (AD)" is excluded by a NOT filter
- **THEN** the trace SHALL record outcome=Filtered, the failing filter, and no episode info

#### Scenario: MatchedEpisodeInfo preserved in MatchedItemInfo
- **WHEN** item "Tatort - Der letzte Schrei" matches to TVDB episode S01E03 "Der letzte Schrei" aired 2024-03-15
- **THEN** the `MatchedItemInfo` response SHALL carry the full `MatchedEpisodeInfo` with Episode.AiredSeason=1, Episode.AiredEpisodeNumber=3, Episode.EpisodeName="Der letzte Schrei", Episode.FirstAired="2024-03-15"
