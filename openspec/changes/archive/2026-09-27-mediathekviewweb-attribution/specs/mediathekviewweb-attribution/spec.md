## ADDED Requirements

### Requirement: Docs landing page feature tile
The docs landing page (`index.md`) SHALL include a feature tile crediting MediathekViewWeb as FunkArr's data source. The tile SHALL link to `https://mediathekviewweb.de/#everywhere=true`. The tile MUST exist in both DE and EN locale versions.

#### Scenario: German landing page shows MVW tile
- **WHEN** a user visits the German docs landing page
- **THEN** a feature tile is visible with title "MediathekViewWeb" and a description crediting it as the search backend with a clickable link

#### Scenario: English landing page shows MVW tile
- **WHEN** a user visits the English docs landing page
- **THEN** a feature tile is visible with title "MediathekViewWeb" and a description crediting it as the search backend with a clickable link

### Requirement: How-it-works callout box
The "How it works" page SHALL include a `:::tip` callout at the "MediathekViewWeb-Abfrage" section (step 3) explaining that MediathekViewWeb is a free community project and linking to it. The callout MUST exist in both DE and EN locale versions.

#### Scenario: German how-it-works page shows MVW callout
- **WHEN** a user reads the German "So funktioniert FunkArr" page
- **THEN** a tip callout appears at the MediathekViewWeb query section with a description and link to `https://mediathekviewweb.de/#everywhere=true`

#### Scenario: English how-it-works page shows MVW callout
- **WHEN** a user reads the English "How FunkArr Works" page
- **THEN** a tip callout appears at the MediathekViewWeb query section with a description and link to `https://mediathekviewweb.de/#everywhere=true`

### Requirement: UI sidebar attribution link
The UI sidebar footer SHALL display a "Powered by MediathekViewWeb" link that opens `https://mediathekviewweb.de/#everywhere=true` in a new tab. The link MUST be hidden when the sidebar is collapsed.

#### Scenario: Sidebar expanded shows attribution
- **WHEN** the sidebar is expanded
- **THEN** a "Powered by MediathekViewWeb" link is visible below the version number

#### Scenario: Sidebar collapsed hides attribution
- **WHEN** the sidebar is collapsed
- **THEN** the attribution link is not visible

#### Scenario: Attribution link opens in new tab
- **WHEN** a user clicks the attribution link
- **THEN** `https://mediathekviewweb.de/#everywhere=true` opens in a new browser tab
