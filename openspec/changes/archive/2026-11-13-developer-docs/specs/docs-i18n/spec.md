## ADDED Requirements

### Requirement: Developer group in both locale sidebars
The sidebar of each locale SHALL end with the collapsed developer group, labelled "Für Entwickler" in German and "For developers" in English, with page titles in the locale's language.

#### Scenario: German developer group
- **WHEN** the sidebar renders in German
- **THEN** the last group is "Für Entwickler" and links to pages under `/develop/`

#### Scenario: English developer group
- **WHEN** the sidebar renders in English
- **THEN** the last group is "For developers" and links to pages under `/en/develop/`
