# User validation checklist (retrieved from history)

The per-batch progress notes were deleted when `plan/` was cleaned, so this list was recovered from
their history (`git show 1d50569:plan/IMPROVEMENT_PROGRESS.md`, `git show 1d50569:plan/REMAINING.md`)
and from `bug_report.md`. It records exactly what each batch's author said a device pass still had to
confirm.

Policy in force: the device pass counts as passed. This file is for optional confirmation, not a gate.
A shipped item reopens only when a real bug is reported, and a new top-level `bug_report.md` entry
starts that.

| Item | Commit(s) | Confirm on the device |
| --- | --- | --- |
| G2 user-video keyword search | `914d32f` | `bilibili.app.interface.v1.Space/SearchArchive` accepts the headers; real `Total` paging; keyword/filter interplay |
| G3 dynamic detail | `d4c6345`, `6ae1981` | hidden/limited dynamics; `ConvertToDisplay2024` on details the REST path used to render |
| G4 user-space feed | `65d5e8c`, `4fca416` | header acceptance; the server's `history_offset` semantics for page > 1; account/region-gate rejection reaching the REST fallback; converted card rendering |
| G5 dynamic repost list | `b39739a`, `9fb3d16` | panel renders; server `offset` semantics for page > 1; how often a gRPC repost page maps to zero usable cards |
| D1 danmaku list | `5e36ec2` | flyout lists the loaded comments around the playhead; mode button switches to the whole loaded pool with no network call; copy puts text on the clipboard with the toast; liking a row with a server id shows 已点赞弹幕 then 取消点赞 (`op=2`) and returns to 点赞; a just-sent comment and any local XML/manifest playback show copy only; switching episodes/reopening shows the new item's comments with no carry-over like marks |
| D2 filter import/export | `e94c843` | real `FileSavePicker`/`FileOpenPicker` round trip incl. cancellation and the `.json` filter; toast text rendering; persistence of the blocked-user key (`SHIELD_USER`) across restart |
| C1 collections | `92ecd6a` | 合集 tab layout (section headers, nested scroll inside the pivot); current-video row highlight; clicking another episode plays and the tab stays populated; refresh/F5 and navigation leave exactly one 合集 tab; `arc.pic` URLs (`http://i0.hdslb.com/...`) render |
| C2 playlist paging | `34ec312` | signed builder accepted by `medialist/resource/list`; `oid`+`type` page correctly for page > 1; the unsigned retry covers the logged-out case; pivot shows count/加载更多/duration; Load more appends below the selection without resetting scroll; `player_AllMediaEndEvent` advances inside the new pivot |
| C3 playlist links/continuation | `c735a73`, `18f71dc` | a real `bilibili://music/playlist/playpage/...` link yields one 播放列表 pivot with page 1 loaded and the first row playing; picking a row replaces only detail/parts while the queue and highlight stay; reaching the last loaded row loads the next page and continues; with the network blocked at that boundary playback stops, 加载更多 stays clickable and exactly one toast appears; 合集 rows of a queued video play in section order without replacing the queue tab |
| API-SMART-01 selector | `797dc3a`, `26d4e57`, `3665bdb` | cold start is gRPC-first; a repeated rejection is remembered across an app restart; recovery back to gRPC after the cooldown; the legacy-first direction falls back symmetrically; a valid empty page is not treated as failure; refresh keeps learned history; histories stay separate per account/app key; `dynamic-api-history.v1.json` survives restart |
| Stability 1/3/4 | `96de3fc`, `0668875`, `546b409` | closing media tabs leaves no Share-handler retention and a later Share does not run a closed page's handler; closing the player detaches the audio element (no audio bound to a disposed `MediaPlayer`); rapid close of a WebPage raises no exception |
| BR-16 quiet load-more | `e5c97e1`, `261317f` | a feed whose `dynamic_history` answers 404: page 2 logs `stage=feed-failed page=2` with **no toast**, rows stay on screen, the footer retry resends the cursor; only a page-1 double failure toasts, with the legacy text |
| BR-13/-663 fallback | `5881dbb` | already device-verified (`bug_report.md`): gRPC rejected → REST publishes 20 cards, no crash |
| BR-15 packaging layout | `build-release-x64.ps1` | already device-verified: install `BiliLite.Packages_*` (root 8 KB launcher + `entrypoint/BiliLiter.exe`), boots |
| N1 player overlays | `312fa23` | one overlay per action (volume, brightness, seek, message, acceleration, speed); held speed stays until release; no duplicate central-label feedback; no leaked timers after closing the player |

