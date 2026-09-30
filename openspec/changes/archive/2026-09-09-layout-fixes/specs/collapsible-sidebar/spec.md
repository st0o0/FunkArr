# collapsible-sidebar (delta)

## ADDED Requirements

### Requirement: Per-route browser tab titles

The browser tab title SHALL reflect the current page. The format SHALL be "{Page} - FunkArr" for sub-pages and "FunkArr" for the Dashboard.

#### Scenario: Queue page title

- **WHEN** user navigates to /queue
- **THEN** the browser tab SHALL show "Downloads - FunkArr"

#### Scenario: Dashboard title

- **WHEN** user navigates to /
- **THEN** the browser tab SHALL show "FunkArr"
