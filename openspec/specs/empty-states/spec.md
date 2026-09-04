## Purpose

Consistent empty state pattern for all views: icon, title, and description in a centered vertical stack replacing raw text placeholders.

## Requirements

### Requirement: Empty state visual pattern
Every view that can display an empty collection SHALL render a consistent empty state composed of three elements in vertical stack, centered within the container:
1. An SVG icon (32px, `text-text-muted` color) drawn from the existing inline icon set used in the sidebar navigation.
2. A title line (`text-text-secondary text-sm font-medium`) stating what is absent.
3. A description line (`text-text-muted text-xs`) explaining what would appear here and how to trigger it.

The empty state block SHALL be vertically padded (`py-10`) and horizontally centered (`text-center`).

#### Scenario: Empty state renders all three visual elements
- **WHEN** a view's data collection is empty
- **THEN** the view SHALL display the icon, title, and description in a centered vertical stack instead of raw text

### Requirement: Queue empty state
The Queue view SHALL display an empty state when `items` is empty, using the download icon, title "No active downloads", and description "Downloads appear here automatically when Sonarr or Radarr trigger a search."

#### Scenario: Queue with zero items
- **WHEN** the queue stream returns zero items
- **THEN** the Queue view SHALL render the empty state with download icon, "No active downloads" title, and Sonarr/Radarr explanation

### Requirement: History empty state
The History view SHALL display an empty state when `history.items` is empty, using the clock icon, title "No download history", and description "Completed and failed downloads are recorded here."

#### Scenario: History with no items
- **WHEN** the history API returns zero items
- **THEN** the History view SHALL render the empty state with clock icon, "No download history" title, and recording explanation

### Requirement: RuleSet list empty state
The RuleSet list view SHALL display an empty state when `rulesets` is empty (no search filter applied), using the ruleset icon, title "No rulesets registered", and description "Create your first ruleset to start matching media." The description SHALL include a router-link to `/rulesets/new` styled as `text-brand-400 hover:text-brand-300`.

#### Scenario: RuleSet list with no rulesets
- **WHEN** the ruleset API returns zero entries and search is empty
- **THEN** the RuleSet list SHALL render the empty state with ruleset icon, "No rulesets registered" title, and a link to create the first ruleset

### Requirement: Dashboard ActiveDownloads widget empty state
The ActiveDownloads widget SHALL display a compact empty state when there are no active or queued downloads, using a smaller icon (24px), title "No active downloads", without a description line (to keep the widget compact).

#### Scenario: ActiveDownloads with no items
- **WHEN** the queue stream returns zero processing and zero queued items
- **THEN** the ActiveDownloads widget SHALL render a compact empty state with icon and title only

### Requirement: Dashboard HealthWidget empty state preservation
The HealthWidget SHALL continue to show "Checking..." during initial load. The existing health check display is not replaced by an empty state because the widget always has content once loaded (health checks always return results).

#### Scenario: HealthWidget during initial load
- **WHEN** the health check has not yet returned
- **THEN** the HealthWidget SHALL display the loading indicator (skeleton per loading-skeletons spec), not an empty state
