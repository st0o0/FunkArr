## Purpose

Design token definitions for the FunkArr UI theme: zinc-based surface colors, text hierarchy, amber accent, semantic status, borders, and font stacks, integrated via Tailwind v4.

## Requirements

### Requirement: Dark-first surface color tokens

The theme SHALL define surface color tokens for layered dark surfaces with adjusted values for more contrast between layers: `surface-base` (`#0f0f11`), `surface-raised` (`#18181b`), `surface-elevated` (`#27272a`), and `surface-overlay` (`#3f3f46`). Surface tones SHALL be zinc-based neutral (no blue tint, no warm tint).

#### Scenario: Page background uses base surface
- **WHEN** any page renders
- **THEN** the page background uses the `surface-base` token (`#0f0f11`)

#### Scenario: Cards use raised surface
- **WHEN** a card component renders
- **THEN** the card background uses the `surface-raised` token (`#18181b`)

#### Scenario: Interactive elements use elevated surface
- **WHEN** a table header, input field, or hovered row renders
- **THEN** the element background uses the `surface-elevated` token (`#27272a`)

#### Scenario: Overlay elements use overlay surface
- **WHEN** a toast, dropdown, or modal renders
- **THEN** the element background uses the `surface-overlay` token (`#3f3f46`)

### Requirement: Text color hierarchy

The theme SHALL define four text color tokens with zinc-based neutral values: `text-primary` (`#fafafa`) for headings and emphasis, `text-body` (`#a1a1aa`) for body content, `text-secondary` (`#71717a`) for labels and supporting text, and `text-muted` (`#52525b`) for placeholders and disabled states.

#### Scenario: Heading text uses primary token
- **WHEN** a heading (`h1`, `h2`) renders
- **THEN** it uses the `text-primary` token (`#fafafa`)

#### Scenario: Body text uses body token
- **WHEN** paragraph or table cell text renders
- **THEN** it uses the `text-body` token (`#a1a1aa`)

#### Scenario: WCAG AA contrast on dark background
- **WHEN** `text-body` (`#a1a1aa`) is rendered on `surface-base` (`#0f0f11`)
- **THEN** the contrast ratio SHALL be at least 4.5:1

### Requirement: Brand accent colors

The theme SHALL define a simplified brand accent using two tokens: `accent` (`#f59e0b`) for primary accent (progress bars, active states, primary buttons) and `accent-dim` (`#b45309`) for hover/pressed states. The previous 7-shade brand scale (brand-300 through brand-900) SHALL be replaced by these two tokens.

#### Scenario: Primary button uses accent
- **WHEN** a primary action button renders
- **THEN** its background uses `accent` (`#f59e0b`) and text SHALL be black

#### Scenario: Progress bar uses accent
- **WHEN** a download progress bar renders
- **THEN** the filled portion uses `accent` (`#f59e0b`)

#### Scenario: No residual brand-300/400/600/700/900 references
- **WHEN** searching the codebase for brand color token usage
- **THEN** no references to `brand-300`, `brand-400`, `brand-600`, `brand-700`, or `brand-900` SHALL exist

### Requirement: Semantic status colors

The theme SHALL define status color tokens: `status-ok` (green), `status-warn` (amber), `status-fail` (red), and `status-info` (blue).

#### Scenario: Health check success indicator
- **WHEN** a health check result has status `ok`
- **THEN** the status dot uses `status-ok` (`#22C55E`)

#### Scenario: Health check failure indicator
- **WHEN** a health check result has status `fail`
- **THEN** the status dot uses `status-fail` (`#EF4444`)

### Requirement: Border tokens

The theme SHALL define `border-default` (`rgba(255 255 255 / 0.08)`) for card/divider borders, `border-subtle` (`rgba(255 255 255 / 0.04)`) for lighter separators, and `border-focus` (`rgba(255 255 255 / 0.24)`) for focus rings. Border visibility SHALL be increased from the previous 0.06/0.03 values.

#### Scenario: Card border rendering
- **WHEN** a card renders
- **THEN** it uses a 1px solid `border-default` (`rgba(255 255 255 / 0.08)`)

#### Scenario: Focus ring rendering
- **WHEN** an input receives focus
- **THEN** its border uses `border-focus` (`rgba(255 255 255 / 0.24)`)

### Requirement: Font stack tokens

The theme SHALL define `font-sans` as `'Inter', -apple-system, 'Segoe UI', system-ui, sans-serif` (adding Inter as primary) and `font-mono` (unchanged: `ui-monospace, 'Cascadia Code', 'JetBrains Mono', Menlo, monospace`).

#### Scenario: Inter as primary font
- **WHEN** the application loads
- **THEN** text SHALL render in Inter if loaded, with system fallbacks otherwise

#### Scenario: Monospace rendering unchanged
- **WHEN** a ruleset ID, regex pattern, or API path renders
- **THEN** it uses the `font-mono` token (unchanged)

### Requirement: Slate secondary color token

The theme SHALL define a `slate` token (`#64748b`) for technical/system UI elements like metadata badges and secondary indicators.

#### Scenario: Slate token available
- **WHEN** a developer writes `text-slate` in a template
- **THEN** Tailwind SHALL generate the corresponding CSS with value `#64748b`

### Requirement: Tailwind v4 integration

All tokens SHALL be defined via Tailwind v4 `@theme` directive in `style.css`, making them available as utility classes (e.g., `bg-surface-base`, `text-brand-500`).

#### Scenario: Token available as utility class
- **WHEN** a developer writes `bg-surface-raised` in a template
- **THEN** Tailwind generates the corresponding CSS with the token value
