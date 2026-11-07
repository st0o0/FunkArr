## ADDED Requirements

### Requirement: Feed-mode Newznab requests are tested across feed states
The contract tests for empty-criteria Newznab requests SHALL cover the combinations of feed state (empty, only TV entries, only movie entries, both) and probe type (`tvsearch`, `movie`, `search` with TV categories, `search` with movie categories). The simulated feed answer SHALL report the total across all media types and the filtered total separately, as the real feed store does.

#### Scenario: Probe for a media type the feed does not hold
- **WHEN** the simulated feed holds TV entries only (total 3, filtered total 0 for movies) and a movie probe is sent
- **THEN** the response SHALL contain exactly one item, the validation item, in the movie category

#### Scenario: Probe for a media type the feed holds
- **WHEN** the simulated feed holds TV entries only and a TV probe is sent
- **THEN** the response SHALL contain the feed entries and no validation item

#### Scenario: Total and filtered total differ
- **WHEN** the simulated feed answers with a total larger than the filtered total
- **THEN** the response `total` SHALL equal the filtered total
