## Context

The FunkArr UI is a Vue 3 + Tailwind v4 SPA with six views and three components. It currently uses a light-background top-nav layout with hardcoded gray utility classes. There is no theme system, no design tokens, and no dark mode. The UI is functional but visually generic.

Tailwind v4 uses CSS-first configuration — custom theme values are defined via `@theme` in CSS, not in a JS config file. This is the right place to establish design tokens.

## Goals / Non-Goals

**Goals:**
- Define a complete color token system (surfaces, text, borders, accents, semantic)
- Replace the top-nav with a sidebar navigation layout
- Restyle all existing views against the token system
- Make dark mode the default, with a light-mode override path via CSS custom properties
- Keep the sidebar dark in both themes (brand anchor)
- Responsive: sidebar collapses to icon-only below 768px

**Non-Goals:**
- No new views or features — this is purely visual
- No component library extraction (no separate design system package)
- No animations or transitions beyond basic hover states
- No logo/icon design — text wordmark only for now
- No JS-based theme toggle — CSS `prefers-color-scheme` only for now

## Decisions

### 1. Tailwind v4 `@theme` for design tokens

Define all custom colors, font stacks, and spacing in `style.css` via `@theme`. This avoids a separate config file and keeps tokens co-located with the CSS reset.

**Alternative:** CSS custom properties without `@theme`. Rejected because Tailwind v4's `@theme` integrates directly with utility class generation — defining `--color-brand-500` in `@theme` makes `bg-brand-500` available automatically.

### 2. Sidebar layout with CSS Grid

```
body
├── grid: "sidebar main" / 64px 1fr
│
├── aside.sidebar (grid-area: sidebar)
│   ├── .sidebar-brand      — wordmark
│   ├── nav.sidebar-nav      — links
│   └── .sidebar-footer      — version
│
└── main (grid-area: main)
    ├── .breadcrumb           — contextual path
    └── .content              — page content (max-w-7xl, centered)
```

Below 768px, sidebar collapses to 48px (icons only, labels hidden via `sr-only`).

**Alternative:** Flexbox layout. Grid is cleaner for named areas and the sidebar's fixed width.

### 3. Color token naming

Flat token names scoped by role, not by Tailwind shade number:

| Token | Dark value | Purpose |
|---|---|---|
| `--color-surface-base` | slate-950 `#020617` | Page background |
| `--color-surface-raised` | slate-900 `#0F172A` | Cards, sidebar |
| `--color-surface-elevated` | slate-800 `#1E293B` | Inputs, table headers, hover states |
| `--color-border-default` | slate-700 `#334155` | Card borders, dividers |
| `--color-border-subtle` | slate-700/50 | Table row separators |
| `--color-text-primary` | slate-50 `#F8FAFC` | Headings, emphasis |
| `--color-text-body` | slate-200 `#E2E8F0` | Body text |
| `--color-text-secondary` | slate-400 `#94A3B8` | Secondary, labels |
| `--color-text-muted` | slate-600 `#475569` | Placeholders, disabled |
| `--color-brand-400` | `#2DD4BF` | Highlights, code/mono accents |
| `--color-brand-500` | `#14B8A6` | Active states, wordmark |
| `--color-brand-600` | `#0D9488` | Primary buttons |
| `--color-brand-700` | `#0F766E` | Pressed states |
| `--color-brand-900` | `#134E4A` | Subtle tint backgrounds |
| `--color-status-ok` | `#22C55E` | Success, matched |
| `--color-status-warn` | `#F59E0B` | Warning |
| `--color-status-fail` | `#EF4444` | Error, failed |
| `--color-status-info` | `#3B82F6` | Informational |

**Alternative:** Use Tailwind's built-in `teal-*` and `slate-*` directly. Rejected because semantic tokens decouple the design intent from specific shade numbers, making theme changes a single-point edit.

### 4. Typography — system stack only

```css
--font-sans: ui-sans-serif, system-ui, -apple-system, sans-serif;
--font-mono: ui-monospace, "Cascadia Code", "JetBrains Mono", Menlo, monospace;
```

No web fonts. Zero network requests for type. The system stack looks native on every OS.

### 5. Component patterns

**Cards:** `bg-surface-raised`, `border border-default`, `rounded-lg`, `p-4`. No shadows.

**Buttons:**
- Primary: `bg-brand-600 hover:bg-brand-500 text-white rounded-md px-4 py-2`
- Secondary: `bg-surface-elevated border border-default hover:border-brand-500 text-body rounded-md`
- Danger: `bg-status-fail/10 border border-status-fail/30 text-status-fail hover:bg-status-fail/20`

**Tables:** Header row `bg-surface-elevated text-secondary text-xs uppercase tracking-wider`. Rows `hover:bg-surface-elevated`. Separator `border-b border-subtle`.

**Status indicators:** Colored dots (`w-2.5 h-2.5 rounded-full`) using status tokens.

**Active nav:** Left border `border-l-2 border-brand-500` with `bg-brand-900/20` tint.

### 6. Sidebar nav items

Three top-level items matching current routes:
- Dashboard (`/`)
- RuleSets (`/rulesets`, covers nested detail/history/scoring routes)
- Setup (`/setup`)

Scoring stays nested under RuleSets (accessed via detail view, not top-level nav).

## Risks / Trade-offs

- **[Risk] Tailwind v4 `@theme` is newer, less documented** → Mitigation: The feature is stable in v4.3+, which is already in use. Fallback: plain CSS custom properties work identically.
- **[Risk] Dark-first may have contrast issues on specific data-dense views** → Mitigation: Use WCAG AA contrast ratios (4.5:1 for body text). slate-200 on slate-950 = 15.4:1 (exceeds AAA).
- **[Trade-off] No JS theme toggle** → Acceptable for v1. `prefers-color-scheme` covers the majority case. A manual toggle can be added later by swapping a `data-theme` attribute.
- **[Trade-off] System font stack means slight visual differences across OS** → Acceptable. Consistency within one user's OS matters more than cross-OS pixel-matching.
