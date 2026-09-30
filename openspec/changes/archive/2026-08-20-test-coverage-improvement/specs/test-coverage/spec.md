## ADDED Requirements

### Requirement: Unit test coverage for HTTP API clients
MediathekClient and TvdbClient SHALL have unit tests covering success, HTTP error, and deserialization failure paths using FakeHttpMessageHandler.

#### Scenario: MediathekClient successful query
- **WHEN** MediathekClient.QueryAsync is called with a valid query and the HTTP response is 200 with valid JSON
- **THEN** a deserialized MediathekQueryResponse is returned with the expected result items

#### Scenario: MediathekClient HTTP error returns null
- **WHEN** MediathekClient.QueryAsync is called and the HTTP response is non-success (e.g. 500)
- **THEN** null is returned without throwing

#### Scenario: MediathekClient malformed JSON returns null
- **WHEN** MediathekClient.QueryAsync is called and the response body is not valid JSON
- **THEN** null is returned without throwing

#### Scenario: TvdbClient GetShowAsync success
- **WHEN** TvdbClient.GetShowAsync is called with a valid TVDB ID and the response is 200 with valid JSON
- **THEN** a TvdbShowInfo with the correct series name and aliases is returned

#### Scenario: TvdbClient GetShowAsync HTTP error returns null
- **WHEN** TvdbClient.GetShowAsync is called and the HTTP response is non-success
- **THEN** null is returned without throwing

#### Scenario: TvdbClient GetEpisodesAsync success
- **WHEN** TvdbClient.GetEpisodesAsync is called with a valid TVDB ID and season and the response is 200
- **THEN** a TvdbEpisodeInfo array with episode names and numbers is returned

#### Scenario: TvdbClient GetEpisodesAsync HTTP error returns null
- **WHEN** TvdbClient.GetEpisodesAsync is called and the HTTP response is non-success
- **THEN** null is returned without throwing

### Requirement: Unit test coverage for SearchChildHelpers
SearchChildHelpers SHALL have unit tests for query building and match record construction.

#### Scenario: BuildGenericPipelineRecord creates valid record
- **WHEN** BuildGenericPipelineRecord is called with a search topic, tvdbId, season, episode, and totalResults
- **THEN** a MatchRecord is returned with a 10-char hex ID, UTC timestamp, the provided values, source "generic-pipeline", and empty match arrays

#### Scenario: SearchMediathekAsync returns results on success
- **WHEN** SearchMediathekAsync is called with a non-blank search term and the MediathekClient returns results
- **THEN** the MediathekResultItem array from the response is returned

#### Scenario: SearchMediathekAsync returns empty on exception
- **WHEN** SearchMediathekAsync is called and the MediathekClient throws an exception
- **THEN** an empty array is returned

#### Scenario: SearchMediathekAsync blank query uses size 100
- **WHEN** SearchMediathekAsync is called with a blank search term
- **THEN** the query sent to MediathekClient has no query items and Size=100

### Requirement: Unit test coverage for PathMappingHelper
PathMappingHelper SHALL have unit tests for path parsing and mapping as pure functions.

#### Scenario: ParsePathMapping with valid colon-separated mapping
- **WHEN** ParsePathMapping is called with "/container/path:/host/path"
- **THEN** a tuple (From: "/container/path", To: "/host/path") is returned

#### Scenario: ParsePathMapping with null or empty input
- **WHEN** ParsePathMapping is called with null or empty string
- **THEN** null is returned

#### Scenario: ParsePathMapping with invalid format
- **WHEN** ParsePathMapping is called with a string that has no colon or more than one colon
- **THEN** null is returned

#### Scenario: MapPath applies matching mapping
- **WHEN** MapPath is called with a path that starts with the From prefix and a valid mapping
- **THEN** the From prefix is replaced with the To prefix

#### Scenario: MapPath with no mapping returns path unchanged
- **WHEN** MapPath is called with a null mapping
- **THEN** the original path is returned unchanged

### Requirement: Unit test coverage for RuleSetFileWriter
RuleSetFileWriter SHALL have unit tests verifying file output.

#### Scenario: Write creates JSON file with slugified name
- **WHEN** RuleSetFileWriter.Write is called with a directory and a RuleSetFile
- **THEN** a JSON file named after the slugified topic is created in that directory and the content deserializes back to the original RuleSetFile

### Requirement: Rename misnamed test file
The test file DownloadQueueActorTests.cs SHALL be renamed to DownloadEventsTests.cs to match the test class name DownloadEventsTests it contains.

#### Scenario: File rename matches class
- **WHEN** the rename is applied
- **THEN** the file at DownloadClient/DownloadEventsTests.cs contains the DownloadEventsTests class and no file named DownloadQueueActorTests.cs exists
