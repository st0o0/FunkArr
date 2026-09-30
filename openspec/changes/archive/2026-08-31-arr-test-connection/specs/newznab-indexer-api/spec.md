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
- **THEN** `<categories>` SHALL include category 2000 (Movies) with subcats 2030 (SD) and 2040 (HD), and category 5000 (TV) with subcats 5030 (SD) and 5040 (HD)

#### Scenario: Limits declared
- **WHEN** the caps XML is returned
- **THEN** `<limits>` SHALL have `max="5000"` and `default="5000"`

#### Scenario: Caps as JSON
- **WHEN** `?t=caps&o=json` is requested
- **THEN** the JSON response SHALL include `server`, `searching` (with `book-search`), and all other fields matching the XML structure

### Requirement: Movie search endpoint
The system SHALL respond to `GET /index/api?t=movie` with Newznab RSS XML. It SHALL accept optional parameters: `imdbid` (string), `tmdbid` (string), `q` (string), `offset` (int), `limit` (int), `cat` (string), `maxage` (int), `extended` (int), `attrs` (string).

#### Scenario: Search by IMDB ID
- **WHEN** `?t=movie&imdbid=tt0806910` is requested
- **THEN** the response SHALL be valid Newznab RSS XML

#### Scenario: Search by TMDB ID
- **WHEN** `?t=movie&tmdbid=12345` is requested
- **THEN** the response SHALL be valid Newznab RSS XML

## ADDED Requirements

### Requirement: TMDB ID parameter binding
The system SHALL accept `tmdbid` as a query parameter on movie search requests. The parameter SHALL be bound to the `IndexerRequest` model and available for forwarding to the Search domain.

#### Scenario: tmdbid parameter accepted
- **WHEN** `?t=movie&tmdbid=550` is requested
- **THEN** the request SHALL be processed without error and the tmdbid value SHALL be available in the request model

#### Scenario: tmdbid parameter absent
- **WHEN** `?t=movie&q=Tatort` is requested without `tmdbid`
- **THEN** the request SHALL be processed normally with tmdbid as null
