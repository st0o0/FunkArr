## Purpose

Micro-interaction patterns for hover, press, progress, and status change animations across the FunkArr UI.

## Requirements

### Requirement: Hover lift on list-item cards
All Level 2 "list item" cards (RuleSet list items, Queue cards) SHALL apply `hover:-translate-y-px hover:shadow-md` with `transition-all duration-200 ease-out` to create a subtle lift effect on hover.

#### Scenario: User hovers over a RuleSet list item
- **WHEN** the user hovers over a RuleSet list card
- **THEN** the card SHALL translate upward by 1px and gain a medium shadow with a 200ms ease-out transition

#### Scenario: User moves mouse away from list item
- **WHEN** the user stops hovering over a list-item card
- **THEN** the card SHALL return to its original position and shadow with the same 200ms transition

### Requirement: Press feedback on buttons
All button elements (primary, secondary, ghost, danger variants) SHALL apply `active:scale-[0.98]` with `transition-transform duration-100` to provide tactile press feedback.

#### Scenario: User clicks a primary button
- **WHEN** the user presses down on any button
- **THEN** the button SHALL scale to 98% during the active state

#### Scenario: User releases button
- **WHEN** the user releases the button
- **THEN** the button SHALL return to 100% scale with a 100ms transition

### Requirement: Smooth progress bar transitions
All progress bars (QueueCard download progress, ActiveDownloads widget progress) SHALL use `transition-all duration-700 ease-out` for width changes to create a smooth fill animation.

#### Scenario: Download progress updates
- **WHEN** a download's percentage value changes
- **THEN** the progress bar width SHALL animate to the new value over 700ms with ease-out timing

### Requirement: Clickable table row hover states
All clickable table rows (History items, ScoringHistory rows) SHALL apply `hover:bg-surface-elevated/60 cursor-pointer transition-colors duration-150` to indicate interactivity.

#### Scenario: User hovers over a clickable table row
- **WHEN** the user hovers over a row in the History or ScoringHistory table
- **THEN** the row SHALL show a subtle elevated background with a 150ms color transition and pointer cursor

### Requirement: Health status dot pulse on change
Health status indicator dots in the HealthWidget SHALL play a single pulse animation (`animate-ping` lasting 600ms, once) when their status value changes between renders (e.g., from "fail" to "ok").

#### Scenario: Health check status transitions from fail to ok
- **WHEN** a health check result changes status
- **THEN** the status dot SHALL play a single ping animation over 600ms to draw attention to the change

### Requirement: Default transition timing
All CSS transitions that do not have a specific duration requirement SHALL use 150ms as the default duration. Transform-based transitions (translate, scale) SHALL use 200ms. Color-only transitions SHALL use 150ms.

#### Scenario: Unspecified transition duration
- **WHEN** a new interactive element is added without an explicit duration
- **THEN** the element SHALL use 150ms for color transitions and 200ms for transform transitions
