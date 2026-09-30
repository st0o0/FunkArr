## MODIFIED Requirements

### Requirement: Topic stats endpoint
The `GET /api/v1/matches/topics` endpoint SHALL aggregate match quality from active ShowActors instead of the deleted MatchQualityActor.

#### Scenario: List all topic stats
- **WHEN** `GET /api/v1/matches/topics` is requested
- **THEN** the endpoint SHALL query `GetMatchQuality` from all ShowActors in the community catalog and return an array of `TopicSummary`

#### Scenario: Single topic stats
- **WHEN** `GET /api/v1/matches/topics/{topic}` is requested
- **THEN** the endpoint SHALL find the ShowActor for that topic (via registry lookup) and return its `TopicSummary`

#### Scenario: Topic not found
- **WHEN** `GET /api/v1/matches/topics/{topic}` is requested for an unknown topic
- **THEN** the endpoint SHALL return HTTP 404

### Requirement: Unmatched items endpoint
The `GET /api/v1/matches/unmatched` endpoint SHALL return unmatched items grouped by topic from active ShowActors.

#### Scenario: List unmatched items
- **WHEN** `GET /api/v1/matches/unmatched` is requested
- **THEN** the endpoint SHALL aggregate unmatched items from ShowActors and return them grouped by topic

#### Scenario: Filter by topic
- **WHEN** `GET /api/v1/matches/unmatched?topic=Tatort` is requested
- **THEN** the endpoint SHALL return unmatched items only for the Tatort topic
