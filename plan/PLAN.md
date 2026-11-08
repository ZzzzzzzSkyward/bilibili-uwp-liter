# Plan — open work only

Updated 2026-11-01 for `dev`. Everything shipped is one line in the commit history. Finished plan
documents, reports and progress logs were removed on purpose — `git log` has them.

**Verification policy:** the maintainer's device pass counts as passed. A shipped item stays closed
until the maintainer reports an actual bug; only such a report reopens it, and then it is written up
in `bug_report.md` first.

## State

| Fact | Value |
| --- | --- |
| workspace | `Z:\bilibili-uwp-liter` as of 2026-11-02 (the `H:\uwp\bilibili-uwp-liter` copy is left behind at `e3b8436`, holds stray uncommitted edits and is no longer the working tree). The Z: clone was made from the H: path, so its `origin` had to be reset to the GitHub URL, and a fresh clone needs `-Restore` plus the referenced project outputs (`src/BiliLite.gRPC/bin`, `src/BiliLite.JSBridge/bin`, and the other `src/*/bin`) before `-SkipProjectReferences` compiles — otherwise `CompileXaml` fails with `WMC1006` on `BiliLite.gRPC.dll`/`BiliLite.JSBridge.winmd` |
| branch | `dev`, in sync with `origin/dev` |
| build | `powershell -NoProfile -ExecutionPolicy Bypass -File build-release-x64.ps1 -Fast -SkipProjectReferences` (compile only; this machine's VS has no .NET SDK component, so the script evaluates the skipped SDK-style `BiliLite.gRPC` through the installed dotnet SDK) or without `-Fast` to package into `Z:\`. **Every build bumps the revision** (both `Package.appxmanifest` files, same source of truth); `-NoVersionBump` is for a throwaway repeated package, not for a batch that ships |
| probe checks | `sh plan/tests/<batch>/run.sh` — net8.0 probe projects under `plan/tests/` compile the real product sources and assert behavior. The scripts expect a portable drop at `$UPSTREAM_UNIT_TOOLS/dotnet` with SDK 8.0.427; that drop is absent on this machine, the installed SDK is 10.0.302 with the 8.0.33 ref pack and an 8.0.33 runtime, so a batch either points `UPSTREAM_UNIT_TOOLS` at a directory containing a `dotnet` root or resolves the installed SDK. `sh` here is MSYS2 (Git Bash) |
| sync | `powershell -NoProfile -ExecutionPolicy Bypass -File sync-dev.ps1` — fetch+rebase+push with TLS retries, stashes the two `Package.appxmanifest` around it (`-NoPull`, `-NoPush`, `-Attempts`, `-RetrySeconds`) |
| review cache | `plan/REVIEW_LOG.md` |
| bug device log | `bug_report.md`, `code_review.md` at the repo root |

## Done

Newest first; every one of these is device-accepted per the policy above, except the 2026-11-01 entries: BR-44 is
device-confirmed, while P4 step 1, BR-45 and the second BR-43 attempt are compiled and probe-covered only — their
device rows are still open in `plan/VALIDATION.md`. The 2026-11-02 entries are compiled
(`-Fast`, 0 errors, package 1.0.9.35), probe-covered and covered by one unified run of all 35 probe batches
(`TOTAL_FAILURES=0`), but none of them has been on a device. The 2026-11-05 entries (`7e628c3`, `65fd423`) are
compiled into `BiliLite.Packages_1.0.9.42_x64.msix` (package directory dated 2026-11-05 00:22); their device rows
are the 1.0.9.42 section in `plan/VALIDATION.md` and have not been run, and no probe run is recorded for them.
The 2026-11-06 entries (`01e51a7`, `798a03a`, `4dc85ce`) are compiled (`-Fast`, 0 errors) and probe-covered
(`cardlayout` 761, `playertoolbar` 90, `toastalign` 41, `g3` 342, `g5` 200); none has been on a device.

- **Playback checkpoint survives a pending report (review `c4aa630`):** `cfc4a15` — `CheckpointHistoryAsync` now writes the local position before any report decision (`PlayerControl.xaml.cs:3404`), and `DrainHistoryReportsAsync` reports the latest pending snapshot until it stops changing, so a pause or the exit flush that lands during a 30 s report still stores and reports the position the slider set. Compile only (`-Fast -NoVersionBump -SkipProjectReferences`, 0 errors).
- **A live room no longer answers with a null reference (BR-53):** `c0b7eb1` — the device log of 2026-11-08 12:27 pinned it to `LoadSuperChat` (a `NullReferenceException` right after a 200 on `SuperChat/getMessageList` with no JSON failure) plus two `[JSON][FAIL]` on `gift/v4/Live/giftConfig` whose `data` is now an array. The superchat list and the gift config are both read as array-or-`{"list":[...]}` (null → empty list), entries without `user_info` are skipped, and `Gifts` no longer receives null elements; `bag_list`/`gift-room` shapes stay as they were. Device pass 2026-11-08 13:02 (session `5399351e…`): both `giftConfig` loads 200 with no JSON failure, `bag_list` 200, no null reference anywhere in the session.
- **A dropped danmaku socket reconnects itself (BR-63):** `722555b` — the receive loop in `Modules/Live/LiveMessage.cs` used to exit as soon as `ws.State` left `Open`, so one remote close killed danmaku, gifts and superchat for the rest of the visit (two device sessions on 2026-11-08 show the same `WebSocketException`). `Connect` now loops: connect → join → heartbeat → receive, replaces the aborted socket, backs off 2→4→8→10 s and resets on success; the heartbeat catches its send failures so the `async void` cannot take the app down, and `Dispose` sets a flag first so nothing reconnects after the page. Compile only (`-Fast`, 0 errors).
- **A minimized window comes back through its frame (BR-61):** `bbfaf7f` — the 2026-11-08 13:12 log showed `Application resumed` followed by a no-op `Window.Current.Activate()`, the window still minimized and a suspension 10 s later. `Helpers/WindowRestoreHelper.cs` (added to the csproj) takes the CoreWindow HWND, resolves the `ApplicationFrameWindow` root, restores it with `ShowWindow(SW_SHOWMAXIMIZED)` when `GetWindowPlacement` reported `WPF_RESTORETOMAXIMIZED`, and logs hwnd/root/iconic/zoomed on both sides. Compile only (`-Fast`, 0 errors).
- **The season page gets its detail back (BR-64):** `b643e77` — the page crashed with `ArgumentNullException` at `Utils.ParseArea` because `SeasonDetailVM` built `Detail` from `pgc/view/web/ep/list`, which only returns `episodes`/`section`/`play_strategy`. `SeasonApi.Detail` (`/pgc/view/web/season`) now supplies the model (title/cover/rating/areas/stat/follow state, so `media_id` and `season_id` stop being 0), `ep/list` keeps the playable list and the merged `Detail.episodes` the download dialog reads, and `ParseArea` no longer runs a regex on a null title. Compile only (`-Fast`, 0 errors).
- **The season page shows its content again (BR-65):** `5132840` — the VM was asking `pgc/view/web/season` and `ep/list` with `ep_id`, which is empty when the page is opened from the feed (only `season_id` is passed), so both answered nothing. It now asks by `season_id` and keeps `ep_id` for the initial episode only; `plan/tests/season1` was re-pinned to that contract.
- **Comment text is decoded before it is escaped (BR-66):** `ec92550` — `ControlHelper.StringToRichText` escaped `&` first, turning API `&quot;` into visible `&amp;quot;`; it now runs one `WebUtility.HtmlDecode` ahead of the escape pass, and short reviews decode their `content` too.
- **Empty player pivots no longer appear (BR-67):** `9fdfbf4` — `节点` was visible before any verdict ran and ignored whether nodes had loaded; `本地轨道` counted video/audio tracks that only have combos in manifest playback. Both are now gated on what the panel would actually render, re-judged after `GetNodes()`, and the interaction VM is rebuilt per episode.
- **Release 1.0.9.61 (2026-11-07):** tag `20261107` on the master sync commit; the release asset is
  `BiliLite.Packages_1.0.9.61_x64_Test.zip` (msix, test certificate, `Install.ps1`/`Add-AppDevPackage.ps1`,
  dependencies). Built with the plain script (full Release build, 0 errors); `g5` 335, `playersess` 203.
- **Recommend card tooltips (BR-59):** `ae90881` — the two card-title TextBlocks in `Pages/Home/RecommendPage.xaml`
  (grid `MaxLines=2`, list `MaxLines=3`) were the only truncated card titles without `ToolTipService.ToolTip`; both
  now bind the full title, and no tap handler is attached because the card is item-click navigated. `g5` 332 → 335.
- **Playback progress survives an app close (BR-60):** `ae90881` — progress was written only from `Dispose` (leaving
  the page) and on media end, so closing the window mid-video lost both the local resume point and the history report
  (log 2026-11-07: 8 minutes of playback, no `history/report`, new session). Now a 30 s `DispatcherTimer` checkpoints
  while playing (local position saved, history posted silently), pause saves once, and `App.OnSuspending`/`OnExiting`
  await `PlayerControl.FlushActivePlaybackAsync()` inside their deferrals. `HistoryKey` is the only key builder and the
  resume read uses it too; `ReportHistory` gained a `reportErrors` flag so checkpoints never toast. `playersess` 161 → 203.
- **Reservation copy and attachment labels (BR-57):** `65ca559` — `Parsing/Dynamic/ReserveCardCopy.cs` formats the
  description as `<time> 开播 <n>人已预约` (+ a third tag for `desc3`), tags separated by one space and no brackets,
  rewriting a trailing 直播 and a pending 预约 while leaving 直播中/预约中 untouched; both the attachment summary and
  the standalone Reserve card use it. Attachment labels map `GOODS`→商品、`UGC`→合集、unknown→附件 and never print a
  wire type; an unsupported row with no title, description or destination is dropped. `g5` 265 → 316.
- **Titles open their tooltip on tap (BR-58):** `65ca559` — `Helpers/TapToolTip.cs` attached behavior; a tap on the
  five truncated dynamic titles opens the same full text a hover shows, for 4 s, and closes on mouse exit/unload.
  It reads `ToolTipService.ToolTip` on every tap (so a recycled list item cannot show the previous title) and never
  writes back the attached property; touch `PointerExited` is ignored because it fires on lift.
- **Text reservation posts keep one panel (review `ac6867c`):** `95934db` — the text branch of
  `ParseOneRowInfo2024` promotes the card to `DynamicDisplayType.Reserve` after `DynamicCardProjection.Apply` already
  projected the reserve into `Attachments`, so the standalone template and the attachment panel drew it twice.
  `DynamicCardProjection.ApplyAttachmentOwnership` now runs right after the parse and drops only the reserve summary
  when the type is `Reserve`; photo posts keep their panel, other kinds stay. `g5` 316 → 332.
- **Local playback keeps its directory (review `dae62b7`):** `9c8920f` — `LocalPlayerPage` builds
  `PlayDirectoryBuilder.FromParts` rows from the `PlayInfos` it is handed (title/cid/duration), reports
  availability only above one item, opens and refreshes the drawer on request, switches Parts rows through
  `ChangePlayIndex(index, PlayTargetStart.Resume)`, refreshes the open drawer from `ChangeEpisodeEvent`, and
  releases its three handlers on navigate-from/close. `plan/tests/localdir` 50 assertions; 28 of its 32 page
  anchors fail against the pre-change body.
- **Reserve attachment panel (BR-56):** `a431b4c` — a reserve attachment (`additional.reserve`) on a photo/text
  dynamic now selects `AttachmentPanelTemplate` through `DynamicAttachmentDataTemplateSelector`: BR-55's bordered
  panel, pink 预约 chip, 18/26 title, 14/21 description and the payload's own button. The summary maps
  `reserve.button.text` / `icon_url` (falling back to the check/uncheck icon) and prefers `button.jump_url` over
  `reserve.jump_url`; vote/link/unknown attachments keep the quiet row. `ImageSourceConvert` treats a missing
  parameter as the original image and the standalone Reserve panel now passes `64h.png`, so its icon no longer
  throws. `g5` 232 → 265 (mapping, panel tokens, selector, button visibility; the pre-fix mapping lands on the
  reservation page instead of the live URL).
- **Package version 1.0.9.57:** `284a714` — both `Package.appxmanifest` files bumped to 1.0.9.57; no other change.
- **Live-room guards and the legacy web escape (BR-53, review `8d496eb`):** `2276f28` — every live-room step that
  parses a response now returns before touching a failed model (no `data.message` on a null), and the ordinary legacy
  unsupported card carries `WebCommand` so it opens `t.bilibili.com`; each failing live step still logs `step`/`stage`.
  `g5` pins both: no `HasParsedData(data, "anchor") && data.success` and at least three `WebCommand = WebCommand`
  branches.
- **One list entry in the player:** `35f6905` — 播放目录 becomes the player's only list entry: `BottomBtnList`,
  `EpisodeList` and `ShowPlaylistButton` are removed, `EpisodePivot` is renamed `NodePivot` and shows only for
  interaction videos, and multi-part switching goes through the directory; `playertoolbar` rewritten.
- **Reserve card and title tooltips (BR-55):** `09ee480` — the 预约 template is one bordered panel with a pink chip,
  two-line title and description and the payload's own button, and every trimmed card title carries a tooltip.

- **Player entries hide when empty:** `4dc85ce` — `Services/PlayerSurfaceVisibility.cs` decides 弹幕列表 / 选集 button /
  选集 tab / 节点 / 本地轨道从 the signals the player already holds (playlist count, interaction, four local track counts,
  loaded danmaku rows for the current item, a local danmaku file, whether the load settled); the danmaku pool carries its
  item so another item's rows cannot show the entry. A selected tab that just became hidden falls back to 弹幕, then to the
  first visible tab. `plan/tests/playertoolbar` (90 checks; 23 fail against the always-visible behaviour).
- **Photo grid and attachment row (CARD_PLAN §6/§7):** `798a03a` — one `DynamicImageGrid` for every count ≥ 2 with
  width-derived tiles (88-DIP fallback to two columns, nine-tile cap with `+N`, shared save menu), geometry in
  `DynamicImageGridLayout`; the card shell gains one attachment row whose 打开 action needs both a destination and a
  command. `plan/tests/cardlayout` 761 checks (40 fail against the old fixed-tile geometry), `g5` 200 after a
  probe-only anchor repair.
- **BR-54 toast pill:** `01e51a7` — horizontal padding 16 → 6, the pill height stays 38, and a negative
  `BaseContentOpticalOffset` moves icon and text together through `ContentOffset`; `toastalign` 41 checks.
- **Feed window and repost bound:** `65fd423` — cards get one shell title slot fed by `major.opus.title` (web and both
  gRPC opus summary sources), so a preview that prints its own title stays single; originals stop at two card-data
  levels with the deeper ancestor collapsed into one 查看原动态 row carrying its dynamic id, and an origin block with
  no card draws no empty frame; previews reserve a 16:9 or natural box before the image loads; the article feed
  parses through the shared web page parser; every publish point dedupes by dynamic id; the list keeps a ten-page
  rolling window with anchor protection and reload checkpoints.
- **Settings gating:** `7e628c3` — the video-format hints render only for HEVC/AV1, the CDN node list and latency test
  collapse while replacement is off, the sample switch disables itself without rewriting the saved value, re-entering
  a pivot refills the page from storage behind a guard instead of re-subscribing every `Loaded` handler, and the
  local `Assets/Text/help.md` is rewritten against the current tabs and controls.
- **Review finding — a login no longer inherits the previous account's token:** `14ac4c3` — `RefreshTokenPolicy`
  decides the owner mismatch before the response-authoritative branch, `PersistCredentials` deletes the previous
  generation's `ac_time_value` when the uid changes, `ReadToken(sessionOwnerId)` refuses a contradictory record,
  `GetRefreshToken` passes the session owner (the only submit path), and a hand-entered token records `manual`
  provenance with the session owner. `auth3` 188 → 218 assertions.
- **Review findings — reused photo body and the reservation lottery line:** `2072d64` — `RichTextBodyState` makes
  the mounted child a function of the current `Content`/`Text` on every change including empty, so a recycled card
  cannot show the previous body; the gRPC mapper maps `desc_text3` and the visible reservation description joins
  all three slots through `JoinText`, which now also drops an exact repeat. `g3` 135 → 176 assertions.
- **Probe truthfulness — BR-31 checked on labels only:** `30bc06a` — `auth1`'s ban on `（ ： ·` covered the whole
  account block and failed on a legitimate help line; it now scans label text and exempts `SettingHelp`/`ToolTip`
  subtrees. A mutation inside a label still fails it.
- **Review finding — a rejected cookie write reported as installed:** `b4cd995` — `Services/CredentialCommitter.cs`
  owns the D2 commit (four-value snapshot/rollback plus per-cookie accounting); a `false` from
  `CookieManager.SetCookie` now counts as a failure exactly like a throw, so `CookieInstalled` is only true when
  at least one write was accepted and none failed and the existing `PartlyCommitted` path reports it. `auth3`
  173 → 188 assertions, with the pre-fix body failing the rejected-jar case.
- **Settings dead controls and the API-key save bug:** `c4c683f` — SETTINGS_PLAN slice 2: the collapsed 语言
  dropdown, the theme-colour panel (`cbColor` was populated before `Loaded`, so its handler could only fire on a
  selection no user could make), the DefaultTheme/示例主题 expander, the disabled `api_platform` combo, the
  注销 accesskey button, the 极验 login test form with its page-private members, and the commented
  外站替换视频 / Akamai 优先 blocks are gone with the code only they reached. Opening the page no longer writes:
  it read `api_web_top` for the `api_web_secondary` box and wrote both keys back on load, which overwrote a
  custom path prefix; only 应用参数 writes now, and it reads the key it displays.
- **Dynamics card layout:** `48c5df5` — CARD_PLAN slice 2: `DynamicItemControl.xaml` is one reading column
  (`MaxWidth` 800, one margin/padding token block at the top of the file) with header | body | content | action
  rows at full width instead of content indented by the 56px pendant column; header collapses to an overflow menu
  under 480 of the card's width, the action row is three star columns, body metrics 16/24, and OneRow's preview
  is a 160-capped media column with a `2*` text column (`CoverWidth` deleted with its seven assignments).
  Ten items need the maintainer's eyes — they are listed in the commit's handoff, not claimed.
- **REFRESH D4/D5 — credential identity:** `75fe40f` — `Services/RefreshCredentialPolicy.cs` holds the only
  decision about the saved web refresh token (`RefreshTokenPolicy.Decide`, a pure function over the saved value,
  its recorded source, the session owner, the cookie candidate and a pending-confirmation flag) plus
  `RefreshTokenMirror`/`AppkeyPolicy`. A response token is authoritative for its session, cookie `ac_time_value`
  adoption only happens when nothing is saved, an owner mismatch refuses the import, and both former mirror
  implementations now call the one unit. `SaveLogin` records which configured appkey acquired the pair (Login for
  password/SMS, Tv for QR, UnknownLegacy otherwise) and `AccountApi.RefreshToken` signs with that same key.
  `auth3` 130 → 173 assertions; `auth1`'s pre-D4 assertion was re-stated as the keep-path plus the migration path.
- **Settings structure (SETTINGS_PLAN slice 1):** `ed5fe9f` — ten tabs in use-case order, controls moved to the tab
  their use case belongs to, each tab grouped with the page's own header/label/help styles, labels stripped of
  parentheticals and duplicated icons with the text moved to the help line. The 直播视频格式 group is deleted:
  it wrote `livevideoformat` that only the page read back, while the live request builds its format from
  `PlayerAPI.videofmt`. Labels, dead-control removal and save errors beyond that one group are the next slice.
- **Dynamics body (CARD_PLAN slice 1):** `2d1c4b3` — web and gRPC mappers keep `additional`, OPUS summary/covers,
  draw pictures, `major.ugc_season`, `no_comment`/`no_forward` and the text-node vocabulary; `Parsing/Dynamic/
  RichTextFlow.cs` joins nodes into one paragraph with the source's own breaks, no character cap and the link
  label kept apart from its target; photo hosts and `ShowContent` read the body; the dead markup builders and the
  reserve `??`/`+` precedence bug are gone. `g3` 73 → 135 assertions, `g5` 44.
- **BR-47 Hot card height budget:** `3ec04aa` — tag on the thumbnail (one ellipsised line, collapsed by its text
  rather than its style object), title plus both metadata lines in a text column, slot 100 → 132 and a sub-480
  stack at 296/320. Layout-visible, so it closes on the device.
- **Probe batches repaired after the unified run:** `488a978` — `g4`'s stub gained the silent `TryGetJObject`
  (the toasting `GetJObject` on top of it), and `ux1c`'s request-builder assertion, which had silently degraded to
  comparing empty strings after `1838108` made the method `async`, now anchors on the surviving signature tail.
- **BR-46 keyboard focus after a switch:** `50bb009` — a `PivotHeaderItem` handles Space itself and every advance funnels through
  `InitializeVideo`, so after auto-next the focus sat on a tab header and Space switched tabs. `PlayerControl.FocusPlayer()`
  is the one entry over the existing `BtnFoucs` sink, called from `InitializeVideo`'s success path only when
  `IsTabStripFocused()` says the focus is on a `Pivot`/`PivotHeaderItem`. `Timer_focus_Tick` keeps its four-type set on
  purpose — a timer that pulls focus off tab headers would break switching tabs from the keyboard. `plan/tests/focus` (33 checks).
- **P4 step 3 — video detail:** `e6c2fb5` — `Models/Api/Video/` + `Parsing/Video/` own web view, related, tags, relation
  stat/attribute, the four state reads, the action endpoints and the favourite-created list. The fallback chain (web → signed
  app → proxied) and the relates merge are decisions over parse results; a failed related read logs its category instead of
  nulling the page; the `relates` setter is null-safe so a wire null no longer throws; `short_link`/`owner_ext`/`pages`
  defaults live in the mapper and `req_user` is never copied from the wire. The `biliproxy` proxy stays as a documented
  last-resort source. `plan/tests/c1` 52→161.
- **Closed-page latch probe refreshed:** `93a8ed3` — `plan/tests/stab` re-pointed at the single-flight paging channel and the
  collection sequence (`AdvanceSequenceAsync`), since `AdvancePlaylistAsync`/`AdvanceUgcSeasonAsync` no longer exist.
  15 checks, 55 assertions, each rewritten anchor shown red with its guard removed.
- **P4 step 5 — comments:** `f03078c` — `Models/Api/Comment/` DTOs + `Parsing/Comment/` (`WebCommentPageParser`,
  `WebCommentActionParser`, `WebCommentPageMapper`). A body without a business code is an envelope failure
  instead of `code = 0` success; the pinned-reply dedupe and the two cursor shapes (`cursor.next_offset` vs the
  legacy `pn`) live in the mapper; `CommentControl` toasts by category and keeps `_page`/cursor source-bound.
  Dead `dataCommentModel`, `CommentItem` and `Comment_New` deleted. `plan/tests/comments` (99 assertions).
- **PLAN item 1 — player session ownership:** `2c3fc09` — `Services/PlayerSessionState.cs` (Begin/Accept/
  Invalidate/Hold/Release/IsCurrent) makes every open carry a session; resources sit in a per-session ledger
  until accepted, an invalidated session releases once, and a resource born after invalidation is released on
  the spot. All 12 open paths are bound, the quality change is awaited, the engine fallback is single in flight,
  and stale continuations/`MediaOpened`/`MediaFailed` are rejected. `plan/tests/playersess` (69 checks).
- **P4 step 2 — playlists:** `1838108` — `Parsing/MediaList/` parsers + mappers for medialist (signed/unsigned),
  favorites and watch later; the service keeps source policy and single-flight but no longer decodes, toasts or
  collapses failures. The toast moved to the publishers through `MediaListFailureText`; an absent `media_list` is
  its own stage and the only case that spends the unsigned retry. `oid = last aid`, `has_more`, `HasKnownTotal`
  and `MediaListPagingState.Append` untouched. `plan/tests/c2` 99→162, `c3` 97→117.
- **P4 step 1 — season detail:** `ed9f248` — `pgc/view/web/ep/list` runs builder → `ApiEnvelope.ReadResult` →
  `Parsing/Season/WebSeasonEpListParser` → mapper, with the field table and the 正片/预告 dedupe rule in the
  parser. `HttpResults.TryGetJObject` is the silent body decode the envelope uses, so a non-JSON body is an
  envelope failure instead of `解析json对象失败`; `GetJObject` keeps its toast for its existing callers on top of
  the same decode. `plan/tests/season1` (109 assertions) pins the envelope, the shapes that used to throw, a
  per-body no-toast check, the dedupe divergence and a refresh that fails after a good load.
- **BR-43 toast alignment, second attempt:** `78a94cb` — the device pass on `a99c3f8` still showed the text lower
  than the icon, so the shared forced line box is gone: both blocks use `TextLineBounds=Tight` with no
  `LineStackingStrategy` and no `LineHeight` at all, the pill keeps its old 38-DIP outer height through a scaled
  `ToastRoot.MinHeight`, and `BaseIconOpticalOffset` is the one residual constant (now 0). `plan/tests/toastalign`
  (36 checks) pins those invariants and exits 1 against `a99c3f8`.
- **BR-44 播放目录 crash:** `a99c3f8` — the fade now targets the button's content
  `StackPanel` and the scale stays on the `CompositeTransform`, so `Storyboard.Begin` no longer throws before
  `DirectoryRequested`; a `Begin` failure is logged instead of swallowing the click. `plan/tests/dirpress`
  (31 checks) reads the XAML names and fails when a `SetTargetProperty` names a property the target type does
  not have — it exits 1 against the pre-fix source. Device-confirmed 2026-11-01.
- **BR-45 list covers:** `433f128` — directory rows, the playlist template, the UGC-season template and the
  live honour icon route their string cover through the existing `imageConvert2` + `'200w'`, so the box gets a
  `BitmapImage` and an empty `Cover` falls back to the placeholder. `DownloadPage` keeps its binding: that model
  already stores a `BitmapImage`.
- **Cookie jar and packaging traps:** `fe335f3` — `Models/CookieOrigins.cs` is the one origin list (read across
  root/www/passport/api, write to root and www) so `ac_time_value` is visible, plus one verdict log line;
  `862e61c` — the build script prints only the installable `BiliLite.Packages_*`, fails before printing
  when it is missing, and deletes the `BiliLite.UWP_*` byproduct that installs into a launch crash.
- **Drawer polish from the device pass:** `5ed8d17` — `Models/DirectoryDrawerMotion.cs` (close token so a
  reopen during the close is never hidden, snap when animations are disabled), a scrim before
  `DirectoryHost` that closes on a tap and swallows the gesture, the 目录 button flashing with an active
  background and one detail-column highlight when the pivot already sits on the target page, a single
  header title (`ShowsSourceTitle` keeps the name only for 正片/预告), crosshair + 正在播放 for the locate
  action, 8px between the playing bar and the ordinal, and tooltips on truncated titles.
- **Login refresh:** `ea7ead8` — `CookieService.NeedsRelogin`/`ReloginRequired`, single-flight
  `RefreshCookieAsync` resetting its slot before releasing waiters, `MirrorRefreshToken` reading
  `ac_time_value` from the jar on every login path, an awaited preflight on both playurl paths, the
  `App.Resuming` hook plus one launch preflight, and 设置 → 账号 with 登录状态/刷新登录/重新登录/退出登录.
- **Collection and playlist navigation (P0–P2 season):** `67b9eae`, `399807d`, `6c9b39c`, `e9e5d2a`,
  `18c519d`, `d905726`, `f68b705`, `1dd359c`, `da62afc`, `4db379c`, `503ceae`, `d0d6bd9`, `0e5b0df` —
  one sequence state for next/previous/auto, a player-hosted 目录 drawer that works in full screen,
  pending/retry/late-result rules, honest counts, one single-flight paging channel with 加载全部,
  search by stable row identity, season 正片/预告 sections, and named queues. Copy rules recorded in
  `bug_report.md` BR-31; the reviewer's open finding became BR-32 and was fixed.

- **Response-evidence workflow:** capture of response shapes and drift behind two diagnostics toggles,
  redacted opt-in samples, one-click zip bundle with `manifest.json`, and `tools/import-capture.ps1`
  turning a bundle into probe fixtures. Finished plans and their references were removed.
- **N1 — player overlay toasts:** `312fa23` — `Controls/PlayerToast.xaml(.cs)` and
  `Services/PlayerToastService.cs`, keyed overlays (volume, brightness, seek, message, acceleration,
  speed) replacing the central `ToolTip` label, one service per `PlayerControl`, timers stopped in
  `Dispose`; build 0 errors.
- **Release tooling:** `build-release-x64.ps1` now captures the whole `-> … .msix` path (spaces
  included), derives the two expected package paths from `OutputDir` + version, refuses to continue
  when neither `BiliLite.UWP_*` nor `BiliLite.Packages_*` is found, and names the `CS2012` holders
  through Restart Manager before retrying. Verified with a full packaging run into `H:\uwp\pkg diag`.
- **BR-16 — quiet load-more:** `e5c97e1`, `261317f` — silent page-2 failure, legacy text in the only
  toast, gRPC cursor preserved so the retry resumes.
- **API-SMART-01:** `797dc3a`, `26d4e57`, `3665bdb` — adaptive gRPC/legacy selection for six dynamic
  paths, persisted `dynamic-api-history.v1.json`.
- **C1/C2/C3:** `92ecd6a`, `34ec312`, `c735a73` (+`18f71dc`) — collections, playlist paging, playlist
  links and continuation.
- **D1/D2:** `5e36ec2`, `e94c843` — danmaku list with copy/like, filter import/export.
- **G1–G5:** `cfcd3de`/`75e8ecb`, `914d32f`, `d4c6345`(+`6ae1981`), `65d5e8c`(+`4fca416`),
  `b39739a`(+`9fb3d16`).
- **Stability 1/3/4:** `96de3fc`, `0668875`, `546b409`.
- **Home-feed REST fallback (BR-13):** `5881dbb`.

## Open

| # | Item | State |
| --- | --- | --- |
| 1 | Player open/replace/close session ownership | **done** — `2c3fc09`; its device rows are 默认通过（维护者 2026-11-04） |
| 2 | Background playback ownership (SMTC + memory budget) | not started, needs device evidence (BR-21 recorded 2026-10-31) |
| 4 | API parsing structure (P4 other features, P5 retire the rest) | P0–P3 done: one `ApiEnvelope` reads every REST response, `WebDynamicFeedParser`/`LegacyDynamicFeedParser` own all dynamic sources, gRPC mapping is protobuf-only. P4 steps 1, 2 and 5 shipped: season detail `ed9f248`, playlists `1838108`, comments `f03078c` — each with a parser/mapper layer, DTOs under `Models/Api/`, a field table beside the parser and its own probe (`season1` 109, `c2` 162 + `c3` 117, `comments` 99). Remaining for P4: **account** only (step 4), which the maintainer is holding back; the video-detail step shipped in `e6c2fb5` and `c1` grew to 161 assertions. Order and traps: `plan/P4_INVENTORY.md` §5 |
| 5 | Collection and playlist navigation / UI | **done** — see the first Done entry; its device rows are 默认通过（维护者 2026-11-04） |
| 6 | Auth refresh defects — `plan/REFRESH_PLAN.md` | **shipped**: D2 `6ee8a81`, D4/D5 `75fe40f`, review findings `b4cd995` (rejected cookie write) and `14ac4c3` (a login is a new credential session), D3 `bba2df4` (explicit, resumable web-refresh transaction with a persisted pending record) and D1 `000bfd0` (cookie-to-app bridge plus one recovery decision table for Home and startup), logout cleanup `e0e454e`, and the review finding `ff26db7` (an account switch drops the saved web token even when the record carries no owner). Device rows are in `VALIDATION.md` and are 默认通过. P4 step 4 (account) stays held back by the maintainer |
| 7 | Hot tab card overflow — `plan/HOT_CARD_PLAN.md` | **shipped** in `3ec04aa` (BR-47). Layout-visible; its device rows are 默认通过（维护者 2026-11-04）, including the 150%/200% text-size case |
| 8 | Settings reorganization — `plan/SETTINGS_PLAN.md` | **shipped through 1.0.9.42**: slices 1–4 (`ed5fe9f`, `c4c683f`, `b9b58c5`, `0f11162`) plus `7e628c3` (HEVC/AV1 hints only for those formats, the CDN list and latency test collapse while replacement is off, the sample switch disables itself without rewriting the saved value, re-entering a pivot refills from storage instead of re-subscribing every `Loaded`, and `Assets/Text/help.md` rewritten against the current tabs). Device rows are the 1.0.9.42 section in `VALIDATION.md` and have not been run |
| 9 | Dynamics card contract — `plan/CARD_PLAN.md` | slices 1–4 and three review findings shipped: `2d1c4b3` (data), `48c5df5` (layout), `2072d64` (body mounting on reuse + the reservation lottery line), `1b51db4` (§4 renderer: paragraph flow, emoji variants, measured expansion, empty-body collapse) and `0b8ad87` (§3: explicit wire kinds, URL normalisation, attachments, comment-target pairs); `65fd423` closed the Opus title slot, the preview ratio box (§6), the two-level repost bound, the article branch through the shared page parser, ID dedupe on append and the ten-page feed window (§9). `798a03a` then shipped §6 (one `DynamicImageGrid` for every count ≥ 2, width-derived tiles, 88-DIP fallback, nine-tile cap with `+N`, the shared save menu) and §7 (one bounded attachment row with an open action only for a real destination). `1.0.9.50` then closed the §2 P1 unknown-kind row (`Parsing/Dynamic/UnsupportedCard.cs` keeps the outer card's author, time, text and id; `Other` renders in the shared shell with 打开原文 only when an id and a command exist). Remaining: the §9 measurement only |
| 10 | Comment popup dark-mode color values — BR-62 | **shipped** — `44d7f0e`. The dialog is a `Popup` child, so it now pins `RequestedTheme` from the host `Frame`'s `ActualTheme` (the reason `MessageToast`/`PlayDirectoryControl` pin one); `Utils.GetBrush` keys its dictionary off the same `ActualTheme`. Five tokens (`CommentDialogLayer`, `CommentDialogBorderColor`, `CommentDividerColor`, `CommentBandColor`, `CommentBadgeColor`) carry the window's neutrals — Light unchanged, Dark the same alpha on white plus the player drawer's `#F21A1A1A` sheet — and the two sort links use `HighLightTextColor`. Compile only (`-Fast -NoVersionBump -SkipProjectReferences`, 0 errors); device-confirmed 2026-11-08 (maintainer) |
| 11 | Playback checkpoint survives a pending history report (review `c4aa630`) | **shipped** — `cfc4a15`. The local position is written before the report gate, only the network report is coalesced, and a single drain loop keeps reporting while the pending snapshot changes; the lifecycle flush awaits that tail. Compile only (`-Fast -NoVersionBump -SkipProjectReferences`, 0 errors) |
| 12 | Live danmaku socket reconnects itself — BR-63 | **shipped** — `722555b`. `Connect` is a supervised loop (connect → join → heartbeat → receive): a dropped or close-framed socket is replaced with a fresh `ClientWebSocket`, the retry backs off 2→4→8→10 s and resets to 2 s on success, the heartbeat's `async void` can no longer throw out of `SendAsync`, and `Dispose` sets a flag first so no reconnect or new socket follows the page. Compile only (`-Fast -NoVersionBump -SkipProjectReferences`, 0 errors) |
| 13 | Minimized window returns maximized (BR-61) | **shipped, awaiting the device** — `bbfaf7f`. The previous `d0e7168` attempt was reproduced on 2026-11-08 13:12: `Application resumed` fired, `Window.Current.Activate()` was a no-op, the window stayed minimized and the system re-suspended the app 10 s later. `Helpers/WindowRestoreHelper.cs` now reads the CoreWindow HWND, resolves the `ApplicationFrameWindow` root with `GetAncestor(GA_ROOT)`, and restores with `ShowWindow(SW_SHOWMAXIMIZED|SW_RESTORE)` + `SetForegroundWindow`; `GetWindowPlacement`'s `WPF_RESTORETOMAXIMIZED` decides maximized-vs-normal because a minimized window reports `IsZoomed == false`. The resume/activation paths log hwnd/root/iconic/zoomed before and after. Compile only (0 errors) |
| 14 | Season page detail comes from `pgc/view/web/season` (BR-64) | **shipped** — `b643e77`. `ep/list` only returns `episodes`/`section`/`play_strategy`, so the whole `Detail` model (title, media_id, rating, areas, stat, user_status) was empty and `Utils.ParseArea(null, …)` crashed the page on `Regex.IsMatch`. `SeasonApi.Detail` now reads the season endpoint, the VM takes `Detail` from it and keeps `ep/list` for the episode/preview lists (and for `Detail.episodes`, the merged list the download dialog needs), `title` falls back to `season_title`, and `ParseArea` skips the 僅…地區 regexes on an empty title. Compile only (0 errors) |
| 15 | Season page requests use the season id (BR-65) | **shipped** — `5132840`. `LoadSeasonDetail` received `ep_id`, which is empty for every in-app entry point (they pass only `season_id`), so both `pgc/view/web/season` and `pgc/view/web/ep/list` were asked with an empty id and the page published an empty `Detail`/`Episodes`. The VM now takes `season_id`, asks both endpoints with `SeasonIdType.SeasonId`, and keeps `ep_id` only to pick the initial episode; the season1 probe follows the new contract. Compile only (0 errors) |
| 16 | Comment text is HTML-decoded before markup (BR-66) | **shipped** — `ec92550`. `ControlHelper.StringToRichText` escaped `&` first, so API text already carrying `&quot;`/`&amp;` rendered as literal entity text; it now decodes once with `WebUtility.HtmlDecode` before the escape pass, and `SeasonReviewVM` decodes short-review `content` the same way. Compile only (0 errors) |
| 17 | Empty player pivots stay hidden (BR-67) | **shipped** — `9fdfbf4` + `b95f2ab`. `NodePivot` was visible by default and the verdict only looked at `IsInteraction`; `Visibility` turned out to be the wrong lever because a `Pivot` draws a header for every entry in `Pivot.Items` regardless of the item's visibility (`LocalTracksPivot` had been `Collapsed` all along and still showed). The tabs are now removed from / inserted back into `Pivot.Items` at slots 0/1 — dropped in the constructor, so nothing shows before the first verdict — gated on the loaded node count and on manifest playback for the video/audio rows, re-judged after `GetNodes()`, with the interaction VM rebuilt per episode. Probe updated. Compile only (0 errors) |

**Order** (dispatcher, 2026-11-02, updated after the parallel wave): the engine-fallback disposal finding
(`0b70113`) is closed; D2 and D4/D5 are in; the first slices of the Hot-card, settings and dynamics plans are in
and were 默认通过. `plan/REFRESH_PLAN.md` is fully shipped (D1 `000bfd0`, D3 `bba2df4`, logout cleanup `e0e454e`), so the auth
row has no code left, and its device rows are 默认通过. `plan/SETTINGS_PLAN.md` slice 3 (`b9b58c5`) and `plan/CARD_PLAN.md` §3/§4 (`1b51db4`, `0b8ad87`) are in and were 默认通过. `7e628c3` then closed the settings row and
`65fd423` closed all but two card items, and `798a03a` closed those two (grid, attachments). `4dc85ce` then
taught the player to hide entries whose content does not exist. Next: `plan/CARD_PLAN.md` §9 measurement with
BR-21 (device journey, no code left), then whatever the 1.0.9.4x device pass reports. BR-53 (any live room shows
a `NullReferenceException` message) is recorded in `bug_report.md` and waits for a log line before any live-room
code changes; BR-54 (toast pill width and content centring) is fixed in `01e51a7` and waits only for the device
look.
Item 2 stays parked until BR-21 has measurements; P4 keeps only the account step, still held back. The 1.0.9.42–1.0.9.60 device rows are the next device pass (BR-56, local playback, the BR-57 copy row and the BR-58 tooltip row are the 1.0.9.59 rows; the BR-59 recommend-tooltip row and the BR-60 progress row are the 1.0.9.60 rows). 2026-11-08: BR-62 (comment popup dark color values) is in as `44d7f0e` and device-confirmed by the maintainer. Row 11 (review `c4aa630`) shipped as `cfc4a15` and BR-53 as `c0b7eb1`; BR-53 is device-confirmed (2026-11-08 13:02), row 11 still wants the pause-then-exit check. BR-63 (live danmaku reconnect) shipped as `722555b`; it needs a real disconnect before it counts. BR-61's first attempt failed on the device and `bbfaf7f` replaces it with the Win32 restore.

## 已知债务（2026-11-06，从源码 TODO 收拢）

源码里原先有 10 处 `TODO/FIXME`，已全部清掉。其中三处是真实欠账，记在这里而不是留在代码里：

| 位置 | 欠账 | 为什么现在不做 |
| --- | --- | --- |
| `Controls/Player.xaml.cs` | 播放器类写得过于复杂，需要整体重写 | 重写会动到 BR-1/BR-21/BR-43/BR-54 都已验收过的播放路径，没有设备测量支撑不做 |
| `Controls/Dynamic/DynamicParse.cs`（`MediaList` 分支） | 旧版合集卡的解析仍是逐字段手写，样例 `https://t.bilibili.com/625835271145782341` | 现代合集已走共享解析器；旧路径只在老接口回退时命中 |
| `Controls/FontAwesome5/FontAwesome.cs` | 多窗口下改字形可能撞线程约束，当前只 `Debug.WriteLine` | 只在多窗口改图标时可能复现，没有设备复现步骤 |

其余 7 处是过时注释或死代码，直接删除：`PlayerControl` 的"本地播放"（该分支已在播）、`VideoPlayHistoryHelper`
的"或许该用数据库"、`RecommendVM` 的重复加载提示（守卫就在下面）、`DynamicVM.OpenTag`（已经会打开话题页）、
`NewPage.xaml` 的折叠"自定义快捷入口"菜单项，以及 `MainPage` 里空的挂起循环与从未订阅的 `GoBack` 处理器。

## 1. Player session ownership

`PlayerControl` opens media in several paths (native DASH, FFmpeg DASH, quality change, engine
fallback) and mutates shared player fields across `await`s. Closing the tab or switching video while
an open is in flight lets the old operation resume against a replaced/cleared player, attach A's
source to B's player, or leak resources created after the close routine finished. `SetQuality` starts
`ChangeQuality` without awaiting it; `Player_ChangeEngine` starts another open on media failure with
no in-flight guard.

Fix shape: one session id per open, new resources kept local until the session is accepted, pending
opens invalidated on close/replacement, resources of an obsolete session disposed, callbacks from
obsolete players rejected. Do not rewrite the player.

## 2. Background playback ownership

`backgroundMediaPlayback` is declared in both manifests and `PlayerControl` wires Play/Pause and
status, so missing permissions are not the issue. The gap is ownership: system media controls are
view-scoped, and `OnUnload` disables them for every control, so a departing tab can disable the
controls of the player that is still playing. `App.OnSuspending` also does no player work and forces a
finalizer wait inside the suspension window.

Fix shape: one active playback owner keeps the SMTC session across tab and visibility changes, optional
visual/danmaku work is released when backgrounded, and suspension work stays bounded. Do not recreate
the player on every foreground return.

This one has no device evidence behind it — only source reading. It needs the maintainer's
observation (minimize vs tab switch, engine, crash details) before it is worth a refactor.

## 4. API parsing structure — names, fields, and methods

**Goal:** give each API response a clear route from transport to an application model. A caller should
know which endpoint produced a field, what that field means, and which method parses it. Start with
the dynamic feeds implicated in BR-17; migrate working features incrementally.

### Responsibilities and locations

The first implementation batch should inventory the existing paths before moving anything:

- `Api/` holds request builders. The general web dynamic feed now lives in
  `Api/User/WebDynamicFeedApi.CreateFeedRequest`, so the old `ArticleV2` name is gone.
- `HttpResults.GetJson/GetJObject/GetData/GetResult` in `Helpers/HttpHelper.cs` still decode responses
  and sometimes show toasts; `ApiDataModel<T>` and `ApiResultModel<T>` describe two different
  response envelopes, and neither is the parse result.
- `Helpers/DynamicGrpcMapper.cs` is protobuf-only again: the web half moved to
  `Parsing/Dynamic/WebDynamicFeedMapper.cs` with the shared scalar readers in `WireScalars.cs`.
  The two feed VMs decode through `Parsing/Dynamic/WebDynamicFeedParser.cs`; legacy REST responses are
  still deserialized inline, which is what P3 separates.

Use these boundaries for migrated features; the paths are proposed destinations, not files added
by this planning change:

| Boundary | Responsibility | Proposed location |
| --- | --- | --- |
| Request | Endpoint, parameters, authentication/signing, request construction | `Api/<Feature>/` |
| Transport | HTTP status/body or gRPC bytes/status; no interpretation of feature fields | Existing `HttpHelper` / `GrpcRequest` |
| Wire DTO | One source's envelope and payload fields; explicit JSON names | `Models/Api/<Feature>/<Source>/` |
| Parser | Decode and check the envelope/field shapes; return data or a parse failure | `Parsing/<Feature>/` |
| Mapper | Convert a source DTO or generated protobuf reply into the existing application model | `Parsing/<Feature>/` |
| Service / VM | Choose sources, own paging and request generations, publish rows and user messages | Existing services and VMs |

Keep the current HTTP library, protobuf generator and renderer. Do not introduce a second transport,
a dependency-injection framework, or a repository-wide rename to achieve these boundaries.

### Naming contract

| Kind | Convention | Dynamic-feed example |
| --- | --- | --- |
| Endpoint/request builder | `<Feature>Api`, with source named when variants coexist; builders are synchronous | `WebDynamicFeedApi.CreateFeedRequest(...)` |
| Network operation | `Get...Async` / `Load...Async`; `Async` means actual asynchronous work | `GetFeedPageAsync(...)` |
| Source DTO | `<Source><Feature><Payload>Dto`; C# properties use PascalCase and explicit wire-name attributes | `WebDynamicFeedItemDto`, `[JsonProperty("id_str")] DynamicId` |
| JSON parsing | `Parse<Source><Payload>` returning `ApiParseResult<T>`; no network or UI work | `ParseWebDynamicFeed(...)` |
| Conditional scalar read | `TryRead<Type>`; failure is distinguishable from a legitimate zero/false | `TryReadInt64`, `TryReadBoolean` |
| Source mapping | `Map<Source><Payload>`; share domain helpers only after source differences are handled | `MapWebDynamicItem`, `MapGrpcDynamicItem` |
| Normalized page | `FeedPage<TItem>` with `Items`, `NextCursor`, `HasMore` and source identity | Existing card models remain the items |

`ApiParseResult<T>` is a proposed small result type containing the parsed value or a failure category
and context. Do not treat `ApiDataModel<T>`, `ApiResultModel<T>`, `ReturnModel<T>` and
`DynamicApiSourceResult` as interchangeable: wire envelopes, parsing results and source-selection
results serve different purposes. Keep compatibility wrappers while callers migrate. Replace names
such as `ArticleV2`, misspelled builders and source-ambiguous mapper methods when their feature is
migrated; do not rename unrelated APIs in the same batch.

### Field contract

Maintain a compact field table beside each migrated parser: logical endpoint/source, wire path,
wire type(s) actually observed, normalized property/type, required/optional status, missing/null
behaviour, and conversion rule. Include the caller or fixture that establishes the rule.

| Field family | Rule |
| --- | --- |
| Envelope | Explicitly select `data` or `result` for the endpoint. A missing or malformed `code` is not implicit success. Normalize `message`/`msg` at this boundary. |
| IDs | Use descriptive names (`DynamicId`, `Aid`, `Cid`, `UserId`) and keep opaque IDs/cursors as strings. Convert to numeric IDs only where the endpoint contract requires it. |
| Nested values | Check object/array shape at every boundary. Missing, JSON null and the wrong token type are distinct; `?.` alone does not make a `JValue` indexable. |
| Numbers and flags | Centralize culture-independent readers. Accept numeric strings or `0`/`1` only where the endpoint's observed contract permits them; never turn a failed required parse into zero/false. |
| Counts and time | Separate display text such as `1.2万` from a numeric count. State timestamp units and distinguish epoch times, durations and presentation labels. |
| Optional content | Apply a documented default or preserve text when optional metadata is unusable, such as a string-valued `emoji`. Unknown card types should retain an existing unsupported-card placeholder. |
| Paging | Keep web `offset`, legacy dynamic IDs and gRPC `historyOffset/updateBaseline` separate and source-bound. A failed append keeps the last successful cursor. Server `has_more`, cursor advancement and filtered rows are separate facts. |

Use DTO deserialization for stable payloads and typed `JToken` readers for genuinely polymorphic
fields. Neither method should require serializing a token back to text merely to parse it again.
Add a shared `ReadObject`/`ReadArray`/scalar helper only when it replaces duplicated rules; keep
endpoint-specific aliases, units and defaults inside that endpoint's parser.

### Parsing and failure contract

1. Transport reports HTTP/gRPC status independently of the response body.
2. Decode once, check the endpoint's envelope, then parse its payload. Separate transport failure,
   server/business rejection and malformed payload; cancellation or a superseded request is not a
   parser failure. A valid empty page is not malformed.
3. Map the source payload into the existing application model. Missing optional metadata may
   degrade one card; a broken required envelope makes that source attempt fail. Record the policy
   for a malformed item instead of silently dropping an entire page.
4. Return the result without toasts or UI mutations. The source-selection service decides whether
   to fall back; the VM alone decides what to publish and which user message to show. Publish rows
   and new paging state only after the page is accepted and its request generation is still current.
5. Attach a logical endpoint/source, stage, field path, expected/actual token type and available
   request ID to parse failures. Reuse the response-evidence workflow in item 3 for captured shapes
   and fixtures rather than creating another diagnostics system.

### Implementation batches and completion criteria

| Batch | Work | Complete when |
| --- | --- | --- |
| P0 — inventory **(done)** | List the dynamic all/video/article, detail, space and repost endpoints, request builders, envelopes, field tables, parsers, mappers and callers. Identify shared rules versus source-specific rules. | Each active response path has a named owner; no existing source or paging cursor is omitted. |
| P1 — minimal contract **(done)** | Define `ApiParseResult<T>` and only the field readers needed by the first feature. Preserve existing callers through adapters. | Success, business rejection, malformed envelope and valid empty payload have distinct, documented results; parsers perform no UI work. |
| P2 — web dynamics **(web half done)** | Extract the web JSON part of `DynamicGrpcMapper` into the web parser/mapper, rename the general feed builder through a compatibility wrapper, and remove direct web-field parsing from the two feed VMs. | All/video/article retain their current rendering and source-bound cursors; the recorded string-emoji failure is handled; malformed pages reach source fallback without partial publication. |
| P3 — other dynamic sources **(done)** | Separate gRPC mapping from legacy REST parsing, reusing the existing application card model. Migrate detail, space and repost callers one path at a time. | Source differences remain explicit; comments, links, empty pages and failed-append retry semantics are preserved. |
| P4 — other features | Apply the same structure to comments, video/season detail, playlists and account responses in separate batches, driven by actual callers. | Each migrated feature has one documented parsing entry point and field contract; untouched features continue through their existing adapters. |
| P5 — retire duplicates **(started: `ClassifyLegacy` removed)** | Remove old parsing wrappers/model aliases only after their callers are gone; update the inventory and field tables. | Source inspection shows no parallel parser for the same response contract or transport/UI work hidden inside a parser. |

Acceptance for each migrated feature: large IDs, optional or mistyped nested fields, unknown cards,
empty pages, separate source cursors, failed append/retry and refresh replacement behave as they do
today.

## Parked

- **Live tiles:** optional; needs the tile service plus a background-task registration whose async
  refresh is awaited before the deferral completes.
- **Real buvid acquisition:** no source-backed flow; app-key headers do not promise it.
- **Alternate danmaku renderer:** nothing needs FrostMaster; keep NSDanmaku.
- **More API/auth variants:** no caller; do not port endpoint classes to mirror the sibling.
- **Local-library expansion:** the five milestones are done; more discovery/formats/artwork needs its
  own request. Keep the current manifest models as the contract.

## 播放器列表入口合并（维护者 2026-11-06 选定方案 A）

维护者指出「选集」按钮与「播放列表/播放目录」功能重叠。核对源码后的实际情况：

- 播放器底栏有三个列表入口：`BottomBtnDirectory`（播放目录，抽屉里带 分P/合集/播放列表/剧集正片/预告、搜索、
  定位正在播放、加载更多、按合集/播放列表连播）、`BottomBtnList`（选集，侧栏只列分P）、`BottomBtnNode`（节点）。
- `PlayerSurfaceVisibility.Decide` 里多P视频会**同时**显示选集与目录，而选集能做的每一件事目录的分P段都能做。

按方案 A 落地：删除 `BottomBtnList`、`EpisodeList`、`EpisodePivot`、`ShowPlaylistButton` 与配套的下标镜像代码；
侧栏只留 弹幕列表 / 节点 / 本地轨道，`EpisodePivot` 改名 `NodePivot` 且只在互动视频出现；分P 切换统一走播放目录
（页面 `DirectoryCaption` 本来就在多P时报告目录可用并给出 `3/12` 这样的位置）。`playertoolbar` 90 项重写，
`--legacy` 基线仍有 22 项失败，说明新断言钉的是这次的行为。
