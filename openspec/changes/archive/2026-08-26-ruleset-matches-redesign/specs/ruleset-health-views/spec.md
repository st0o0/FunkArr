## ADDED Requirements

### Requirement: Fleet overview dashboard
The rulesets list view SHALL display a health dashboard section above the ruleset list, showing stat cards for total rulesets, average match rate, attention count (rulesets with matchRate < 0.75), and total unmatched items. Data SHALL be fetched from `GET /api/v1/rulesets/stats`.

#### Scenario: Dashboard with data
- **WHEN** the user navigates to `/#/rulesets` and fleet stats are available
- **THEN** the view SHALL display four stat cards with total rulesets, avg match rate (percentage), attention count, and total unmatched items

#### Scenario: Dashboard loading
- **WHEN** fleet stats are being fetched
- **THEN** the stat cards SHALL display loading placeholders

#### Scenario: Dashboard error
- **WHEN** the fleet stats endpoint fails
- **THEN** the dashboard section SHALL be hidden and the ruleset list SHALL still display

### Requirement: Enriched ruleset list cards
Each ruleset in the list SHALL be displayed as a card (not a table row) showing: topic name, source badge, rule count, channel badges, inline match rate progress bar, unmatched item count, status indicator (green dot for healthy, amber warning for needsAttention), and last activity timestamp.

#### Scenario: Healthy ruleset card
- **WHEN** a ruleset has matchRate >= 0.75
- **THEN** the card SHALL show a green status indicator and the match rate progress bar in the default color

#### Scenario: Attention-needed ruleset card
- **WHEN** a ruleset has matchRate < 0.75
- **THEN** the card SHALL show an amber warning indicator and the match rate progress bar in amber

#### Scenario: No match data card
- **WHEN** a ruleset has never been searched
- **THEN** the card SHALL show "--" for match rate and "No data" for last activity

#### Scenario: Sort options
- **WHEN** the user views the ruleset list
- **THEN** they SHALL be able to sort by: match rate ascending (worst first, default), name alphabetically, last activity (most recent first)

#### Scenario: Filter options
- **WHEN** the user views the ruleset list
- **THEN** they SHALL be able to filter by: source (community/local/generated), status (healthy/needs attention), and text search on topic name

### Requirement: Ruleset detail sub-tabs
The ruleset detail view SHALL organize information into sub-tabs: Rules (default), Match History, Unmatched, and Coverage.

#### Scenario: Rules sub-tab
- **WHEN** the user views the Rules sub-tab
- **THEN** the view SHALL display enriched rule cards with per-rule match stats, recent matched episode names (last 5), and dead rule warnings

#### Scenario: Match History sub-tab
- **WHEN** the user views the Match History sub-tab
- **THEN** the view SHALL display a timeline of recent search evaluations for this ruleset, each showing timestamp, item counts (matched/filtered/unmatched), and expandable item detail with full pipeline traces

#### Scenario: Unmatched sub-tab
- **WHEN** the user views the Unmatched sub-tab
- **THEN** the view SHALL display failure patterns grouped by type (from `GET /api/v1/rulesets/{id}/failures`) with pattern count, summary, suggestion, and expandable item list

#### Scenario: Coverage sub-tab
- **WHEN** the user views the Coverage sub-tab
- **THEN** the view SHALL display an episode coverage grid (from `GET /api/v1/rulesets/{id}/coverage`) showing matched/unmatched episodes per season

### Requirement: Ruleset detail stat cards
The ruleset detail view SHALL display stat cards above the sub-tabs showing: match rate (percentage), total items evaluated, matched count, and unmatched count. A match rate trend sparkline SHALL be rendered inline using data from `GET /api/v1/rulesets/{id}/trend`.

#### Scenario: Stat cards with trend
- **WHEN** the ruleset detail loads and trend data is available
- **THEN** the stat cards SHALL display current match rate with a sparkline showing the trend over the available snapshot period

#### Scenario: Stat cards without trend
- **WHEN** the ruleset detail loads but no trend snapshots exist
- **THEN** the stat cards SHALL display current match rate without a sparkline

### Requirement: Enriched rule cards with per-rule stats
Each rule card in the detail view SHALL display: rule index, priority, strategy name, filters, regex patterns, title rules, match count with percentage bar, last 5 matched episode names, and a dead rule warning if hitCount is 0 after 3+ total searches.

#### Scenario: Active rule with matches
- **WHEN** a rule has hitCount=156 representing 74% of all matches
- **THEN** the rule card SHALL show a progress bar at 74%, the count "156 matches", and the last 5 episode names matched by this rule

#### Scenario: Dead rule warning
- **WHEN** a rule has hitCount=0 and the ruleset has had 5 or more total searches
- **THEN** the rule card SHALL display a warning badge "Dead rule — never matches"

#### Scenario: No match data
- **WHEN** no match data exists yet (no searches performed)
- **THEN** the rule card SHALL display "No match data yet" in muted text

### Requirement: Item-level forensics inline
Matched and unmatched items in the Match History and Unmatched sub-tabs SHALL be expandable to show full pipeline forensics inline: every rule evaluation with filter checks (field, op, value, actual, passed) and strategy detail (regex pattern, input, captured value, constructed title, TVDB match).

#### Scenario: Expand matched item
- **WHEN** the user clicks a matched item in the Match History sub-tab
- **THEN** the view SHALL expand to show the full rule evaluation pipeline with the winning rule highlighted

#### Scenario: Expand unmatched item
- **WHEN** the user clicks an unmatched item in the Unmatched sub-tab
- **THEN** the view SHALL expand to show all rule evaluations with each filter check and strategy detail, highlighting the specific failure point per rule

#### Scenario: Recurrence info on unmatched items
- **WHEN** an unmatched item has been seen multiple times
- **THEN** the item SHALL display "Seen N times since <first-seen-date>"

### Requirement: Episode coverage grid
The Coverage sub-tab SHALL display a grid of episodes per season, with each cell colored to indicate matched (green), unmatched (neutral), or never-seen (muted). Hovering over a cell SHALL show the episode name and last-seen date if matched.

#### Scenario: Coverage grid for multi-season show
- **WHEN** a show has 3 seasons with varying coverage
- **THEN** the grid SHALL display one row per season with episode cells, and a per-season coverage percentage

#### Scenario: Coverage grid for single-season show
- **WHEN** a show has 1 season (e.g., Tatort with 1200+ episodes in "season 1")
- **THEN** the grid SHALL paginate or scroll horizontally to accommodate large episode counts

#### Scenario: Coverage grid for movie
- **WHEN** viewing a movie ruleset's Coverage tab
- **THEN** the view SHALL display a simple matched/not-matched indicator with first-matched and last-seen dates

### Requirement: Matches tab removal and routing
The top-level Matches tab SHALL be removed from the navigation bar. Routes `/#/matches` and `/#/matches/:id` SHALL redirect to `/#/rulesets` and the corresponding ruleset's Match History sub-tab respectively.

#### Scenario: Matches tab not shown
- **WHEN** the navigation bar renders
- **THEN** "Matches" SHALL NOT appear as a tab

#### Scenario: Redirect from old matches URL
- **WHEN** a user navigates to `/#/matches`
- **THEN** the router SHALL redirect to `/#/rulesets`

#### Scenario: Redirect from old match detail URL
- **WHEN** a user navigates to `/#/matches/:id` and the evaluation can be resolved to a ruleset
- **THEN** the router SHALL redirect to the corresponding ruleset detail view with the Match History sub-tab active
