## MODIFIED Requirements

### Requirement: Strategy evaluation uses 5-member enum
The MatchMagic evaluation SHALL switch on the 5-member `IdentificationStrategy` enum directly, without checking `TitleMatchMode`. The evaluation for `SeasonAndEpisodeNumber` and `AbsoluteEpisodeNumber` SHALL both use regex capture logic (with and without season pattern). The evaluation for `TitleExact` and `TitleIncludes` SHALL both use title construction logic (with exact and contains matching).

#### Scenario: SeasonAndEpisodeNumber evaluation
- **WHEN** a rule with strategy `SeasonAndEpisodeNumber` is evaluated
- **THEN** the engine extracts season and episode numbers using regex patterns

#### Scenario: AbsoluteEpisodeNumber evaluation
- **WHEN** a rule with strategy `AbsoluteEpisodeNumber` is evaluated
- **THEN** the engine extracts only the episode number using episodeRegex

#### Scenario: TitleExact evaluation
- **WHEN** a rule with strategy `TitleExact` is evaluated
- **THEN** the engine constructs a title from parts and matches exactly

#### Scenario: TitleIncludes evaluation
- **WHEN** a rule with strategy `TitleIncludes` is evaluated
- **THEN** the engine constructs a title from parts and matches as substring

#### Scenario: No TitleMatchMode in evaluation
- **WHEN** the evaluation code is inspected
- **THEN** there are no references to `TitleMatchMode`
