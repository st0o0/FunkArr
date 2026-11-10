## REMOVED Requirements

### Requirement: Pause returns success scenario
Remove the "Pause returns success" scenario — the `POST /api/downloads/pause` endpoint it tests was replaced by pipeline-level pause (which is tested elsewhere via `download-pause-control` spec).
