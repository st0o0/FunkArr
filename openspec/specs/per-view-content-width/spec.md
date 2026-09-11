## Purpose

Per-view content width strategy removing the global max-width constraint and letting each view choose its own width for optimal content presentation.

## Requirements

### Requirement: Remove global content width constraint
The `AppLayout.vue` main content area SHALL NOT apply a global max-width constraint. The `<slot>` SHALL render within a container that provides only horizontal padding (`px-6`) and vertical padding (`py-5`), without a max-width class.

#### Scenario: AppLayout slot has no max-width
- **WHEN** AppLayout renders its slot content
- **THEN** the main content wrapper SHALL have `px-6 py-5` but no `max-w-*` or `mx-auto` class

### Requirement: Overview full-width layout
The Overview view root element SHALL NOT set a max-width, allowing the compact status/feed layout to fill the available width up to a natural content limit.

#### Scenario: Overview on wide viewport
- **WHEN** the Overview renders on a wide screen
- **THEN** the content SHALL use `max-w-3xl mx-auto`

### Requirement: Activity focused width
The Activity view root element SHALL use `max-w-3xl mx-auto` to constrain the tabbed content.

#### Scenario: Activity view width
- **WHEN** the Activity view renders
- **THEN** the content SHALL be constrained to `max-w-3xl` and centered

### Requirement: RuleSet list focused width
The RuleSetList view root element SHALL use `max-w-3xl mx-auto`.

#### Scenario: RuleSetList view width
- **WHEN** the RuleSetList view renders
- **THEN** the content SHALL be constrained to `max-w-3xl` and centered

### Requirement: RuleSet detail focused width
The RuleSetDetail view root element SHALL use `max-w-3xl mx-auto`.

#### Scenario: RuleSetDetail view width
- **WHEN** the RuleSetDetail view renders
- **THEN** the content SHALL be constrained to `max-w-3xl` and centered

### Requirement: RuleSet builder constrained width
The RuleSetBuilder view root element SHALL use `max-w-5xl mx-auto`. The two-column grid (form + search/test panel) SHALL use `grid-cols-[1fr_380px]`.

#### Scenario: RuleSetBuilder layout
- **WHEN** the RuleSetBuilder view renders
- **THEN** the content SHALL be constrained to `max-w-5xl` and centered with an asymmetric grid split

### Requirement: Scoring views focused width
The ScoringHistory and ScoringDetail views SHALL use `max-w-3xl mx-auto`.

#### Scenario: ScoringHistory view width
- **WHEN** the ScoringHistory view renders
- **THEN** the content SHALL be constrained to `max-w-3xl` and centered

#### Scenario: ScoringDetail view width
- **WHEN** the ScoringDetail view renders
- **THEN** the content SHALL be constrained to `max-w-3xl` and centered

### Requirement: Setup wizard narrow focus
The Setup view root element SHALL use `max-w-2xl mx-auto`.

#### Scenario: Setup wizard width
- **WHEN** the Setup view renders
- **THEN** the content SHALL be constrained to `max-w-2xl` and centered
