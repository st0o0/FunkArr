## Why

RuleSets are the core configuration that drives how FunkArr matches Mediathek content to series. Currently they are hand-edited JSON files, and the UI only displays them read-only. Users have no way to search across a growing ruleset collection, no visual editor, and no way to test rules against real candidates before committing — debugging means editing JSON, restarting, and waiting for a real search to trigger scoring. A Regex101-style live debugger built into the UI would let users build rules visually and immediately see which candidates match and why, shortening the feedback loop from minutes to seconds.

## What Changes

- **RuleSet list search**: Client-side text filter across ruleSetId, topic, aliases, and media IDs on the list page.
- **RuleSet write API**: New endpoints to create, update, and delete local rulesets via JSON files on disk, with automatic reload into the actor system.
- **Ad-hoc scoring test endpoint**: New endpoint that accepts a raw matching config + candidates, runs them through the existing MatchMagicActor scoring engine, and returns full ItemTrace results — without persisting to history.
- **MediathekViewWeb search proxy**: New endpoint that proxies search queries to MediathekViewWeb, returning candidates in ScoreCandidate shape — avoids CORS and provides real test data.
- **RuleSet Builder UI**: Visual editor for local rulesets — identity (topic, aliases, media IDs), rules with strategy picker and strategy-specific parameters, filter builder with all/any/not groups, priority and confidence controls.
- **Live Debugger UI**: Split-pane Regex101-style debugger — builder on left, test results on right. Two input modes (manual candidate entry, MediathekViewWeb fetch). Full rule pipeline trace visualization with color-coded pass/fail/skip per filter condition and identification step.

## Capabilities

### New Capabilities
- `ruleset-write-api`: REST endpoints to create, update, and delete local rulesets with actor system reload
- `ruleset-test-api`: Ad-hoc scoring test endpoint and MediathekViewWeb search proxy for the debugger
- `ruleset-builder-ui`: Visual ruleset editor with strategy picker, filter builder, and identity editing
- `ruleset-debugger-ui`: Live Regex101-style debugger with candidate input, scoring execution, and trace visualization

### Modified Capabilities
- `ruleset-api`: Add write endpoints (POST, PUT, DELETE) alongside existing read endpoints
- `ruleset-management`: RuleSetManager gains reload-single-ruleset capability triggered by write API
- `ruleset-ui`: Add search/filter on the list page

## Impact

- **FunkArr.Api**: New endpoint groups for write operations, test scoring, and mediathek proxy
- **FunkArr.RuleSet**: RuleSetManager gets new message handler for single-ruleset reload
- **FunkArr.Core**: IDataFiles may need a write method (WriteText) if not already present
- **FunkArr.Messages**: New command messages for ruleset write and reload, ad-hoc scoring
- **FunkArr.UI**: New views (builder, debugger), updated list view with search, new API client functions
- **FunkArr.Search**: MediathekViewWeb query logic reused (or referenced) by the proxy endpoint
