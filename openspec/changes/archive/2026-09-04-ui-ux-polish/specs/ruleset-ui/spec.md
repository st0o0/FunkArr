## MODIFIED Requirements

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

#### Scenario: Card hover effect
- **WHEN** the user hovers over a ruleset card
- **THEN** the card translates up by 1px and shows a subtle shadow

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
