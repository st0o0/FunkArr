## ADDED Requirements

### Requirement: Capabilities endpoint
The system SHALL respond to `GET /index/api?t=caps` with a Newznab capabilities XML document declaring supported search types and categories.

#### Scenario: Caps response structure
- **WHEN** `?t=caps` is requested
- **THEN** the response SHALL be `application/xml` with root element `<caps>` containing `<limits>`, `<registration>`, `<searching>`, and `<categories>`

#### Scenario: Search types declared
- **WHEN** the caps XML is returned
- **THEN** `<searching>` SHALL declare `<search available="yes" supportedParams="q"/>`, `<tv-search available="yes" supportedParams="q,season,ep,tvdbid"/>`, `<movie-search available="yes" supportedParams="q,imdbid"/>`, and `<audio-search available="no"/>`

#### Scenario: Categories declared
- **WHEN** the caps XML is returned
- **THEN** `<categories>` SHALL include category 2000 (Movies) with subcats 2030 (SD) and 2040 (HD), and category 5000 (TV) with subcats 5030 (SD) and 5040 (HD)

#### Scenario: Limits declared
- **WHEN** the caps XML is returned
- **THEN** `<limits>` SHALL have `max="5000"` and `default="5000"`

### Requirement: TV search endpoint
The system SHALL respond to `GET /index/api?t=tvsearch` with Newznab RSS XML containing search results. It SHALL accept optional parameters: `tvdbid` (int), `season` (string), `ep` (string), `q` (string).

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
The system SHALL respond to `GET /index/api?t=search` with Newznab RSS XML. It SHALL accept parameter `q` (string).

#### Scenario: Text search
- **WHEN** `?t=search&q=Tatort` is requested
- **THEN** the response SHALL be valid Newznab RSS XML

### Requirement: Movie search endpoint
The system SHALL respond to `GET /index/api?t=movie` with Newznab RSS XML. It SHALL accept optional parameters: `imdbid` (string), `q` (string).

#### Scenario: Search by IMDB ID
- **WHEN** `?t=movie&imdbid=tt0806910` is requested
- **THEN** the response SHALL be valid Newznab RSS XML

### Requirement: Unknown function type
The system SHALL return HTTP 404 for unrecognized `t` parameter values.

#### Scenario: Unknown t parameter
- **WHEN** `?t=unknown` is requested
- **THEN** the system SHALL return HTTP 404

### Requirement: Newznab RSS XML format
Search results SHALL be formatted as RSS 2.0 XML with the Newznab namespace `http://www.newznab.com/DTD/2010/feeds/attributes/`.

#### Scenario: RSS structure
- **WHEN** results are returned
- **THEN** the XML SHALL have `<rss>` root with `<channel>` containing `<title>`, `<description>`, `<newznab:response>`, and zero or more `<item>` elements

#### Scenario: Item structure
- **WHEN** an item is in the results
- **THEN** it SHALL contain `<title>`, `<guid>`, `<link>`, `<comments>`, `<pubDate>`, `<category>`, `<description>`, `<enclosure>` with url/length/type attributes, and `<newznab:attr>` elements for category and season

#### Scenario: Enclosure attributes
- **WHEN** an item has an enclosure
- **THEN** the enclosure SHALL have `url` (pointing to fake NZB download), `length` (size in bytes), and `type="application/x-nzb"`

### Requirement: Fake NZB download endpoint
The system SHALL respond to `GET /index/api/nzb?url=<base64>&title=<base64>` with a minimal NZB XML file containing the download URL and title as XML comments.

#### Scenario: Valid NZB download
- **WHEN** the endpoint receives base64-encoded URL and title
- **THEN** the response SHALL be `application/x-nzb` with a valid NZB XML containing `<!-- title -->` and `<!-- url -->` comments

#### Scenario: Invalid base64
- **WHEN** the URL or title parameter is not valid base64
- **THEN** the system SHALL return HTTP 400

#### Scenario: NZB XML structure
- **WHEN** a valid NZB is generated
- **THEN** it SHALL contain `<nzb>` root with `<file>` containing `<groups>` and `<segments>`, and the URL/title in XML comments
