# Bug report — round 2 (implemented against `code_review.md`)

Orchestrator → remote reviewer. Branch `work`, review round implemented on top of `1fb7aa4`, this increment on `5237b9d`.

`code_review.md` decisions applied: BR-01 approved (mapping), BR-02 approved (label only), BR-03 approved (option 3), BR-04 approved with the skipped-refresh fix, BR-05 approved a+b, BR-06 rejected, BR-07 deferred, BR-08 approved (error fidelity + login gate; empty-success shortcut and REST fallback rejected). Two bugs the user confirmed at runtime were added as BR-09/BR-10 and fixed.

Verification on this machine: `-Fast` 0 errors in 90 s; full run 0 errors in 121 s producing
`Z:\BiliLite.UWP_1.0.9.9_x64_Test\BiliLite.UWP_1.0.9.9_x64.msix` and
`Z:\BiliLite.Packages_1.0.9.9_x64_Test\BiliLite.Packages_1.0.9.9_x64.msix`.
The `CS2012 … intermediatexaml\BiliLiter.exe … locked by MSBuild.exe` lock still occurred once even with `-nodeReuse:false -p:UseSharedCompilation=false`; the script now detects that exact error in its own output, kills leftover MSBuild nodes and retries once, which is what produced the clean 1.0.9.9 artifacts. Recorded in `document/BUILD.md`. No runtime/UI check was possible here; behavioural claims that need eyes on screen are marked `[device]`.

## Status per ID

| ID | Decision | State |
|---|---|---|
| BR-01 | approved | done — mapping, filtering, placeholders |
| BR-02 | approved | done — label mapped, no empty-card diagnosis acted on |
| BR-03 | approved (option 3) | done — synthetic cards marked, chrome hidden |
| BR-04 | approved incl. skipped refresh | done — generation + queue + captured values |
| BR-05 | approved a+b, c skipped | done — DP owner + short-viewport trigger |
| BR-06 | rejected | no code |
| BR-07 | deferred | no code; limitation documented below |
| BR-08 | approved (narrow) | done — error fidelity + login gate; no REST fallback, no empty-success |
| BR-09 | user-confirmed bug | done — pointer cursor restored on live teardown |
| BR-10 | user-confirmed bug | done — fan count fetched for web detail |
| BR-11 | user-confirmed bug | resolved — REST feed fallback (user overrode the review's rejection) |
| BR-12 | user-reported leak | done — seven retention edges closed |

## BR-01 — unmapped families, medialists, ad/banner

- `MapUserCard` now returns `null` for `GrpcDynamicType.Ad`/`Banner` before normalization, and `MapToUserCards` skips null; a forwarded card whose origin maps to null falls back to `MissingCard(OrigDynIdStr)` so the parent stays readable.
- `MapCardMajor` has a `DynMedialist` branch → `DynamicCardModel2024_major.medialist` (`DynamicCardModel2024_list`, new `long id` member); `MapCardType` → `DYNAMIC_TYPE_MEDIALIST`; `ParseType2024` → `MediaList`; `OneRowTemplate`.
- `DynamicParse` splits the `MediaList` case by major member: `medialist != null` → tag `收藏夹`, id = medialist id, subtitle = server `sub_title`, **no** `AID`; otherwise the previous UGC block verbatim (`合集`, `AID = ugc_season.aid`, 播放/点赞).
- Comments for medialists stay on the dynamic id: `GetCommentId` has no medialist case, so it falls through to `Extend.DynIdStr`; the page's `Tag == "收藏夹"` path already sends a `Dynamic` comment on `ReplyID`.
- Other unmapped families keep a placeholder card; both JSON dumps in the conversion paths and both in the conversion-catch paths are now the compact string `动态加载失败` / empty `ContentStr`, so the `Other` template renders only its own line. The `DynamicID` in the 2024 catch was `item.modules…jump_url` (a URL, and a null deref); it is now `item.id_str`.
- Filtering happens before display conversion; `hasMore`/`offset` still come from the server, so a filtered page that maps to zero visible cards does not end pagination.

## BR-02 — music label

`MapDynamic` DynMusic branch now writes `Label1` to `article.label` (the accessor `viewtext`/subtitle reads) instead of `article.desc`; title, cover, jump URL, song comments and navigation untouched. The shared DTO stays, per the review.

## BR-03 — synthetic follow-season cards

`MapToUserSeasonCards` emits `type = "CUSTOM_SEASON"` and an empty `basic` (the previous `comment_type = 1` / `rid_str = season_id` is gone, so no video-comment request can be built from a season id). New `DynamicDisplayType.CustomSeason` routes to `OneRowTemplate`; `ParseOneRowInfo2024` sets `ShowAuthorHeader = false` / `ShowInteractionRow = false`, and `DynamicItemControl.xaml` now binds avatar, author row, repost header and interaction row to computed `Visibility` properties that AND the new flags with the existing `IsRepost` conditions — every non-synthetic card keeps its exact previous visibility. Navigation stays on `OneRowInfo.Url` (`FollowListItem.Url`); no per-card detail call, no invented comment type.

## BR-04 — feed request races

`GetDynamicItems` captures `generation = ++requestGeneration`, `DynamicType` and `UserDynamicType` before the first await, and calls `IsCurrentRequest` after every await (gRPC all/video/season, article REST, legacy REST) and again before publishing `Items`, `feedPage`, `historyOffset`, `updateBaseline`, `articleOffset`, `CanLoadMore`, `lastPageFailed`, error state and toasts. `Loading`/`feedRequestInFlight` are cleared only by the owning request. `Refresh()` no longer returns while loading: it invalidates, sets `refreshQueued`, and the superseded request's `finally` starts the queued refresh for the newest tab. Latest tab wins; no per-tab cache. Legacy branch dispatch also uses the captured values.

## BR-05 — `MyListView`

`LoadMoreBottomOffsetProperty` registered with `typeof(MyListView)` (default `100d`); `ScrollViewer` null-guarded and unsubscribed before resubscribe. New short-viewport trigger for every paginated `MyListView`: single-flight `Dispatcher.Low` check, then guards `scrollViewer != null` → `LoadMoreCommand != null` → `CanLoadMore` → `ActualHeight > 0` (else one-shot `LayoutUpdated`) → `ScrollableHeight == 0` → `_triggeredItemsVersion != _itemsVersion`. Only item-count or `ItemsSource` changes bump the version, so a second trigger requires new content; the version is consumed before `Execute`, so a synchronous command cannot re-enter. Re-arm sources: `Items.VectorChanged`, `ItemsSource` change, plus non-arming re-checks on `Loaded`/`SizeChanged`/`CanLoadMore false→true`.

Coordination note for you: the same control backs the repost pane (`Controls/Dynamic/DynamicRepostControl.xaml`, `LoadMoreBottomOffset="0"`), so that pane now auto-paginates while short. Bounded by `has_more`; if you want it excluded, say so and I will gate the trigger to the feed pages only. `[device]`

## BR-06 / BR-07 — rejected and deferred, no code

BR-06: no web-card fallback added, no fabricated live/VIP values. BR-07: the label heuristic stays; the limitation is that a machine-generated track labelled as a plain language (no `自动`, `lan` not `ai-*`) is not detected. `HasSubtitleItemModel` still deserializes only `id/lan/lan_doc/subtitle_url`.

## BR-08 — feed rejection fidelity and login gate

Transport (`src/BiliLite.gRPC/GrpcRequest.cs`): non-2xx now returns `status = false` before the length heuristic, and `grpc-status`, `grpc-message`, `bili-status-code` are read from `response.Headers` (never `TrailingHeaders`) into the message and, when numeric, into `code`. The short/empty-body case stays a failure (`-102` or the server code) with a message that says the response was empty and whether server status was available. No `Array.Empty<byte>()` shortcut, no REST fallback; `GetDynAll`/`GetDynVideo` still throw the enriched message.

Feed (`Modules/User/DynamicVM.cs`): before any `UserDynamic` request, a signed-out session gets the login dialog; cancel leaves `ShowError = true`, `ErrorMsg = "请先登录后查看动态"`, no request sent. Failures remain retryable: `CanLoadMore = false`, `lastPageFailed = true`, error published only when the list is empty, items/cursors of a failed load-more untouched. `Pages/Home/UserDynamicPage.xaml` now renders that state — message plus a 重试 button bound to `RefreshCommand` — so the login-required and load-failed states are visible, not just a toast. `[device]`

Two follow-ups for you:
1. The login gate covers the Article tab too, because it shares `DynamicType.UserDynamic`. Drop it to gRPC-only if the REST article feed must work signed out.
2. Your measured-`-400/-101` evidence was not reproducible here against a signed-in session, so the enriched message may still read `HTTP 200 … 没有数据返回 … 未返回grpc-status等服务端状态` when the server sends trailers only. That is the approved "clear unknown-response failure", not a success.

## BR-09 — pointer cursor stays hidden after closing a live (user-confirmed)

`Pages/LiveDetailPage.xaml.cs` hid the OS cursor in `ShowControl(false)` and restored it only on pointer move/exit, so closing the page while controls were hidden left `PointerCursor = null` app-wide. New `RestorePointerCursor()` (null-safe, assigns only when the cursor is currently null) is called from `StopPlay`, `LiveDetailPage_Unloaded`, `SetFullScreen(false)` and `ShowControl(true)`. Hide sites and timers unchanged. `[device]`

## BR-10 — subscriber count missing on video detail (user-confirmed)

`VideoDetailPage.xaml:270` binds `VideoInfo.owner_ext.fans`, but the primary web endpoints carry no `owner_ext`, so it stayed null and the number rendered empty. `LoadVideoDetail` now synthesizes `owner_ext` before assigning `VideoInfo` (a plain property with no notifier, so a later assignment would not reach the binding), and only when the payload had none it calls the new `VideoAPI.RelationStat(mid)` (`/x/relation/stat`) and sets `fans`, raising `DoPropertyChanged("owner_ext")`. Failures leave `fans = 0`, no toast, no error page. New `HasFans` hides the label instead of leaving a dangling "粉丝". `DoAttentionUP` adjusts the counter only when `fans > 0`, so a synthesized zero cannot go negative. The app-endpoint path never issues the extra request. `[device]`

Note on your agent's concern about `btnOpenUser`: `DisplayConvert` inverts booleans and `showStaff` is `staff != null`, so `Visibility` on that button means "visible when there is no staff list" — the staff-less UP row is correct, not a mis-wire. No change made.

## BR-11 — `DynAll` rejection now falls back to the REST feed (user-confirmed)

Original failure (before this round):

```
请求失败,HTTP 200,没有数据返回,responseBytes=0,服务端状态: grpc-status=2; grpc-message=-663; bili-status-code=-663
   at GrpcService.GetDynAll(...) at DynamicVM.GetDynamicItems(...)
```

Facts: `-663` is not in the public error table (`-352` risk, `-412` blocked, `-799` too frequent), the body is empty, HTTP is 200, and the gRPC metadata ships a hardcoded fake buvid (`src/BiliLite.gRPC/GrpcRequestHedaer.cs:27`) shared by every install. `DynAll` also sent `local_time = 0` while `DynVideo` sends 8. None of that identifies the server's rule, so the app cannot rely on gRPC for the feed.

**The user overrode the earlier decision that a clearer error is enough: the feed must work.** Implemented:

1. **Fallback (primary):** for the All and Video tabs, a rejected gRPC request now switches the VM to the REST feed (`dynamicAPI.DyanmicNew`) instead of throwing. `restFeedMode` is sticky per VM; the switch logs `stage=rest-fallback reason=<message>`, keeps the enriched text for the case where the REST page is also empty, and clears the gRPC cursors.
2. **Separate cursors:** REST pagination uses its own `restOffset` from `data.offset` (`DyanmicNew` gained an `offset` parameter, signed like the rest of the query) and its own `has_more`; `historyOffset`/`update_baseline` are never sent to REST, which answers the original objection to a fallback rather than ignoring it. A rejection during `LoadMore` republishes REST page 1 instead of appending onto gRPC items.
3. **Bounded retry:** `Refresh()` resets `restFeedMode` once and retries gRPC; a second failure falls back again. One gRPC attempt per request, so it cannot loop.
4. **Diagnostics kept:** `DynAll` now sends `LocalTime = 8` like `DynVideo`, transport failures still name `grpc-status`/`bili-status-code`, and the VM maps `-352/-412/-663` to `服务端拒绝了动态请求（-663），请稍后重试` when a REST page fails too.

**For you to review:** this deliberately overrides your BR-11 rejection of a REST fallback, on the user's explicit instruction. If you still object, the alternative with a real chance of removing the root cause is obtaining a server-issued buvid (`/x/frontend/finger/spi`) and using it in the gRPC metadata instead of the constant - say the word and I will build that instead, keeping this fallback behind the same flag. `[device]` - I cannot reproduce `-663` without a signed-in session, so the fallback path is source-verified, not runtime-proven.

## BR-12 — memory leaks (user-reported)

A read-only audit produced seven retention edges with source evidence; all seven are closed. Every one was a delegate on a static or per-view singleton that captured a page/control, so navigation could never collect the graph.

|#|Retained by|Where|Fix|
|---|---|---|---|
|1|`DataTransferManager.GetForCurrentView().DataRequested` (per-view singleton, subscribed in ctors of three pages, never removed; also made stale handlers run on the next share and dereference nulled VMs)|`VideoDetailPage:62`, `SeasonDetailPage:41`, `LiveDetailPage:53`|subscribe in `OnNavigatedTo` (`-=` then `+=`), unsubscribe in `OnNavigatedFrom`|
|2|`SystemMediaTransportControls.ButtonPressed` accumulated on every `Loaded` (`NavigationCacheMode.Enabled`), retaining `PlayerControl` + `MediaPlayer` and firing duplicate media keys|`PlayerControl.xaml.cs:330`|`-=` in `Loaded` before `+=`, `-=` in `Unloaded` before nulling|
|3|`MyFrame.ClosedPage` — five pages used a no-op `-=`/`+=` pair, so every page instance ever loaded stayed rooted and `frame.Close()` invoked dead pages|`VideoDetailPage`, `SeasonDetailPage`, `LiveDetailPage`, `WebPage`, `LocalPlayerPage`|one `SubscribeClosedPage`/`UnsubscribeClosedPage` helper per page, hooked from `Loaded`/`OnNavigatedTo`, detached in `OnNavigatedFrom`|
|4|static `MessageCenter.LoginedEvent/LogoutedEvent` — `LiveRoomVM` never removed its handler in `Dispose`, pinning timers/collections; `AnimePage`/`MoviePage` never unsubscribed|`LiveRoomVM.cs:43`, `AnimePage.xaml.cs:25`, `MoviePage.xaml.cs:32`|remove in `Dispose`, page-level subscribe/unsubscribe in `OnNavigatedTo`/`OnNavigatedFrom` plus the tab-close path|
|5|`LiveMessage.heartBeatTimer` was `static` but per-connection: reconnecting stranded the previous enabled timer forever and `Dispose` could stop another room's timer|`LiveMessage.cs:91`|instance field, own timer stopped/disposed in `Dispose`; `LiveRoomVM` now disposes the previous `LiveMessage` on reconnect|
|6|`Utils.sb`: static `StringBuilder` appended per HTTP call, never cleared|`Utils.cs:111`|capped at 8 KiB (tail keeps the newest text), lock added because callers append from background threads|
|7|`new HttpClient()` per gRPC request, undisposed|`GrpcRequest.cs:122`|one shared static client; `authorization` and `x-bili-metadata-bin` (both access-key dependent) moved to the per-request message so concurrent calls cannot cross-contaminate|

Left alone deliberately: `Utils.sp` pins one `SettingPage` (bounded, one instance); `MyListView` was audited and is **not** a leak (its subscriptions are self-to-self; the audit says so explicitly); `Controls/Carousel`, `LiveDanmaku`, `CookieService` and `HttpHelper` were checked and found clean.

`[device]` confirmation: open/close ~10 video and ~10 live tabs, force GC twice, and count `VideoDetailPage`/`PlayerControl`/`LiveRoomVM`/`LiveMessage` instances plus handles. That measurement was not possible here.

## Open items / risks I want ruled on## Open items / risks I want ruled on

1. BR-05 trigger also affects the repost pane (see above) — gate it or keep it?
2. BR-08 login gate on the Article tab — keep or limit to gRPC feeds?
3. `DynamicParse` still labels UGc collections `合集`; the split relies on `major.medialist` vs `major.ugc_season`, which is what the proto union provides. If the server ever emits a medialist with an empty `sub_title`, the OneRow subtitle is blank by design (no fabricated play counts).
4. No runtime verification was possible for BR-04/BR-05/BR-09/BR-10/BR-11 or the login overlay; all five need a sideload of 1.0.9.7 to confirm.
5. BR-11 needs one of the three remedies above; option 2 is the only one that plausibly removes the root cause, option 1 is the only one that plausibly restores the feed today.
