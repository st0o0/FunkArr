## MODIFIED Requirements

### Requirement: RuleSet list page
The Vue frontend SHALL render a ruleset list page at route `/rulesets`. On mount, the page SHALL fetch `GET /api/rulesets` and display all registered rulesets as cards with Level 2 card styling (`hover:-translate-y-px hover:shadow-md transition-all`). Each card SHALL show the ruleSetId, topic, resolved media name (when available, as a subtitle below or beside the topic), source type badge (community/local/merged with distinct colors), rule count, aliases, media IDs (TVDB, IMDB, TMDB where present), last scoring run (relative time), and match rate (percentage). Each card SHALL link to the detail page at `/rulesets/:id`. The page SHALL include a text search input above the card grid that filters rulesets client-side. The page SHALL include a "New RuleSet" button linking to `/rulesets/new`.

#### Scenario: List with rulesets
- **WHEN** the user navigates to `/rulesets` and 3 rulesets are registered
- **THEN** 3 ruleset cards are rendered with identity information, source badges, rule counts, scoring stats, and Level 2 hover effects

#### Scenario: Card with resolved media name
- **WHEN** a ruleset has a non-null `mediaName`
- **THEN** the card SHALL display the media name as a subtitle below the topic in `text-secondary`

#### Scenario: Card with source type badge
- **WHEN** a ruleset has `sourceType` "community"
- **THEN** the card SHALL display a badge with text "community" in a neutral color
- **WHEN** a ruleset has `sourceType` "local"
- **THEN** the card SHALL display a badge with text "local" in `brand-400` color
- **WHEN** a ruleset has `sourceType` "merged"
- **THEN** the card SHALL display a badge with text "merged" in `status-ok` color

#### Scenario: Card with rule count
- **WHEN** a ruleset has 5 matching rules
- **THEN** the card SHALL display "5 rules" in `text-secondary`

#### Scenario: Card with scoring stats
- **WHEN** a ruleset was last scored 2 hours ago with 94% match rate
- **THEN** the card SHALL display "2h ago" and "94%" in `text-secondary`

#### Scenario: Card without scoring stats
- **WHEN** a ruleset has null `lastScoringRun` and null `matchRate`
- **THEN** the scoring stats section SHALL not be rendered

#### Scenario: Search filters by media name
- **WHEN** the user types a term that matches a ruleset's `mediaName`
- **THEN** that ruleset SHALL appear in the filtered results

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
- **WHEN** the user types "Munster" in the search input
- **THEN** rulesets with an alias containing "Munster" are displayed

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

### Requirement: RuleSet list presentation
The ruleset list view SHALL display ruleset entries as Level 2 cards on `surface-raised` background with `rounded-lg`. The ruleset ID SHALL render in `font-mono` with `brand-400` color. Topic text SHALL use `text-body`. Media name SHALL use `text-secondary`. Source badge SHALL use `text-xs` with `rounded-full px-2 py-0.5` and color per source type. Rule count and scoring stats SHALL use `text-secondary text-sm`. Metadata (aliases, external IDs) SHALL use `text-secondary`.

#### Scenario: RuleSet card rendering
- **WHEN** the ruleset list loads with entries
- **THEN** each entry renders as a Level 2 card with `surface-raised` background and `rounded-lg`

#### Scenario: RuleSet ID styling
- **WHEN** a ruleset card renders
- **THEN** the ruleset ID appears in `font-mono` with `brand-400` color

#### Scenario: Source badge styling
- **WHEN** a ruleset card renders with source type
- **THEN** the badge appears as a pill with `text-xs rounded-full px-2 py-0.5`

#### Scenario: Hover state
- **WHEN** the user hovers over a ruleset card
- **THEN** the card lifts with `-translate-y-px` and gains a `shadow-md`
