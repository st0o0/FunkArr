## Context

FunkArr's UI (Vue 3 + Tailwind v4 + Vite) has 8 views, 5 components, and a custom dark theme defined via `@theme` tokens in `style.css`. The palette is cold navy (#0c0f1a base) with blue brand accents. All views share one `max-w-6xl` container. There are no transitions, no toast system, no skeleton loading, and no shared breadcrumb component. Every card uses identical `bg-surface-raised rounded-xl border border-border-default` styling regardless of context.

The UI has no external dependencies beyond Vue Router and Tailwind — no component library, no animation framework, no icon library.

## Goals / Non-Goals

**Goals:**
- Make navigation feel smooth and responsive (transitions, feedback)
- Replace the cold palette with a warm, *arr*-family-fitting color scheme
- Add missing UX fundamentals (loading states, action feedback, empty states)
- Create visual hierarchy through card/button variants
- Make the sidebar space-efficient with a collapsible mode

**Non-Goals:**
- Adding a component library (Headless UI, Radix, etc.)
- Adding an animation library (Motion, GSAP, etc.)
- Responsive/mobile layout (the app runs on a home server, accessed on desktop)
- Light mode / theme switching
- Redesigning the information architecture (same views, same nav structure)
- Backend changes of any kind

## Decisions

### D1: Warm amber brand color

**Choice**: Amber (#f59e0b / #fbbf24) as the primary brand color.

**Alternatives considered**:
- Keep blue, just warm the surfaces → Still feels generic and cold. Blue is also Sonarr's territory.
- Orange (#f97316) → Too close to Sonarr's identity.
- Teal/Cyan → Cold, doesn't fit the "Rundfunk" warmth.

**Rationale**: Amber is warm, distinct in the *arr* family, and works well as an accent on dark surfaces. It reads as "broadcast/signal" and pairs naturally with green (ok), red (fail), and yellow (warn) status colors without clashing.

### D2: Neutral warm surfaces (no blue tint)

**Choice**: Replace navy-tinted surfaces (#0c0f1a, #141829, etc.) with neutral dark greys (#16181d, #1e2028, etc.).

**Rationale**: The current blue tint on every surface competes with the brand color and makes the UI feel monochrome. Neutral surfaces let the amber brand and status colors stand out. Same lightness levels, just desaturated.

### D3: CSS transitions only, no animation library

**Choice**: All animations via CSS `transition` and `@keyframes`. Vue `<Transition>` for route changes and conditional renders.

**Rationale**: The animations needed (fade, slide, shimmer, scale) are all achievable with CSS. Adding a motion library for this scope would be overhead. The shimmer skeleton uses a CSS gradient animation.

### D4: Collapsible sidebar via CSS width transition

**Choice**: Sidebar toggles between 56px (icons) and 200px (full) via `transition: width 200ms ease`. Toggle button in sidebar. State stored in `localStorage('funkarr-sidebar')`.

**Alternatives considered**:
- Overlay sidebar (slides over content) → Feels mobile-centric, loses the persistent nav benefit.
- Auto-expand on hover → Frustrating when passing the mouse over the sidebar accidentally. Toggle is more intentional.

**Rationale**: Width transition is the simplest approach. The grid template changes from `grid-cols-[56px_1fr]` to `grid-cols-[200px_1fr]` and content reflows smoothly.

### D5: Toast via composable + Teleport, no store

**Choice**: `useToast()` composable with a reactive array and `<Teleport to="body">`. No Pinia/Vuex dependency.

**Rationale**: The toast state is global but ephemeral (auto-dismiss after 3s). A composable with module-level state is sufficient. Each toast is `{ id, message, variant, timeout }`. The `ToastContainer.vue` renders the stack and handles enter/leave transitions.

### D6: Per-view content width via slot/class, not nested layouts

**Choice**: `AppLayout.vue` provides a `<slot>` with no width constraint. Each view sets its own `max-w-*` class on its root `<div>`.

**Alternatives considered**:
- Named slots or layout variants → Overengineered for setting a max-width.
- Keep global max-w but make it wider → Tables still want full width.

**Rationale**: Simplest approach. Views already have a root `<div>`, just add the appropriate `max-w-*` class. Dashboard and tables omit it, forms use `max-w-3xl`, detail views use `max-w-4xl`.

### D7: Skeleton components as pure visual wrappers

**Choice**: Three skeleton variants: `SkeletonLine` (single line), `SkeletonCard` (card-shaped block), `SkeletonTable` (rows with columns). All use the same shimmer animation. Views compose them to match their real layout.

**Rationale**: The skeletons don't need to know about the data — they just mimic the visual shape. A loading view renders skeletons in place of real content. The shimmer is a CSS gradient animation on a pseudo-element.

### D8: Card hierarchy via utility classes, not wrapper components

**Choice**: Define card styles as Tailwind class patterns, not as `<Card level="1">` wrapper components.

- Level 1 (section): `bg-surface-raised rounded-xl border border-border-default` (existing)
- Level 2 (list item): `bg-surface-raised rounded-lg hover:-translate-y-px hover:shadow-md transition-all`
- Level 3 (table row): no background, `hover:bg-surface-elevated/60 transition-colors`

**Rationale**: Adding wrapper components for styling-only concerns adds indirection without benefit. The patterns are documented here and applied directly in templates. Button variants similarly use class patterns.

## Risks / Trade-offs

- **Amber on dark can be low-contrast** → Mitigation: Use brand-400 (#fbbf24) for text on dark, brand-600 (#d97706) for backgrounds with white text. Test all combinations against WCAG AA (4.5:1 for text, 3:1 for large text/UI).
- **Sidebar collapse changes content width** → Mitigation: Content uses fluid layout (no fixed widths). The 144px difference between collapsed and expanded sidebar is absorbed by the flex/grid layouts.
- **Many files touched** → Mitigation: Changes are purely additive CSS/template changes. No logic changes, no API changes. Every change is independently revertable.
- **Toast composable is global mutable state** → Mitigation: Toasts are ephemeral (auto-dismiss), append-only during their lifetime, and the array is small (max ~3 concurrent). No persistence, no cross-tab sync needed.

## Open Questions

None — all decisions were explored during the discovery conversation.
