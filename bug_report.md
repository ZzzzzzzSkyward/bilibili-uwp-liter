# Bug report — round 2 (implemented against `code_review.md`)

Orchestrator → remote reviewer. Branch `work`, implemented on top of `1fb7aa4`.

`code_review.md` decisions applied: BR-01 approved (mapping), BR-02 approved (label only), BR-03 approved (option 3), BR-04 approved with the skipped-refresh fix, BR-05 approved a+b, BR-06 rejected, BR-07 deferred, BR-08 approved (error fidelity + login gate; empty-success shortcut and REST fallback rejected). Two bugs the user confirmed at runtime were added as BR-09/BR-10 and fixed.

Verification on this machine: `-Fast` 0 errors in 52-56 s; full run 79 s producing
`Z:\BiliLite.UWP_1.0.9.6_x64_Test\BiliLite.UWP_1.0.9.6_x64.msix` and
`Z:\BiliLite.Packages_1.0.9.6_x64_Test\BiliLite.Packages_1.0.9.6_x64.msix`.
One full run aborted first with `CS2012 … intermediatexaml\BiliLiter.exe … locked by MSBuild.exe (27452)`: a reused MSBuild node still held the intermediate exe. The node exited, the rerun at the same version was clean. Recorded in `document/BUILD.md`. No runtime/UI check was possible here; the three behavioural claims below that need eyes on screen are marked `[device]`.

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

## Open items / risks I want ruled on

1. BR-05 trigger also affects the repost pane (see above) — gate it or keep it?
2. BR-08 login gate on the Article tab — keep or limit to gRPC feeds?
3. `DynamicParse` still labels UGc collections `合集`; the split relies on `major.medialist` vs `major.ugc_season`, which is what the proto union provides. If the server ever emits a medialist with an empty `sub_title`, the OneRow subtitle is blank by design (no fabricated play counts).
4. No runtime verification was possible for BR-04/BR-05/BR-09/BR-10 or the login overlay; all four need a sideload of 1.0.9.6 to confirm.
