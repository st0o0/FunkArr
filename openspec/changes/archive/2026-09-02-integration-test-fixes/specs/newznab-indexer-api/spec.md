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
- **THEN** `<searching>` SHALL declare `<search available="yes" supportedParams="q"/>`, `<tv-search available="yes" supportedParams="q,season,ep,tvdbid"/>`, `<movie-search available="yes" supportedParams="q,imdbid,tmdbid"/>`, `<audio-search available="no" supportedParams=""/>`, and `<book-search available="no" supportedParams=""/>`

#### Scenario: Categories declared
- **WHEN** the caps XML is returned
- **THEN** `<categories>` SHALL include category 5000 (TV) with subcategories 5030 (SD) and 5040 (HD), and category 2000 (Movies) with subcategories 2030 (SD) and 2040 (HD)

#### Scenario: Limits declared
- **WHEN** the caps XML is returned
- **THEN** `<limits>` SHALL have `max="500"` and `default="100"`

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
