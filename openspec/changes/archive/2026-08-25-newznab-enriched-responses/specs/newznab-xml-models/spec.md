## ADDED Requirements

### Requirement: Declarative XML model for RSS feed
The system SHALL use XmlSerializer-compatible model classes to represent the Newznab RSS feed structure. The root model `NewznabRssFeed` SHALL serialize to valid Newznab RSS 2.0 XML with the `http://www.newznab.com/DTD/2010/feeds/attributes/` namespace.

#### Scenario: Serialize empty feed
- **WHEN** a `NewznabRssFeed` with an empty items list is serialized
- **THEN** the output SHALL be valid XML with `<rss version="2.0">`, a `<channel>` with `<title>FunkArr</title>`, and `<newznab:response offset="0" total="0" />`

#### Scenario: Serialize feed with items
- **WHEN** a `NewznabRssFeed` with two items is serialized
- **THEN** the output SHALL contain two `<item>` elements each with `<title>`, `<guid>`, `<link>`, `<pubDate>`, `<enclosure>`, and `<newznab:attr>` elements

#### Scenario: Newznab namespace declaration
- **WHEN** the RSS feed is serialized
- **THEN** the root `<rss>` element SHALL include `xmlns:newznab="http://www.newznab.com/DTD/2010/feeds/attributes/"`

### Requirement: RSS item model
Each `NewznabRssItem` SHALL have properties for: Title, Guid (with IsPermaLink), Link, PubDate (RFC 2822 string), Category, Description, Enclosure (url, length, type), and a list of Attributes (name-value pairs serialized as `<newznab:attr>`).

#### Scenario: Item with all fields populated
- **WHEN** an item has title "Tatort.S01E03.Koepfe.GERMAN.720p.WEB.h264-FA", guid, link, pubDate, enclosure, and attributes for category, size, language, resolution, video, tvdbid, season, episode
- **THEN** all fields SHALL serialize to their respective XML elements and attributes

#### Scenario: Item with optional fields absent
- **WHEN** an item has no tvdbid, season, or episode attributes
- **THEN** those `<newznab:attr>` elements SHALL NOT appear in the output

### Requirement: Enclosure model
The `NewznabEnclosure` SHALL serialize with attributes `url`, `length` (bytes as long), and `type` (always "application/x-nzb").

#### Scenario: Enclosure serialization
- **WHEN** an enclosure has url="/api/fake_nzb?...", length=1288490188, type="application/x-nzb"
- **THEN** the XML SHALL produce `<enclosure url="..." length="1288490188" type="application/x-nzb" />`

### Requirement: Caps response model
The system SHALL provide a caps response either as a serializable model or as a static method, producing Newznab capabilities XML with server info, search capabilities (search, tv-search, movie-search), and categories.

#### Scenario: Caps XML structure
- **WHEN** the caps endpoint is called
- **THEN** the response SHALL contain `<caps>` with `<server>`, `<searching>`, and `<categories>` elements matching Newznab spec

### Requirement: Error response model
The system SHALL serialize Newznab error responses as `<error code="..." description="..." />`.

#### Scenario: Error serialization
- **WHEN** an error with code 100 and description "Incorrect user credentials" is serialized
- **THEN** the output SHALL be `<?xml ...?><error code="100" description="Incorrect user credentials" />`

### Requirement: Static serializer with cached instance
The `NewznabSerializer` SHALL cache the `XmlSerializer` instance as a static field to avoid repeated JIT compilation cost. Serialization SHALL produce UTF-8 XML with indentation.

#### Scenario: Multiple serializations reuse instance
- **WHEN** `NewznabSerializer.Serialize` is called multiple times
- **THEN** the same `XmlSerializer` instance SHALL be reused (no reflection overhead after first call)
