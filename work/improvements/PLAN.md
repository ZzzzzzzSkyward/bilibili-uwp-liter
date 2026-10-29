# Remaining feature plan

Updated 2026-10-29 against target `work` at `e9c624aa4300f781bbad523925d1358b644f2b12`.

## Working agreement

- This turn produces a plan only. No application or release-script implementation.
- Use `/workspace/biliuwp-lite` at `ca2caddc597b3faf05b4dac6dcb226aa0694365b` as the sole reference for ported behavior. Target source and history establish what already exists. Do not use external API documentation, other forks, or guesses about server behavior.
- GitHub access is for repository setup/source retrieval only. No internet API experiments or application API calls.
- No tests or validation, including existing offline probes, builds, restore, application launch, protocol generation, package inspection, XML checks, or runtime checks. The only check is `git diff --check` for formatting. These restrictions also apply to the implementation batches below until the user changes them.
- Root plans and coordinates; use `gpt-6-luna` subagents with high reasoning effort for detailed source work and later implementation. This document does not authorize starting implementation in the planning turn.
- Prefer small working changes in the existing classes. Refactoring is allowed when it directly helps the feature. Avoid a DI migration, generic service framework, renderer rewrite, or unrelated defensive-code pass.
- Keep the existing player, NSDanmaku, download/single-file routes, authentication services, and working REST fallbacks as the integration points. SponsorBlock stays excluded. Live tiles remain optional.

Target paths below are relative to `/workspace/bilibili-uwp-liter`; sibling paths are relative to `/workspace/biliuwp-lite`. Both use the `src/` layout. The sibling has been restored locally at the pinned revision. The target now tracks `origin/work`; the initial checkout was stale at `e2c7958`.

## What is already implemented

These are source/history findings, not new runtime verification. Do not repeat the old plan's completed phases.

| Area | Current implementation | Planning consequence |
| --- | --- | --- |
| LM-01–LM-05 | Manifest models/loader; folder import and saved grants; playlist adapter; independent video/audio selection; JSON/SRT/VTT subtitles; toggleable merged XML danmaku | Core local-media work is complete. Keep the current schema and services. |
| API-01 | `CookieService`, `WbiKeyService`, compatibility `ApiHelper`, `BaseApi`, Emote shim, `DanmakuApi.Like` request builder | Do not re-create these services or the like endpoint. |
| AUTH-01a/b | Verification/refresh builders, password secondary-verification UI, isolated cookie exchange, account persistence, logout cleanup | No established remaining authentication feature. Do not add speculative QR/history variants. |
| Read API migration | Playback/detail, feeds, catalog/search, comments, account/library/live reads already migrated | No blanket endpoint or namespace migration. Add methods only with a concrete caller. |
| gRPC playback/danmaku | Current playback and TV danmaku protos, request/parser integration, user-info overload | Already ported; adapt transport in place. |
| gRPC feeds | `GrpcService.GetDynAll/GetDynVideo`, home/user callers, `DynamicGrpcMapper`, paging and REST fallback | Do not port these again. |
| Danmaku settings | Existing player settings, filter editing/sync, local track loading and NSDanmaku rendering | Remaining work is list/actions and targeted settings additions. |
| Player notifications | Generic `MessageToast` and central `ToolTip/TxtToolTip` exist; sibling-style keyed player overlays do not | Adapt existing input handlers to a player-local overlay service. |

Useful implementation landmarks: LM commits `87b3480`, `e939a6b`, `1bc25b6`, `90ac3de`, `8106491`; API services `a6ae587`/`241c4e5`; authentication `fbcf805`/`c4687a0`; dynamic protocol/feed work `1ca07b6`/`96fb4c7`. Later changes matter: `d0d3e8d` adds REST fallback, `082ec8e` fixes feed-specific paging/login behavior, and `d51f5e4` fixes filtered-empty REST paging and rich-content ownership. Preserve those results when extending feeds.

`HANDOVER.md` and older progress sections contain historical instructions to skip fetches or run offline probes. The current user request supersedes those instructions. Its old “GRPC-01 next” statement predates later gRPC/feed commits.

## Suggested implementation order

| Batch | Concrete deliverable | Depends on |
| --- | --- | --- |
| ~~G1~~ | ~~Honor the account app key when building gRPC headers~~ — **done**, commits `cfcd3de`, `75e8ecb`; report `work/improvements/G1_REPORT.md` | Existing transport |
| G2 | Use sibling archive search in the existing user-video keyword search | G1; two additional proto files |
| G3 | Load dynamic details through the existing gRPC mapper | G1; no new proto |
| G4 | Add gRPC user-space dynamics with source-specific paging | G1; existing mapper and proto |
| D1 | Browse loaded danmaku, copy text, and like a selected online comment | Existing pool and like builder |
| D2 | Import/export filter JSON and fix user-filter persistence | Existing settings collections |
| N1 | Keyed player overlays for volume, brightness, seek and held speed | Existing player handlers |
| G5 | Add gRPC repost listing using the existing detail/repost UI | G3's reusable card conversion |
| C1–C3 | Display Bilibili video collections and remote playlists, with selection and continued playback | Existing video detail, medialist builders and playlist UI; independent of gRPC |

Finish one usable batch at a time. D1, D2 and N1 can be implemented independently of the gRPC sequence; avoid simultaneous edits to `PlayerControl.xaml(.cs)`. For each batch, the implementation handoff should name changed files, describe the resulting behavior, update the relevant progress note, and report only the permitted formatting check. Do not append a testing or hardening phase.

## G1 — App-key-aware gRPC headers

**Status: implemented and verified locally (commits `cfcd3de`, `75e8ecb`).** Ported the pinned header factory into `src/BiliLite.gRPC/Api/`, deleted the old `GrpcRequestHedaer.cs`, and moved every profile/account header onto the per-request `HttpRequestMessage`. Unknown or empty app keys fall back to the Android profile, so the target's default login key keeps its pre-G1 behavior. Verification and the two parity decisions are recorded in `work/improvements/G1_REPORT.md` and `IMPROVEMENT_PROGRESS.md`.

**Gap:** `src/BiliLite.gRPC/GrpcRequest.cs::SendMessage(..., GrpcBiliUserInfo)` currently forwards only `AccessKey`. `UserMid` and `AppKey` never influence headers. `CreateSharedHttpClient` fixes Android headers globally. The sibling chooses Android/iOS header builders by app key.

**Sibling references:** `src/BiliLite.gRPC/Api/{GrpcRequestHeaderFactory,IGrpcRequestHeader,AndroidGrpcRequestHeader,AndroidGrpcRequestHeaderConfig,IosGrpcRequestHeader,IosGrpcRequestHeaderConfig,Constants}.cs`, plus its `GrpcRequest.cs` and `GrpcBiliUserInfo.cs`.

**Target changes:** `src/BiliLite.gRPC/GrpcRequest.cs`, `GrpcRequestHedaer.cs`, and only the header helper files needed under `src/BiliLite.gRPC/Api/`.

1. Build the per-request headers from the full `GrpcBiliUserInfo`, choosing the sibling's supported Android/iOS profile by `AppKey`. Keep the existing Android behavior for the access-key-only overload and target-specific keys that have no sibling profile; do not break existing custom-key configuration.
2. Keep the shared `HttpClient`; put profile/account headers on `HttpRequestMessage`, rather than changing the client's global headers when accounts or app profiles change.
3. Reuse the current metadata/config proto types. Retain the target's HTTP status reporting, readable server status, diagnostics and response handling. Port header construction only, rather than replacing transport with the sibling's older implementation.
4. Keep `Api/PlayURL.cs` and UWP `Services/GrpcService.cs` using the existing user-info overload. No new caller abstraction or DI setup is necessary.

**Result:** current playback/feed callers can actually use the app-key context they already pass. This is the remaining transport part of old GRPC-01. The sibling also uses fixed buvid values; this batch does not establish a real-buvid solution or prove the server's feed rejection is fixed. Keep REST fallback.

## G2 — User-video keyword search

**Existing entry point:** `Pages/UserInfoPage.xaml::searchVideo` binds `Keyword`; `.xaml.cs::searchVideo_QuerySubmitted` refreshes `Modules/User/UserDetail/UserSubmitVideoVM`. Its `GetSubmitVideo` currently sends both browsing and keyword searches through `UserDetailAPI.SubmitVideosWbi`.

**Sibling references:** `Services/GrpcService.cs::SearchSpaceArchive`, `ViewModels/User/UserSubmitVideoViewModel.cs::GetSubmitVideo`, and `bilibili/app/interfaces/v1/space.proto` under `src/BiliLite.gRPC/`.

1. Add only `bilibili/app/interfaces/v1/space.proto` and `bilibili/app/archive/v1/archive.proto` to the target gRPC project. `space.proto` also imports preload and Dynamic V2; their avatar dependency chain is already present. The target has 16 proto entries now, not the original nine. Do not copy the sibling's entire proto tree or generate code in this session.
2. Add `GrpcService.SearchSpaceArchive(mid, page, pageSize, keyword)`. Match sibling `SearchArchiveReq.Keyword/Mid/Pn/Ps`, endpoint `bilibili.app.interface.v1.Space/SearchArchive`, and reply `Archives/Total`.
3. In `UserSubmitVideoVM.GetSubmitVideo`, use that method for nonempty keyword searches with the default category/order. `SearchArchiveReq` has no category/order fields: keep the REST branch for nondefault `SelectTid` or `SelectOrder`, rather than silently ignoring controls.
4. Map each reply's nested `Archive` directly into the target's existing `SubmitVideoItemModel`: aid, title, cover, description, author, duration text, publication time, view/comment/danmaku counts. Follow the sibling mapping's field meanings; do not introduce AutoMapper for this one conversion.
5. Use `Total` and the existing page/item count for loading more and `Nothing`. A successful empty search is an empty result, not a reason to switch sources. Keep REST as the fallback for a failed gRPC search; start from page one when changing source and keep the chosen source for subsequent pages of that query.

**Result:** the current search box gains the sibling's archive-search path without a new page or changes to ordinary category/order browsing.

## G3 — Dynamic detail through gRPC

**Target:** `Services/GrpcService.cs`, `Modules/User/DynamicVM.cs::GetDynamicDetail`, `Helpers/DynamicGrpcMapper.cs`. Existing navigation is `Pages/User/DynamicDetailPage.xaml.cs::OnNavigatedTo`.

**Sibling:** `Services/GrpcService.cs::GetDynDetail`, `ViewModels/UserDynamic/UserDynamicDetailViewModel.cs::LoadData`.

1. Add `GetDynDetail(id)` using `DynDetailReq.DynamicId`, the `Dynamic/DynDetail` endpoint, and `DynDetailReply.Item`. All message types are in the already-included Dynamic V2 proto.
2. Map `reply.Item` through `DynamicGrpcMapper.MapToUserCards`, then the existing `ConvertToDisplay2024` path. Keep the existing IDs, comment targets, origin/repost cards and navigation behavior; no new card templates.
3. Keep `DynamicAPI.DynamicDetail` and `ConvertToDisplay` as the REST fallback when the new route fails or provides no usable detail item. Keep detail loading separate from the All/Video feed's account gate.
4. If repost listing needs the same conversion in G5, extract only the small card-to-display operation from `DynamicVM`; do not refactor the entire view model.

**Result:** an opened dynamic uses the same modern mapping already used by feed cards, while the existing REST detail path remains available.

## G4 — User-space dynamics

**Existing caller:** `Pages/UserInfoPage.xaml.cs` selects `DynamicType.Space`. `Modules/User/DynamicVM.cs` currently handles it with `DynamicAPI.SpaceHistory(Uid, idx)` and legacy cards.

**Sibling:** `Services/GrpcService.cs::GetDynSpace`, `ViewModels/UserDynamic/UserDynamicSpaceViewModel.cs::GetDynamicItems/HandleDynamicResults`.

1. Add `GetDynSpace(mid, from, page, offset)` using `DynSpaceReq.HostUid/Page/From/HistoryOffset`. Read `DynSpaceRsp.List/HasMore/Offset`. No additional proto files.
2. In the existing Space branch, map `List` with `MapToUserCards` and `ConvertToDisplay2024`. Use `HasMore/Offset` for paging and reset them when the viewed user changes or refresh starts.
3. Retain `SpaceHistory` as REST fallback. Keep its `idx` independent from gRPC's history offset, following the source-switch arrangement already present for the All feed. Do not reuse the All feed's account gate for Space, or alter Topic/Article behavior.
4. Reuse the current request lifecycle and page-loading behavior. Keep the recent filtered-empty-page fix: displayed item count alone does not determine whether another source page exists.

**Result:** user-profile dynamics gain the sibling's gRPC data source within the existing tab and card renderer.

## D1 — Danmaku list and actions

**Already done:** `Models/Requests/Api/DanmakuApi.cs::Like(dmid, oid, op)` constructs the cookie/CSRF request. `Modules/Player/PlayerVM.cs::GetDanmaku` already maps server IDs into NSDanmaku `rowID` and author hashes into `sendID`.

**Sibling:** `Controls/Common/DanmakuListControl.xaml(.cs)`, `ViewModels/Common/DanmakuListViewModel.cs`, `Services/NsDanmakuController.cs::FindDanmakus`, and `Models/Common/Danmaku/`.

1. Create `Controls/DanmakuListControl.xaml(.cs)` with a small row model in its code-behind, and place its flyout in the existing `Controls/PlayerControl.xaml` toolbar. Reuse the sibling's two modes: comments within ten seconds of the playhead, and all comments currently loaded in the pool. “All” means loaded comments; do not download an entire video just to open the list.
2. In `PlayerControl.xaml.cs`, expose a narrow snapshot from the existing `danmakuPool`, sorted by time. Map `rowID/sendID/text/time` to a small row model. A helper and the existing NSDanmaku model are enough; no `IDanmakuController`, alternate renderer, or AutoMapper dependency.
3. Add Copy and Like row actions. Use the target clipboard/message helpers and the existing `DanmakuApi.Like(rowID, CurrentPlayItem.cid, 1).Request()`, interpreting the API response through the target's current `ApiResults` conventions.
4. Scope remote Like to online Bilibili playback with the current cid/account and a server `rowID`. Newly sent comments have no server ID in the current local insertion path, so those rows offer copy only. Local XML and manifest playback still support browsing/copying without a remote action. Refresh the list for the current item when reopened.
5. The sibling UI implements Like only. An explicit Cancel like action may send `op=2` for a row liked during this open session; do not display an invented server-synchronized liked state or count. If adding block-author, reuse `ShieldUsers` and the existing settings persistence, not a separate filter store.
6. Add the new C#/XAML entries to `BiliLite.UWP.csproj` using its explicit Compile/Page pattern.

**Result:** users can inspect loaded comments and use the already-ported like API. Existing rendering, sending, XML import and local-track selection continue through their current paths.

## D2 — Useful danmaku settings additions

**Target:** `Modules/SettingVM.cs`, `Pages/SettingPage.xaml(.cs)`, and `Helpers/SettingHelper.cs` constants already used there.

**Sibling:** `ViewModels/Settings/VideoDanmakuSettingsControlViewModel.cs::ImportDanmuFilter/ExportDanmuFilter/ImportDanmakuFilterCore/ExportDanmakuFilterCore` and `Controls/Settings/VideoDanmakuSettingsControl.xaml(.cs)`.

1. Add Import JSON and Export JSON beside the current filter-sync controls. Keep the existing keyword, regex and user collections and existing settings keys.
2. Use the sibling interchange fields: a JSON array with `type` and `filter`, where type 0 = keyword, 1 = regex, 2 = user. The sibling also serializes `id` and `opened`; its importer uses only type/filter, matching the target's string collections. Export `bilibili.block.json`; import unions entries into the corresponding current collections without duplicates. Reuse `SettingVM.IsValidDanmuRegex` instead of copying the sibling's narrow export heuristic that drops legitimate patterns. File import does not invoke cloud sync.
3. Fix the concrete target mismatch in `SettingPage.xaml.cs::DanmuSettingAddUser_Click`: it currently persists `ShieldUsers` under `VideoDanmaku.SHIELD_WORD`. Match the sibling's user-list storage under `SHIELD_USER`.
4. Use the target's existing file pickers/Newtonsoft.Json and ordinary XAML buttons. No SettingsCard package, new settings architecture, engine selector, or purely cosmetic regrouping is required.

**Result:** filters can be moved between installations as files, and adding a blocked user persists to the correct collection. Keep cloud filter sync as its separate existing command.

## N1 — Player overlay toasts

**Target integration points:** `Controls/PlayerControl.xaml` video overlay near `ToolTip/TxtToolTip`; `.xaml.cs` keyboard actions, `HandleSlideProgressDelta`, `HandleSlideVolumeDelta`, `HandleSlideBrightnessDelta`, holding-speed start/end, and `Dispose`.

**Sibling:** `Services/PlayerToastService.cs`, `Controls/PlayerToast.xaml(.cs)`, `ViewModels/PlayerToastViewModel.cs`, and their callers in `Controls/PlayerControl.xaml.cs`.

1. Add `Controls/PlayerToast.xaml(.cs)`, `Services/PlayerToastService.cs` and a player-local overlay Canvas. Construct one service per `PlayerControl`; the target has no sibling-style DI startup to register into. Use direct properties or its existing notification pattern instead of adding a container.
2. Adapt the sibling's keyed `Show`, `KeepStart` and `KeepClose` behavior for Volume, Brightness, Progress, Msg, Accelerating and Speed. Updating a key replaces its text and restarts its timeout; held acceleration stays visible until released.
3. Replace existing central-tooltip writes for those same actions with the service calls. Keep their existing volume, brightness, seeking and playback-speed calculations. Each action should produce one overlay rather than both old and new feedback.
4. Keep timer/UI ownership within that player; remove overlays and stop timers in `Dispose`. Position overlays from the current player dimensions rather than copying the sibling's fixed six-slot index. Use UWP dispatcher/timer facilities already present in the target.
5. Omit SponsorBlock types, skip-segment buttons and dependencies from the source toast control. Keep generic application `MessageToast` for existing non-player callers. Add explicit Compile/Page entries for new files.

**Result:** player feedback can update independently by action and held speed has a persistent indicator. This is in-player feedback; it does not require Windows notifications or background tasks.

The old plan's warning about an empty `ShowToast()` referred to `Extensions/Notifications/NotificationShowExtensions.cs`, not `PlayerToastService.Show`, which is implemented in the sibling. Do not port that unrelated stub.

## G5 — Dynamic repost list

**Target:** `Services/GrpcService.cs`, `Modules/User/DynamicRepostVM.cs::GetDynamicItems/ConvertToDisplayRepost`, `Controls/Dynamic/DynamicRepostControl.xaml.cs::LoadData` and the existing detail page.

**Sibling:** `Services/GrpcService.cs::GetDynRepostList`, `ViewModels/UserDynamic/UserDynamicRepostViewModel.cs::GetDynamicItemRepostsCore/HandleDynamicResults`.

1. Add `GetDynRepostList(id, offset)` with `RepostListReq.DynamicId/Offset` and reply `List/HasMore/Offset`. Reuse the existing Dynamic V2 proto.
2. Convert returned items with the modern mapper and the small reusable display conversion from G3, adapting only the repost-list fields/templates that differ. The current `ConvertToDisplayRepost` expects legacy `DynamicCardModel`; do not pass protobuf items into it.
3. Keep `DynamicAPI.DynamicRepost(ID, next)` as fallback and separate its paging cursor from gRPC's. Keep existing repost submission/actions outside this read migration.

**Result:** the existing repost panel gains the sibling data source without a separate page or a full dynamic renderer rewrite.

## Carry-forward release item, separate from feature ports

Current `code_review.md` identifies an unresolved target-specific issue in `build-release-x64.ps1`: `->\s+(\S+\.msix)$` omits artifact paths containing spaces, so the subsequent package-layout work can be skipped. This is not evidence for changing a sibling API.

If release-tooling work is selected later, capture the entire artifact path (including spaces), or derive the current package paths from `OutputDir`/version; do not treat a missing required artifact as successful completion. Keep existing layout helpers and CS2012 retry behavior. Do not build, inspect packages, or run the script under the current format-only instruction.

`bug_report.md` records the earlier splash/layout issue as resolved on 1.0.9.14; that historical result is distinct from this newer review finding. The packaging project remains the source of the export-capable artifact. Whether to offer an additional UWP-only download is a product choice, not a prerequisite for this plan.

## Parked work

- **Live tiles:** optional, outside the sequence above. If selected, port sibling `Services/Notification/LiveTileService.cs`, the necessary parts of `Extensions/Notifications/NotificationShowExtensions.cs`, `src/BackgroundTasks/TileFeedBackgroundTask.cs` and its project/manifest registration as a separate feature. Its async refresh must finish before completing the background-task deferral; the sibling task currently disposes a returned Task without awaiting it. The sibling's `ShowToast()` stub supplies no implemented notification feature.
- **Real buvid:** no source-backed acquisition flow has been established by this plan. App-key-aware headers are useful independently; do not promise they remove feed rejection or remove REST fallback.
- **Alternate danmaku renderer:** no current feature requires FrostMaster or the sibling's controller hierarchy. Keep NSDanmaku.
- **More API/auth variants:** no identified caller requires them. Do not copy all endpoint classes into a different namespace just to match the sibling tree.
- **Local-library expansion:** the original five milestones are implemented. Automatic discovery, new manifest versions, chapters, extra subtitle formats, artwork UI and library management polish need their own concrete request. Keep the current manifest models as the contract; the old illustrative schema does not need to be recreated here.

## Execution boundary

This rewrite is the deliverable for the current turn. No feature, packaging fix, test, validation, build, restore, launch or protocol generation is executed. The next implementation session can take G1 as its first batch, or choose D1/D2/N1 for an independent user-facing feature. Keep all checks limited to formatting until instructed otherwise.

## C1–C3 — Show Bilibili video collections and playlists

**Requested feature:** display the collection/playlist supplied by Bilibili, including its videos and current selection, and let users select a video or continue through the list. This covers UGC collections attached to a video and remote medialist playlists. Local-media manifests, a video's own multi-part `pages`, and PGC seasons remain their existing separate flows.

**Current target:** `Api/VideoAPI.cs::GetMediaList/GetMediaListNotLogin` already build the resource-list requests but have no production caller. `Pages/VideoDetailPage.xaml.cs` already defines `VideoPlaylist` (`Index/Playlist`) and `VideoPlaylistItem` (`ID/Author/Cover/Title`); `OnNavigatedTo`, `ListView_SelectionChanged`, and `player_AllMediaEndEvent` display and advance a materialized list. `Modules/Detail/VideoDetailVM.cs::VideoDetailModel` does not retain `ugc_season`, so collections returned by video detail are currently discarded. Dynamic-feed UGC cards do not supply this detail-page feature.

### C1 — Collection metadata and grouped video list

**Sibling references:** `Models/Common/Video/{VideoUgcSeason,VideoUgcSeasonSection,VideoUgcSeasonSectionEpisode}.cs`, `Models/Common/Video/Detail/VideoDetailModel.cs`, `ViewModels/Video/VideoDetailViewModel.cs::ShowUgcSeason`, and `Pages/VideoDetailPage.xaml.cs::InitUgcSeason/VideoListView_SelectionChanged`.

**Target files:** `Modules/Detail/VideoDetailVM.cs`, new `Models/VideoCollections.cs`, `Pages/VideoDetailPage.xaml(.cs)`, and `BiliLite.UWP.csproj` for the new Compile entry.

1. Add `[JsonProperty("ugc_season")]` to the current detail model and adapt the sibling's collection/section/episode models. Retain collection ID/title/cover/intro/statistics; section ID/title; episode aid/bvid/cid/title/cover/author and duration. Follow the sibling's `arc.pic` cover fallback and episode `page.duration` mapping.
2. Deserialize this through the existing `LoadVideoDetail` pipeline: `DetailWebInterface` reads `/x/web-interface/view` with aid/bvid and cookies; the existing signed `/x/v2/view` app request remains a fallback. Collection metadata is embedded in the detail response. Do not invent a separate collection-metadata endpoint or request the same video detail again just for its collection.
3. Add a collection tab/section to `VideoDetailPage`, showing collection title/intro and section headers with each video's cover, title, author and duration. Adapt the current `PlayListTpl` with grouped items or simple section expanders; the sibling's whole `VideoListView`, DI setup and AutoMapper stack are unnecessary.
4. Select the current entry by canonical aid from `VideoInfo`, so opening by BV ID selects the same video. Clicking another entry calls the current `InitializeVideo(entry.aid)` path while retaining the collection list. On refresh/navigation, update the existing collection view rather than inserting duplicate tabs.

**Result:** opening a video belonging to a UGC collection shows its source-provided sections and videos, with the current video selected. A detail response without a collection uses the existing page layout.

### C2 — Remote playlist loading and paging

**Sibling references:** `Services/Biz/MediaListService.cs::GetMediaList/LoadMoreMediaList`, `Models/Common/Video/MediaListResources.cs`, `Models/Requests/Api/VideoAPI.cs::GetMediaList/GetMediaListNotLogin`, and `Controls/VideoListView.xaml.cs::BtnLoadMore_OnTapped`.

**Target files:** existing `Api/VideoAPI.cs` builders, new `Models/MediaListResources.cs` and `Services/MediaListService.cs`, plus `Pages/VideoDetailPage.xaml(.cs)` and explicit project entries.

1. Reuse `GET https://api.bilibili.com/x/v2/medialist/resource/list`. Keep its existing parameters: `type`, `biz_id=mediaListId`, `oid=lastAid`, `otype=2`, `ps` (default 20), `direction=false`, `desc=true`, `sort_field=1`, `tid=0`, `with_current=false`. Reuse the signed Android-key builder and the unsigned variant as provided; do not add a second endpoint implementation or a new cookie requirement.
2. Adapt response fields `media_list`, `has_more`, `total_count`; each item supplies `id/title/cover/upper.name/duration`. Map them directly into the current playlist item shape, adding duration. List resource IDs map to video IDs; duration is seconds. Preserve source order.
3. Add a small directly constructed service. The first request uses an empty `oid`; match the sibling's unsigned retry when the signed response lacks `media_list`. A returned empty list is distinct from missing list data. Report request failures through the current API/message conventions.
4. Carry `MediaListId`, `MediaListType`, source kind, `HasMore`, `TotalCount` and the last returned aid with the active playlist. Load more using that last aid as `oid`, append to the same bound list, and update from the new `has_more`. The next cursor comes from the response items, not the selected playback row. Reuse returned IDs to avoid duplicate visible rows.
5. Show loaded/total counts and a Load more action while `HasMore` is true. Metadata comes from available source fields: the sibling resource model does not establish a remote playlist title/owner beyond its items, so use a generic playlist label unless the navigation source supplies a title.

**Result:** remote playlists can display more than their first page without downloading the entire list upfront.

### C3 — Source links and playback continuity

**Sibling references:** `Services/MessageCenter.cs` medialist route, `Models/Common/Video/VideoPlaylist.cs`, `Models/Common/Video/VideoPlaylistItem.cs`, `Pages/VideoDetailPage.xaml.cs::OnNavigatedTo/player_AllMediaEndEvent`, and `Controls/VideoListView.xaml.cs` selection behavior.

**Target files:** `Helpers/MessageCenter.cs`, `Pages/VideoDetailPage.xaml(.cs)` and its existing `VideoPlaylist/VideoPlaylistItem` types. Extend those types in place unless moving them directly simplifies reuse.

1. Add the sibling-supported `bilibili://music/playlist/playpage/{id}` route, reading optional `page_type` and using the sibling route's default of 2. Keep that separate from the API method's default type of 1. Load the first page through C2, then navigate using an extended `VideoPlaylist` carrying the remote ID/type and paging state.
2. Keep the existing `ml...` favorite-folder route intact. Collection IDs, uploader mids, favorite-folder IDs and medialist `biz_id` are not interchangeable. Accept IDs/types from the supported source URI; do not derive a medialist ID from a UGC collection ID.
3. Use the existing playlist selection handler to load each chosen video's details and its own `pages`. Keep the active queue and selected row while that video's detail response changes. A collection embedded in a queued video can be browsed separately without replacing the remote queue the user opened.
4. Extend `player_AllMediaEndEvent` to advance after the current video's own parts finish. For collections, continue in section/episode order. For remote queues, fetch the next page when playback reaches the final loaded item and `HasMore` is true, then advance; finish only when the source is exhausted. A failed next-page request leaves a visible retry/Load more action.
5. Keep caller-supplied in-memory `VideoPlaylist` playback working through the same handler. This display/read feature does not add collection editing, favorite mutations or batch downloads.

**Result:** a supported Bilibili playlist link opens a visible, playable remote queue; choosing a row and reaching a page boundary both continue through the existing player.

**Sequence and scope:** implement C1 first for collection display directly from a normal video, then C2 and C3 for remote lists. These batches are independent of G1–G5. All behavior references remain the pinned sibling. This addition is planning only; perform no live API calls, tests, builds or validation, and use only `git diff --check` for formatting.
