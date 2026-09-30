## MODIFIED Requirements

### Requirement: Mediathek messages model the external API contract

MediathekQuery and MediathekQueryCompleted SHALL model the MediathekViewWeb API
request and response as primitive records. `QueryMediathekFailed` SHALL be an
abstract record base type with two sealed subtypes: `QueryMediathekQueueFull`
for retryable queue-full failures and `QueryMediathekError(Exception Cause)` for
terminal failures.

#### Scenario: MediathekQuery record

- **WHEN** a query to MediathekViewWeb is constructed
- **THEN** MediathekQuery SHALL contain: Fields (MediathekQueryField[]), SortBy (string?), SortOrder (string?), Future (bool), Offset (int), Size (int), DurationMin (int?), DurationMax (int?)

#### Scenario: MediathekQueryField record

- **WHEN** a query field is specified
- **THEN** MediathekQueryField SHALL contain: Fields (string[]) for searchable field names and Query (string) for the search term

#### Scenario: MediathekQueryCompleted record

- **WHEN** a query succeeds
- **THEN** MediathekQueryCompleted SHALL contain: Items (MediathekItem[]), Total (int)

#### Scenario: QueryMediathekFailed is abstract base

- **WHEN** a mediathek query fails for any reason
- **THEN** the response SHALL be a subtype of `abstract record QueryMediathekFailed : QueryMediathekResponse`

#### Scenario: QueryMediathekQueueFull for retryable failures

- **WHEN** the MediathekViewWebManager's queue is full and cannot accept a new request
- **THEN** the response SHALL be `QueryMediathekQueueFull` which extends `QueryMediathekFailed`
- **AND** the record SHALL have no parameters (the failure reason is implicit in the type)

#### Scenario: QueryMediathekError for terminal failures

- **WHEN** the mediathek query fails due to an HTTP error, parse error, or other non-retryable cause
- **THEN** the response SHALL be `QueryMediathekError(Exception Cause)` which extends `QueryMediathekFailed`

#### Scenario: Pattern matching on failure type

- **WHEN** a caller receives a `QueryMediathekFailed` response
- **THEN** it SHALL be able to pattern-match on `QueryMediathekQueueFull` (retryable) vs `QueryMediathekError` (terminal) using C# pattern matching

#### Scenario: Catch-all compatibility

- **WHEN** a caller pattern-matches on `QueryMediathekFailed` without distinguishing subtypes
- **THEN** the match SHALL succeed for both `QueryMediathekQueueFull` and `QueryMediathekError`

#### Scenario: MediathekItem with all quality variants

- **WHEN** an API response item has url_video_low, url_video, url_video_hd, and url_subtitle
- **THEN** the MediathekItem SHALL contain all four URLs as nullable string fields plus url_website

#### Scenario: MediathekItem with missing variants

- **WHEN** an API response item has only url_video (no HD, no low, no subtitle)
- **THEN** the MediathekItem SHALL have null for UrlVideoLow, UrlVideoHd, and UrlSubtitle
