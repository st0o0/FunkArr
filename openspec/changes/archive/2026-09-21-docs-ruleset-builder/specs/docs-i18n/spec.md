## MODIFIED Requirements

### Requirement: Localized nav and sidebar
The VitePress config SHALL define separate nav and sidebar labels per locale. German nav: "Anleitung", "Konfiguration", "Regelwerke". English nav: "Guide", "Config", "Rulesets". Sidebar section titles and page titles SHALL match the locale. The Rulesets sidebar section SHALL include a "Regelwerk-Builder" entry (German) and "Builder" entry (English) linking to the respective builder pages.

#### Scenario: German nav labels
- **WHEN** the docs render in German
- **THEN** the nav shows "Anleitung", "Konfiguration", "Regelwerke"

#### Scenario: English nav labels
- **WHEN** the docs render in English
- **THEN** the nav shows "Guide", "Config", "Rulesets"

#### Scenario: German sidebar
- **WHEN** the sidebar renders in German
- **THEN** section titles and page links are in German (e.g., "Erste Schritte", "Eigene Regelwerke")

#### Scenario: German sidebar includes builder
- **WHEN** the sidebar renders in German
- **THEN** the Regelwerke section includes a "Regelwerk-Builder" entry linking to `/rulesets/builder`

#### Scenario: English sidebar includes builder
- **WHEN** the sidebar renders in English
- **THEN** the Rulesets section includes a "Builder" entry linking to `/en/rulesets/builder`
