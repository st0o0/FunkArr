## REMOVED Requirements

### Requirement: DownloadPriority enum — Paused value
Remove `Paused = -2` from the DownloadPriority enum. The enum retains `Low = -1`, `Normal = 0`, `High = 1`.

### Requirement: SABnzbd pause priority mapping
Remove the scenario mapping SABnzbd priority value -2 to `DownloadPriority.Paused`.

### Requirement: SABnzbd queue priority operation
Remove the entire requirement for `mode=queue&name=priority&value={nzo_id}&value2={priority_int}`. This SABnzbd controller arm was removed.

### Requirement: SABnzbd queue switch operation
Remove the entire requirement for `mode=queue&name=switch&value={nzo_id1}&value2={nzo_id2}`. This SABnzbd controller arm was removed.

### Requirement: DispatchNext skips paused items
Remove — there is no longer a `Paused` priority value to skip.

### Requirement: QueueSlot reports per-item paused status
Remove — per-item pause via priority no longer exists.

### Requirement: Addfile with pause priority
Remove the scenario for priority -2 on addfile.

### Requirement: Persistence round-trip for Paused priority
Remove — `PersistedDownloadPriority.Paused` was removed.

### Requirement: Internal API pause and resume endpoints
Remove — `POST /api/downloads/{id}/pause` and `POST /api/downloads/{id}/resume` were removed.
