## MODIFIED Requirements

### Requirement: TVDB show and episode resolution

SeriesResolver SHALL handle `ResolveSeries(tvdbId, season?)` requests and respond with `SeriesResolved(showName, episodes)` containing the resolved show name and episode list from TVDB. When the TVDB API is unreachable or returns an error, SeriesResolver SHALL log a warning with the TVDB ID and HTTP status code, and respond with `SeriesResolved(null, null)` instead of failing silently.

#### Scenario: Successful series resolution
- **WHEN** SeriesResolver receives a `ResolveSeries` request with a valid `tvdbId`
- **THEN** it SHALL query the TVDB API and respond with `SeriesResolved` containing the show name and episodes

#### Scenario: TVDB API returns error
- **WHEN** SeriesResolver receives a `ResolveSeries` request and the TVDB API returns a non-success status code
- **THEN** it SHALL log a warning including the tvdbId and status code, and respond with `SeriesResolved(null, null)`

#### Scenario: TVDB API unreachable
- **WHEN** SeriesResolver receives a `ResolveSeries` request and the TVDB API call throws an exception
- **THEN** it SHALL log a warning including the tvdbId and exception message, and respond with `SeriesResolved(null, null)`

#### Scenario: Season-scoped resolution
- **WHEN** SeriesResolver receives a `ResolveSeries` request with a `tvdbId` and a `season` parameter
- **THEN** it SHALL respond with `SeriesResolved` containing only episodes for the specified season
