## REMOVED Requirements

### Requirement: SubtitlePreparer uses HttpClient via DI
**Reason:** SubtitlePreparer functionality absorbed into Remuxer. The class and its ISubtitlePreparer interface are deleted.
**Migration:** Remuxer receives IHttpClientFactory via DI and handles subtitle download internally.

### Requirement: SubtitlePreparer routes through proxy
**Reason:** SubtitlePreparer class removed.
**Migration:** Remuxer uses the same named HttpClient pattern (`route:{routeName}`) for subtitle downloads.
