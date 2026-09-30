## Why

The browser tab always shows "FunkArr" regardless of which page is active. Users with multiple tabs open can't distinguish between pages. Standard *arr apps show "Page - AppName" format.

## What Changes

- Add `document.title` updates per route using Vue Router's `afterEach` hook or route meta
- Format: "Downloads - FunkArr", "History - FunkArr", "RuleSets - FunkArr", etc.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `collapsible-sidebar`: Add per-route browser tab titles

## Impact

- **FunkArr.UI**: `main.ts` - add router afterEach hook to set document.title from route meta
