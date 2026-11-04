## Why

Six domain projects (Core, Search, Download, Feed, Enrichment, RuleSet) are completely flat — all .cs files in the project root, single namespace per project. As these projects grow (Download has 29 files, Core 27, Search 26), discoverability suffers: actors, state objects, clients, models, FFmpeg wrappers, and subtitle parsers are all peers in one folder. Organizing now prevents the pattern where subfolders get added later but namespace adjustments are forgotten.

## What Changes

- Introduce subfolders in 6 domain/core projects, grouping files by role/concern
- Mirror the same folder structure in corresponding test projects
- Update namespace declarations in all moved files to match their new folder path
- Update `using` statements across the entire solution to reference the new namespaces
- No logic changes, no API changes, no behavior changes

Domain/Core projects affected:
- **FunkArr.Core** — Options/, Actors/, Data/
- **FunkArr.Download** — Manager/, Scheduler/, History/, Worker/, Ffmpeg/, Subtitles/
- **FunkArr.Search** — Manager/, Workers/, Client/, Models/
- **FunkArr.Feed** — Crawl/, Store/, Eval/, Page/, Scheduler/
- **FunkArr.Enrichment** — Tvdb/, Tmdb/
- **FunkArr.RuleSet** — Manager/, Resolver/, Worker/, Updater/

Test projects mirroring their domain structure:
- **FunkArr.Download.Tests** — Manager/, Worker/, Scheduler/, Ffmpeg/, Subtitles/
- **FunkArr.Search.Tests** — Manager/, Workers/, Client/, Models/
- **FunkArr.Feed.Tests** — Crawl/, Store/
- **FunkArr.RuleSet.Tests** — Manager/, Resolver/, Worker/, DiskModel/
- **FunkArr.Enrichment.Tests** — Tvdb/, Tmdb/

Projects unchanged: Messages, Persistence, Api, ArrApi, Api.Tests, ArrApi.Tests (already organized), Scoring, History, Scoring.Tests, History.Tests (too small).

## Capabilities

### New Capabilities

- `project-folder-structure`: Target folder layout and namespace mapping for each of the 6 projects being reorganized

### Modified Capabilities

(none — pure structural refactoring, no requirement changes)

## Impact

- All 6 domain projects and Core get subfolders + namespace changes
- 5 test projects get matching subfolders + namespace changes
- Api, ArrApi mapping extensions may need updated `using` statements
- `git mv` preserves file history
- Build must pass after each project is reorganized
