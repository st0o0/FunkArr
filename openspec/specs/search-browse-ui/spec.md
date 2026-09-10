# search-browse-ui Specification

## Purpose
TBD - created by archiving change standalone-search-view. Update Purpose after archive.
## Requirements
### Requirement: Search page route
The application SHALL register a `/search` route rendering the Search view within the AppLayout.

#### Scenario: Navigation to search page
- **WHEN** the user navigates to `/search`
- **THEN** the Search view SHALL render within the AppLayout

#### Scenario: Sidebar navigation entry
- **WHEN** the sidebar renders
- **THEN** a "Search" entry SHALL appear in the "Media" group, between Dashboard and Downloads

### Requirement: Search input with debounce
The Search page SHALL display a search input field that triggers a debounced API call after 400ms of inactivity.

#### Scenario: Debounced search
- **WHEN** the user types "tagesschau" in the search input
- **THEN** the system SHALL wait 400ms after the last keystroke before sending the API request

#### Scenario: Explicit search via button
- **WHEN** the user clicks the search button or presses Enter
- **THEN** the system SHALL send the API request immediately, canceling any pending debounce

#### Scenario: Empty query
- **WHEN** the search input is empty
- **THEN** no API request SHALL be sent and the page SHALL show an initial empty state

### Requirement: Search filters
The Search page SHALL provide optional filter inputs for channel, topic, and duration range, displayed above the results grid.

#### Scenario: Channel filter
- **WHEN** the user enters "ARD" in the channel filter
- **THEN** the search request SHALL include `channel=ARD` as a query parameter

#### Scenario: Duration range filter
- **WHEN** the user sets minimum duration to 10 and maximum to 60 (minutes)
- **THEN** the search request SHALL include `durationMin=600&durationMax=3600` (converted to seconds)

#### Scenario: Combined filters
- **WHEN** the user searches for "tatort" with channel "ARD" and duration min 60
- **THEN** the API request SHALL include all parameters: `q=tatort&channel=ARD&durationMin=3600`

### Requirement: Search URL state sync
The Search page SHALL sync search state with URL query parameters for deep linking and browser history.

#### Scenario: URL reflects search state
- **WHEN** the user searches for "tatort" with channel "ARD"
- **THEN** the URL SHALL update to `/search?q=tatort&channel=ARD`

#### Scenario: Page load with URL params
- **WHEN** the user navigates to `/search?q=tatort&channel=ARD`
- **THEN** the search input SHALL be populated with "tatort", the channel filter with "ARD", and a search SHALL execute automatically

### Requirement: Search results display as cards
The Search page SHALL display results as cards in a responsive grid layout (1 column on mobile, 2 columns on wider screens).

#### Scenario: Result card content
- **WHEN** search results are returned
- **THEN** each card SHALL display: title (heading), topic and channel (metadata), quality badge (e.g., "1080p", "720p"), duration formatted as "Xm" or "Xh Xm", aired date (relative for recent, absolute for older), size formatted in MB/GB, and description (truncated to 2 lines with expand option)

#### Scenario: Quality badge styling
- **WHEN** a result has quality 1080
- **THEN** the quality badge SHALL display "1080p"
- **WHEN** a result has quality 720
- **THEN** the quality badge SHALL display "720p"
- **WHEN** a result has quality 480 or 0
- **THEN** the quality badge SHALL display "SD" or no badge

#### Scenario: Subtitle indicator
- **WHEN** a result has `hasSubtitles` true
- **THEN** the card SHALL display a subtitle indicator icon

#### Scenario: HD indicator
- **WHEN** a result has `hasHd` true
- **THEN** the card SHALL display an HD badge or icon

### Requirement: Search results pagination
The Search page SHALL paginate results using offset-based pagination with page controls.

#### Scenario: First page of results
- **WHEN** a search returns results
- **THEN** the page SHALL display up to 20 results and show "Page 1 of N" (N computed from totalResults / pageSize)

#### Scenario: Next page navigation
- **WHEN** the user clicks the next page button
- **THEN** the system SHALL fetch results with `offset=20` and display the next page

#### Scenario: Page size
- **WHEN** results are fetched
- **THEN** the default page size SHALL be 20

#### Scenario: Total results display
- **WHEN** search results include a totalResults count
- **THEN** the page SHALL display "N results found" above the result grid

### Requirement: Search loading state
The Search page SHALL display loading skeletons while a search is in progress.

#### Scenario: Search in progress
- **WHEN** a search API call is in flight
- **THEN** skeleton card placeholders SHALL be displayed in the results area

#### Scenario: Results loaded
- **WHEN** the API response arrives
- **THEN** skeletons SHALL be replaced with result cards or the empty state

### Requirement: Search empty and error states
The Search page SHALL handle empty results and API errors.

#### Scenario: No results
- **WHEN** a search returns zero results
- **THEN** an empty state SHALL display with a search icon, "No results found", and "Try different search terms or adjust your filters."

#### Scenario: API error
- **WHEN** the search API returns an error (502 or 504)
- **THEN** an error message SHALL display "Search failed. MediathekViewWeb may be unavailable."

#### Scenario: Initial state
- **WHEN** the Search page loads without a query
- **THEN** a prompt state SHALL display "Search the Mediathek" with a subtitle encouraging the user to start typing

