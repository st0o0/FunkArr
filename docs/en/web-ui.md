# Web UI

FunkArr includes a Vue.js web interface. After startup it is available at the configured port (default: `http://localhost:6969`). The sidebar on the left leads to the main sections: **Overview**, **Activity**, **RuleSets** and **Settings**. **Setup** sits at the bottom of the sidebar, next to a language selector (English, Deutsch, Österreichisch, Schwizerdütsch), the app version and a button to collapse the sidebar.

## Overview

The home page shows four tiles at a glance:

- **System** - Traffic light indicator (green/yellow/red) based on the health checks, with a short summary (Healthy, number of warnings or issues). Click leads to Setup.
- **Recent Downloads** - Number of recently completed downloads (taken from the last 10 history entries). Click leads to the download history.
- **Storage** - Used and total disk space in the complete directory with a progress bar (turns yellow above 90%).
- **Rulesets** - Total number of loaded rulesets and the installed community ruleset version. Click leads to the ruleset list.

When downloads are active or queued, a progress bar shows the number of active and queued downloads, the current total speed and the overall progress, with a link to the Activity page.

Below that, the "Recent Activity" section lists the last 10 downloads with status, size, and time.

## Activity

The activity page has two tabs: **Queue** and **History**. A search field at the top right filters by title, and the current total download speed is shown next to the page title while downloads are running. The old paths `/queue` and `/history` redirect here.

### Queue

Shows all active and waiting downloads:

- **Active** - Currently running downloads with an overall progress bar and one card per download (phase: Downloading/Remuxing, speed, progress).
- **Priority sections** - Waiting downloads, split into three priority levels:
  - **High** - Downloads that are processed first
  - **Normal** - Default priority
  - **Low** - Downloads that are processed last

You can drag and drop waiting downloads between priority levels and reorder them within a level. The context menu (right-click) provides additional options:

- **Priority** - Move the download to a different level
- **Force Start** - Start the download immediately regardless of the queue (waiting downloads only)
- **Delete** - Remove the download from the queue (also possible for active downloads)

The top-right corner of the tab lets you pause and resume the entire download pipeline. A "Paused" badge is shown while paused. If a schedule is configured and currently outside its window, the next window ("Next: ...") is displayed.

### Download Detail

Click a card in the queue or a row in the history to open the detail view (`/activity/:id`). It shows:

- **Header** - Title, status badge (current phase/Queued, Completed or Failed), channel badge, subtitle indicator (SUB)
- **Progress bar** - Downloaded bytes / total size, speed and ETA (for active downloads)
- **Details grid** - Category, size, phase (active), priority, file path (history), duration (history), completion time (history)
- **Failure message** - Highlighted in red for failed downloads

Available actions depending on status:

| Status | Actions |
|--------|---------|
| Active/Queued | Force Start, Delete |
| Failed | Retry, Delete |
| Completed | Delete |

For downloads in the queue the view updates live via Server-Sent Events.

### History

Table view of all completed and failed downloads with:

- Title, quality, file size, download duration and completion time
- Status (Completed/Failed) with an expandable error message on failure
- Category filter, and the search field filters the titles on the current page
- Pagination (25 entries per page)
- Per row: failed downloads can be retried, and every entry can be deleted from the history

A click on a row opens the download detail.

## Setup

The setup wizard guides you through configuration. It starts with the health check and the service selection, followed by one step per selected service. Completed steps can be revisited by clicking them in the step bar.

### Step 1: System Health Check

Automatically checks eight items:

| Check | What it verifies |
|-------|-----------------|
| API Key | Whether the default key was changed |
| MediathekViewWeb | Reachability of the Mediathek API |
| Data Directory | Write access to the data path |
| Complete Directory | Write access to the download output path |
| Incomplete Directory | Write access to the temporary download path |
| Indexer API | Whether the Newznab API responds |
| Download API | Whether the SABnzbd API responds |
| FFmpeg | Whether FFmpeg is found on PATH (with version) |

Each item shows a status (OK, Warning, Error) with a fix hint for errors. "Re-check" runs the checks again. You can only continue while no check has failed.

### Step 2: Select Services

Choose which *arr apps you want to connect with FunkArr (at least one):

- **Prowlarr** - Indexer manager, adds FunkArr as a Newznab indexer
- **Sonarr** - TV series, adds FunkArr as a SABnzbd download client
- **Radarr** - Movies, adds FunkArr as a SABnzbd download client

### Step 3+: Configure Services

Each selected service gets its own step with two options:

**Automatic:** Enter the URL and API key of your *arr service. In the optional "FunkArr URL" field you can specify how the service reaches FunkArr (leave empty if it is the same network as your browser). The **Create Indexer** button (and for Sonarr/Radarr also **Create Download Client**) sets up the resources automatically via API call. Failures are shown inline.

**Manual:** "Configure manually" expands all required values (Name, Host, Port, URL Base, API Key, Category) ready to copy, plus hints such as enabling *Show Advanced* in the *arr app and testing the connection. For Sonarr there is also a tip about the *Daily* series type for shows identified by air date.

## Rulesets

The old path `/search` redirects to the ruleset list.

### List

Shows all loaded rulesets with search and sorting (by name or ID). Filter tabs with counts:

- **Media type** - All, Shows or Movies
- **Source** - All Sources, Community or Local (local includes merged rulesets); only shown if more than one source exists

Each entry shows topic, aliases, rule count, metadata IDs, time of the last scoring run, match rate and enrichment rate. The "+ New" button creates a new local ruleset.

### Detail

Shows all information about a single ruleset, with a breadcrumb back to the list:

- **Identity** - RuleSet ID, aliases, TVDB/IMDB/TMDB IDs, source (Community, Local or Community + Local) and date of the last community update
- **Enrichment** - Whether TMDB/TVDB enrichment is enabled and which methods and tolerances are used
- **Matching Rules** - Default confidence and the list of all rules (ID, strategy, priority, confidence, number of title parts and filters). Rules can be expanded to show regexes, title parts and filters.

Buttons at the top lead to the **Scoring History** and the **Editor**. For rulesets with a local file there are also **Export for Community** (validates and exports the ruleset for the community repository, validation errors are listed) and **Delete Local** (removes the local overlay after confirmation).

### Builder

A visual editor for creating (`/rulesets/new`) and editing (`/rulesets/:id/edit`) rulesets. The page is split in two:

- **Left: Form** - Identity (ID in kebab-case, locked when editing, topic, aliases, media type, metadata IDs), default confidence, individual matching rules (ID, priority, confidence, strategy, regexes, title rules, filters) and the enrichment settings (methods, thresholds, tolerances). Validation errors are listed at the top and when saving.
- **Right: Live preview** - Fetches up to 30 Mediathek entries for the entered topic (automatically after a short delay, or via "Refresh") and shows in real time which of them the current rules would match.

The live preview is only an approximation. The **Full Test** button runs the actual scoring and enrichment for the candidates and shows the complete results: per candidate the rule pipeline (matched, filter failed, identification failed, skipped), the identification and the enrichment result (e.g. title match, airdate match, year match). "Back to Live Preview" returns to the quick view.

## Settings

The settings page shows the current configuration and system information in five sections (read-only).

### Downloads

Shows the number of concurrent downloads and the configured schedule. When download time slots are defined, the active periods are displayed, otherwise "Always active".

### Metadata Cache

Statistics for the TVDB and TMDB cache: number of entries and age of the oldest entry.

### Network Routes

Shows configured routes (name, proxy address or "Direct connection", default route) and the channel-to-route mappings.

### System

General system information:

- App version
- Community ruleset version
- FFmpeg version
- API key (masked)

### Logs

Real-time log viewer using Server-Sent Events. Features:

- **Filter by level** - Information, Warning, Error (toggle buttons)
- **Structured display** - Time, level, source context and message per entry
- **Auto-scroll** - Automatically scrolls to the latest entries
- **Buffer** - Shows the last 500 entries

On page load the most recent entries are fetched via HTTP, after which new entries are streamed live.

## Scoring

### History

Accessible from the ruleset detail page (`/rulesets/:id/history`). Lists all scoring runs for a ruleset:

- Source (Sonarr, Radarr, Prowlarr or Test)
- Search query
- Time
- Number of candidates and matches

Entries are paginated. Click an entry to open the detail view.

### Detail

Shows the full scoring trace for a single search run. A filter switches between all, matched and unmatched candidates (with counts). For each Mediathek candidate you can see:

- Whether it was matched, the score and the matched rule ID
- Channel, topic, duration, quality
- Expandable rule traces per rule with priority, outcome (matched, filter failed, identification failed), the filter trace and the identification step
