## Purpose

Vue.js frontend pages for browsing rulesets, viewing ruleset details, and inspecting scoring history and traces.

## Requirements

### Requirement: Dashboard page
The Vue frontend SHALL render a dashboard page at route `/`. The page SHALL display the application name "FunkArr" and serve as a landing page. It SHALL include navigation to the rulesets page.

#### Scenario: Dashboard renders
- **WHEN** the user navigates to `/`
- **THEN** the page displays "FunkArr" and a link/navigation to `/rulesets`

### Requirement: RuleSet list page
The Vue frontend SHALL render a ruleset list page at route `/rulesets`. On mount, the page SHALL fetch `GET /api/rulesets` and display all registered rulesets as cards with Level 2 card styling (`hover:-translate-y-px hover:shadow-md transition-all`). Each card SHALL show the ruleSetId, topic, aliases, and media IDs (TVDB, IMDB, TMDB where present). Each card SHALL link to the detail page at `/rulesets/:id`. The page SHALL include a text search input above the card grid that filters rulesets client-side. The page SHALL include a "New RuleSet" button linking to `/rulesets/new`.

#### Scenario: List with rulesets
- **WHEN** the user navigates to `/rulesets` and 3 rulesets are registered
- **THEN** 3 ruleset cards are rendered with identity information and Level 2 hover effects

#### Scenario: Empty list
- **WHEN** the user navigates to `/rulesets` and no rulesets are registered
- **THEN** the page displays a structured empty state with an icon, title "No rulesets registered", and a call-to-action "Create your first RuleSet" linking to `/rulesets/new`

#### Scenario: Loading state
- **WHEN** the API request is in flight
- **THEN** the page displays skeleton card placeholders with shimmer animation matching the card layout shape

#### Scenario: Error state
- **WHEN** the API request fails
- **THEN** the page displays an error message

#### Scenario: Search filters by ruleSetId
- **WHEN** the user types "tat" in the search input
- **THEN** only rulesets whose ruleSetId contains "tat" are displayed

#### Scenario: Search filters by topic
- **WHEN** the user types "Tatort" in the search input
- **THEN** rulesets whose topic contains "Tatort" are displayed

#### Scenario: Search filters by alias
- **WHEN** the user types "Münster" in the search input
- **THEN** rulesets with an alias containing "Münster" are displayed

#### Scenario: Search filters by media ID
- **WHEN** the user types "83214" in the search input
- **THEN** rulesets whose tvdbId, imdbId, or tmdbId contains "83214" are displayed

#### Scenario: Search is case-insensitive
- **WHEN** the user types "tatort" in lowercase
- **THEN** rulesets with topic "Tatort" are still displayed

#### Scenario: Search with no matches
- **WHEN** the user types a search term that matches no rulesets
- **THEN** the page displays "No matching rulesets" instead of the empty state message

#### Scenario: Clear search
- **WHEN** the user clears the search input
- **THEN** all rulesets are displayed again

#### Scenario: New RuleSet button
- **WHEN** the user clicks the "New RuleSet" button
- **THEN** the browser navigates to `/rulesets/new`

#### Scenario: Card hover effect
- **WHEN** the user hovers over a ruleset card
- **THEN** the card translates up by 1px and shows a subtle shadow

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

### Requirement: RuleSet list presentation
The ruleset list view SHALL display ruleset entries as Level 2 cards on `surface-raised` background with `rounded-lg`. The ruleset ID SHALL render in `font-mono` with `brand-400` color. Topic text SHALL use `text-body`. Metadata (aliases, external IDs) SHALL use `text-secondary`.

#### Scenario: RuleSet card rendering
- **WHEN** the ruleset list loads with entries
- **THEN** each entry renders as a Level 2 card with `surface-raised` background and `rounded-lg`

#### Scenario: RuleSet ID styling
- **WHEN** a ruleset card renders
- **THEN** the ruleset ID appears in `font-mono` with `brand-400` color

#### Scenario: Hover state
- **WHEN** the user hovers over a ruleset card
- **THEN** the card lifts with `-translate-y-px` and gains a `shadow-md`

### Requirement: RuleSet detail layout

The ruleset detail view SHALL use cards with `surface-raised` background for each section (Identity, Source, Matching Rules). Section headings SHALL use `text-secondary` with `font-semibold`. Key-value grids SHALL use `text-secondary` for labels and `text-body` for values.

#### Scenario: Detail section rendering
- **WHEN** a ruleset detail loads
- **THEN** Identity, Source, and Matching Rules sections render as separate cards with `surface-raised` background

#### Scenario: Rule card styling
- **WHEN** matching rules render
- **THEN** each rule displays in a card with `surface-raised` background, rule ID in `font-mono brand-400`, and metadata in `text-secondary`

### Requirement: Scoring history table

The scoring history SHALL render as a table with `surface-elevated` header row, `text-secondary` uppercase column headers, and `surface-raised` data rows. Hover rows SHALL use `surface-elevated` background.

#### Scenario: Table header rendering
- **WHEN** the scoring history table renders
- **THEN** the header row has `surface-elevated` background with `text-secondary text-xs uppercase tracking-wider`

#### Scenario: Table row interaction
- **WHEN** the user hovers over a scoring history row
- **THEN** the row background changes to `surface-elevated`

### Requirement: Scoring detail trace styling

Scoring detail item traces SHALL use cards with colored left borders: `status-ok` for matched items and `border-default` for unmatched. Match/no-match badges SHALL use status colors on transparent backgrounds.

#### Scenario: Matched item rendering
- **WHEN** a scoring trace item has `matched: true`
- **THEN** the card has a left border in `status-ok` and a badge with `status-ok` text on `status-ok/10` background

#### Scenario: Unmatched item rendering
- **WHEN** a scoring trace item has `matched: false`
- **THEN** the card has a left border in `border-default` and a badge with `text-muted` on `surface-elevated` background

#### Scenario: Rule trace expansion
- **WHEN** the user expands a rule trace details section
- **THEN** trace entries display with colored left borders: `status-ok` for matched, `status-fail` for filterFailed, `border-default` for others

### Requirement: Navigation and layout
The Vue frontend SHALL use a consistent layout with a sidebar or header navigation. Navigation SHALL include links to Dashboard (`/`) and RuleSets (`/rulesets`). Breadcrumb-style back navigation SHALL be available on detail pages (e.g., "RuleSets > tatort > History > request-id").

#### Scenario: Navigation between pages
- **WHEN** the user clicks "RuleSets" in the navigation
- **THEN** the browser navigates to `/rulesets` without a full page reload

#### Scenario: Breadcrumb on detail page
- **WHEN** the user is on `/rulesets/tatort`
- **THEN** breadcrumbs show "RuleSets > tatort" with "RuleSets" linking back to the list

### Requirement: RuleSet detail page edit action
The ruleset detail page SHALL include an "Edit" button that navigates to `/rulesets/:id/edit`. The button SHALL be visible for all rulesets (community, local, or merged).

#### Scenario: Edit button on detail page
- **WHEN** the user views `/rulesets/tatort`
- **THEN** an "Edit" button is visible that links to `/rulesets/tatort/edit`

### Requirement: RuleSet detail page delete action
The ruleset detail page SHALL include a "Delete Local" button that is only visible when a local file exists for the ruleset (source.localPath is not null). Clicking the button SHALL show a confirmation dialog. On confirmation, it SHALL call `DELETE /api/rulesets/:id`. On success for local-only rulesets, it SHALL navigate to `/rulesets`. On success for merged rulesets, it SHALL reload the detail page (community data remains).

#### Scenario: Delete button visible for local ruleset
- **WHEN** the user views a ruleset that has a local file
- **THEN** a "Delete Local" button is visible

#### Scenario: Delete button hidden for community-only ruleset
- **WHEN** the user views a ruleset that has only a community file
- **THEN** no delete button is visible

#### Scenario: Delete local-only ruleset
- **WHEN** the user confirms deletion of a local-only ruleset
- **THEN** the API is called, the ruleset is removed, and the browser navigates to `/rulesets`

#### Scenario: Delete local overlay
- **WHEN** the user confirms deletion of a merged ruleset's local overlay
- **THEN** the API is called, the local file is removed, and the detail page reloads showing community-only data

### Requirement: Navigation updates
The sidebar navigation SHALL include links to Dashboard (`/`), RuleSets (`/rulesets`), and the builder page SHALL be accessible via the "New RuleSet" button on the list page and "Edit" button on the detail page. Breadcrumbs on the builder page SHALL show "RuleSets > New" for create or "RuleSets > {id} > Edit" for edit.

#### Scenario: Builder breadcrumb on create
- **WHEN** the user is on `/rulesets/new`
- **THEN** breadcrumbs show "RuleSets > New"

#### Scenario: Builder breadcrumb on edit
- **WHEN** the user is on `/rulesets/tatort/edit`
- **THEN** breadcrumbs show "RuleSets > tatort > Edit" with "RuleSets" and "tatort" as links
