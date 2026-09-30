## MODIFIED Requirements

### Requirement: Setup wizard step indicators

Step indicators SHALL use `brand-500` background for the current step, `status-ok` background with checkmark for completed steps, and `surface-elevated` with `text-muted` for future steps. Connecting lines SHALL use `border-default`.

#### Scenario: Current step indicator
- **WHEN** the setup wizard is on step 2
- **THEN** step 2 shows a `brand-500` background circle with white text

#### Scenario: Completed step indicator
- **WHEN** step 1 is completed
- **THEN** step 1 shows a `status-ok` background circle with a white checkmark

#### Scenario: Future step indicator
- **WHEN** step 3 is not yet reached
- **THEN** step 3 shows a `surface-elevated` background circle with `text-muted` text

### Requirement: Health check result cards

Health check results SHALL display on `surface-raised` cards with colored left borders based on status: `status-ok` for ok, `status-warn` for warn, `status-fail` for fail. Status dot colors SHALL match. Fix hints for failed checks SHALL use `status-fail` text color.

#### Scenario: Healthy check rendering
- **WHEN** a health check has status `ok`
- **THEN** the card shows a `status-ok` left border and status dot

#### Scenario: Failed check with hint
- **WHEN** a health check has status `fail`
- **THEN** the card shows a `status-fail` left border and the fix hint text in `status-fail` color

### Requirement: Service selection cards

Service selection cards (Prowlarr, Sonarr, Radarr) SHALL use `surface-raised` background with `border-default` border. Selected state SHALL show `brand-500` border. Hover SHALL show `surface-elevated` background.

#### Scenario: Unselected service card
- **WHEN** a service is not selected
- **THEN** the card has `surface-raised` background and `border-default` border

#### Scenario: Selected service card
- **WHEN** a service checkbox is checked
- **THEN** the card border changes to `brand-500`

### Requirement: Service configuration table

The service configuration table (field/value pairs) SHALL use `surface-raised` background with `surface-elevated` label column. Copy buttons SHALL use secondary button styling.

#### Scenario: Configuration field rendering
- **WHEN** a service configuration step renders
- **THEN** the table has `surface-raised` background, label cells have `surface-elevated` background with `text-secondary`, and value cells use `font-mono text-body`

### Requirement: Setup action buttons

Primary action buttons (Next, Done) SHALL use `brand-600` with `brand-500` hover. Secondary buttons (Back, Re-check) SHALL use `surface-elevated` with `border-default`. Disabled buttons SHALL have `opacity-40` and `cursor-not-allowed`.

#### Scenario: Primary button rendering
- **WHEN** the Next button is enabled
- **THEN** it renders with `brand-600` background and white text

#### Scenario: Disabled button rendering
- **WHEN** the Next button is disabled (health check failures)
- **THEN** it renders with `opacity-40` and `cursor-not-allowed`

### Requirement: Info callout styling

The info callout (e.g., "use the Test button...") SHALL use `brand-900/20` background with `brand-400` text and `brand-500/30` border, replacing the blue tint.

#### Scenario: Info callout rendering
- **WHEN** a service configuration step shows the test-connection hint
- **THEN** the callout uses `brand-900/20` background with `brand-400` text
