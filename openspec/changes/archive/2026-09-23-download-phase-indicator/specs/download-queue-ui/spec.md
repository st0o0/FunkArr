## ADDED Requirements

### Requirement: Active download phase label
The ActiveDownloadCard SHALL display the current download phase alongside the progress percentage. During the "downloading" phase, the label SHALL read the localized equivalent of "Downloading". During the "remuxing" phase, the label SHALL read the localized equivalent of "Remuxing".

#### Scenario: Downloading phase shows download label
- **WHEN** a queue item has `phase = "downloading"`
- **THEN** the progress line SHALL show "Downloading" (or localized equivalent) before the percentage

#### Scenario: Remuxing phase shows remux label
- **WHEN** a queue item has `phase = "remuxing"`
- **THEN** the progress line SHALL show "Remuxing" (or localized equivalent) before the percentage
