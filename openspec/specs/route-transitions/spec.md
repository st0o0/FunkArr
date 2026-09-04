## Purpose

Fade and slide-up page transition on route changes using Vue's `<Transition>` component with CSS-only implementation.

## Requirements

### Requirement: Route change transition
The `<router-view>` SHALL be wrapped in a Vue `<Transition>` component that animates page changes with a fade and subtle slide-up effect.

#### Scenario: Navigating between routes
- **WHEN** the user navigates from one route to another
- **THEN** the old page MUST fade out
- **AND** the new page MUST fade in with a slight upward slide (translate-y)

#### Scenario: Transition timing
- **WHEN** a route transition occurs
- **THEN** the total transition duration MUST be 150ms
- **AND** the mode MUST be `out-in` (old page exits before new page enters)

### Requirement: No transition on initial page load
The route transition SHALL NOT play when the application first loads.

#### Scenario: First page load
- **WHEN** the application loads for the first time
- **THEN** the initial view MUST render without a fade/slide animation

### Requirement: CSS-only transition implementation
The route transition SHALL be implemented using CSS transition classes only, without JavaScript animation hooks.

#### Scenario: Transition classes defined in CSS
- **WHEN** inspecting the transition implementation
- **THEN** it MUST use Vue's CSS transition class naming convention (e.g., `.page-enter-active`, `.page-leave-active`)
- **AND** no `@before-enter`, `@enter`, `@leave`, or other JS hook listeners SHALL be used
