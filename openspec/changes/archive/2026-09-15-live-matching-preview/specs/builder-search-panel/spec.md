## MODIFIED Requirements

### Requirement: Builder right pane dual tabs
The RuleSetBuilder right pane SHALL display a single live preview panel instead of the previous Search and Test Results tabs. The panel SHALL show auto-fetched Mediathek candidates with live match results. A "Full Test" button SHALL be available for server-side pipeline validation.

#### Scenario: Default state on mount
- **WHEN** the RuleSetBuilder page loads
- **THEN** the right pane SHALL display the live preview panel with an empty state prompting the user to enter a Topic

#### Scenario: Auto-fetch replaces manual search
- **WHEN** the user enters a Topic
- **THEN** candidates are fetched automatically without the user needing to type a search query or click a search button

### Requirement: Search tab Mediathek search
The manual search input, channel/topic filters, and explicit search button are replaced by topic-driven auto-fetch. The search query is derived from the Topic field in the builder form. Results are cached and displayed as a live match list instead of a selectable candidate list.

#### Scenario: Topic-driven fetch
- **WHEN** the user sets Topic to "Tatort" in the identity section
- **THEN** after 800ms the system SHALL call `GET /api/mediathek/search?q=Tatort&limit=30`

#### Scenario: Results displayed as match list
- **WHEN** candidates are fetched
- **THEN** each candidate SHALL display title, topic, channel, duration, and its live match state against current rules

### Requirement: Candidate selection
Candidate selection checkboxes and "Select All" toggle are removed. All fetched candidates are automatically used for matching.

#### Scenario: All candidates used
- **WHEN** 30 candidates are fetched
- **THEN** all 30 are matched against the current rules without manual selection

## REMOVED Requirements

### Requirement: Test execution from search
**Reason**: Replaced by automatic live matching and on-demand "Full Test" button
**Migration**: Live preview provides immediate feedback; Full Test button provides server-side validation
