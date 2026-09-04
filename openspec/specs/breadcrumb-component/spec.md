## Purpose

Reusable breadcrumb navigation component replacing manual breadcrumb markup across views with a consistent `AppBreadcrumb.vue` component.

## Requirements

### Requirement: AppBreadcrumb component interface
An `AppBreadcrumb.vue` component SHALL accept a single prop `items` of type `Array<{ label: string; to?: string }>`. Each item represents one breadcrumb segment. The `to` property is an optional Vue Router path; when present, the segment is a navigable link. When absent, the segment is the current page (terminal segment).

#### Scenario: Component receives items array
- **WHEN** AppBreadcrumb is rendered with `[{label: "RuleSets", to: "/rulesets"}, {label: "my-show"}]`
- **THEN** it SHALL render two segments: "RuleSets" as a link to /rulesets and "my-show" as plain text

### Requirement: Navigable breadcrumb segments
Each breadcrumb item that has a `to` value SHALL render as a `<router-link>` with classes `text-text-muted hover:text-text-secondary transition-colors`.

#### Scenario: User clicks a breadcrumb link
- **WHEN** the user clicks on a breadcrumb segment that has a `to` value
- **THEN** Vue Router SHALL navigate to the specified path

### Requirement: Terminal breadcrumb segment
The last item in the `items` array SHALL render as a `<span>` with class `text-text-secondary`. It MUST NOT be wrapped in a `<router-link>` regardless of whether `to` is provided.

#### Scenario: Terminal segment rendering
- **WHEN** AppBreadcrumb renders the last item in the array
- **THEN** the segment SHALL be a non-clickable span with `text-text-secondary` styling

### Requirement: Chevron separator between segments
A chevron SVG separator (`<svg viewBox="0 0 16 16" class="w-3.5 h-3.5 text-text-muted"><path d="M6 4l4 4-4 4" fill="none" stroke="currentColor" stroke-width="1.5"/></svg>`) SHALL be rendered between each pair of adjacent breadcrumb items. No separator SHALL appear before the first item or after the last item.

#### Scenario: Three-segment breadcrumb
- **WHEN** AppBreadcrumb is rendered with three items
- **THEN** two chevron separators SHALL appear: one between item 1 and item 2, and one between item 2 and item 3

### Requirement: Breadcrumb container layout
The breadcrumb container SHALL use `flex items-center gap-1.5 text-sm text-text-muted mb-4` to match the existing breadcrumb visual style.

#### Scenario: Breadcrumb layout consistency
- **WHEN** AppBreadcrumb renders
- **THEN** the wrapper element SHALL be a flex container with 6px gap, 14px font size, muted text color, and 16px bottom margin

### Requirement: Replace existing breadcrumb markup
The manual breadcrumb markup in RuleSetDetail, RuleSetBuilder, ScoringHistory, and ScoringDetail SHALL be replaced with `<AppBreadcrumb :items="[...]" />`. The visual output MUST remain identical.

#### Scenario: RuleSetDetail breadcrumb replacement
- **WHEN** RuleSetDetail renders its breadcrumb
- **THEN** it SHALL use `<AppBreadcrumb :items="[{label: 'RuleSets', to: '/rulesets'}, {label: id}]" />`

#### Scenario: ScoringDetail deep breadcrumb
- **WHEN** ScoringDetail renders its breadcrumb
- **THEN** it SHALL use `<AppBreadcrumb :items="[{label: 'RuleSets', to: '/rulesets'}, {label: id, to: '/rulesets/' + id}, {label: 'History', to: '/rulesets/' + id + '/history'}, {label: requestId.substring(0, 8) + '...'}]" />`
