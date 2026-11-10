## REMOVED Requirements

### Requirement: SABnzbd queue priority operation (from download-priority spec, cross-ref)
Already removed via download-priority delta. Noted here for completeness — the SABnzbd-specific priority and switch scenarios in this spec's context are no longer applicable at the SABnzbd adapter layer. The internal API endpoints (`POST /api/downloads/queue/{id}/priority` and `POST /api/downloads/queue/swap`) remain and are unchanged.
