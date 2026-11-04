## Context

Six domain projects and Core are completely flat — all .cs files in the project root with a single namespace per project. As projects grew (Download 29 files, Core 27, Search 26), actors, state objects, infrastructure, and domain models became indistinguishable peers. The already-organized projects (Messages, Persistence, Api, ArrApi) prove the pattern works.

## Goals / Non-Goals

**Goals:**
- Organize files by role/concern within each project
- Namespaces match folder paths (convention already followed in organized projects)
- Preserve git history via `git mv`
- Build stays green after each project is reorganized

**Non-Goals:**
- Reorganizing Messages, Persistence, Api, ArrApi (already done)
- Reorganizing Scoring (10 files) or History (7 files) — too small
- Any logic, API, or behavior changes
- Changing class names or public API surface

## Decisions

### 1. Folder naming by role, not by class name

Folders are named for the role: `Manager/`, `Worker/`, `Scheduler/`, `Client/`, `Models/` — not the class name (`DownloadManager/`). The project name already provides the domain prefix; repeating it in the folder would be redundant (`FunkArr.Download.DownloadManager` vs `FunkArr.Download.Manager`).

**Alternative considered:** Class-name folders (`DownloadManager/`, `DownloadWorker/`). Rejected — redundant with project namespace.

### 2. Actor + State colocated in role folder

Each actor and its state class live together in the same folder. They are tightly coupled (state is only used by its actor) and are always read/edited together.

**Alternative considered:** Separate `Actors/` and `States/` folders. Rejected — splits tightly coupled files.

### 3. Small projects keep actors in root

Projects with ≤12 files where the actors/states are the primary content (Scoring, History) stay flat. Only "support" concerns (Ffmpeg, Subtitles, Client, Models) that form a clear subgroup justify a folder.

For projects being reorganized, actors that don't have enough peers for a meaningful group also stay in root (e.g. MediathekViewWebManager in Search — it's the only singleton that doesn't fit a role folder).

### 4. Core uses functional grouping

Core has no actors — it's infrastructure. Folders group by function: `Options/` (all *Options + validators), `Actors/` (ActorKeys, ShardMessageExtractor), `Data/` (DataPaths, DataFiles). Remaining abstractions and extensions stay in root.

### 5. Test projects mirror domain folder structure

Each test project gets the same subfolders as its domain project. Test files move to the subfolder matching the domain type they test. Test files that don't map to a specific subfolder (options validators, shared fixtures, root-level type tests) stay in the test project root.

Test namespaces follow `{DomainNamespace.Tests}.{Subfolder}` — e.g. `FunkArr.Download.Tests.Ffmpeg`.

Domain + tests are reorganized together in the same step, before build verification.

### 6. One project at a time, build between each

Process per project: create folders → `git mv` files → update namespaces in moved files → update `using` statements solution-wide → `dotnet build` → `dotnet format`. This catches issues early and keeps each step reviewable.

**Order:** Core first (most referenced by other projects — namespace changes ripple widest), then domains in any order.

### 7. PersistenceMapping stays in project root

Each domain has one `PersistenceMapping.cs` that maps multiple actor states to persistence DTOs. It's domain-wide, not actor-specific, so it stays in the project root rather than being duplicated or arbitrarily assigned to one actor folder.

## Risks / Trade-offs

- **[Large diff]** → Many files touched, but each change is mechanical (move + namespace). Review by project keeps it manageable.
- **[ArchUnitNET tests]** → Architecture tests may reference specific namespaces. Need to check and update if so. → Verify after Core (first project) whether any arch tests break.
- **[Merge conflicts]** → Any in-flight branches touching these files will conflict. → Do this when no other changes are in progress.
- **[IDE refactoring]** → IDE "move to folder" would do namespace + usings automatically, but we're doing this via CLI. → `dotnet build` after each project catches any missed usings.
