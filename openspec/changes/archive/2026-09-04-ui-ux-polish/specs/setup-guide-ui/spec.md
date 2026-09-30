## MODIFIED Requirements

### Requirement: Setup guide step 1 -- self-check

The first step of the setup guide SHALL automatically run the health check (`GET /api/health/setup`) and display all check results. While the health check is loading, skeleton placeholders SHALL be displayed mimicking the health check card layout. Critical failures (any check with `"fail"` status) SHALL block progression to step 2. The step SHALL show inline fix hints for each failure.

#### Scenario: Loading state
- **WHEN** step 1 loads and the health check request is in flight
- **THEN** skeleton card placeholders with shimmer animation SHALL be displayed in place of the health check results

#### Scenario: All checks pass
- **WHEN** step 1 loads and all checks return `"ok"` or `"warn"`
- **THEN** the skeletons are replaced with result cards and the "Next" button is enabled

#### Scenario: Critical failure blocks progress
- **WHEN** step 1 loads and at least one check returns `"fail"`
- **THEN** the "Next" button is disabled and the failure message is shown with a fix hint

#### Scenario: Re-check button
- **WHEN** the user clicks "Re-check" after fixing an issue
- **THEN** the health check runs again and the results are updated

### Requirement: Setup action buttons

Primary action buttons (Next, Done) SHALL use primary button variant (`brand-600` background, white text, `brand-500` hover). Secondary buttons (Back, Re-check) SHALL use secondary button variant (`surface-elevated` background, `border-default` border, `text-body` text). Danger buttons (where applicable) SHALL use danger button variant (`status-fail/10` background, `status-fail` text, `status-fail/20` border). Disabled buttons SHALL have `opacity-40` and `cursor-not-allowed`. All buttons SHALL have `active:scale-[0.98]` press feedback.

#### Scenario: Primary button rendering
- **WHEN** the Next button is enabled
- **THEN** it renders with `brand-600` background, white text, and `active:scale-[0.98]` on click

#### Scenario: Secondary button rendering
- **WHEN** the Back button renders
- **THEN** it uses `surface-elevated` background with `border-default` and `active:scale-[0.98]` on click

#### Scenario: Disabled button rendering
- **WHEN** the Next button is disabled (health check failures)
- **THEN** it renders with `opacity-40` and `cursor-not-allowed`
