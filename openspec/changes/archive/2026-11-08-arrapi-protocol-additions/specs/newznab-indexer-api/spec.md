## MODIFIED Requirements

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
- **THEN** `<searching>` SHALL declare `<search available="yes" supportedParams="q,title,maxage,minsize,maxsize"/>`, `<tv-search available="yes" supportedParams="q,season,ep,tvdbid,imdbid,tmdbid,title,maxage,minsize,maxsize"/>`, `<movie-search available="yes" supportedParams="q,imdbid,tmdbid,title,maxage,minsize,maxsize"/>`, `<audio-search available="no" supportedParams=""/>`, and `<book-search available="no" supportedParams=""/>`

#### Scenario: Categories declared

- **WHEN** the caps XML is returned
- **THEN** `<categories>` SHALL include category 5000 (TV) with subcategories 5030 (SD) and 5040 (HD), and category 2000 (Movies) with subcategories 2030 (SD) and 2040 (HD)

#### Scenario: Limits declared

- **WHEN** the caps XML is returned
- **THEN** `<limits>` SHALL have `max="500"` and `default="100"`

### Requirement: TV search endpoint

The system SHALL respond to `GET /index/api?t=tvsearch` with Newznab RSS XML containing search results. It SHALL accept optional parameters: `tvdbid` (int), `season` (string), `ep` (string), `q` (string), `offset` (int), `limit` (int), `cat` (string), `maxage` (int), `extended` (int), `attrs` (string), `title` (string), `minsize` (long), `maxsize` (long).

#### Scenario: Filter params accepted without error

- **WHEN** `?t=tvsearch&q=Tatort&maxage=30&minsize=100000000&maxsize=5000000000&apikey=<key>` is requested
- **THEN** the response SHALL be valid Newznab RSS XML (filter params are accepted but not applied server-side)

### Requirement: Search filter parameters

The system SHALL accept optional filter parameters on search endpoints: `cat` (comma-separated category IDs), `maxage` (int, days), `minsize` (long, bytes), `maxsize` (long, bytes), `extended` (int, 0 or 1), `attrs` (comma-separated attribute names), `title` (string).

#### Scenario: Filter parameters accepted without error

- **WHEN** `?t=search&q=Tatort&cat=5000&maxage=30&title=Tatort&minsize=100000000&maxsize=5000000000&extended=1` is requested
- **THEN** the response SHALL be valid Newznab RSS XML (parameters are parsed and available for forwarding to the Search domain when ready)

### Requirement: Newznab RSS XML format

Search results SHALL be formatted as RSS 2.0 XML with the Newznab namespace `http://www.newznab.com/DTD/2010/feeds/attributes/`. Each item SHALL include media ID attributes when available. Category attributes SHALL reflect the search type.

#### Scenario: Language attribute always emitted

- **WHEN** any search result item is returned
- **THEN** the item SHALL include `<newznab:attr name="language" value="German"/>`

#### Scenario: Subs attribute when subtitles available

- **WHEN** a search result item has a non-empty SubtitleUrl
- **THEN** the item SHALL include `<newznab:attr name="subs" value="German"/>`

#### Scenario: Subs attribute omitted when no subtitles

- **WHEN** a search result item has a null or empty SubtitleUrl
- **THEN** no `subs` attribute SHALL be emitted

#### Scenario: Usenetdate attribute on feed items

- **WHEN** a feed-backed RSS item has a FirstSeenAt timestamp
- **THEN** the item SHALL include `<newznab:attr name="usenetdate" value="<RFC 822 formatted date>"/>`

#### Scenario: Usenetdate omitted on search items

- **WHEN** a search-backed RSS item is returned (not from feed)
- **THEN** no `usenetdate` attribute SHALL be emitted

### Requirement: Newznab error XML format

The system SHALL return errors as XML `<error code="X" description="Y"/>` using standard Newznab error codes.

#### Scenario: Full error code catalog

- **WHEN** examining the error code definitions
- **THEN** the system SHALL define codes 100 (Invalid API Key), 101 (Account Suspended), 102 (Insufficient Privileges), 103 (Registration Denied), 104 (Registrations Closed), 200 (Missing Parameter), 201 (Incorrect Parameter), 202 (No Such Function), 203 (Function Not Available), and 900 (Unknown Error)
