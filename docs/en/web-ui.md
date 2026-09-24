# Web UI

FunkArr includes a Vue.js web interface. After startup it is available at the configured port (default: `http://localhost:6969`). The sidebar on the left leads to all sections.

## Dashboard

The home page shows four tiles at a glance:

- **System status** - Traffic light indicator (green/yellow/red) based on health checks. Click leads to Setup.
- **Recent downloads** - Number of recently completed downloads. Click leads to the download history.
- **Storage** - Used and available disk space in the complete directory with a progress bar.
- **Rulesets** - Total number of loaded rulesets and the installed community ruleset version.

When downloads are active, a progress bar shows the current total speed and the number of queued downloads.

Below that, the "Recent Activity" section lists the last 10 downloads with status, size, and timestamp.

## Activity

The activity page has two tabs: **Queue** and **History**.

### Queue

Shows all active and waiting downloads. The queue is split into three priority levels:

- **High** - Downloads that are processed first
- **Normal** - Default priority
- **Low** - Downloads that are processed last

You can drag and drop downloads between priority levels and reorder them within a level. The context menu (right-click) provides additional options:

- **Change priority** - Move the download to a different level
- **Force start** - Start the download immediately regardless of the queue
- **Cancel** - Remove the download from the queue

The top-right corner lets you pause and resume the entire download pipeline. If a schedule is active, the next window is displayed.

### History

Table view of all completed and failed downloads with:

- Title, quality, file size, and download duration
- Status (Completed/Failed) with error message on failure
- Category filter and search field
- Failed downloads can be retried

## Setup

The setup wizard guides you through configuration in three steps.

### Step 1: System Health Check

Automatically checks eight items:

| Check | What it verifies |
|-------|-----------------|
| API Key | Whether the default key was changed |
| MediathekViewWeb | Reachability of the Mediathek API |
| Data directory | Write access to the data path |
| Complete directory | Write access to the download output path |
| Incomplete directory | Write access to the temporary download path |
| Indexer API | Whether the Newznab API responds |
| Download API | Whether the SABnzbd API responds |
| FFmpeg | Whether FFmpeg is found on PATH |

Each item shows a status (OK, Warning, Error) with fix hints for problems.

### Step 2: Select Services

Choose which *arr apps you want to connect with FunkArr:

- **Prowlarr** - Indexer management
- **Sonarr** - Series management
- **Radarr** - Movie management

### Step 3: Configure Services

For each selected service there are two options:

**Automatic:** Enter the URL and API key of your *arr service. FunkArr creates the indexer (and for Sonarr/Radarr also the download client) automatically via API call.

**Manual:** Expand the manual settings to see all required values (Name, Host, Port, URL Base, API Key, Category) ready to copy.

## Rulesets

### List

Shows all loaded rulesets with search and sorting (by name or ID). Filter by:

- **Media type** - Series or movies
- **Source** - Community, local, or merged

The "New" button lets you create a new local ruleset.

### Detail

Shows all information about a single ruleset:

- **Identity** - Topic, aliases, TVDB/IMDB/TMDB IDs, source (Community/Local/Merged)
- **Enrichment** - Whether TMDB/TVDB enrichment is enabled and which methods are used
- **Rules** - List of all matching rules with their configuration

From here you can navigate to the scoring history and the editor.

### Builder

A visual editor for creating and editing rulesets. The page is split in two:

- **Left: Form** - Identity (ID, topic, aliases, media type, metadata IDs), confidence, and individual rules with strategy, filters, and title mapping
- **Right: Live preview** - Shows in real time which Mediathek entries the current configuration would match

Changes in the form update the preview instantly, so you can test rules directly against live Mediathek data.

## Scoring

### History

Accessible from the ruleset detail page. Lists all scoring runs for a ruleset:

- Source (e.g. Sonarr, Radarr, manual search)
- Search query
- Timestamp
- Number of candidates and matches

Click an entry to open the detail view.

### Detail

Shows the full scoring trace for a single search run. For each Mediathek candidate you can see:

- Whether it was matched (with score and matched rule ID)
- Channel, topic, duration, quality
- Expandable rule traces per rule with the outcome (Matched, Filter failed, No match) and the individual filter steps
