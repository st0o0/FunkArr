# playwright-smoke-tests Specification

## Purpose

Smoke tests verifying all routes load without JavaScript errors and redirects resolve correctly.

## Requirements

### Requirement: All views load without errors

Every registered route SHALL load and render its primary content without JavaScript console errors.

#### Scenario: Dashboard loads
- **WHEN** navigating to `/`
- **THEN** the page SHALL render the "Overview" heading without console errors

#### Scenario: Activity loads
- **WHEN** navigating to `/activity`
- **THEN** the page SHALL render the "Activity" heading without console errors

#### Scenario: Download Detail loads
- **WHEN** navigating to `/activity/{mock-id}` for a completed download
- **THEN** the page SHALL render the download title without console errors

#### Scenario: Setup loads
- **WHEN** navigating to `/setup`
- **THEN** the page SHALL render the setup wizard without console errors

#### Scenario: RuleSet List loads
- **WHEN** navigating to `/rulesets`
- **THEN** the page SHALL render the rulesets heading without console errors

#### Scenario: RuleSet Detail loads
- **WHEN** navigating to `/rulesets/{mock-id}`
- **THEN** the page SHALL render the ruleset detail without console errors

#### Scenario: RuleSet Builder loads
- **WHEN** navigating to `/rulesets/new`
- **THEN** the page SHALL render the builder form without console errors

#### Scenario: Scoring History loads
- **WHEN** navigating to `/rulesets/{mock-id}/history`
- **THEN** the page SHALL render the history table without console errors

#### Scenario: Feed loads
- **WHEN** navigating to `/feed`
- **THEN** the page SHALL render the feed heading without console errors

#### Scenario: Settings loads
- **WHEN** navigating to `/settings`
- **THEN** the page SHALL render settings sections without console errors

### Requirement: Redirects resolve

Legacy and catch-all routes SHALL redirect to their correct targets.

#### Scenario: Queue redirect
- **WHEN** navigating to `/queue`
- **THEN** the URL SHALL become `/activity`

#### Scenario: History redirect
- **WHEN** navigating to `/history`
- **THEN** the URL SHALL become `/activity`

#### Scenario: Search redirect
- **WHEN** navigating to `/search`
- **THEN** the URL SHALL become `/rulesets`

#### Scenario: Catch-all redirect
- **WHEN** navigating to `/nonexistent-page`
- **THEN** the URL SHALL become `/`
