## ADDED Requirements

### Requirement: Empty-criteria requests are answered per requested media type
For a `tvsearch`, `movie` or `search` request without search criteria, the response SHALL contain feed entries of the media type the request asks for. When the feed holds no entries of that media type, the response SHALL contain the validation item instead of an empty result, even if the feed holds entries of other media types. The media type is derived from the function (`tvsearch` is TV, `movie` is movies) and, for `search`, from the requested categories; a `search` without categories asks for all media types.

#### Scenario: Feed has only TV entries, movie probe
- **WHEN** the feed holds TV entries and no movie entries and `t=movie&cat=2030,2040` is requested
- **THEN** the response SHALL contain the validation item in the movie category
- **AND** SHALL NOT be an empty result

#### Scenario: Feed has only movie entries, TV probe
- **WHEN** the feed holds movie entries and no TV entries and `t=tvsearch&cat=5030,5040` is requested
- **THEN** the response SHALL contain the validation item in the TV category

#### Scenario: Feed has entries of the requested type
- **WHEN** the feed holds entries of the requested media type
- **THEN** the response SHALL contain those entries
- **AND** SHALL NOT contain the validation item

#### Scenario: Feed is empty
- **WHEN** the feed holds no entries
- **THEN** the response SHALL contain the validation item for every probe type

#### Scenario: Search without categories
- **WHEN** `t=search` without `cat` is requested and the feed holds entries of any media type
- **THEN** the response SHALL contain feed entries

#### Scenario: Feed unavailable
- **WHEN** the feed store does not answer
- **THEN** the response SHALL contain the validation item
- **AND** SHALL NOT be cached

### Requirement: Reported total matches the returned entries
The `total` of the Newznab response for a feed-mode request SHALL be the number of feed entries matching the request filters, not the number of entries across all media types.

#### Scenario: Filtered total
- **WHEN** the feed holds 5 TV entries and 2 movie entries and `t=tvsearch` is requested with limit 100
- **THEN** the response SHALL contain 5 items and report `total` 5

#### Scenario: Offset beyond the last entry
- **WHEN** the feed holds 5 matching entries and the request has `offset=5`
- **THEN** the response SHALL contain no items and report `total` 5
- **AND** SHALL NOT contain the validation item
