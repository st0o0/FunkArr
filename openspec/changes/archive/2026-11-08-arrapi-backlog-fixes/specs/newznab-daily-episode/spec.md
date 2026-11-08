## MODIFIED Requirements

### Requirement: Daily show episode parsing deferred to enrichment

Daily shows (e.g. Tagesschau, heute journal) use date-based episode identifiers (MMDD format). Sonarr sends `ep=1108` for November 8th. When `season >= 1900`, the filter SHALL treat `season` as a year and `ep` as MMDD, matching against `SearchResultItem.AiredAt` instead of `MatchMetadata.Episode`.

#### Scenario: MMDD episode parameter parses as integer

- **WHEN** `?t=tvsearch&season=2026&ep=1108` is requested
- **THEN** the `ep` value SHALL parse as integer 1108

#### Scenario: Daily show filter matches by airdate

- **WHEN** `season` is >= 1900 (daily show mode) and `ep` is an MMDD value
- **AND** a `SearchResultItem` has `AiredAt` matching the constructed date (year from `season`, month and day from `ep`)
- **THEN** the filter SHALL include that item in results

#### Scenario: Daily show filter excludes non-matching airdate

- **WHEN** `season` is >= 1900 and `ep` is 1108 (November 8)
- **AND** a `SearchResultItem` has `AiredAt` on November 9
- **THEN** the filter SHALL exclude that item

#### Scenario: Daily show filter handles missing AiredAt

- **WHEN** `season` is >= 1900
- **AND** a `SearchResultItem` has `AiredAt` as null
- **THEN** the filter SHALL exclude that item (no match possible)

#### Scenario: Regular season/episode filter unchanged

- **WHEN** `season` is < 1900 (regular TV show mode)
- **THEN** the filter SHALL compare `ep` against `MatchMetadata.Episode` as integer (existing behavior)

#### Scenario: Invalid MMDD value

- **WHEN** `season` is >= 1900 and `ep` cannot be decomposed into a valid month/day (e.g. 1332 = month 13)
- **THEN** the filter SHALL exclude all items (no valid date to match against)
