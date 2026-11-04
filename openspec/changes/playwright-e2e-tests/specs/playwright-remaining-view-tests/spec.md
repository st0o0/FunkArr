# playwright-remaining-view-tests Specification

## Purpose

Tests for Feed, Settings, and Setup Wizard views covering data display, filtering, pagination, log streaming, and multi-step wizard flow.

## Requirements

### Requirement: Feed page data display

The feed page SHALL show stat cards, entries table, and support filtering.

#### Scenario: Stat cards render
- **WHEN** the feed API returns overview data
- **THEN** stat cards SHALL show entries count, last crawl time, and outcome

#### Scenario: Search filters entries
- **WHEN** the user types a search query
- **THEN** the feed entries SHALL be re-fetched with the query parameter

#### Scenario: Channel filter
- **WHEN** the user selects a channel from the dropdown
- **THEN** entries SHALL be re-fetched filtered by that channel

#### Scenario: Crawl now button
- **WHEN** the user clicks "Crawl now"
- **THEN** the system SHALL call `POST /api/feed/crawl`, show a toast, and disable the button

#### Scenario: Pagination with page size
- **WHEN** there are more entries than the selected page size
- **THEN** pagination controls SHALL appear with prev/next and page size selector

#### Scenario: Empty feed
- **WHEN** the feed API returns 0 entries
- **THEN** an empty state message SHALL be visible

### Requirement: Settings page sections

The settings page SHALL render all 6 configuration sections with correct data.

#### Scenario: Download settings section
- **WHEN** settings loads
- **THEN** concurrent downloads count and schedule SHALL be visible

#### Scenario: Schedule display
- **WHEN** no download schedule is configured
- **THEN** "No schedule" SHALL be displayed

#### Scenario: Metadata cache section
- **WHEN** cache stats are loaded
- **THEN** TVDB and TMDB cache entry counts SHALL be visible

#### Scenario: Network routes section
- **WHEN** routes are loaded
- **THEN** route entries with default badge and proxy info SHALL be visible

#### Scenario: Log streaming
- **WHEN** the settings page mounts
- **THEN** the log section SHALL connect to the SSE log stream and display log entries

#### Scenario: Log level filter
- **WHEN** the user clicks the "Error" log filter button
- **THEN** only error-level log entries SHALL be visible

#### Scenario: Individual section API failure
- **WHEN** one settings API call fails but others succeed
- **THEN** only the failed section SHALL show a loading/fallback state; other sections SHALL render normally

### Requirement: Setup wizard flow

The setup wizard SHALL guide through health checks, service selection, and service configuration.

#### Scenario: Health check step
- **WHEN** the setup wizard loads
- **THEN** health check results SHALL be visible with status indicators (green/yellow/red)

#### Scenario: Health check failure blocks next
- **WHEN** any health check has "Fail" status
- **THEN** the "Next" button SHALL be disabled

#### Scenario: All health checks pass
- **WHEN** all health checks pass
- **THEN** the "Next" button SHALL be enabled

#### Scenario: Service selection step
- **WHEN** the user reaches step 1
- **THEN** checkboxes for Prowlarr, Sonarr, and Radarr SHALL be visible

#### Scenario: No services selected blocks next
- **WHEN** no services are checked
- **THEN** the "Next" button SHALL be disabled

#### Scenario: Service configuration step
- **WHEN** the user selects Sonarr and proceeds to its config step
- **THEN** URL, API Key, and FunkArr URL input fields SHALL be visible

#### Scenario: Auto-create indexer success
- **WHEN** the user fills in all fields and clicks "Create Indexer"
- **THEN** the button SHALL show a success state (green with checkmark)

#### Scenario: Auto-create indexer failure
- **WHEN** the API returns an error for create indexer
- **THEN** an error message SHALL appear below the button

#### Scenario: Copy to clipboard
- **WHEN** the user clicks a Copy button in the manual config section
- **THEN** a "Copied" toast SHALL appear

#### Scenario: Final step done
- **WHEN** the user reaches the last step and clicks "Done"
- **THEN** the browser SHALL navigate to `/`
