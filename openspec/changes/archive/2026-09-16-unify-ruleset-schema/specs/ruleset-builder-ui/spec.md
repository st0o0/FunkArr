## MODIFIED Requirements

### Requirement: RuleSet builder page
The Vue frontend SHALL render a ruleset builder page at route `/rulesets/new` for creating new rulesets and `/rulesets/:id/edit` for editing existing ones. The page SHALL use an asymmetric split-pane layout: the builder form on the left (wider) and the search + test panel on the right (narrower). The grid SHALL use `grid-cols-[1fr_380px]` within a `max-w-5xl mx-auto` container.

In edit mode, the builder SHALL fetch data from `GET /api/rulesets/:id` (the structured Detail endpoint) instead of a separate raw endpoint. The response fields SHALL be mapped directly to the form state. The `strategy` field SHALL already be in wire enum format (e.g. `"seasonAndEpisodeNumber"`), matching the form's select option values.

#### Scenario: Navigate to create new ruleset
- **WHEN** the user navigates to `/rulesets/new`
- **THEN** the builder form renders with empty fields and the search panel on the right

#### Scenario: Navigate to edit existing ruleset
- **WHEN** the user navigates to `/rulesets/tatort/edit`
- **THEN** the builder form is populated with the existing ruleset data from `GET /api/rulesets/tatort`

#### Scenario: Asymmetric split-pane layout
- **WHEN** the builder page renders
- **THEN** the builder form occupies the left pane (fluid, `1fr`) and the search/test panel occupies the right pane (fixed `380px`)
- **AND** the entire layout is constrained to `max-w-5xl mx-auto`

#### Scenario: Strategy label uses shared utility
- **WHEN** a rule's collapsed header shows the strategy label
- **THEN** the label SHALL be produced by the shared `strategyLabel()` utility, not a local function

### Requirement: Builder API client functions
The frontend SHALL expose API client functions for the write endpoints: `createRuleSet(data)` calling `POST /api/rulesets`, `updateRuleSet(id, data)` calling `PUT /api/rulesets/:id`, and `deleteRuleSet(id)` calling `DELETE /api/rulesets/:id`. All functions SHALL throw on non-2xx responses. The `getRuleSetRaw(id)` function SHALL be removed — the Builder SHALL use `getRuleSetDetail(id)` instead.

#### Scenario: Create function sends POST
- **WHEN** `createRuleSet` is called with ruleset data
- **THEN** a POST request is sent to `/api/rulesets` with the JSON body

#### Scenario: Update function sends PUT
- **WHEN** `updateRuleSet("tatort", data)` is called
- **THEN** a PUT request is sent to `/api/rulesets/tatort` with the JSON body

#### Scenario: Delete function sends DELETE
- **WHEN** `deleteRuleSet("my-show")` is called
- **THEN** a DELETE request is sent to `/api/rulesets/my-show`

#### Scenario: Raw function removed
- **WHEN** the codebase is inspected
- **THEN** `getRuleSetRaw()` SHALL NOT exist in the API client
