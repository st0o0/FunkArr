## MODIFIED Requirements

### Requirement: Search command messages use primitive types only

Search command and response messages SHALL use only primitive types and simple records. TvSearch SHALL contain: SearchId (Guid), Source (string), Query (string?), Season (int?), Episode (int?), TvdbId (int?), ImdbId (string?), Limit (int?), Offset (int?). MovieSearch SHALL contain: SearchId (Guid), Source (string), Query (string?), ImdbId (string?), TmdbId (int?), Limit (int?), Offset (int?). SearchCompleted SHALL contain: SearchId (Guid), Items (SearchResultItem[]), Total (int). SearchFailed SHALL contain: SearchId (Guid), Cause (Exception).

#### Scenario: TvSearch message

- **WHEN** Sonarr searches for a show
- **THEN** a TvSearch record SHALL be created with primitive fields only

#### Scenario: MovieSearch message

- **WHEN** Radarr searches for a movie
- **THEN** a MovieSearch record SHALL be created with primitive fields only

#### Scenario: SearchFailed carries Exception

- **WHEN** a search fails for any reason (timeout, query failure, scoring failure)
- **THEN** SearchFailed SHALL contain the Exception as Cause, not a string Reason
