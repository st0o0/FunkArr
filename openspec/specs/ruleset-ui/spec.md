## Purpose

Vue.js frontend pages for browsing rulesets, viewing ruleset details, and inspecting scoring history and traces.

## Requirements

### Requirement: Dashboard page
The Vue frontend SHALL render a dashboard page at route `/`. The page SHALL display the application name "FunkArr" and serve as a landing page. It SHALL include navigation to the rulesets page.

#### Scenario: Dashboard renders
- **WHEN** the user navigates to `/`
- **THEN** the page displays "FunkArr" and a link/navigation to `/rulesets`

### Requirement: RuleSet list page
The Vue frontend SHALL render a ruleset list page at route `/rulesets`. On mount, the page SHALL fetch `GET /api/rulesets` and display all registered rulesets as cards. Each card SHALL show the ruleSetId, topic, aliases, and media IDs (TVDB, IMDB, TMDB where present). Each card SHALL link to the detail page at `/rulesets/:id`.

#### Scenario: List with rulesets
- **WHEN** the user navigates to `/rulesets` and 3 rulesets are registered
- **THEN** 3 ruleset cards are rendered with identity information

#### Scenario: Empty list
- **WHEN** the user navigates to `/rulesets` and no rulesets are registered
- **THEN** the page displays an empty state message

#### Scenario: Loading state
- **WHEN** the API request is in flight
- **THEN** the page displays a loading indicator

#### Scenario: Error state
- **WHEN** the API request fails
- **THEN** the page displays an error message

### Requirement: RuleSet detail page
The Vue frontend SHALL render a ruleset detail page at route `/rulesets/:id`. On mount, the page SHALL fetch `GET /api/rulesets/:id` and display three sections: Identity, Source, and Matching Rules.

The Identity section SHALL show: topic, aliases, and media IDs (TVDB, IMDB, TMDB).

The Source section SHALL show: community file path and last modified timestamp, local file path and last modified timestamp, and the effective merge mode (community only, local only, merged, standalone).

The Matching Rules section SHALL show: default confidence, and for each rule: ID, priority, confidence override, identification strategy with its parameters, and filter conditions.

The page SHALL include a link to the scoring history at `/rulesets/:id/history`.

#### Scenario: Detail with community + local overlay
- **WHEN** the user views a ruleset that has both community and local files
- **THEN** the source section shows both file paths with timestamps and merge mode "merged"

#### Scenario: Detail with community only
- **WHEN** the user views a ruleset with only a community file
- **THEN** the source section shows only the community path and merge mode "community only"

#### Scenario: Rule display with RegexCapture strategy
- **WHEN** a rule uses RegexCapture with season and episode patterns
- **THEN** the rule card shows strategy "RegexCapture", the season regex, and the episode regex

#### Scenario: Rule display with filters
- **WHEN** a rule has filter conditions
- **THEN** the rule card shows the filter summary

#### Scenario: Not found
- **WHEN** the user navigates to `/rulesets/nonexistent` and the API returns 404
- **THEN** the page displays a "not found" message

### Requirement: Scoring history page
The Vue frontend SHALL render a scoring history page at route `/rulesets/:id/history`. On mount, the page SHALL fetch `GET /api/rulesets/:id/history` and display a table of past scoring runs. Each row SHALL show: request ID (truncated), source, query, timestamp (relative), candidate count, and matched count. Each row SHALL link to the scoring detail page.

#### Scenario: History with entries
- **WHEN** the user views scoring history with 5 past runs
- **THEN** a table with 5 rows is rendered, each linking to its detail

#### Scenario: Empty history
- **WHEN** the user views scoring history with no past runs
- **THEN** the page displays an empty state message

#### Scenario: Pagination
- **WHEN** more scoring runs exist than the default page size
- **THEN** the page provides controls to navigate to the next/previous page

### Requirement: Scoring detail page
The Vue frontend SHALL render a scoring detail page at route `/rulesets/:id/history/:requestId`. On mount, the page SHALL fetch `GET /api/rulesets/:id/history/:requestId` and display the full item trace for that scoring run. The page SHALL show the query, source, and timestamp at the top, followed by a list of item traces.

Each item trace SHALL show: candidate title, topic, channel, duration, quality, matched status, score, matched rule ID, and an expandable section showing individual rule traces with filter results and identification results.

#### Scenario: Detail with matched and unmatched items
- **WHEN** the user views a scoring detail with 10 candidates where 3 matched
- **THEN** all 10 items are shown, matched items are visually distinguished, and each shows its score

#### Scenario: Expanded rule trace
- **WHEN** the user expands an item's rule trace
- **THEN** each rule's outcome (Matched/Filtered/NoIdentification), filter trace, and identification trace are shown

#### Scenario: Not found
- **WHEN** the user navigates to a scoring detail with an unknown requestId
- **THEN** the page displays a "not found" message

### Requirement: Navigation and layout
The Vue frontend SHALL use a consistent layout with a sidebar or header navigation. Navigation SHALL include links to Dashboard (`/`) and RuleSets (`/rulesets`). Breadcrumb-style back navigation SHALL be available on detail pages (e.g., "RuleSets > tatort > History > request-id").

#### Scenario: Navigation between pages
- **WHEN** the user clicks "RuleSets" in the navigation
- **THEN** the browser navigates to `/rulesets` without a full page reload

#### Scenario: Breadcrumb on detail page
- **WHEN** the user is on `/rulesets/tatort`
- **THEN** breadcrumbs show "RuleSets > tatort" with "RuleSets" linking back to the list
