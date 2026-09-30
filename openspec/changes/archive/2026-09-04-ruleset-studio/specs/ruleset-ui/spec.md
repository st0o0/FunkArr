## MODIFIED Requirements

### Requirement: RuleSet list page
The Vue frontend SHALL render a ruleset list page at route `/rulesets`. On mount, the page SHALL fetch `GET /api/rulesets` and display all registered rulesets as cards. Each card SHALL show the ruleSetId, topic, aliases, and media IDs (TVDB, IMDB, TMDB where present). Each card SHALL link to the detail page at `/rulesets/:id`. The page SHALL include a text search input above the card grid that filters rulesets client-side. The page SHALL include a "New RuleSet" button linking to `/rulesets/new`.

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

## ADDED Requirements

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
