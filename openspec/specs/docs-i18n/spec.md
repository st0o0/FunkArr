## Purpose

VitePress documentation site internationalization with German as default locale and English under `/en/` subfolder.

## Requirements

### Requirement: VitePress locale configuration
The VitePress config SHALL define two locales: `root` (German, label "Deutsch", lang "de") and `en` (English, label "English", lang "en"). The locale switcher SHALL appear automatically in the nav bar.

#### Scenario: German as default locale
- **WHEN** a user navigates to `/funkarr/getting-started`
- **THEN** the page renders in German

#### Scenario: English locale path
- **WHEN** a user navigates to `/funkarr/en/getting-started`
- **THEN** the page renders in English

#### Scenario: Locale switcher visible
- **WHEN** any docs page renders
- **THEN** a locale switcher is visible in the nav bar with options "Deutsch" and "English"

### Requirement: German documentation content
All existing documentation pages SHALL be translated to German and placed at the root level (replacing the current English content): `index.md`, `getting-started.md`, `configuration.md`, `rulesets/index.md`, `rulesets/catalog.md`, `rulesets/custom.md`. All new ruleset guide pages SHALL also exist in German.

#### Scenario: Getting started in German
- **WHEN** a user visits `/funkarr/getting-started`
- **THEN** the page content is in German

#### Scenario: All pages have German versions
- **WHEN** the docs site builds
- **THEN** every page accessible under `/funkarr/en/` also exists under `/funkarr/`

### Requirement: English documentation content
All documentation pages SHALL be maintained in English under the `en/` subfolder, mirroring the German root structure.

#### Scenario: English getting started
- **WHEN** a user visits `/funkarr/en/getting-started`
- **THEN** the page content is in English

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

### Requirement: Landing page feature tiles
The docs landing page SHALL include feature tiles for all key capabilities. Both DE and EN locale pages MUST include a MediathekViewWeb attribution tile alongside the existing six tiles.

#### Scenario: German landing page has 7 feature tiles
- **WHEN** a user visits the German docs landing page
- **THEN** 7 feature tiles are displayed including the MediathekViewWeb tile

#### Scenario: English landing page has 7 feature tiles
- **WHEN** a user visits the English docs landing page
- **THEN** 7 feature tiles are displayed including the MediathekViewWeb tile

### Requirement: How-it-works page content
Both DE and EN versions of the how-it-works page MUST include a tip callout in the MediathekViewWeb query section crediting the project.

#### Scenario: German how-it-works includes MVW callout
- **WHEN** a user reads the German how-it-works page section 3
- **THEN** a tip callout with MediathekViewWeb credit and link is shown

#### Scenario: English how-it-works includes MVW callout
- **WHEN** a user reads the English how-it-works page section 3
- **THEN** a tip callout with MediathekViewWeb credit and link is shown

### Requirement: No AT/CH docs locales
The documentation site SHALL NOT include Austrian or Swiss dialect variants. Dialect content is limited to the Vue.js frontend.

#### Scenario: Only two doc locales
- **WHEN** the VitePress i18n config is defined
- **THEN** only `root` (de) and `en` locales exist
