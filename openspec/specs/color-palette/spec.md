## Purpose

Concrete color values for the FunkArr dark theme: zinc-based neutral surfaces, amber accent tokens, zinc text hierarchy, neutral borders, and WCAG AA compliance.

## Requirements

### Requirement: Warm neutral surface tokens
The theme SHALL define four surface color tokens with zinc-based neutral grey values:

| Token | Value |
|---|---|
| `surface-base` | `#0f0f11` |
| `surface-raised` | `#18181b` |
| `surface-elevated` | `#27272a` |
| `surface-overlay` | `#3f3f46` |

All surface tokens MUST be defined in the Tailwind `@theme` block in `style.css`.

#### Scenario: Surface tokens applied
- **WHEN** the application loads
- **THEN** `body` background-color MUST use `surface-base` (`#0f0f11`)
- **AND** all card/panel backgrounds MUST use `surface-raised` or `surface-elevated`

#### Scenario: Zinc-based neutrals
- **WHEN** inspecting any surface token's HSL value
- **THEN** the hue SHALL be near 240 (zinc family) with saturation below 10%

### Requirement: Amber brand color tokens
The theme SHALL define two accent color tokens replacing the previous 7-shade scale:

| Token | Value |
|---|---|
| `accent` | `#f59e0b` |
| `accent-dim` | `#b45309` |

The previous `brand-300`, `brand-400`, `brand-500`, `brand-600`, `brand-700`, `brand-900` tokens SHALL be removed.

#### Scenario: Accent color in interactive elements
- **WHEN** rendering a primary button
- **THEN** the background MUST use `accent` (`#f59e0b`) and text MUST be black

#### Scenario: Accent dim for hover
- **WHEN** a primary button is hovered
- **THEN** the background MUST transition to `accent-dim` (`#b45309`)

### Requirement: Neutral warm text tokens
The theme SHALL define four text color tokens with zinc-based values:

| Token | Value |
|---|---|
| `text-primary` | `#fafafa` |
| `text-body` | `#a1a1aa` |
| `text-secondary` | `#71717a` |
| `text-muted` | `#52525b` |

#### Scenario: Text tokens use zinc values
- **WHEN** inspecting any text token
- **THEN** the values MUST match the zinc family palette above

### Requirement: Border tokens with neutral focus
The theme SHALL define three border tokens:

| Token | Value |
|---|---|
| `border-default` | `rgba(255, 255, 255, 0.08)` |
| `border-subtle` | `rgba(255, 255, 255, 0.04)` |
| `border-focus` | `rgba(255, 255, 255, 0.24)` |

#### Scenario: Focus ring uses neutral white
- **WHEN** an input or interactive element receives focus
- **THEN** the focus border MUST use `border-focus` (`rgba(255, 255, 255, 0.24)`)

### Requirement: Status color tokens updated
The theme SHALL define updated status color tokens with slightly adjusted values:

| Token | Value |
|---|---|
| `status-ok` | `#4ade80` |
| `status-warn` | `#fbbf24` |
| `status-fail` | `#f87171` |
| `status-info` | `#60a5fa` |

#### Scenario: Status colors applied
- **WHEN** rendering health check indicators
- **THEN** ok MUST render as `#4ade80`, warn as `#fbbf24`, fail as `#f87171`

### Requirement: WCAG AA contrast compliance
All text-on-surface color combinations MUST meet WCAG AA contrast requirements: minimum 4.5:1 ratio for normal text, 3:1 for large text and UI components.

#### Scenario: Primary text on base surface
- **WHEN** rendering `text-primary` (`#fafafa`) on `surface-base` (`#0f0f11`)
- **THEN** the contrast ratio MUST be at least 4.5:1

#### Scenario: Body text on base surface
- **WHEN** rendering `text-body` (`#a1a1aa`) on `surface-base` (`#0f0f11`)
- **THEN** the contrast ratio MUST be at least 4.5:1

#### Scenario: Muted text on raised surface
- **WHEN** rendering `text-muted` (`#52525b`) on `surface-raised` (`#18181b`)
- **THEN** the contrast ratio MUST be at least 3:1 (UI component threshold)
