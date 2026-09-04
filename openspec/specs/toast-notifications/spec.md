## Purpose

Global toast notification system with `useToast` composable, auto-dismiss, bottom-right stacking, enter/leave transitions, and Teleport rendering.

## Requirements

### Requirement: useToast composable
A `useToast()` composable SHALL be provided that returns a `toast(message, variant?)` function for triggering toast notifications from any component.
The `variant` parameter MUST support: `success` (green), `error` (red), `info` (amber). Default variant MUST be `success`.

#### Scenario: Triggering a success toast
- **WHEN** a component calls `toast('RuleSet saved')`
- **THEN** a success-variant toast MUST appear with the message "RuleSet saved"

#### Scenario: Triggering an error toast
- **WHEN** a component calls `toast('Failed to delete', 'error')`
- **THEN** an error-variant toast MUST appear with the message "Failed to delete"

#### Scenario: Triggering an info toast
- **WHEN** a component calls `toast('Copied to clipboard', 'info')`
- **THEN** an info-variant toast MUST appear with the message "Copied to clipboard"

### Requirement: Auto-dismiss after timeout
Each toast SHALL automatically dismiss after 3000ms.

#### Scenario: Toast disappears after timeout
- **WHEN** a toast notification appears
- **THEN** it MUST automatically dismiss after 3000ms without user interaction

### Requirement: Positioned bottom-right fixed
The toast container SHALL be positioned at the bottom-right corner of the viewport using `position: fixed`.

#### Scenario: Toast position
- **WHEN** one or more toasts are visible
- **THEN** they MUST appear stacked vertically in the bottom-right corner of the viewport
- **AND** the most recent toast MUST appear at the bottom of the stack

### Requirement: Maximum visible toasts
At most 3 toasts SHALL be visible simultaneously. When a new toast is added and 3 are already visible, the oldest MUST be dismissed.

#### Scenario: Fourth toast triggers dismissal
- **WHEN** 3 toasts are visible
- **AND** a new toast is triggered
- **THEN** the oldest toast MUST be dismissed
- **AND** the new toast MUST appear

### Requirement: Enter and leave transitions
Toasts SHALL animate on appear and dismiss:
- **Enter**: slide in from the right
- **Leave**: fade out

#### Scenario: Toast appears
- **WHEN** a new toast is triggered
- **THEN** it MUST slide in from the right edge of the viewport

#### Scenario: Toast dismisses
- **WHEN** a toast is dismissed (by timeout or overflow)
- **THEN** it MUST fade out

### Requirement: ToastContainer rendered via Teleport
A `ToastContainer.vue` component SHALL be rendered via Vue `<Teleport to="body">` to ensure toasts appear above all other content.

#### Scenario: Toast renders above page content
- **WHEN** a toast is visible
- **THEN** it MUST render as a direct child of `<body>` via Teleport
- **AND** it MUST have a z-index high enough to appear above all other UI elements

### Requirement: Toast applied to user actions
Toasts MUST be triggered for the following user actions across all views:
- **Save**: success toast on successful save (RuleSet builder)
- **Delete**: success toast on successful delete (History, RuleSet detail)
- **Copy to clipboard**: info toast when a value is copied (Setup guide)
- **Cancel download**: success toast when a download is cancelled (Queue)
- **Error**: error toast when any of the above actions fail

#### Scenario: Successful save triggers toast
- **WHEN** a RuleSet is saved successfully
- **THEN** a success toast MUST appear with a message indicating the save succeeded

#### Scenario: Failed delete triggers error toast
- **WHEN** a history item delete fails
- **THEN** an error toast MUST appear with the error message

#### Scenario: Clipboard copy triggers info toast
- **WHEN** a value is copied to clipboard in the Setup guide
- **THEN** an info toast MUST appear confirming the copy
