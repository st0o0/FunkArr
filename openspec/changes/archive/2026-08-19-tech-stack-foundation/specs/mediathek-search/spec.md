## ADDED Requirements

### Requirement: MediathekViewWeb query
The system SHALL send POST requests to the MediathekViewWeb API (`mediathekviewweb.de/api/query`) to search for content by topic, title, and other fields.

#### Scenario: Search for a TV show
- **WHEN** a search is initiated for show "Tatort" season 1 episode 3
- **THEN** the system sends a POST to MediathekViewWeb with query fields for topic and title, requesting up to 5000 results sorted by timestamp

### Requirement: Result caching
The system SHALL cache search results for a configurable duration (default 55 minutes) to avoid redundant API calls.

#### Scenario: Repeated search within cache window
- **WHEN** the same search query is executed twice within 55 minutes
- **THEN** the second request returns cached results without hitting MediathekViewWeb

#### Scenario: Cache expiry
- **WHEN** a cached search result is older than the configured cache duration
- **THEN** the next request for that query hits MediathekViewWeb and refreshes the cache

### Requirement: Rate limiting
The system SHALL rate-limit outgoing requests to MediathekViewWeb to avoid overloading the service. The rate limit MUST be configurable.

#### Scenario: Burst of search requests
- **WHEN** 10 search requests arrive within 1 second
- **THEN** the system queues them and dispatches to MediathekViewWeb at the configured rate, returning results as they complete

### Requirement: Runtime duration filtering
The system SHALL filter search results by comparing the content's runtime duration against the expected episode duration, rejecting items that deviate by more than a configurable percentage (default 35%).

#### Scenario: Content with matching duration
- **WHEN** Sonarr expects a 45-minute episode and MediathekViewWeb returns a 42-minute result
- **THEN** the result passes the duration filter (7% deviation, below 35% threshold)

#### Scenario: Content with wrong duration
- **WHEN** Sonarr expects a 45-minute episode and MediathekViewWeb returns a 5-minute trailer
- **THEN** the result is filtered out (89% deviation, above 35% threshold)

### Requirement: Title matching
The system SHALL match search results against expected titles using normalized string comparison. Title normalization MUST handle German special characters (umlauts, eszett) and common variations.

#### Scenario: Title with umlauts matches
- **WHEN** the expected title is "Über den Dächern" and a result has title "Ueber den Daechern"
- **THEN** the normalized comparison considers them a match

#### Scenario: Skip keywords
- **WHEN** a search result title contains "Audiodeskription", "Trailer", "Gebärdensprache", or other configured skip keywords
- **THEN** the result is excluded from the output

### Requirement: Episode pattern matching
The system SHALL extract season and episode numbers from result titles using S##E## pattern matching as a fallback when title matching alone is insufficient.

#### Scenario: S##E## pattern in title
- **WHEN** a result title contains "S01E03" and the search is for season 1 episode 3
- **THEN** the pattern match confirms the result

### Requirement: TVDB metadata lookup
The system SHALL look up show metadata (name, episode titles, air dates) from TheTVDB using the TVDB ID provided by Sonarr/Radarr.

#### Scenario: TVDB lookup for show name
- **WHEN** a tvsearch request includes `tvdbid=12345`
- **THEN** the system resolves the TVDB ID to the show's German title for use in the MediathekViewWeb query

#### Scenario: TVDB lookup failure
- **WHEN** the TVDB API is unavailable or the ID is unknown
- **THEN** the system falls back to the `q` parameter text if provided, or returns empty results
