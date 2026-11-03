## ADDED Requirements

### Requirement: Feed API pagination with offset
The `GET /api/feed` endpoint SHALL accept an `offset` query parameter to support pagination. The response SHALL include `filteredTotal` alongside `total` for correct pagination math.

#### Scenario: Paginated query with offset
- **WHEN** client sends `GET /api/feed?limit=25&offset=25`
- **THEN** the response contains entries 26-50 (0-indexed), `total` as all-time count, and `filteredTotal` as the count matching current filters

#### Scenario: Offset beyond available entries
- **WHEN** client sends `GET /api/feed?limit=25&offset=9999`
- **THEN** the response contains an empty entries array with correct `total` and `filteredTotal`

### Requirement: Feed API type filter
The `GET /api/feed` endpoint SHALL accept a `type` query parameter with values `show` or `movie` to filter entries by media type.

#### Scenario: Filter by show type
- **WHEN** client sends `GET /api/feed?type=show`
- **THEN** only entries with mediaType=Show are returned and `filteredTotal` reflects the show-only count

### Requirement: Feed API channel filter
The `GET /api/feed` endpoint SHALL accept a `channel` query parameter to filter entries by broadcast channel.

#### Scenario: Filter by channel
- **WHEN** client sends `GET /api/feed?channel=ARD`
- **THEN** only entries with channel matching "ARD" (case-insensitive) are returned

### Requirement: Feed API quality filter
The `GET /api/feed` endpoint SHALL accept a `quality` query parameter (integer: 480, 720, 1080) to filter entries by video quality.

#### Scenario: Filter by quality
- **WHEN** client sends `GET /api/feed?quality=1080`
- **THEN** only entries with quality=1080 are returned

### Requirement: Feed API text search
The `GET /api/feed` endpoint SHALL accept a `q` query parameter to search entries by title or topic (case-insensitive contains match).

#### Scenario: Search by title
- **WHEN** client sends `GET /api/feed?q=Tatort`
- **THEN** entries whose title or topic contains "Tatort" (case-insensitive) are returned

### Requirement: Combined feed filters
All feed query parameters (offset, limit, type, channel, quality, q) SHALL be composable. Filters are applied with AND logic before pagination.

#### Scenario: Combined filter with pagination
- **WHEN** client sends `GET /api/feed?type=show&channel=ARD&limit=25&offset=0`
- **THEN** only Show entries from ARD are returned, `filteredTotal` reflects the combined filter count, and `total` remains the all-time unfiltered count
