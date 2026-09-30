## MODIFIED Requirements

### Requirement: RuleSet list page
The Vue frontend SHALL render a ruleset list page at route `/rulesets`. On mount, the page SHALL fetch `GET /api/rulesets` and display all registered rulesets as cards with Level 2 card styling (`hover:-translate-y-px hover:shadow-md transition-all`). Each card SHALL show the ruleSetId, topic, resolved media name (when available, as a subtitle below or beside the topic), source type badge (community/local/merged with distinct colors), media type badge (`show` or `movie`), rule count, aliases, media IDs (TVDB, IMDB, TMDB where present), last scoring run (relative time), and match rate (percentage). Each card SHALL link to the detail page at `/rulesets/:id`. The page SHALL include a text search input above the card grid that filters rulesets client-side. The page SHALL include type filter tabs above the list. The page SHALL include a "New RuleSet" button linking to `/rulesets/new`.

#### Scenario: List with rulesets
- **WHEN** the user navigates to `/rulesets` and 3 rulesets are registered
- **THEN** 3 ruleset cards are rendered with identity information, source badges, type badges, rule counts, scoring stats, and Level 2 hover effects

#### Scenario: Card with media type badge
- **WHEN** a ruleset has `mediaType` `"show"`
- **THEN** the card SHALL display a small badge with text "show"
- **WHEN** a ruleset has `mediaType` `"movie"`
- **THEN** the card SHALL display a small badge with text "movie"
- **WHEN** a ruleset has `mediaType` `null`
- **THEN** the card SHALL display a badge with text "show" (default)

#### Scenario: Type filter tabs
- **WHEN** the user views the RuleSet list page
- **THEN** the page SHALL display three tabs: "All", "Shows", and "Movies", each showing the count of matching rulesets

#### Scenario: Type filter selects shows
- **WHEN** the user clicks the "Shows" tab
- **THEN** only rulesets with `mediaType` `"show"` or `null` SHALL be displayed

#### Scenario: Type filter selects movies
- **WHEN** the user clicks the "Movies" tab
- **THEN** only rulesets with `mediaType` `"movie"` SHALL be displayed

#### Scenario: Type filter composes with search
- **WHEN** the user selects the "Movies" tab and types a search term
- **THEN** only rulesets matching both the type filter and the search term SHALL be displayed

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
