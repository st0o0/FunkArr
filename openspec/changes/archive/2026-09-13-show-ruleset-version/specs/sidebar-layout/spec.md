## MODIFIED Requirements

### Requirement: Sidebar navigation structure

The application layout SHALL use a CSS Grid with a collapsible sidebar and fluid main content area. The sidebar SHALL toggle between a collapsed state (52px, icon-only) and an expanded state (192px, icons + labels). The grid template SHALL be `grid-cols-[52px_1fr]` when collapsed and `grid-cols-[192px_1fr]` when expanded. The sidebar SHALL contain the FunkArr logo mark, navigation links, a Setup gear icon, a collapse toggle, and a community ruleset version indicator at the bottom.

#### Scenario: Collapsed sidebar rendering
- **WHEN** the sidebar is in collapsed state
- **THEN** the layout renders as a two-column grid with a 52px sidebar showing only icons and the content area filling the remaining width

#### Scenario: Expanded sidebar rendering
- **WHEN** the sidebar is in expanded state
- **THEN** the layout renders as a two-column grid with a 192px sidebar showing icons and labels and the content area filling the remaining width

#### Scenario: Sidebar sections
- **WHEN** the sidebar renders in expanded state
- **THEN** it SHALL contain sections top-to-bottom: logo mark with "FunkArr" text, navigation links, and a bottom utility area with Setup gear icon, collapse toggle, and community ruleset version text

#### Scenario: Sidebar toggle button
- **WHEN** the sidebar renders
- **THEN** a toggle button is visible at the bottom that switches between collapsed and expanded states

#### Scenario: Sidebar width transition
- **WHEN** the sidebar toggles between collapsed and expanded
- **THEN** the width animates smoothly via CSS transition (200ms ease)

#### Scenario: Sidebar state persistence
- **WHEN** the user toggles the sidebar state
- **THEN** the state is persisted to `localStorage` under key `funkarr-sidebar`
- **AND** on next page load, the sidebar restores the persisted state
