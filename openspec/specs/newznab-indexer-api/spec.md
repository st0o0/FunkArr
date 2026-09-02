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

The system SHALL accept `offset` (int, default 0) and `limit` (int, default 100) query parameters on all search endpoints (`t=search`, `t=tvsearch`, `t=movie`). The system SHALL cap `limit` to the Caps-advertised max (500) before forwarding to the search pipeline. The SearchHandler SHALL build a unified `SearchCommand` for all search types.

#### Scenario: TV search builds SearchCommand with TvParams

- **WHEN** `?t=tvsearch&q=Tatort&season=01&ep=05&tvdbid=83214` is requested
- **THEN** the SearchHandler SHALL build `SearchCommand(Query: "Tatort", Cat: null, Limit: null, Offset: null, Params: TvParams(Season: 1, Episode: 5, TvdbId: 83214, ImdbId: null))`

#### Scenario: Movie search builds SearchCommand with MovieParams

- **WHEN** `?t=movie&imdbid=tt0806910` is requested
- **THEN** the SearchHandler SHALL build `SearchCommand(Query: null, Cat: null, Limit: null, Offset: null, Params: MovieParams(ImdbId: "tt0806910", TmdbId: null))`

#### Scenario: General search builds SearchCommand without type params

- **WHEN** `?t=search&q=Tatort&cat=5040` is requested
- **THEN** the SearchHandler SHALL build `SearchCommand(Query: "Tatort", Cat: 5040, Limit: null, Offset: null, Params: null)`

#### Scenario: Pagination parameters forwarded

- **WHEN** `?t=tvsearch&q=Tatort&offset=10&limit=25` is requested
- **THEN** the system SHALL forward Limit=25 and Offset=10 in the SearchCommand

#### Scenario: Default pagination

- **WHEN** a search request omits `offset` and `limit`
- **THEN** the system SHALL forward Limit=null and Offset=null (workers apply their own defaults)

#### Scenario: Limit exceeds max

- **WHEN** `?t=tvsearch&q=Tatort&limit=1000` is requested
- **THEN** the system SHALL cap limit to 500 before forwarding to the search pipeline

#### Scenario: RSS response pagination

- **WHEN** search results are returned with offset=10
- **THEN** the RSS response SHALL have `<newznab:response offset="10" total="N"/>` where N is the total number of matching items

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

### Requirement: IndexerApiEndpoints resolves dependencies via DI

`MapIndexerApi` SHALL be a parameterless extension method on `WebApplication`. The endpoint handler SHALL resolve `IActorRegistry` and `IOptions<FunkArrOptions>` via Minimal API DI parameter injection instead of receiving them as closure-captured values.

#### Scenario: Endpoint resolves actor registry from DI

- **WHEN** a search request arrives at `/index/api`
- **THEN** the handler SHALL resolve `IActorRegistry` from DI and look up the SearchManager actor

#### Scenario: API key validated from options

- **WHEN** a request arrives at `/index/api`
- **THEN** the `ApiKeyEndpointFilter` SHALL resolve the API key from `IOptions<FunkArrOptions>` via `HttpContext.RequestServices`
