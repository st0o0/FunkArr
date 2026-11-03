## ADDED Requirements

### Requirement: Feed page stat cards
The /feed page SHALL display four stat cards at the top: Total Entries (all-time count), Last Crawl (delta count + relative time), Unmatched (entries without ruleset), and Status (crawl outcome + failure count). A "Crawl now" button SHALL appear in the page header.

#### Scenario: Stat cards after successful crawl
- **WHEN** user navigates to /feed after a completed crawl
- **THEN** stat cards show: total entry count, "+N new" with relative time for last crawl, unmatched count, and "completed" status

#### Scenario: Stat cards with consecutive failures
- **WHEN** the feed has consecutiveFailures > 0
- **THEN** the Status card displays the failure count prominently

#### Scenario: Stat cards with gap warning
- **WHEN** lastGap is true
- **THEN** the Status card indicates a gap occurred in the last crawl

### Requirement: Feed page filter bar
The /feed page SHALL display a filter bar below the stat cards with: a text search input, a channel dropdown, a type dropdown (Show/Movie/All), and a quality dropdown.

#### Scenario: Filter by channel
- **WHEN** user selects a channel from the dropdown
- **THEN** the entry table updates to show only entries from that channel and pagination reflects the filtered count

#### Scenario: Filter by type
- **WHEN** user selects "Show" or "Movie" from the type dropdown
- **THEN** only entries of that media type are displayed

#### Scenario: Text search
- **WHEN** user types a search term
- **THEN** entries are filtered by title or topic containing the term (case-insensitive) after a 300ms debounce

#### Scenario: Combined filters
- **WHEN** user sets multiple filters (e.g. channel=ARD + type=Show + search="Tatort")
- **THEN** all filters are applied together (AND logic) and the filtered total reflects the intersection

#### Scenario: Clear filters
- **WHEN** user clears all filters
- **THEN** the full unfiltered entry list is shown with total count restored

### Requirement: Feed page paginated entry table
The /feed page SHALL display entries in a paginated table with columns: Time (relative), Title (with S##E## badge if present), Channel, Quality, Type.

#### Scenario: Default page load
- **WHEN** user navigates to /feed with no filters
- **THEN** the first page of entries is shown with default page size (25), sorted by firstSeenAt descending

#### Scenario: Page navigation
- **WHEN** user clicks "Next"
- **THEN** the next page of entries loads and the page indicator updates

#### Scenario: Page size selector
- **WHEN** user changes page size from 25 to 50 or 100
- **THEN** the table reloads with the new page size, resets to page 1, and the selection persists in localStorage
