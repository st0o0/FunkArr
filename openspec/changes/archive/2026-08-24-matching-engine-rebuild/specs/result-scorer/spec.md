## ADDED Requirements

### Requirement: Result scoring
The ResultScorer SHALL assign a numeric score to each SearchResult based on quality tier, topic name match, and air date proximity.

#### Scenario: Quality tier scoring
- **WHEN** a SearchResult has quality HD1080
- **THEN** it SHALL receive 30 quality points

#### Scenario: Quality tier scoring SD
- **WHEN** a SearchResult has quality SD
- **THEN** it SHALL receive 10 quality points

### Requirement: Topic name match scoring
The ResultScorer SHALL award points when the SearchResult's Topic matches the search context's show name (case-insensitive, with German umlaut normalization).

#### Scenario: Exact topic match
- **WHEN** SearchResult.Topic is "Tatort" and the search show name is "Tatort"
- **THEN** it SHALL receive 50 topic match points

#### Scenario: Partial topic match
- **WHEN** SearchResult.Topic contains the normalized show name
- **THEN** it SHALL receive 30 topic match points

#### Scenario: No topic match
- **WHEN** SearchResult.Topic does not contain the search show name
- **THEN** it SHALL receive 0 topic match points

### Requirement: Air date proximity scoring
The ResultScorer SHALL award up to 20 points based on how close the item's timestamp is to an expected air date, when one is provided.

#### Scenario: Same day
- **WHEN** the item timestamp is on the same day as the expected air date
- **THEN** it SHALL receive 20 air date proximity points

#### Scenario: No expected air date
- **WHEN** no expected air date is provided in the scoring context
- **THEN** it SHALL receive 0 air date proximity points
