## MODIFIED Requirements

### Requirement: Search result items always have a valid pubDate
Every `<item>` in the Newznab RSS response SHALL have a valid RFC-822 `<pubDate>` element.

#### Scenario: Item with AiredAt
- **WHEN** a search result has `AiredAt` set to a valid timestamp
- **THEN** `<pubDate>` SHALL be the RFC-822 formatted `AiredAt` value

#### Scenario: Item without AiredAt
- **WHEN** a search result has `AiredAt` equal to null or 0
- **THEN** `<pubDate>` SHALL be the current UTC time formatted as RFC-822
- **AND** the item SHALL NOT be omitted from the feed
