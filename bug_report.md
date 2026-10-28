# Bug report — current state

Branch `work`, baseline `2507931` (round-2 review merged), this increment on top of it. Report is rewritten each turn; history lives in the commits.

## Changed this turn

Round-2 review items:
- **BR-05 follow-up (must-fix):** a page that filters to zero visible cards left pagination stuck, because `MyListView` consumed `_itemsVersion` before firing and only item changes bumped it. A `CanLoadMore` false→true edge is now treated as a completed page (it bumps the version), so the short-viewport check re-arms even when the page added nothing. Termination comes from the VM: `CanContinuePaging` stops on `HasMore == false`, on a cursor that did not advance (gRPC needs a changed non-empty `history_offset`; Article stops only on a truly repeated `offset`), on failure, and on a 5-consecutive-empty-page cap (`stage=empty-page-cap`). `PublishFeedError` now clears `CanLoadMore`, so a failed page ends the cascade.
- **Article login gate (scope):** the gate now applies to All/Video/Season only. Article reaches `ArticleV2` with cookies while signed out; cancel still leaves a visible login-required state and sends no request on the gRPC tabs.
- **Repost pane:** kept as approved (no feed-page restriction).

## Status

| ID | State |
|---|---|
| BR-01 | resolved — medialist mapping, 收藏夹/合集 split, ad/banner dropped, compact placeholders |
| BR-02 | resolved — music label mapped, no empty-card rework |
| BR-03 | resolved — synthetic season cards marked, author/interaction chrome hidden, nav via source URL |
| BR-04 | resolved — generation + captured tab guard, queued refresh, owner-only completion |
| BR-05 | resolved this turn — DP owner fix + short-viewport trigger + filtered-page completion |
| BR-06 | rejected — no code (no web-card fallback, no fabricated VIP/live values) |
| BR-07 | deferred — label heuristic kept, unmarked AI tracks remain undetectable |
| BR-08 | resolved — HTTP/header status preserved, login-required state with retry, no empty-success, no REST fallback *for errors* |
| BR-09 | resolved — cursor restored on live teardown (`StopPlay`, `Unloaded`, `SetFullScreen(false)`, `ShowControl(true)`) |
| BR-10 | resolved — `/x/relation/stat` fills `owner_ext.fans`, `HasFans` hides the label, follow toggle cannot drive a synthesized zero negative |
| BR-11 | resolved by fallback — gRPC rejection no longer blanks the feed (see below) |
| BR-12 | resolved — seven leak sites closed |

## BR-11 in one paragraph

`DynAll` is rejected with HTTP 200, an empty body and `bili-status-code: -663` (undocumented; `-352`/`-412`/`-799` are the documented risk codes). The user overrode the review's rejection of a REST fallback, so the All and Video tabs now serve `dynamicAPI.DyanmicNew` when gRPC fails: `restFeedMode` is sticky, REST keeps its own `offset`/`has_more` cursor, gRPC cursors are cleared on the switch and never sent to REST, a rejection during `LoadMore` republishes REST page 1, and `Refresh()` retries gRPC exactly once. `DynAll` also sends `LocalTime = 8` now, mirroring `DynVideo`. The transport still names `grpc-status`/`bili-status-code`, so a future report can identify the server's code.

## BR-12 leak sites (all fixed)

Retention edge → fix: `DataTransferManager.DataRequested` (3 pages) → subscribe/detach with navigation; `SystemMediaTransportControls.ButtonPressed` → `-=` before `+=` and on unload; `MyFrame.ClosedPage` (5 pages) → idempotent subscribe helper detached in `OnNavigatedFrom`; static `MessageCenter` events (`LiveRoomVM`, `AnimePage`, `MoviePage`) → removed in `Dispose`/`OnNavigatedFrom`; `LiveMessage.heartBeatTimer` static-per-connection → instance timer, previous `LiveMessage` disposed on reconnect; `Utils.sb` unbounded → 8 KiB tail cap under a lock; per-request `HttpClient` → shared client with access-key headers moved per request. Deliberately unchanged: `Utils.sp` (bounded), `MyListView` subscriptions (self-to-self, not a leak), `Carousel`/`LiveDanmaku`/`CookieService`/`HttpHelper` (audited clean).

## Verification

- `-Fast`: 0 errors, 98 s.
- Full run produced `Z:\BiliLite.UWP_1.0.9.10_x64_Test\BiliLite.UWP_1.0.9.10_x64.msix` (69,696,220 B) and `Z:\BiliLite.Packages_1.0.9.10_x64_Test\BiliLite.Packages_1.0.9.10_x64.msix` (95,369,891 B). The first attempt hit `CS2012` locked by `csc`; the script's new detect-and-retry path killed the leftover node and rebuilt cleanly, which is why the log carries two error lines and still ends with both packages.

## Needs `[device]` confirmation

Feed fallback on a rejected `DynAll`; filtered-page pagination; Article while signed out; login overlay + retry; cursor after closing a live; fan count on a web-loaded video; leak counts (open/close ~10 video and ~10 live tabs, force GC twice, count page/control/VM instances and handles).

## Open for the reviewer

1. BR-11: accept the fallback, or ask for the real-buvid fix (`/x/frontend/finger/spi` → metadata) to replace it?
2. Anything you want changed in the empty-page cap (currently 5) or in the cursor-progress rule for Article?
