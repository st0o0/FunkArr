## ADDED Requirements

### Requirement: Attribution i18n keys
All four UI locales (en, de, de-AT, de-CH) SHALL include an `attribution.poweredBy` key with locale-appropriate text for the powered-by sidebar link.

#### Scenario: English locale has attribution key
- **WHEN** the UI renders in English
- **THEN** the attribution text reads "Powered by"

#### Scenario: German locale has attribution key
- **WHEN** the UI renders in German (de)
- **THEN** the attribution text reads "Betrieben mit"

#### Scenario: Austrian locale has attribution key
- **WHEN** the UI renders in Austrian German (de-AT)
- **THEN** the attribution text reads "Betrieben mit"

#### Scenario: Swiss locale has attribution key
- **WHEN** the UI renders in Swiss German (de-CH)
- **THEN** the attribution text reads "Betriibe mit"
