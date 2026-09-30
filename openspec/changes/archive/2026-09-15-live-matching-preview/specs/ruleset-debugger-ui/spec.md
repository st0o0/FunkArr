## MODIFIED Requirements

### Requirement: Debugger panel
The right pane of the builder page SHALL display a live preview panel instead of the previous dual-tab panel. The panel SHALL show auto-fetched candidates with live client-side match results by default. When the user runs a Full Test, the panel SHALL switch to showing server-side pipeline results with the existing trace visualization.

#### Scenario: Panel renders live preview
- **WHEN** the builder page loads and Topic is set
- **THEN** the panel shows fetched candidates with live match indicators

#### Scenario: Panel switches to full test results
- **WHEN** the user clicks "Full Test" and results return
- **THEN** the panel shows server-side results with expandable rule pipeline traces

#### Scenario: Return to live preview
- **WHEN** the user edits a rule after viewing full test results
- **THEN** the panel returns to live preview mode with updated client-side matches

### Requirement: Results display
After a full test completes, the panel SHALL display results as a list of candidate cards sorted with matched candidates first. Each card SHALL show candidate title, topic, channel, and duration, plus the match result badge. This requirement is unchanged from the current behavior but now applies only to full test results, not to the live preview.

#### Scenario: Display matched candidate
- **WHEN** a full test candidate matched rule "regex-se" with score 0.95
- **THEN** the card shows a green "Matched" badge, rule ID "regex-se", and score 0.95

#### Scenario: Display unmatched candidate
- **WHEN** a full test candidate did not match any rule
- **THEN** the card shows a gray "No Match" badge

#### Scenario: Sort order
- **WHEN** 3 of 10 candidates matched in full test
- **THEN** the 3 matched candidates appear first, followed by 7 unmatched

## ADDED Requirements

### Requirement: Live preview mode indicator
The panel SHALL clearly indicate whether it is showing live client-side preview results or full server-side test results.

#### Scenario: Live preview mode
- **WHEN** the panel shows client-side match results
- **THEN** a subtle label "Live Preview" is visible and a disclaimer "Run Full Test for accurate scoring" is shown

#### Scenario: Full test mode
- **WHEN** the panel shows server-side test results
- **THEN** a label "Full Test Results" is visible with no disclaimer
