## Purpose

Per-view content width strategy removing the global max-width constraint and letting each view choose its own width for optimal content presentation.

## Requirements

### Requirement: Remove global content width constraint
The `AppLayout.vue` main content area SHALL NOT apply a global `max-w-6xl` constraint. The `<slot>` SHALL render within a container that provides only horizontal padding (`px-8`) and vertical padding (`py-6`), without a max-width class.

#### Scenario: AppLayout slot has no max-width
- **WHEN** AppLayout renders its slot content
- **THEN** the main content wrapper SHALL have `px-8 py-6` but no `max-w-*` or `mx-auto` class

### Requirement: Dashboard full-width layout
The Home (Dashboard) view root element SHALL NOT set a max-width, allowing content to fill the available width.

#### Scenario: Dashboard on wide viewport
- **WHEN** the Dashboard renders on a wide screen
- **THEN** the grid of widgets SHALL expand to fill the full content area

### Requirement: Queue focused width
The Queue view root element SHALL use `max-w-4xl mx-auto` to center the download cards in a focused column.

#### Scenario: Queue view width
- **WHEN** the Queue view renders
- **THEN** the content SHALL be constrained to `max-w-4xl` and centered

### Requirement: History full-width table
The History view root element SHALL NOT set a max-width, allowing the table to use the full available width for its columns.

#### Scenario: History view on wide viewport
- **WHEN** the History table renders on a wide screen
- **THEN** the table SHALL expand to fill the available width

### Requirement: RuleSet list focused width
The RuleSetList view root element SHALL use `max-w-4xl mx-auto` to center the list cards.

#### Scenario: RuleSetList view width
- **WHEN** the RuleSetList view renders
- **THEN** the content SHALL be constrained to `max-w-4xl` and centered

### Requirement: RuleSet detail focused width
The RuleSetDetail view root element SHALL use `max-w-4xl mx-auto`.

#### Scenario: RuleSetDetail view width
- **WHEN** the RuleSetDetail view renders
- **THEN** the content SHALL be constrained to `max-w-4xl` and centered

### Requirement: RuleSet builder full-width layout
The RuleSetBuilder view root element SHALL NOT set a max-width. The two-column grid (form + debugger) SHALL use `grid-cols-[1fr_380px]` to give the form panel more space than the debugger.

#### Scenario: RuleSetBuilder layout
- **WHEN** the RuleSetBuilder view renders
- **THEN** the content SHALL fill the available width with an asymmetric grid split

### Requirement: Scoring history full-width table
The ScoringHistory view root element SHALL NOT set a max-width, allowing the scoring table to use the full available width.

#### Scenario: ScoringHistory view on wide viewport
- **WHEN** the ScoringHistory table renders
- **THEN** the table SHALL expand to fill the available width

### Requirement: Scoring detail constrained width
The ScoringDetail view root element SHALL use `max-w-5xl mx-auto`.

#### Scenario: ScoringDetail view width
- **WHEN** the ScoringDetail view renders
- **THEN** the content SHALL be constrained to `max-w-5xl` and centered

### Requirement: Setup wizard narrow focus
The Setup view root element SHALL use `max-w-2xl mx-auto` to create a focused, narrow wizard experience.

#### Scenario: Setup wizard width
- **WHEN** the Setup view renders
- **THEN** the content SHALL be constrained to `max-w-2xl` and centered
