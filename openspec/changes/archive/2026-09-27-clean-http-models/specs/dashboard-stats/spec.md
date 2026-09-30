## MODIFIED Requirements

### Requirement: Overview recent activity feed
The home page overview SHALL display the most recent downloads fetched via `GET /api/downloads/history?start=0&limit=10`. The "Recent Downloads" stats card and "View All" link SHALL navigate to the Activity page with the history tab active.

#### Scenario: Feed with recent downloads
- **WHEN** the home page loads and there are recent downloads
- **THEN** the feed SHALL display each item with status indicator, title, size, and relative date

#### Scenario: Feed with failure details
- **WHEN** a download in the feed has a failure message
- **THEN** the feed SHALL show a collapsible failure summary

#### Scenario: Empty feed
- **WHEN** the home page loads and there are no recent downloads
- **THEN** the feed SHALL show an empty state with icon and hint text

#### Scenario: Feed refresh on visibility
- **WHEN** the browser tab becomes visible again
- **THEN** the feed SHALL re-fetch recent downloads

#### Scenario: Stats card navigates to history tab
- **WHEN** the user clicks the "Recent Downloads" stats card on the home page
- **THEN** the browser SHALL navigate to `/activity?tab=history` and the Activity page SHALL open with the history tab active

#### Scenario: View All navigates to history tab
- **WHEN** the user clicks "View All" in the recent activity section
- **THEN** the browser SHALL navigate to `/activity?tab=history` and the Activity page SHALL open with the history tab active
