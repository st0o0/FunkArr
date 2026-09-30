## ADDED Requirements

### Requirement: Network routing documentation exists in both languages

The VitePress documentation SHALL contain a "Network Routes" / "Netzwerk-Routen" section in the configuration page for both DE and EN locales.

#### Scenario: User reads German configuration docs
- **WHEN** a user navigates to the German configuration page
- **THEN** they SHALL find a "Netzwerk-Routen" section with variable table, pattern explanation, and Docker Compose example

#### Scenario: User reads English configuration docs
- **WHEN** a user navigates to the English configuration page
- **THEN** they SHALL find a "Network Routes" section with the same content in English
