## REMOVED Requirements

### Requirement: Search page route
**Reason**: Standalone Search view is removed. Mediathek search functionality is absorbed into the RuleSetBuilder's right pane (builder-search-panel capability) where search results become test candidates for rule validation.
**Migration**: The `/search` route SHALL redirect to `/rulesets`. The Search nav item SHALL be removed from the sidebar. The `mediathek.ts` API module SHALL be deleted; the existing `searchMediathek` function in `rulesets.ts` is used instead.

#### Scenario: Legacy search route redirect
- **WHEN** the user navigates to `/search`
- **THEN** the router SHALL redirect to `/rulesets`

### Requirement: Search input with debounce
**Reason**: Absorbed into builder-search-panel capability.
**Migration**: Debounced search is reimplemented in the RuleSetBuilder's Search tab.

#### Scenario: Search in builder context
- **WHEN** the user wants to search the Mediathek
- **THEN** they SHALL use the Search tab in the RuleSetBuilder right pane

### Requirement: Search filters
**Reason**: Absorbed into builder-search-panel capability with channel and topic filters.
**Migration**: Channel and topic filters are available in the builder's Search tab.

#### Scenario: Filters in builder context
- **WHEN** the user wants to filter Mediathek search by channel
- **THEN** they SHALL use the channel filter in the RuleSetBuilder's Search tab

### Requirement: Search URL state sync
**Reason**: No longer applicable. Search within the builder does not need URL persistence.
**Migration**: No replacement needed.

#### Scenario: No URL sync for builder search
- **WHEN** the user searches in the builder
- **THEN** search state SHALL NOT be reflected in the URL

### Requirement: Search results display as cards
**Reason**: Absorbed into builder-search-panel with selectable candidate cards.
**Migration**: Search results display in the builder's Search tab as selectable candidate cards.

#### Scenario: Results in builder
- **WHEN** search returns results in the builder
- **THEN** results display as selectable cards with checkbox toggling

### Requirement: Search results pagination
**Reason**: Absorbed into builder-search-panel. Builder search uses a fixed limit of 20 results without pagination.
**Migration**: No pagination in builder search; 20 results is sufficient for test candidate selection.

#### Scenario: Fixed result limit
- **WHEN** the builder search returns results
- **THEN** up to 20 results SHALL be displayed without pagination

### Requirement: Search loading state
**Reason**: Absorbed into builder-search-panel.
**Migration**: Loading state is handled in the builder's Search tab.

#### Scenario: Loading in builder
- **WHEN** a search is in progress in the builder
- **THEN** a loading indicator SHALL display in the Search tab

### Requirement: Search empty and error states
**Reason**: Absorbed into builder-search-panel.
**Migration**: Empty and error states are handled in the builder's Search tab.

#### Scenario: Empty state in builder
- **WHEN** builder search returns no results
- **THEN** the Search tab SHALL show "No results found"
