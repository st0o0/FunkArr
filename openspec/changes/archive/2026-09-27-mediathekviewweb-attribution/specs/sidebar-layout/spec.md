## MODIFIED Requirements

### Requirement: Sidebar footer content
The sidebar footer area SHALL display, in order from top to bottom: setup link, language selector (expanded only), version number (expanded only), powered-by attribution link (expanded only), and collapse toggle.

#### Scenario: Expanded sidebar footer with attribution
- **WHEN** the sidebar is expanded
- **THEN** the footer shows setup link, language selector, version number, "Powered by MediathekViewWeb" link, and collapse toggle in that order

#### Scenario: Collapsed sidebar hides text elements
- **WHEN** the sidebar is collapsed
- **THEN** the footer shows only the setup icon and collapse toggle
