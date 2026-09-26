## Approach

Pure text edits across 6 existing VitePress markdown files. No structural changes, no new pages, no code changes.

## Decisions

- Replace `MatchHistory` with `ScoringHistory` everywhere (matches `ScoringHistoryOptions.SectionName`)
- Update comparison table cell and remove the "Kein Proxy-Support" weakness paragraph
- Add new config sections for `ArrApi` and `DownloadSchedule` following the existing table format in each configuration page
- All changes mirrored in DE and EN versions
