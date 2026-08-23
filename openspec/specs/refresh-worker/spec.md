## Purpose

Akka.NET actor responsible for executing GitHub community ruleset refresh operations. Permanent child of RuleSetCoordinator, encapsulating the download-and-extract logic previously inline in the coordinator.

## Requirements

### Requirement: RefreshWorker for GitHub community refresh
`RefreshWorker` SHALL be a permanent child of `RuleSetCoordinator` that handles the GitHub community ruleset refresh. It SHALL receive `DoRefresh(communityPath)` and tell the parent `RefreshComplete(bool updated)`.

#### Scenario: Refresh triggers reload
- **WHEN** `RefreshWorker` completes a successful refresh that found updates
- **THEN** it SHALL tell the parent `RefreshComplete(true)` and the parent SHALL reload rulesets from disk
