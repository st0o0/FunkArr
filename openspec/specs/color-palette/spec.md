## Purpose

Concrete color values for the FunkArr dark theme: warm neutral surfaces, amber brand accents, neutral text hierarchy, amber-tinted borders, and WCAG AA compliance.

## Requirements

### Requirement: Warm neutral surface tokens
The theme SHALL define four surface color tokens with neutral dark grey values (no blue tint):

| Token | Value |
|---|---|
| `surface-base` | `#16181d` |
| `surface-raised` | `#1e2028` |
| `surface-elevated` | `#272a33` |
| `surface-overlay` | `#31353f` |

All surface tokens MUST be defined in the Tailwind `@theme` block in `style.css`.

#### Scenario: Surface tokens applied
- **WHEN** the application loads
- **THEN** `body` background-color MUST use `surface-base`
- **AND** all card/panel backgrounds MUST use `surface-raised` or `surface-elevated`

#### Scenario: No blue tint in surfaces
- **WHEN** inspecting any surface token's HSL value
- **THEN** the saturation component MUST be below 15% (neutral, not navy-tinted)

### Requirement: Amber brand color tokens
The theme SHALL define four brand color tokens using the amber family:

| Token | Value |
|---|---|
| `brand-400` | `#fbbf24` |
| `brand-500` | `#f59e0b` |
| `brand-600` | `#d97706` |
| `brand-900` | `#451a03` |

All brand tokens MUST be defined in the Tailwind `@theme` block. No blue brand color values SHALL remain.

#### Scenario: Brand color in interactive elements
- **WHEN** rendering a primary button
- **THEN** the background MUST use `brand-600` and text MUST be white

#### Scenario: Brand color in active navigation
- **WHEN** a navigation item is active
- **THEN** it MUST use `brand-400` for its accent color (text or border)

#### Scenario: No residual blue brand references
- **WHEN** searching the `style.css` file for brand color definitions
- **THEN** no blue color values (e.g., `#63b3ed`, `#4299e1`, `#3182ce`, `#2b6cb0`, `#1a365d`) SHALL be present

### Requirement: Neutral warm text tokens
The theme SHALL define four text color tokens with no blue tint:

| Token | Value |
|---|---|
| `text-primary` | `#f0f0f0` |
| `text-body` | `#b8bcc6` |
| `text-secondary` | `#7d8494` |
| `text-muted` | `#4a4f5c` |

#### Scenario: Text tokens replace blue-tinted values
- **WHEN** inspecting any text token
- **THEN** the value MUST NOT match the previous blue-tinted values (`#f0f2f8`, `#c8cee0`, `#8892ab`, `#4e576e`)

### Requirement: Border tokens with amber focus
The theme SHALL define three border tokens:

| Token | Value |
|---|---|
| `border-default` | `rgba(255, 255, 255, 0.07)` |
| `border-subtle` | `rgba(255, 255, 255, 0.035)` |
| `border-focus` | `rgba(251, 191, 36, 0.4)` |

#### Scenario: Focus ring uses amber
- **WHEN** an input or interactive element receives focus
- **THEN** the focus border MUST use `border-focus` (amber-tinted, not blue-tinted)

### Requirement: Status color tokens unchanged
The theme SHALL retain the existing status color tokens without modification:

| Token | Value |
|---|---|
| `status-ok` | `#48bb78` |
| `status-warn` | `#ecc94b` |
| `status-fail` | `#fc8181` |
| `status-info` | `#63b3ed` |

#### Scenario: Status colors preserved
- **WHEN** rendering health check indicators
- **THEN** ok MUST render as `#48bb78`, warn as `#ecc94b`, fail as `#fc8181`

### Requirement: WCAG AA contrast compliance
All text-on-surface color combinations MUST meet WCAG AA contrast requirements: minimum 4.5:1 ratio for normal text, 3:1 for large text and UI components.

#### Scenario: Primary text on base surface
- **WHEN** rendering `text-primary` (#f0f0f0) on `surface-base` (#16181d)
- **THEN** the contrast ratio MUST be at least 4.5:1

#### Scenario: Muted text on raised surface
- **WHEN** rendering `text-muted` (#4a4f5c) on `surface-raised` (#1e2028)
- **THEN** the contrast ratio MUST be at least 3:1 (UI component threshold, as muted text is used for labels)

#### Scenario: Brand text on dark surface
- **WHEN** rendering `brand-400` (#fbbf24) text on `surface-base` (#16181d)
- **THEN** the contrast ratio MUST be at least 4.5:1
