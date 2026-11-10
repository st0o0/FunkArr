## REMOVED Requirements

### Requirement: SABnzbd pause mode
The `mode=pause` arm in the SABnzbd controller was removed. Requests with `mode=pause` now fall through to the "Invalid mode" error response.

### Requirement: SABnzbd resume mode
The `mode=resume` arm in the SABnzbd controller was removed. Requests with `mode=resume` now fall through to the "Invalid mode" error response.

## MODIFIED Requirements

### Requirement: Download GET request parameters
Remove `value2` from the bound query parameters — it was only used by the priority and switch subcommands which no longer exist.
