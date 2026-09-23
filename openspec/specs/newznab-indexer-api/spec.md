# Newznab Indexer API

## Purpose

Newznab-compatible indexer API exposing capabilities, search endpoints (TV, movie, general), RSS XML result format, standard NZB download by GUID, error handling, pagination, filtering, and JSON output for integration with Prowlarr and the *arr ecosystem.

## Requirements

### Requirement: Capabilities endpoint
The system SHALL respond to `GET /index/api?t=caps` with a Newznab capabilities XML document declaring supported search types, categories, and server metadata.

#### Scenario: Caps response structure
- **WHEN** `?t=caps` is requested
- **THEN** the response SHALL be `application/xml` with root element `<caps>` containing `<server>`, `<limits>`, `<registration>`, `<searching>`, and `<categories>`

#### Scenario: Server element declared
- **WHEN** the caps XML is returned
- **THEN** `<caps>` SHALL contain `<server title="FunkArr"/>` as the first child element

#### Scenario: Search types declared
- **WHEN** the caps XML is returned
- **THEN** `<searching>` SHALL declare `<search available="yes" supportedParams="q"/>`, `<tv-search available="yes" supportedParams="q,season,ep,tvdbid"/>`, `<movie-search available="yes" supportedParams="q,imdbid,tmdbid"/>`, `<audio-search available="no" supportedParams=""/>`, and `<book-search available="no" supportedParams=""/>`

#### Scenario: Categories declared
- **WHEN** the caps XML is returned
- **THEN** `<categories>` SHALL include category 5000 (TV) with subcategories 5030 (SD) and 5040 (HD), and category 2000 (Movies) with subcategories 2030 (SD) and 2040 (HD)

#### Scenario: Limits declared
- **WHEN** the caps XML is returned
- **THEN** `<limits>` SHALL have `max="500"` and `default="100"`

### Requirement: TV search endpoint
The system SHALL respond to `GET /index/api?t=tvsearch` with Newznab RSS XML containing search results. It SHALL accept optional parameters: `tvdbid` (int), `season` (string), `ep` (string), `q` (string), `offset` (int), `limit` (int), `cat` (string), `maxage` (int), `extended` (int), `attrs` (string).

#### Scenario: Search by tvdbid with season and episode
- **WHEN** `?t=tvsearch&tvdbid=83214&season=01&ep=05` is requested
- **THEN** the response SHALL be valid Newznab RSS XML (currently with zero items since Search domain is not built)

#### Scenario: Search by query string
- **WHEN** `?t=tvsearch&q=Tatort` is requested
- **THEN** the response SHALL be valid Newznab RSS XML

#### Scenario: Empty results format
- **WHEN** no results are found
- **THEN** the RSS XML SHALL have `<newznab:response offset="0" total="0"/>` and an empty items list

### Requirement: General search endpoint
The system SHALL respond to `GET /index/api?t=search` with Newznab RSS XML. It SHALL accept parameters: `q` (string), `offset` (int), `limit` (int), `cat` (string), `maxage` (int), `extended` (int), `attrs` (string).

#### Scenario: Text search
- **WHEN** `?t=search&q=Tatort` is requested
- **THEN** the response SHALL be valid Newznab RSS XML

### Requirement: Movie search endpoint
The system SHALL respond to `GET /index/api?t=movie` with Newznab RSS XML. It SHALL accept optional parameters: `imdbid` (string), `tmdbid` (string), `q` (string), `offset` (int), `limit` (int), `cat` (string), `maxage` (int), `extended` (int), `attrs` (string).

#### Scenario: Search by IMDB ID
- **WHEN** `?t=movie&imdbid=tt0806910` is requested
- **THEN** the response SHALL be valid Newznab RSS XML

#### Scenario: Search by TMDB ID
- **WHEN** `?t=movie&tmdbid=12345` is requested
- **THEN** the response SHALL be valid Newznab RSS XML

### Requirement: TMDB ID parameter binding
The system SHALL accept `tmdbid` as a query parameter on movie search requests. The parameter SHALL be bound to the `IndexerRequest` model and available for forwarding to the Search domain.

#### Scenario: tmdbid parameter accepted
- **WHEN** `?t=movie&tmdbid=550` is requested
- **THEN** the request SHALL be processed without error and the tmdbid value SHALL be available in the request model

#### Scenario: tmdbid parameter absent
- **WHEN** `?t=movie&q=Tatort` is requested without `tmdbid`
- **THEN** the request SHALL be processed normally with tmdbid as null

### Requirement: Unknown function type
The system SHALL return Newznab error XML with code 202 for unrecognized `t` parameter values.

#### Scenario: Unknown t parameter
- **WHEN** `?t=unknown` is requested
- **THEN** the system SHALL return `<error code="202" description="No such function"/>` with HTTP 400

### Requirement: Newznab RSS XML format
Search results SHALL be formatted as RSS 2.0 XML with the Newznab namespace `http://www.newznab.com/DTD/2010/feeds/attributes/`. Each item SHALL include media ID attributes when available. Category attributes SHALL reflect the search type.

#### Scenario: RSS structure
- **WHEN** results are returned
- **THEN** the XML SHALL have `<rss>` root with `<channel>` containing `<title>`, `<description>`, `<newznab:response>`, and zero or more `<item>` elements

#### Scenario: Item structure
- **WHEN** an item is in the results
- **THEN** it SHALL contain `<title>`, `<guid>`, `<link>`, `<comments>`, `<pubDate>`, `<category>`, `<description>`, `<enclosure>` with url/length/type attributes, and `<newznab:attr>` elements for category, size, and media IDs

#### Scenario: TV search category attributes
- **WHEN** a search result item is returned from a `t=tvsearch` request
- **THEN** the `<newznab:attr name="category">` SHALL be `5040` for HD (quality >= 720) or `5030` for SD, and the `<category>` element SHALL be `TV > HD` or `TV > SD`

#### Scenario: Movie search category attributes
- **WHEN** a search result item is returned from a `t=movie` request
- **THEN** the `<newznab:attr name="category">` SHALL be `2040` for HD (quality >= 720) or `2030` for SD, and the `<category>` element SHALL be `Movies > HD` or `Movies > SD`

#### Scenario: General search category attributes
- **WHEN** a search result item is returned from a `t=search` request
- **THEN** the category SHALL default to TV categories (5040/5030) unless the `cat` parameter indicates movie categories (2000-2999)

#### Scenario: Item with tvdbid attribute
- **WHEN** a search result item has TvdbId=83214
- **THEN** the item SHALL include `<newznab:attr name="tvdbid" value="83214"/>`

#### Scenario: Item with imdb attribute
- **WHEN** a search result item has ImdbId="tt0806910"
- **THEN** the item SHALL include `<newznab:attr name="imdb" value="tt0806910"/>` (note: attribute name is "imdb", not "imdbid")

#### Scenario: Item with tmdbid attribute
- **WHEN** a search result item has TmdbId=2116
- **THEN** the item SHALL include `<newznab:attr name="tmdbid" value="2116"/>`

#### Scenario: Item without media IDs
- **WHEN** a search result item has no media IDs (all null)
- **THEN** no media ID `<newznab:attr>` elements SHALL be emitted for that item

#### Scenario: Enclosure attributes
- **WHEN** an item has an enclosure
- **THEN** the enclosure SHALL have `url` (pointing to NZB download via `?t=get&id=<guid>`), `length` (size in bytes), and `type="application/x-nzb"`

### Requirement: Standard NZB download by GUID
The system SHALL respond to `GET /index/api?t=get&id=<guid>` with a minimal NZB XML file. The GUID SHALL be a base64-encoded string containing the download URL and title separated by a pipe character (`|`).

#### Scenario: Valid GUID download
- **WHEN** `?t=get&id=<base64>` is requested with a valid base64-encoded GUID
- **THEN** the response SHALL be `application/x-nzb` with a valid NZB XML containing the URL and title as XML comments

#### Scenario: Missing id parameter
- **WHEN** `?t=get` is requested without an `id` parameter
- **THEN** the system SHALL return Newznab error XML with code 200 (missing parameter)

#### Scenario: Invalid GUID encoding
- **WHEN** `?t=get&id=<invalid>` is requested with non-base64 content
- **THEN** the system SHALL return Newznab error XML with code 201 (incorrect parameter)

### Requirement: Newznab error XML format
The system SHALL return errors as XML `<error code="X" description="Y"/>` using standard Newznab error codes.

#### Scenario: Invalid API key error
- **WHEN** a request is made with an invalid or missing `apikey` parameter
- **THEN** the response SHALL be `application/xml` with `<error code="100" description="Invalid API Key"/>` and HTTP 403

#### Scenario: Missing parameter error
- **WHEN** a required parameter is missing (e.g., `t=get` without `id`)
- **THEN** the response SHALL be `<error code="200" description="Missing parameter"/>` with HTTP 400

#### Scenario: Incorrect parameter error
- **WHEN** a parameter value is malformed (e.g., invalid base64 in `id`)
- **THEN** the response SHALL be `<error code="201" description="Incorrect parameter"/>` with HTTP 400

#### Scenario: Function undefined error
- **WHEN** an unrecognized `t` parameter value is provided
- **THEN** the response SHALL be `<error code="202" description="No such function"/>` with HTTP 400

### Requirement: Search pagination parameters
The system SHALL accept `offset` (int, default 0) and `limit` (int, default 100) query parameters on all search endpoints. Paging SHALL be applied exactly once in `NewznabSearchService` after retrieving cached results and after any season/episode filtering. The RSS mapper SHALL NOT apply additional paging.

#### Scenario: Paging applied once in service
- **WHEN** `?t=tvsearch&q=Tatort&offset=20&limit=25` is requested
- **AND** the cache contains 100 total results
- **THEN** `NewznabSearchService` SHALL apply `Skip(20).Take(25)` to get items 20-44
- **AND** the RSS mapper SHALL receive exactly those 25 items and map them without further skip/take

#### Scenario: RSS response pagination metadata
- **WHEN** search results are returned with offset=20 and 100 total items
- **THEN** the RSS response SHALL have `<newznab:response offset="20" total="100"/>`

#### Scenario: TV search builds SearchCommand with TvParams
- **WHEN** `?t=tvsearch&q=Tatort&season=01&ep=05&tvdbid=83214` is requested
- **THEN** the SearchHandler SHALL build `SearchCommand(Query: "Tatort", Cat: null, Limit: null, Offset: null, Params: TvParams(Season: 1, Episode: 5, TvdbId: 83214, ImdbId: null))`

#### Scenario: Movie search builds SearchCommand with MovieParams
- **WHEN** `?t=movie&imdbid=tt0806910` is requested
- **THEN** the SearchHandler SHALL build `SearchCommand(Query: null, Cat: null, Limit: null, Offset: null, Params: MovieParams(ImdbId: "tt0806910", TmdbId: null))`

#### Scenario: General search builds SearchCommand without type params
- **WHEN** `?t=search&q=Tatort&cat=5040` is requested
- **THEN** the SearchHandler SHALL build `SearchCommand(Query: "Tatort", Cat: 5040, Limit: null, Offset: null, Params: null)`

#### Scenario: Limit exceeds max
- **WHEN** `?t=tvsearch&q=Tatort&limit=1000` is requested
- **THEN** the system SHALL cap limit to 500 before forwarding to the search pipeline

#### Scenario: Default pagination
- **WHEN** a search request omits `offset` and `limit`
- **THEN** the system SHALL forward Limit=null and Offset=null (workers apply their own defaults)

### Requirement: Search filter parameters
The system SHALL accept optional filter parameters on search endpoints: `cat` (comma-separated category IDs), `maxage` (int, days), `minsize` (long, bytes), `maxsize` (long, bytes), `extended` (int, 0 or 1), `attrs` (comma-separated attribute names).

#### Scenario: Filter parameters accepted without error
- **WHEN** `?t=search&q=Tatort&cat=5000&maxage=30&extended=1` is requested
- **THEN** the response SHALL be valid Newznab RSS XML (parameters are parsed and available for forwarding to the Search domain when ready)

#### Scenario: Invalid category ID ignored
- **WHEN** `?t=search&cat=invalid` is requested
- **THEN** the system SHALL treat the parameter as absent and return valid RSS XML

### Requirement: JSON output format
The system SHALL support `o=json` query parameter on all Newznab endpoints to return JSON instead of XML.

#### Scenario: Caps as JSON
- **WHEN** `?t=caps&o=json` is requested
- **THEN** the JSON response SHALL include `server`, `searching` (with `book-search`), and all other fields matching the XML structure

#### Scenario: Search results as JSON
- **WHEN** `?t=search&q=Tatort&o=json` is requested
- **THEN** the response SHALL be `application/json` with a JSON representation of the RSS result set

#### Scenario: Default output is XML
- **WHEN** `o` parameter is absent or set to `xml`
- **THEN** the response SHALL be `application/xml` as before

### Requirement: NewznabApiEndpoints resolves dependencies via DI
`NewznabController` SHALL be an `[ApiController]` with constructor-injected `NewznabSearchService`, `NzbService`, and `ILogger<NewznabController>`. The controller SHALL dispatch requests based on the `t` query parameter to the appropriate service method. The controller SHALL NOT instantiate services inline or resolve them from `HttpContext.RequestServices`.

#### Scenario: Controller receives services via constructor
- **WHEN** `NewznabController` is instantiated by the DI container
- **THEN** it SHALL receive `NewznabSearchService`, `NzbService`, and `ILogger<NewznabController>` via constructor injection

#### Scenario: Search dispatched to service
- **WHEN** a request with `?t=tvsearch`, `?t=movie`, or `?t=search` arrives
- **THEN** the controller SHALL delegate to `NewznabSearchService.Search(request)` and return the result

#### Scenario: NZB get dispatched to service
- **WHEN** a request with `?t=get&id=<guid>` arrives
- **THEN** the controller SHALL delegate to `NzbService.GetNzb(id)` and return the result

#### Scenario: Caps returned directly
- **WHEN** a request with `?t=caps` arrives
- **THEN** the controller SHALL return a serialized `Caps` response without service delegation

### Requirement: NewznabCategory handles TV range
`NewznabCategory.FromCat` SHALL explicitly map TV category range (5000-5999) to the TV category, in addition to the existing Movie range (2000-2999).

#### Scenario: Movie category mapped
- **WHEN** `FromCat(2040)` is called
- **THEN** the result SHALL be `NewznabCategory.Movie`

#### Scenario: TV category mapped
- **WHEN** `FromCat(5040)` is called
- **THEN** the result SHALL be `NewznabCategory.Tv`

#### Scenario: Unknown category returns null
- **WHEN** `FromCat(9999)` is called
- **THEN** the result SHALL be `null`

#### Scenario: Null category returns null
- **WHEN** `FromCat(null)` is called
- **THEN** the result SHALL be `null`

### Requirement: SearchResultCache cleanup
`SearchResultCache.GetOrAddAsync` SHALL remove pending entries in a `finally` block only. There SHALL be no redundant removal in a `catch` block. The cache key for TV searches SHALL be built from query, tvdbid, and imdbid only — season and episode SHALL NOT be part of the cache key because the Mediathek returns the same results regardless of the requested episode.

#### Scenario: Pending entry removed after success
- **WHEN** `GetOrAddAsync` completes successfully
- **THEN** the pending entry SHALL be removed from the `_pending` dictionary

#### Scenario: Pending entry removed after failure
- **WHEN** `GetOrAddAsync` throws an exception
- **THEN** the pending entry SHALL be removed from the `_pending` dictionary via the `finally` block
- **AND** the exception SHALL be rethrown

#### Scenario: TV search cache key excludes season and episode
- **WHEN** two TV searches are made for the same series but different episodes (`season=2026&ep=17` then `season=2026&ep=18`)
- **THEN** both SHALL use the same cache key (`tv:{query}:{tvdbid}:{imdbid}`)
- **AND** the second search SHALL return cached results from the first

#### Scenario: Different series use different cache keys
- **WHEN** TV searches are made for different series (different tvdbid)
- **THEN** they SHALL use different cache keys and not share cached results

### Requirement: Newznab TV search filters results by season and episode
When a TV search request includes both `season` and `ep` parameters and the cached results contain items with enriched season/episode newznab attributes, `NewznabSearchService` SHALL filter the results to only include items whose `season` and `episode` attributes match the requested values. Items without season/episode attributes SHALL be excluded when specific season/episode filtering is active. Filtering SHALL occur after cache retrieval and before pagination.

#### Scenario: Filter returns only matching episode
- **WHEN** `?t=tvsearch&q=Tatort&season=2026&ep=17` is requested
- **AND** cached results contain items with season=2026/episode=17, season=2026/episode=18, and items without season/episode
- **THEN** only items with season="2026" AND episode="17" SHALL be returned

#### Scenario: Unmatched items excluded when filtering
- **WHEN** `?t=tvsearch&q=Tatort&season=2026&ep=17` is requested
- **AND** some cached items have no season/episode attributes
- **THEN** those items SHALL NOT be included in the response

#### Scenario: No season/episode means no filtering
- **WHEN** `?t=tvsearch&q=Tatort` is requested without season or ep parameters
- **THEN** all cached results SHALL be returned (no filtering applied)

#### Scenario: Only season provided without episode
- **WHEN** `?t=tvsearch&q=Tatort&season=2026` is requested without ep
- **THEN** results SHALL be filtered to items with season="2026" (any episode)
- **AND** items without season attribute SHALL be excluded

#### Scenario: Pagination applied after filtering
- **WHEN** `?t=tvsearch&q=Tatort&season=2026&ep=17&offset=0&limit=10` is requested
- **AND** filtering reduces 150 cached results to 6 matching items
- **THEN** the response SHALL contain 6 items with `total="6"`

#### Scenario: RSS sync without episode returns all
- **WHEN** Sonarr performs an RSS sync via `?t=tvsearch&tvdbid=83214` without season or ep
- **THEN** all cached results SHALL be returned unfiltered
