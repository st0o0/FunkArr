## MODIFIED Requirements

### Requirement: Newznab RSS XML format

Search results SHALL be formatted as RSS 2.0 XML with the Newznab namespace `http://www.newznab.com/DTD/2010/feeds/attributes/`. Each item SHALL include media ID attributes when available.

#### Scenario: RSS structure

- **WHEN** results are returned
- **THEN** the XML SHALL have `<rss>` root with `<channel>` containing `<title>`, `<description>`, `<newznab:response>`, and zero or more `<item>` elements

#### Scenario: Item structure

- **WHEN** an item is in the results
- **THEN** it SHALL contain `<title>`, `<guid>`, `<link>`, `<comments>`, `<pubDate>`, `<category>`, `<description>`, `<enclosure>` with url/length/type attributes, and `<newznab:attr>` elements for category, size, and media IDs

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
