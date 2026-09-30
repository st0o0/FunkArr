## REMOVED Requirements

### Requirement: ReleaseTitleBuilder formats scene-style titles

**Reason:** Replaced by `SceneRelease.FormatTitle()` instance method. All formatting logic (Sanitize, MapQuality, CollapseDots, Format) moves onto the `SceneRelease` record as private helpers. The title format and output remain identical.

**Migration:** Callers of `ReleaseTitleBuilder.Format(...)` use `SceneRelease.ForShow/ForMovie(...).FormatTitle(quality)` instead. `ReleaseTitleBuilder.FormatSeasonEpisode` and `PadNumber` become internal to `SceneRelease.ForShow`. File `FunkArr.Core/ReleaseTitleBuilder.cs` is deleted.

### Requirement: MetadataSpec record carries identification results

**Reason:** Not affected by this change. MetadataSpec remains in FunkArr.Messages.Scoring unchanged.

**Migration:** None required.

### Requirement: Umlaut preservation

**Reason:** Replaced by identical behavior in `SceneRelease.FormatTitle()`. The umlaut preservation requirement transfers to the new spec `scene-release`.

**Migration:** Existing tests move to `SceneReleaseTests`.

### Requirement: Special character handling

**Reason:** Replaced by identical behavior in `SceneRelease.FormatTitle()`. The special character handling requirement transfers to the new spec `scene-release`.

**Migration:** Existing tests move to `SceneReleaseTests`.

### Requirement: Quality tier mapping

**Reason:** Replaced by identical behavior in `SceneRelease.FormatTitle()`. The quality mapping requirement transfers to the new spec `scene-release`.

**Migration:** Existing tests move to `SceneReleaseTests`.
