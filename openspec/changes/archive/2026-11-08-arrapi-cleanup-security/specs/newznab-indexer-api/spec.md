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
- **THEN** `<searching>` SHALL declare `<search available="yes" supportedParams="q"/>`, `<tv-search available="yes" supportedParams="q,season,ep,tvdbid,imdbid,tmdbid"/>`, `<movie-search available="yes" supportedParams="q,imdbid,tmdbid"/>`, `<audio-search available="no" supportedParams=""/>`, and `<book-search available="no" supportedParams=""/>`

#### Scenario: Categories declared

- **WHEN** the caps XML is returned
- **THEN** `<categories>` SHALL include category 5000 (TV) with subcategories 5030 (SD) and 5040 (HD), and category 2000 (Movies) with subcategories 2030 (SD) and 2040 (HD)

#### Scenario: Limits declared

- **WHEN** the caps XML is returned
- **THEN** `<limits>` SHALL have `max="500"` and `default="100"`
