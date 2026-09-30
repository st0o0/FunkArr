## MODIFIED Requirements

### Requirement: Debugger panel
The right pane of the builder page SHALL display a live preview panel instead of the previous dual-tab panel. The panel SHALL show auto-fetched candidates with live client-side match results by default. When the user runs a Full Test, the panel SHALL switch to showing server-side pipeline results with the existing trace visualization. All labels, headings, and status text SHALL use `$t()` translation calls.

#### Scenario: Panel renders live preview
- **WHEN** the builder page loads and Topic is set
- **THEN** the panel shows fetched candidates with live match indicators

#### Scenario: Panel switches to full test results
- **WHEN** the user clicks "Full Test" and results return
- **THEN** the panel shows server-side results with expandable rule pipeline traces

#### Scenario: Return to live preview
- **WHEN** the user edits a rule after viewing full test results
- **THEN** the panel returns to live preview mode with updated client-side matches

#### Scenario: Translated labels in German
- **WHEN** the locale is `de` and the debugger panel renders
- **THEN** labels display German translations (e.g., "Vollständiger Test" instead of "Full Test", "Live-Vorschau" instead of "Live Preview")

### Requirement: Live preview mode indicator
The panel SHALL clearly indicate whether it is showing live client-side preview results or full server-side test results. The indicator text SHALL use translation keys.

#### Scenario: Live preview mode
- **WHEN** the panel shows client-side match results
- **THEN** a subtle label with the translated "Live Preview" text is visible and a translated disclaimer is shown

#### Scenario: Full test mode
- **WHEN** the panel shows server-side test results
- **THEN** a label with the translated "Full Test Results" text is visible with no disclaimer

### Requirement: Results display
After a full test completes, the panel SHALL display results as a list of candidate cards sorted with matched candidates first. Each card SHALL show candidate title, topic, channel, and duration, plus the match result badge. Badge labels ("Matched", "No Match") SHALL use translation keys.

#### Scenario: Display matched candidate
- **WHEN** a full test candidate matched rule "regex-se" with score 0.95
- **THEN** the card shows a translated "Matched" badge, rule ID "regex-se", and score 0.95

#### Scenario: Display unmatched candidate
- **WHEN** a full test candidate did not match any rule
- **THEN** the card shows a translated "No Match" badge

#### Scenario: Sort order
- **WHEN** 3 of 10 candidates matched in full test
- **THEN** the 3 matched candidates appear first, followed by 7 unmatched

#### Scenario: German badge labels
- **WHEN** the locale is `de`
- **THEN** badges read "Treffer" and "Kein Treffer" instead of "Matched" and "No Match"
