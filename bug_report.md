# Bug report — unresolved issues

Orchestrator → remote reviewer. Branch `work`, base `2a0949d`.

Purpose: hand over the defects that are still open after the last fix batch, with source evidence and the decision each one needs. Reply with `code_review.md` in the next commit: for every ID either an approved approach or a rejection with reason. I will implement approved items through subagents, one batch per ID, and update this file with the outcome.

Build/verify commands (both work on this machine):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-release-x64.ps1 -Fast -NoVersionBump   # ~56 s, compile only
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-release-x64.ps1                       # ~85 s, bumps version + MSIX
```

Already fixed in `b0782b8` and earlier, do not re-report: playurl helper accessibility (compile break), `SubtitleTrackParser` namespace, `DynamicGrpcMapper` PascalCase member names, duplicate `api` local, subtitle AI-vs-manual initial selection, traditional→simplified prefix guard, feed `rid_str`/`comment_id_str` per family, PGC `aid`, live card `live_state`, video detail `attention` sentinel + follow-state restore + null `owner_ext`, `-Fast` build mode.

---

## BR-01 — Dynamic families with no mapper branch render a raw JSON dump

**Severity:** high (feed shows machine output instead of a card)

`DynamicParse.ParseType2024` (`src/BiliLite.UWP/Controls/Dynamic/DynamicParse.cs:140-169`) handles only `WORD, FORWARD, DRAW, AV, ARTICLE, MUSIC, NONE, PGC_UNION, UGC_SEASON, LIVE_RCMD, LIVE`. Everything else falls to `DynamicDisplayType.Other`, and `ConvertToDisplay2024` (`src/BiliLite.UWP/Modules/User/DynamicVM.cs:904-917`) then builds a card whose `ContentStr` is `未适配的类型{type}:` plus `JsonConvert.SerializeObject(item)`. The legacy path has the same dump at `src/BiliLite.UWP/Modules/User/DynamicVM.cs:693`.

Non-exhaustive list of types that hit this today: `DYNAMIC_TYPE_MEDIALIST`, `DYNAMIC_TYPE_COURSES_SEASON`, `DYNAMIC_TYPE_COURSES_BATCH`, `DYNAMIC_TYPE_COMMON_SQUARE`, `DYNAMIC_TYPE_APPLET`, `DYNAMIC_TYPE_COMMON_VERTICAL_BANNER`, `DYNAMIC_TYPE_AD`.

`DYNAMIC_TYPE_MEDIALIST` is fully mappable already: the proto models it (`src/BiliLite.gRPC/bilibili/app/dynamic/v2/dynamic.proto:3329` `message MdlDynMedialist`, wired as `dyn_medialist = 12` at `:3942`) and `DynamicCardModel2024_major` has no field for it (`src/BiliLite.UWP/Modules/Home/DynamicVM.cs:769-779`), while `MapCardMajor` (`src/BiliLite.UWP/Helpers/DynamicGrpcMapper.cs:400-533`) has no `DynMedialist` branch.

**Decision requested:** pick one policy, per family:
1. map `MEDIALIST` into a real card (needs a new `major` field + template), and
2. for the remaining families — hide the card, or render a compact "unsupported" placeholder without JSON?

My lean: map `MEDIALIST`, drop `AD`/banner, placeholder for the rest. Dropping cards changes list counts and `has_more` accounting, so I want that called explicitly.

---

## BR-02 — Music dynamics are mapped into the article slot

**Severity:** medium (music cards render empty)

`MapCardMajor` (`src/BiliLite.UWP/Helpers/DynamicGrpcMapper.cs:519-528`) assigns `DynMusic` to `result.major.article`; `DynamicCardModel2024_major` (`Modules/Home/DynamicVM.cs:769-779`) has no `music` member. `ParseType2024` still returns `Music` for `DYNAMIC_TYPE_MUSIC` (`DynamicParse.cs:154-155`), so the Music template binds a `major` that carries article fields: no title, no cover, no jump.

**Decision requested:** add `DynamicCardModel2024_music` + `major.music` and map there (my preference), or fold music into the article template and change `ParseType2024` to `Article`.

---

## BR-03 — 追番追剧 tab: no author, comments target a season id as a video oid

**Severity:** medium

`DynamicGrpcMapper.MapToUserSeasonCards` (`src/BiliLite.UWP/Helpers/DynamicGrpcMapper.cs:157-200`) builds every card from `FollowListItem` with `rid_str = comment_id_str = id = SeasonId.ToString()` and `comment_type = 1`, and never sets `modules.module_author`. `FollowListItem` (`dynamic.proto:2501-2516`) genuinely has **no** author and no `aid` — only `season_id`, `title`, `cover`, `url`, `new_ep`, `sub_title`, `pos`.

Consequences: blank avatar/name on every row; pressing 评论 opens the comment page for `CommentType.Video` with a `season_id` as oid, which is not a video oid; `major.pgc.aid` stays null so the card cannot jump to an episode either.

**Decision requested:** choose the resolution:
1. resolve `aid`/`up_mid` per card through a follow-list/season detail call (extra request per page, needs a cache),
2. pass `CommentType.Season`-equivalent (does not exist in `CommentApi.CommentType` — enum has Video 1, Dynamic 17, Article 12, Photo 11, MiniVideo 5, SongMenu 19, Song 14),
3. disable the comment and author chrome on this tab.

I did not implement anything here because each branch has a product-level tradeoff.

---

## BR-04 — Stale dynamic-feed reply overwrites the new tab's items and cursor

**Severity:** medium (wrong content after a quick tab switch)

`GetDynamicItems` (`src/BiliLite.UWP/Modules/User/DynamicVM.cs:400-512`) captures nothing about which tab the request was issued for. On completion it publishes unconditionally: `Items = mapped` (`:498`), then `feedPage = page; historyOffset = nextOffset; updateBaseline = nextBaseline;` (`:500-502`) and `CanLoadMore = hasMore` (`:507`). `UserDynamicPage.pivot_SelectionChanged` (`src/BiliLite.UWP/Pages/Home/UserDynamicPage.xaml.cs:201-211`) switches `UserDynamicType` and calls `Refresh()` immediately, so a reply that lands after the switch replaces the new tab's list and stores the old tab's cursor in the shared `historyOffset`, which the next 加载更多 then sends.

**Decision requested:** approve a request-generation guard (`++requestGeneration` on refresh/tab change; drop replies whose generation or captured `UserDynamicType` no longer match, and skip the cursor commit) — or prefer independent per-tab cursors (bigger change: cursor state per `UserDynamicType`).

---

## BR-05 — `MyListView.LoadMoreBottomOffset` is registered under the wrong owner

**Severity:** high when set from XAML (throws), medium overall (no first-page auto-load)

`src/BiliLite.UWP/Controls/MyListView.cs:25-26`:

```csharp
public static readonly DependencyProperty LoadMoreBottomOffsetProperty =
    DependencyProperty.Register("LoadMoreBottomOffset", typeof(double), typeof(MyAdaptiveGridView), new PropertyMetadata(100));
```

`MyListView` derives from `ListView`, not from `MyAdaptiveGridView`, so `GetValue`/`SetValue` (lines 18-22) reject the DP: setting `LoadMoreBottomOffset` on a `MyListView` throws `InvalidOperationException` ("… is not a DependencyProperty of the given type"). Second gap: `CanLoadMore` is a plain CLR property (`:17`) and `ScrollViewer_ViewChanged` (`:39-44`) only fires on scroll, so a first page shorter than the viewport leaves `ScrollableHeight == 0` and never loads page 2 — `MyAdaptiveGridView` handles exactly that (`MyAdaptiveGridView.cs:66,80`), `MyListView` does not. (For the record: `x:Bind OneWay` to that CLR property is legal — compiled bindings call the setter, no DP required — so the binding itself is not the defect.)

**Decision requested:** approve (a) re-register the DP with `typeof(MyListView)`, (b) add the `ScrollableHeight == 0 && CanLoadMore` trigger to `MyListView` after layout, (c) also promote `CanLoadMore` to a DP for consistency with `MyAdaptiveGridView`. Items are mechanical; I only need confirmation that (b) should apply to every `MyListView` in the app, not just the dynamic page.

---

## BR-06 — Profile `/x/v2/space`: live badge and VIP/official chrome rely on fields the app endpoint may omit

**Severity:** low (cosmetic)

`UserDetailVM.GetUserInfo` (`src/BiliLite.UWP/Modules/User/UserDetail/UserDetailVM.cs:55-66`) maps `live_room = data["live"]`, `vip = card["vip"]`, `official = card["official_verify"]`. `UserCenterInfoLiveRoomModel` (`:223-234`) has `roomStatus` and `liveStatus`, but nothing sets or normalizes them when the endpoint omits them, so the 直播中 badge depends on a field the app response is not guaranteed to carry; when `vip`/`official_verify` are absent the models stay null and the badge/pill silently disappear.

**Decision requested:** fall back to the web card endpoint (`/x/web-interface/card`) for these three fields, or accept the nulls and hide the chrome deliberately (no code change). I have no response capture to prove which fields the app endpoint returns, so I am not patching blind.

---

## BR-07 — AI-subtitle detection still depends on label text

**Severity:** low (residual of #1311)

`IsAutoGeneratedSubtitle` (`src/BiliLite.UWP/Controls/PlayerControl.xaml.cs`) decides AI vs manual from the track label: `自动` in `lan_doc`, or `lan` starting with `ai-`. `HasSubtitleItemModel` (`src/BiliLite.UWP/Modules/Player/PlayerVM.cs:303-309`) deserializes only `id/lan/lan_doc/subtitle_url`, so a track that is machine-generated but labelled as a plain language (no `自动`, `lan` not `ai-*`) is indistinguishable and the AI-vs-manual setting picks wrong.

**Decision requested:** approve adding an `ai_type` (or `is_ai`) member to `HasSubtitleItemModel` and preferring it in the predicate while keeping the label heuristic as fallback — or confirm the endpoint used here never returns that field, in which case this stays a documented limitation and I will not touch it.

---

## Process notes for the reviewer

- Every approved item gets its own commit, message `Fix BR-0x …`, with the build evidence quoted.
- Verification per batch: `-Fast` (compile + XAML) must reach 0 errors; one plain run per push group produces the bumped MSIX.
- I cannot verify UI behaviour on a device from here; anything that needs eyes on screen will be listed as such rather than claimed working.
- Ambiguity in `code_review.md` gets one clarifying question in the next `bug_report.md` instead of a guess.
