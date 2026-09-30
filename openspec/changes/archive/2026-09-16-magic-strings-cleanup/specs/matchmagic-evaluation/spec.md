## MODIFIED Requirements

### Requirement: ItemTitleEqualsAirdate strategy
When a Rule uses strategy ItemTitleEqualsAirdate, it SHALL extract a date from the MediaItem's Title using common German date formats (dd.MM.yyyy, dd.MM.yy, "d. MMMM yyyy" with German month names). It SHALL use `CultureInfo("de-DE")` with `DateTime.TryParseExact` for parsing German month names instead of a hand-rolled month name array. It SHALL produce an EpisodeIdentification with the extracted date as the Title.

#### Scenario: Numeric date in title
- **WHEN** the item title is "heute-show vom 24.10.2024"
- **THEN** the identification SHALL have Title "2024-10-24"

#### Scenario: German month name in title
- **WHEN** the item title is "heute-show vom 16. Juli 2024"
- **THEN** the identification SHALL have Title "2024-07-16"

#### Scenario: Two-digit year
- **WHEN** the item title contains "24.10.24"
- **THEN** the identification SHALL interpret it as 2024-10-24

#### Scenario: No date in title
- **WHEN** the item title contains no recognizable date
- **THEN** the strategy SHALL return null

#### Scenario: German month parsing uses CultureInfo
- **WHEN** the date parsing code is inspected
- **THEN** it SHALL use `CultureInfo.GetCultureInfo("de-DE")` and `DateTime.TryParseExact` instead of a hand-rolled `germanMonths` string array
