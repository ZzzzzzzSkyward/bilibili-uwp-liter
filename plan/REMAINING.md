# Remaining work — single index

Compiled 2026-10-30 for branch `work`. One entry per open item, each pointing at the markdown
that owns its detail. Read this file first; open the referenced section only when you start the item.

## Next session: build the UWP app

Nothing in this repository has ever been compiled as UWP on this machine (no UWP workload), so the
next step is a real build, not more source edits:

```powershell
# from the repo root, in a VS2022 dev prompt with the UWP workload installed
msbuild src/BiliLite.UWP/BiliLite.UWP.csproj /p:Configuration=Debug /p:Platform=x64
# or the packaging route that produces an installable MSIX
powershell -NoProfile -ExecutionPolicy Bypass -File build-release-x64.ps1
```

The two `Package.appxmanifest` files already carry a `1.0.9.15` version bump in the working tree
(uncommitted, the maintainer's). Expect the first build to surface UWP-only compile errors from the
recent page/XAML batches (C1–C3, D1, stability fixes) — they were written without a compiler.
Check after install: home dynamics (should fall back to REST on `-663`), playlist link handling,
collection tab, danmaku list flyout, and the selector's `Dynamic api stage=choose` log lines.

State of the branch when this was written:

| Fact | Value |
| --- | --- |
| `origin/work` head | `709ecf8 Record the current plan state and make the UWP build the next step` |
| Local commits ahead | none |
| Shipped | G1–G5 (+ review rounds), D1, D2, C1, C2, C3, API-SMART-01 (3 batches: selector, VM wiring, persistence), stability items 1/3/4, home-feed REST fallback, space `LocalTime`, repost fallback replacement, `plan/` consolidation + review log |
| Open review findings | none outstanding: the async `out`-parameter finding is fixed in `058c899`; earlier findings are logged in `plan/REVIEW_LOG.md` |
| Offline probes | `c1` 52 · `c2` 99 · `c3` 97 · `d1` 82 · `d2` 58 · `g2` 22 · `g3` 34 · `g4` 56 · `g5` 44 · `stab` 53 · `smart1` 160 — all green |
| Buildable here | `src/BiliLite.gRPC` only (0/0). No UWP build, no launch, no live API call |
| Uncommitted in tree | the maintainer's two `Package.appxmanifest` version bumps only |

--- | --- |
| `origin/work` head | `c735a73 Open remote playlist links and continue playback across pages` (all work below is pushed) |
| Local commits ahead | API-SMART-01 batches `797dc3a`, `26d4e57`, `Persist and report the adaptive source history` — local only, not pushed |
| Shipped | G1, G2, G3 (+ both review rounds), G4, G5 (+ its review fix), D2, C1, C2, C3, home-feed REST fallback, space `LocalTime`, repost fallback replacement, stability items 1 (tab Share), 3 (audio detach), 4 (WebPage close), D1, API-SMART-01 |
| Offline probes | `c1` 52 · `c2` 99 · `c3` 97 · `d1` 82 · `g2` 22 · `g3` 34 · `g4` 57 · `g5` 65 · `d2` 58 · `stab` 51 · `smart1` 160 — all `PASS`, exit 0 |
| Buildable here | `src/BiliLite.gRPC` only (0/0). No UWP workload, so no UWP build, no launch, no live API call |
| Uncommitted in tree | only the maintainer's two `Package.appxmanifest` version bumps (1.0.9.15) — not part of any batch |

--- | --- |
| `origin/work` head | `96c26cd Review origin/work 6ae19816…` (reviewer) |
| Local commits ahead | G5 `b39739a`, space-timezone review fix `4fca416` |
| Shipped this cycle | G1, G2, G3 (+ its review fixes), G4, D2, G5 |
| Offline probes | `plan/tests/{g2,g3,g4,d2,g5}/run.sh` → 22 / 34 / 57 / 58 / 61 assertions |
| Buildable here | `src/BiliLite.gRPC` only (0/0). No UWP workload, so no UWP build, no launch, no live API call |

---

## 1. D1 — Danmaku list and actions (feature, DONE — needs a device check)

**Status: implemented (commit `Add the loaded-danmaku list with copy and like actions`, probe `d1` 82).** `Controls/DanmakuListControl.xaml(.cs)` puts the flyout in the player toolbar, `PlayerControl.GetDanmakuListSnapshot` reads the existing pool, `Models/Common/Danmaku/DanmakuListSnapshot.cs` holds the window/order/action rules, and the csproj has the Compile/Page entries. Like/cancel reuse `DanmakuApi.Like(rowID, cid, 1|2)`, so only online rows with a server id offer them. The on-device pass (flyout, mode switch, copy, like + cancel, copy-only local rows) is still open; details in `IMPROVEMENT_PROGRESS.md`.

- **Goal:** browse the comment pool in a flyout (last 10 s / whole loaded pool), copy text, like an
  online comment; new locally-sent comments get copy only.
- **Detail:** `PLAN.md` → `## D1 — Danmaku list and actions` (also lists what already exists: the
  like request builder and `rowID/sendID` mapping).
- **Targets:** new `Controls/DanmakuListControl.xaml(.cs)`, `Controls/PlayerControl.xaml(.cs)`,
  `BiliLite.UWP.csproj` (explicit Compile/Page entries).
- **Why deferred:** pure XAML/UI; nothing here can be compiled or launched, so the batch would ship
  source-reviewed-only. Needs a device check by the maintainer.
- **Conflict:** touches `PlayerControl.xaml(.cs)`, which N1 also edits — do not run them together.
- **Sibling reference:** `Controls/Common/DanmakuListControl.xaml(.cs)`,
  `ViewModels/Common/DanmakuListViewModel.cs`, `Services/NsDanmakuController.cs::FindDanmakus`.

## 2. N1 — Keyed player overlay toasts (feature, unstarted)

- **Goal:** replace central-tooltip writes with per-key overlays for volume, brightness, seek,
  message, held acceleration and speed; held speed stays visible until release.
- **Detail:** `PLAN.md` → `## N1 — Player overlay toasts`.
- **Targets:** new `Controls/PlayerToast.xaml(.cs)`, `Services/PlayerToastService.cs`,
  `Controls/PlayerControl.xaml(.cs)` handlers (`HandleSlideProgressDelta`, `HandleSlideVolumeDelta`,
  `HandleSlideBrightnessDelta`, hold-speed start/end, `Dispose`).
- **Why deferred:** same UI-only limitation; also the largest handler-rewrite of the remaining set.
- **Traps:** one service per `PlayerControl` (no DI container in the target); remove timers in
  `Dispose`; omit SponsorBlock types; do not touch generic `MessageToast`.
- **Conflict:** `PlayerControl.xaml(.cs)` is shared with D1.

## 3. C1–C3 — Video collections and remote playlists (feature, C1–C3 done)

Ordered: C1 → C2 → C3. Independent of the gRPC sequence.

- **C1 — collection metadata + grouped list.** **Done** (commit
  `Show the source UGC collection on the video detail page`): `VideoDetailModel.UgcSeason` keeps
  `ugc_season` from the existing detail response, new `Models/VideoCollections.cs`, a 合集 pivot in
  `Pages/VideoDetailPage.xaml(.cs)` with sections and episode rows, selection keyed on the canonical
  `VideoInfo.aid`. Verification in `IMPROVEMENT_PROGRESS.md`.
- **C2 — remote medialist loading/paging.** **Done** (commit
  `Load remote playlists with paging on the video detail page`): small `Services/MediaListService.cs`
  over the existing `Api/VideoAPI.cs::GetMediaList/GetMediaListNotLogin` builders,
  `Models/MediaListResources.cs` with the paging state, cursor from the last returned aid,
  load-more while `has_more`. Verification in `IMPROVEMENT_PROGRESS.md`.
- **C3 — source links + playback continuity.** **Done** (commit
  `Open remote playlist links and continue playback across pages`):
  `Models/MediaListResources.cs::MediaListRoute.TryParse` (link default `page_type` 2, separate from
  the API default 1) and `MediaListQueue.NextAction`, `Models/VideoCollections.cs::VideoUgcSeasonOrder`
  for section/episode order, the `bilibili://music/playlist/playpage/{id}` route in
  `Helpers/MessageCenter.cs` (first page through the C2 service, `ml` route untouched), and
  `Pages/VideoDetailPage.xaml(.cs)` reusing the paging state and advancing across page boundaries
  when `HasMore`, with the visible Load more as the retry after a failed next page. Verification in
  `IMPROVEMENT_PROGRESS.md`.
- **Detail:** `PLAN.md` → `## C1–C3 — Show Bilibili video collections and playlists` (each
  sub-batch has its own numbered section with sibling references and exact constraints).
- **Traps:** `Pages/VideoDetailPage.xaml.cs` now builds the 合集 pivot item at index 0, so anything
  reading `pivot.Items[0]` must use `playlistPivotItem`/`ugcSeasonPivotItem` instead (C3's
  page-boundary advance lands in the same file).

## 4. Home dynamics show an empty page when the gRPC feed is rejected (`-663`) — **done** (batch commit `Fall back to the legacy feed when the home dynamic gRPC call is rejected`, the single local commit on `work` that closed this item)

- **Symptom (maintainer log, 2026-10-30 06:54):**
  `Home dynamics stage=request-start failed … grpc-status=2; grpc-message=-663; bili-status-code=-663`
  from `GrpcService.GetDynVideo` via `Modules/Home/DynamicVM.GetDynamicItems`. The page then shows a
  toast and stays empty.
- **Root cause:** `src/BiliLite.UWP/Modules/Home/DynamicVM.cs` is gRPC-only (996 lines; no REST
  branch, no `dynamicAPI` field). Every other dynamic surface was given a REST fallback —
  `Modules/User/DynamicVM.cs` switches to `stage=rest-fallback` and serves the legacy feed, and
  `bug_report.md` records that path as verified on device (BR-13). The home tab never got it.
- **`-663` itself is a known environment condition, not a new regression:** `src/BiliLite.UWP/Assets/Text/dbginfo.md:21-22`
  states the UWP app keys are rejected by some APIs with `-663`; `bug_report.md` (BR-13 log block)
  shows the same code on the working 动态 tab before it falls back to REST.
- **Fix shape:** restore the pre-gRPC legacy branch from `96fb4c7^` (`DyanmicNew(UserDynamicType.Video)`
  for the newest page, `HistoryDynamic(idx, …)` for older pages, cards deserialized straight into
  `ObservableCollection<DynamicItemModel>`, which still carries the legacy `card`/`desc` JSON shape
  its templates expect), triggered from the existing `catch`, with its own cursor fields next to
  `_historyOffset`/`_updateBaseline` and reset in `Refresh()`.
- **Verification that is possible offline:** `plan/tests/g4` already compiles the real
  `Modules/Home/DynamicVM.cs` against WinRT stubs and a stand-in `GrpcService`, so a stubbed
  throwing `GetDynVideo` plus a REST fixture can execute the fallback, its stickiness and its cursor
  separation for real.

## 5. Long-running stability — `LONG_RUNNING_STABILITY_REPORT.md` (source study only, no device data)

That report (root of the repo) investigated the long-session playback/tab/WebView complaints at
`59f89eb`. Its items, and where they stand here:

| # | Finding | State |
| --- | --- | --- |
| 1 | Closed media tabs keep their window `Share` (`DataRequested`) subscription, because `MainPage.ClosePage`/`MyFrame.Close` never navigate away; `VideoDetailPage`/`SeasonDetailPage`/`LiveDetailPage` only unsubscribe in `OnNavigatedFrom`. Retains pages for the window's life and lets a closed page answer a later Share. | **Done** — `Release the window Share subscription when a media tab closes`; guarded by `plan/tests/stab/run.sh` |
| 2 | Player opens are unowned: `Controls/Player.xaml.cs` awaits media creation and then reuses shared player fields, and `SetQuality`/`ChangeQuality`/`Player_ChangeEngine` can overlap. Needs a per-open session id, local-until-accepted resources, and rejection of obsolete callbacks. | **Open — deferred, largest refactor in the report** |
| 3 | Separate-audio element was detached from the wrong element in the close path. | **Done** — `0668875`; guarded by `plan/tests/stab/run.sh` |
| 4 | `WebPage` close could race its `EnsureCoreWebView2Async` continuations; the WebView2 was never explicitly closed. | **Done** — `546b409`; same guard script |
| 5 | Background playback: transport-control ownership follows the visual control's load/unload, so a departing tab can disable the shared system media controls; no mute-on-minimize code exists. | **Open — analysis only; the report itself says device evidence is required before claiming a cause** |

Item 1 practical shape (from the report): one repeatable cleanup routine used by both permanent tab
close and navigation departure, removing window-level subscriptions there, and making Share belong to
the active page. Item 2's shape is a session id per open — do not attempt it inside a UI batch.

## 6. Release tooling — MSIX artifact path regex

- **Item:** `build-release-x64.ps1` uses `->\s+(\S+\.msix)$`, which drops artifact paths containing
  spaces, after which package-layout validation can be skipped and a broken layout passes.
- **Detail:** `PLAN.md` → `## Carry-forward release item, separate from feature ports`.
- **Constraint recorded there:** capture the whole path, derive package paths from `OutputDir`/version,
  never treat a missing required artifact as success, keep the CS2012 retry.
- **Context on why layout matters:** `bug_report.md` → BR-15 (1.0.9.12 shipped a non-bootable root
  `BiliLiter.exe`).

## 7. Review loop — nothing open

- Latest review: `code_review.md` at `96c26cd`, covering `6ae1981`. Its single finding
  (`DynSpaceReq.LocalTime` unset while `DynAllReq`/`DynVideoReq` send 8) is fixed in `4fca416`, with g4
  pinning all three assignments (41 assertions); both earlier detail findings were confirmed addressed by that same review.
- Earlier reviews and their resolutions: `code_review.md` history (`22f3a63`, `d68fe0a`, `96c26cd`),
  `IMPROVEMENT_PROGRESS.md` sections for API-01 and G3–G5.

## 8. Parked / explicitly out of scope

`PLAN.md` → `## Parked work`: live tiles, real buvid acquisition, alternate danmaku renderer,
extra auth/API variants, local-library expansion. Not scheduled; each needs its own request.

## 9. Environment constraints that shape every batch

- No UWP workload on this machine: UWP source changes are probe-verified and source-asserted only;
  `src/BiliLite.gRPC` is the only project that truly builds. Say so in every report.
- `github.com:443` is intermittently unreachable; pushes and fetches need retry loops (both succeeded
  eventually in this cycle).
- `~/.omp/agent/config.yml` → `modelRoles.task` currently `opencode-go/deepseek-v4.1-flash`; the
  original `opencode-go/longcat-2.5-preview-free` fails with `No model selected`, so delegation only
  works with the current value.
- Two `Package.appxmanifest` version bumps (`1.0.9.15`) sit uncommitted in the working tree; they are
  the maintainer's, not part of any batch here.

## 10. API-SMART-01 — Adaptive dynamic API selection (DONE — device check open)

- **State:** all three batches are implemented and committed locally: selector core `797dc3a`
  (`Services/DynamicApiSelector.cs`), VM wiring `26d4e57` (`Services/DynamicApiSelection.cs` plus the
  three dynamic VMs), persistence and diagnostics `Persist and report the adaptive source history`
  (`Services/DynamicApiHistoryFileStore.cs`, app-local `dynamic-api-history.v1.json`, 32-bucket LRU,
  two-second coalesced writes). Probe `plan/tests/smart1/run.sh` → 160 assertions `PASS`.
- **Detail:** `ADAPTIVE_PLAN.md` (spec and acceptance scenarios); `PLAN.md` →
  `## API-SMART-01 — Adaptive dynamic API selection` (three-batch ledger, snapshot shape, log lines).
- **Remaining work:** none found in source. Nothing was deferred, and no follow-up is invented here.
- **Why it is still listed:** no UWP workload on this machine, so device behaviour is unverified. The
  maintainer's pass has to cover cold start, repeat-rejection preference persistence across a restart,
  recovery to gRPC, symmetric legacy-first fallback, valid empty pages, refresh keeping learned history,
  independent histories per account/app key, and the persisted file surviving an app restart.

---

## Suggested next order

1. Item 4 (home dynamics fallback) — the only open item backed by a real failure log, and the only
   one that can be tested by execution offline.
2. Stability item 1 (closed-tab `Share` subscriptions), then item 2 only if a session-ownership
   refactor is wanted.
3. N1 (D1 is done; both touch `PlayerControl`, so it cannot run beside N1).
4. C1 → C2 → C3 (done).
5. Item 6 whenever a packaging change is wanted.
6. Item 10 (API-SMART-01) needs no more source work — only the maintainer's device pass.

Sources: `plan/PLAN.md`, `IMPROVEMENT_PROGRESS.md`, `code_review.md`, `bug_report.md`,
`LOGGING_PROGRESS.md`, `plan/bug-fixes/dynamic-feed-mapper.md`,
`plan/bug-fixes/video-dynamics-pagination.md`.
