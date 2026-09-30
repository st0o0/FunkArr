## REMOVED Requirements

### Requirement: History page route
**Reason**: The standalone `/history` route is removed. History functionality is absorbed into the Activity view's History tab.
**Migration**: The `/history` route SHALL redirect to `/activity` with the History tab selected. The History.vue file SHALL be deleted.

#### Scenario: Legacy history route redirect
- **WHEN** the user navigates to `/history`
- **THEN** the router SHALL redirect to `/activity`

### Requirement: History page table layout
**Reason**: Absorbed into the Activity view's History tab (activity-view capability).
**Migration**: The history table renders in the Activity view's History tab with the same columns and styling.

#### Scenario: History table in Activity view
- **WHEN** the user views the History tab in Activity
- **THEN** a table with Title, Quality, Size, Duration, Status, Completed, and Actions columns SHALL render

### Requirement: History page empty state
**Reason**: Absorbed into the Activity view's History tab empty state.
**Migration**: Empty state renders in the History tab.

#### Scenario: Empty state in Activity History tab
- **WHEN** no download history exists
- **THEN** the History tab SHALL show "No download history"

### Requirement: History page loading state
**Reason**: Absorbed into the Activity view's History tab loading state.
**Migration**: Skeleton table displays in the History tab while loading.

#### Scenario: Loading in Activity History tab
- **WHEN** the History tab is loading data
- **THEN** a skeleton table SHALL display

### Requirement: History page pagination
**Reason**: Absorbed into the Activity view's History tab.
**Migration**: Pagination controls render within the History tab.

#### Scenario: Pagination in Activity History tab
- **WHEN** history has more than 25 items
- **THEN** pagination controls SHALL display within the History tab

### Requirement: History page retry action
**Reason**: Absorbed into the Activity view's History tab.
**Migration**: Retry button appears on failed rows in the History tab.

#### Scenario: Retry in Activity History tab
- **WHEN** the user clicks retry on a failed download in the History tab
- **THEN** the system SHALL send `POST /api/downloads/{id}/retry`

### Requirement: History page delete action
**Reason**: Absorbed into the Activity view's History tab.
**Migration**: Delete button appears on each row in the History tab.

#### Scenario: Delete in Activity History tab
- **WHEN** the user clicks delete on a history row in the History tab
- **THEN** the system SHALL send `DELETE /api/downloads/history/{id}`

### Requirement: History page category filter
**Reason**: Absorbed into the Activity view's History tab.
**Migration**: Category filter dropdown renders within the History tab.

#### Scenario: Category filter in Activity History tab
- **WHEN** the user selects a category in the History tab
- **THEN** history items SHALL be filtered by that category
