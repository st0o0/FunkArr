# route-transitions (delta)

## ADDED Requirements

### Requirement: Unknown routes redirect to Dashboard

The Vue router SHALL include a catch-all route that redirects any unmatched path to the Dashboard (`/`). This prevents blank content areas when navigating to non-existent routes.

#### Scenario: Non-existent path redirects

- **WHEN** a user navigates to `/nonexistent` or `/downloads` (not a defined route)
- **THEN** the router SHALL redirect to `/`
- **AND** the Dashboard view SHALL render

#### Scenario: Valid routes unaffected

- **WHEN** a user navigates to `/queue`, `/history`, `/rulesets`, or any other defined route
- **THEN** the route SHALL render normally (catch-all does not interfere)
