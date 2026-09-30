# setup-guide-ui (delta)

## ADDED Requirements

### Requirement: Daily series type guidance

The Sonarr configuration step SHALL include a tip explaining that shows with date-based episodes need "Daily" series type in Sonarr for automatic import.

#### Scenario: Sonarr step shows daily type tip

- **WHEN** the Sonarr configuration step is displayed
- **THEN** a tip SHALL be visible explaining that date-based shows (e.g., Die Sendung mit der Maus) need "Daily" series type in Sonarr
