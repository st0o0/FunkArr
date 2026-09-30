## 1. Message Layer Changes

- [x] 1.1 Extend DownloadStatus enum with Extracting (4), Moving (5), Verifying (6) values
- [x] 1.2 Add Priority parameter (int, default 0) to AddDownload record
- [x] 1.3 Add Start (int, default 0), Limit (int, default 0), Category (string?, default null) to QueryQueue record
- [x] 1.4 Add Start (int, default 0), Limit (int, default 0), Category (string?, default null) to QueryHistory record
- [x] 1.5 Add DeleteFiles parameter (bool, default false) to DeleteDownload record

## 2. Request Binding Changes

- [x] 2.1 Add del_files (int?), category (string?), archive (int?) query parameters to DownloadGetRequest
- [x] 2.2 Verify priority parameter is already bound in DownloadPostRequest

## 3. Config Endpoint Fixes

- [x] 3.1 Add config.categories array with sonarr/radarr/tv/movies entries (name, order, dir, newzbin, priority fields)
- [x] 3.2 Add config.misc.pre_check field (false)

## 4. Queue and History Endpoint Changes

- [x] 4.1 Forward start/limit/category from DownloadGetRequest to QueryQueue message
- [x] 4.2 Forward start/limit/category from DownloadGetRequest to QueryHistory message
- [x] 4.3 Add speed field per queue slot (format QueueItem.Speed as bytes/second string)
- [x] 4.4 Add aggregate speed field to fullstatus response
- [x] 4.5 Map intermediate DownloadStatus values (Extracting, Moving, Verifying) to SABnzbd status strings in history response

## 5. Delete and Addfile Parameter Forwarding

- [x] 5.1 Forward del_files parameter from request to DeleteDownload message in queue delete
- [x] 5.2 Forward del_files parameter from request to DeleteDownload message in history delete
- [x] 5.3 Accept archive parameter on history delete (parameter accepted, no behavior change)
- [x] 5.4 Forward priority parameter from DownloadPostRequest to AddDownload message

## 6. Response Model Updates

- [x] 6.1 Add speed field to FullStatusResponse/FullStatusData record
- [x] 6.2 Add speed field to QueueSlot record

## 7. Spec Sync

- [x] 7.1 Update main sabnzbd-download-api spec with all new requirements
- [x] 7.2 Update main download-messages spec with all modified message definitions
