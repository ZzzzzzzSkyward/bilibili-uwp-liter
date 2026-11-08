# User validation checklist — current cycle

The per-batch progress notes were deleted when `plan/` was cleaned, and every batch older than the
collection/playlist UX cycle was removed after the maintainer's device pass. `git log` has them.

Policy in force: the device pass counts as passed. This file is for optional confirmation, not a gate.
A shipped item reopens only when a real bug is reported, and a new top-level `bug_report.md` entry
starts that.

**维护者 2026-11-04：本文件内的设备行一律按通过处理（默认通过）。** 不需要逐条回报；
只有真实报告的缺陷才会重开，并按 `bug_report.md` 新增条目记录。下面的表格保留为说明「这条改动在设备上
意味着什么」，不再是待办。

| Item | Commit(s) | Confirm on the device |
| --- | --- | --- |
| Watch Later opens the clicked row | `67b9eae` | a queue with invalid rows before the clicked one opens that same video; an all-invalid queue shows an empty state instead of indexing into nothing |
| Honest remote counts | `67b9eae` | a page without `total_count` never renders `已加载 n / n`; a later page that omits the total keeps the earlier server total |
| One sequence state decides next/previous/auto | `399807d` | P1 of a two-part video in a collection: X lands on P2 with the part highlight following, and the next button then reaches the outer target; keyboard next never replays the same part (`6c9b39c` fixed the stale selection) |
| 目录 drawer in full screen | `e9e5d2a`, `18c519d` | one-part collection video in full screen opens 目录; the playlist tab marks only the video that is actually playing; 定位正在播放 stays inside the browsed tab |
| 自动连播 covers both boundaries | `1dd359c` | with it off, a multipart video's last part and a one-part video both stop; manual next still advances; forward continuation starts the target at 0 while 上一个 resumes its saved position |
| Drawer paging and retry | `1dd359c` | a remote list with 20 loaded rows pages from inside the drawer while still in full screen; a boundary failure keeps rows, shows 重试, and the retry plays the waiting row once |
| Pending transition | `da62afc` | ring plus the bare target title while a row loads, a second next is ignored rather than queued, and the pending row never shows the 正在播放 bar |
| Search inside the drawer | `4db379c` | Enter on a filtered result plays that row, not its position in the filtered view; Escape clears the query before closing |
| Copy rules | `d905726`, `f68b705` | settings labels have no `（重启生效）` style notes; toasts show `50%` beside the volume/brightness glyph and nothing beside the mute glyph; 目录 is a list glyph plus `7/24`, and the next button's tooltip is the bare target title |
| P2b 加载全部 | `503ceae` | a 200+ row list keeps scrolling smoothly while pages land and does not jump; 取消 stops after the in-flight page and leaves every loaded row; the footer stays reachable |
| Stale queue teardown | `d0d6bd9` | entering from a playlist, then opening another video from a cached page, shows no leftover 播放列表 tab or directory section anywhere |
| Season directory | `0e5b0df` | 正片 and 预告 stay in separate tabs with the correct current episode; a preview never plays as an episode; a launcher link that carries `title=` names the queue |
| Drawer motion and dismissal | `5ed8d17` | the drawer slides in and out instead of jumping; a click on the video left of it closes it without starting or pausing playback or going full screen; closing and reopening inside one animation leaves it open |
| 目录 button feedback | `5ed8d17` | every click flashes the button, it keeps an active background while the drawer is open, and in a wide window whose detail column is already on that page one highlight shows the click landed |
| Drawer header and rows | `5ed8d17` | one title on the header, no second copy of the source name unless the title is 正片/预告, locate shows a crosshair plus 正在播放, the playing bar clears the ordinal, and a two-line truncated title shows its full text on hover |
| Login refresh | `ea7ead8` | after an overnight session the first play still offers 1080p and above; 设置 → 账号 reports 已登录 and 刷新登录 keeps it so; an expired session shows one 登录已失效 toast and 重新登录 restores quality |
| Cookie jar reads every login origin | `fe335f3` | the first preflight of a launch writes `Cookie refresh state refresh=… timestamp=…; token=present`, and once the server reports `refresh=True` the log shows the correspond → refresh → confirm chain instead of nothing |
| Only the installable package is delivered | `862e61c` | a full build prints one 交付包 path under `Z:\BiliLite.Packages_<version>_x64_Test\`, no `BiliLite.UWP_*` folder is left beside it, and installing that file starts the app instead of blinking out |

## Shipped 2026-11-01 — the batch that was uncommitted, now built

Compiled with `build-release-x64.ps1 -Fast -NoVersionBump -SkipProjectReferences` (0 errors), probes green:
`plan/tests/season1` (109 assertions) and `plan/tests/dirpress` (31 checks). No package was installed, so
every row below still needs the device pass.

| Item | Commit(s) | Confirm on the device |
| --- | --- | --- |
| BR-43 toast alignment | `78a94cb` | volume `100%` and brightness `100%` show the digits' ink centre on the same line as the glyph's ink centre — the digits must no longer sit lower than the icon; the pill keeps its old height and corner radius; the same relation at a small and a large window. The text-less 静音 pill was not reachable on the device, so it stays unverified. If a residual gap remains, `BaseIconOpticalOffset` (now `0`) is the only edit |
| BR-44 播放目录 button | `a99c3f8` | **confirmed 2026-11-01** — the directory button works on the device |
| Season detail parse (P4) | `ed9f248` | 正片 and 预告 tabs keep their contents and ordinals; an episode that also appears inside a section shows as 预告 only; a season with `result.episodes` empty (or a section with no episodes) shows the empty state instead of an error panel; `total: 0` still lists the episodes |
| Non-JSON body stays quiet | `ed9f248` | a season page whose response is not JSON shows the error panel and no `解析json对象失败` toast; other screens that still use `GetJObject` keep their toast |
| BR-45 list covers | `433f128` | directory rows show a thumbnail instead of an empty box, at the same size as the row; a row whose cover is empty shows the placeholder; playlist rows, UGC-season rows and a live anchor's honour icons likewise show an image, and no row downloads a full-size cover into a 64×36 box |
| Playlist sources (P4 step 2) | `1838108` | remote playlist pages, both medialist sources and the `bilibili://music/playlist/…` link open and page without a toast on success; a failed page keeps the rows and offers 重试 once; favorites and 稍后再看 lists render (their titles and rows) as before; a very large list still pages from the last loaded row |
| Comments (P4 step 5) | `f03078c` | a video's comments load, page and keep 置顶 once; a reply thread opens; like/reply/delete still work and still toast on their own failures; a closed comment section (12002) says so; a broken body shows 解析评论失败 instead of an empty section |
| Player session ownership | `2c3fc09`, ordering fix `340a031` | switching P and switching video while a URL is still being fetched never plays the previous video's audio/video and never leaves a stuck player; a quality change no longer needs a second click; a decode failure still falls back to the other engine exactly once; closing the tab mid-load leaves no sound and no error dialog |
| BR-46 space after a switch | `50bb009` | after auto-next, the 下一个/上一个 buttons, a directory row or a playlist row, pressing Space plays/pauses instead of activating a tab; pressing Space while the focus is on a tab header (reached deliberately with the keyboard) still does not steal focus; the directory list and every input keep their focus across a switch |


## Shipped 2026-11-02 — player attempt ownership and the renewal result

Compiled from `Z:\bilibili-uwp-liter` with `build-release-x64.ps1 -Fast -NoVersionBump -SkipProjectReferences
-Restore` (0 errors, package `BiliLite.UWP_1.0.9.34_x64`); probes green: `playersess` 161 checks, `auth3` 130
assertions. Nothing installed, so both rows still need the device pass.

| Item | Commit(s) | Confirm on the device |
| --- | --- | --- |
| Engine attempt releases what it built | `0b70113` | force the fallback a few times (a video whose native DASH source fails to build, or repeated quality switches on a long video): playback still lands on the fallback engine, audio stays in sync, closing the tab leaves no ghost audio, and the working set does not keep climbing across 10+ opens. This row is about leaks, so watch the memory graph rather than the picture |
| Renewal says what it actually did | `6ee8a81` | cut the network, then 设置 → 账号 → 刷新登录: it says the refresh can be retried later and the account stays 已登录 with no login dialog. A genuinely expired session still shows 登录已失效 and 重新登录 restores quality. A normal refresh still reports success and keeps 1080p and above |

## Shipped 2026-11-02 — the parallel wave (four slices, one build, one probe run)

Compiled from `Z:\bilibili-uwp-liter` with `build-release-x64.ps1 -Fast -NoVersionBump -SkipProjectReferences`
(0 errors, package `BiliLite.UWP_1.0.9.35_x64`), then **all 35 probe batches ran once** — `TOTAL_FAILURES=0`.
That run is also what found `ux1c`'s vacuous assertion (an `async` signature change had made its source scan
compare empty strings) and the three batches whose stubs predated the silent envelope decode; both are repaired
in `488a978`. Nothing installed, so every row below still needs the device pass.

| Item | Commit(s) | Confirm on the device |
| --- | --- | --- |
| Refresh identity | `75fe40f` | with `刷新登录` pressed after an overnight session, the app keeps working on the token it has; a QR login and a password login both still renew the next day. The failure this fixes is a stale `ac_time_value` cookie replacing a fresh token, so the visible symptom is "renewal stopped working until re-login" — if that reappears, the log line names the decision (`unknown-source-kept` / `response-authoritative` / `legacy-adoption`) |
| Settings page structure | `ed5fe9f` | the ten tabs appear in the new order; CDN sits under 网络与解析, 简繁转换 under 播放, the API-capture switches under 诊断, and the manual session fields inside 账号 (collapsed); every control still changes its setting; the old 直播视频格式 group is gone and live playback formats are unchanged |
| Hot card height | `3ec04aa` | on 热门, cards with a recommendation reason no longer clip or overlap: the tag sits on the thumbnail as one ellipsised line, the title keeps two lines, both metadata lines are whole. Narrow the window under ~480 to see the stacked card, and set Windows text size to 150% — if it still clips there, the plan's trigger cannot see it and the slot needs a measured height |
| Dynamics body | `2d1c4b3` | a dynamics card with an attachment card, a multi-picture post, or a sentence that spans an @/topic/emoji renders the whole body as one flowing paragraph; a photo-only card shows its text; a reserve卡 shows both description lines when the server sends two, and never a lone blank line; nothing regressed in the video/season/article tabs or in paging |
| Probe harness truthfulness | `488a978` | no device row: `g4` and `ux1c` are harness repairs. Their value is that the next unified run cannot pass while `ux1c`'s request-builder check compares empty strings |

## Shipped 2026-11-02 — wave 3 (cookie write, settings slice 2, dynamics layout)

Compiled with `build-release-x64.ps1 -Fast -SkipProjectReferences` (0 errors, package
`BiliLite.UWP_1.0.9.36_x64`), then the whole suite again: **35 batches, `TOTAL_FAILURES=0`**. Nothing installed;
every row below needs the device pass.

| Item | Commit(s) | Confirm on the device |
| --- | --- | --- |
| A rejected cookie write is reported | `b4cd995` | no visible change when everything works. The case it fixes is a renewal that reports 成功 while cookie-authenticated pages are unauthenticated: if a renewal succeeds but the app still behaves as logged-out, the log line says `token=saved cookie=failed`, and 设置 → 账号 should not claim 已登录 |
| Settings: dead controls gone, API keys safe | `c4c683f` | open 设置 with a custom API address/path set, close it without pressing 应用参数: the custom values must still be in effect and still filled in next time you open the page. The page no longer shows the 语言 dropdown, theme-colour picker, DefaultTheme/示例主题 notice, platform combo, 注销 accesskey button or 极验 test form |
| Dynamics card layout | `48c5df5` | the feed reads as one column: header (avatar, name, time, actions), body, then three evenly spaced action buttons; nothing is indented under the avatar any more. Narrow the window under ~480 to see the header collapse into one ⋯ menu with 详情/网页/删除/装扮信息. Check at 480/800/1200 and at 150%/200% text size that the name truncates instead of colliding, the pendant/verification overlay does not overlap the name or the tag badge, the OneRow cover is not letterboxed, and the divider between cards reads correctly on the page background |

## Shipped 2026-11-03 — wave 4 (three review findings, probe truthfulness)

Compiled with `build-release-x64.ps1 -Fast -SkipProjectReferences` (0 errors, package
`BiliLite.UWP_1.0.9.37_x64`), then the whole suite again: **35 batches, `TOTAL_FAILURES=0`**. Nothing installed.

| Item | Commit(s) | Confirm on the device |
| --- | --- | --- |
| A login starts a new credential session | `14ac4c3` | while signed in as A, use 设置 → 账号 → 重新登录 to sign in as B without logging out first: B's session must behave as B (its own feed/progress), and after a restart 刷新登录 must keep working for B. If B ever sees A's history or a renewal starts failing right after the switch, this is the code path to reopen. A hand-typed `ac_time_value` in 账号 keeps working, and clearing the box clears it |
| Reused card body | `2072d64` | scroll the dynamics feed so photo cards and text cards alternate; every card shows only its own body, never the previous card's text or links, including after returning from a detail page and after paging |
| Reservation lottery line | `2072d64` | a 预约 card whose server data carries a lottery line (抽奖) shows time, reservation count and the lottery text in one line, with no repeated sentence when the server fills the same string into two slots |
| No-explanation rule on labels | `30bc06a` | 账号 tab: labels are short (登录状态, 刷新登录, 重新登录, 退出登录, access_key, SESSDATA, bili_jct, ac_time_value); the grey help lines under them may explain, and no label carries a colon or a parenthetical |

## Shipped 2026-11-03 — auth wave (D1 + D3)

Compiled with `build-release-x64.ps1 -Fast -SkipProjectReferences` (0 errors, `BiliLite.UWP_1.0.9.38_x64`), then the
whole suite: **37 batches, `TOTAL_FAILURES=0`**. Nothing installed. Every row below is source + probe evidence; the
device column is what only the device can settle.

| Item | Commit(s) | Confirm on the device |
| --- | --- | --- |
| Resumable web refresh | `bba2df4` | sign in, leave the app on a page that refreshes the web session, then kill it while the confirmation is in flight (the app must not be in the foreground when it dies). Relaunch: the log must show the pending confirmation completed, and there must be **no** second `/cookie/refresh` for that generation. If the log shows a fresh rotation instead, the record was not read |
| Cookie-to-app recovery | `000bfd0` | sign in with the password/TV path (no OAuth refresh token), then invalidate the app key: the session must attempt the cookie-to-app conversion before any logout, and only log out when the web session is unusable as well. Worth watching the log line for the outcome; a transient failure must keep the session and toast, not log out |
| Pending record does not survive a logout | `e0e454e` | log out, sign back in as the same account: renewal must behave like a fresh login (no resume of the previous generation), and 设置 → 账号 must not show a stale access-key provenance |
| App-key expiry is metadata | `000bfd0` | a cookie-derived key expires locally after the estimated 240 hours; that alone must not trigger a renewal request on the next start (the log line says `estimated`) |

Two probe-side repairs rode along and are worth knowing about: the stub `ApplicationData.Current.LocalFolder` is now
per-run instead of per-PID (a stale `cookie-refresh-pending.json` in `%TEMP%\bili-probe-<pid>` made `api02` fail in a
sweep while passing alone), and no probe leaves `%TEMP%\bili-probe*` behind.

### Follow-up same day — the legacy-owner finding (`ff26db7`)

37 batches again, `TOTAL_FAILURES=0`; `auth3` 239. Device row: on an installation that has been signed in since before
this batch (its saved web token has no owner), use 设置 → 重新登录 to sign into a **different** account. After that
login, a web-session refresh must submit the new account's token; if the log shows the refresh rejected right after the
switch, this is the path to reopen. A same-account re-login must behave exactly as before.

## Shipped 2026-11-04 — settings slice 3 and card §3/§4

Compiled (`-Fast`, `BiliLite.UWP_1.0.9.39`), then the whole suite: **38 batches, `TOTAL_FAILURES=0`**
(`settings1` is new at 44, `g3` 176 → 281, `g5` 44 → 134, `g4` 149, `d2` 58). Nothing installed.

| Item | Commit(s) | Confirm on the device |
| --- | --- | --- |
| Rule import keeps local-only rules | `b9b58c5` | add a keyword, a user rule and a regex that the account does not have, then 从账号导入: all three must survive, and the imported ones must be appended once. Repeat with the network off — the lists must be unchanged, not emptied |
| Add buttons are local | `b9b58c5` | add a keyword from 设置 and from the player overlay: no “远程同步失败” on success, and the value appears once. Add a keyword that already exists: nothing is duplicated and the box is not cleared on a rejected regex |
| Regex validation | `b9b58c5` | type `(` and add: the pattern stays in the box, an error line appears under it, and the list does not grow. Reload the settings page: the bad pattern must not be gone-from-somewhere-else, because it was never stored |
| Live keyword box | `b9b58c5` | type into the video keyword box, then add a live keyword: the video box keeps its text and the live box is the one that clears |
| Mouse back in single-window mode | `b9b58c5` | with 鼠标返回 off, neither middle-click nor the back side button navigates back or closes a page; with it on, both do |
| Body paragraph flow and emoji | `1b51db4` | a post mixing text, a mention and an emoji renders as one sentence with no stray spaces; web emoji render as images rather than as text. A whitespace-only body shows no body row |
| Expansion | `1b51db4` | a long feed post shows six lines with 展开; a repost origin shows four; the detail page shows the whole body. Collapse and scroll: the anchor must not jump. Scroll a long feed back to an expanded card: it must keep its text |
| Unknown kinds and links | `0b8ad87` | a post with a kind the mapper does not know still shows its text (not blank, not a fake link). A `//host` link and a root-relative link open the right page; a link without a destination is selectable text |
| Attachments | `0b8ad87` | a photo or text post that also carries a vote or a reservation: the primary content renders and the attachment is not lost. Nothing renders attachments yet — this row is data only, so watch for a card that looks unchanged rather than for a new block |
| Comment routing | `0b8ad87` | open comments from a photo post, an article post, a music post and an ordinary text post: each must land on the right comment thread, and a post whose comments are forbidden must not open an empty thread |

The detail page's expanded body needed one line that no slice owned: a `BodyRole="Detail"` style on
`Pages/User/DynamicDetailPage.xaml`. It is compiled but not device-verified.

## Shipped 2026-11-04 — device reports and diagnostics cleanup (1.0.9.40)

Compiled to `BiliLite.Packages_1.0.9.40_x64.msix` with 0 errors (the first attempt hit `CS2012` on
`BiliLite.gRPC.dll` because a compiler server from a worker's own MSBuild still held the file; the servers were stopped and
the rebuild was clean), then the whole suite: **38 batches, `TOTAL_FAILURES=0`** (`g5` 134 → 200).

| Item | Commit(s) | Confirm on the device |
| --- | --- | --- |
| Card hairline gone (BR-49) | `ea3f42f` | the dynamics page has no line under any card, light and dark theme; cards are separated by spacing only |
| Hot card density (BR-48) | `ea3f42f` | the Hot page rows are as tight as before BR-47, two-line titles are not clipped, the recommendation tag still sits on the thumbnail, and the stacked layout under 480 is not left at the old height |
| Live dynamic card (BR-50) | `5173132` | a live card shows the room's real title, cover, area and viewer label with no 【直播有待修复】, and tapping it opens the matching room — not a wrong or dead one. A card whose stream has ended shows 直播已结束; a card with unknown state shows no badge |
| Realtime log removed | `0f11162` | the diagnostics tab has no 实时日志 switch and no log box; the log file still grows, and 导出日志 / 导出诊断包 still produce files |
| API parameter editor removed | `0f11162` | the diagnostics tab has no 编辑 API 参数; requests still work with the built-in appkeys, and an existing install's old overrides are simply ignored |
| 打开日志目录 | `0f11162` | the button opens the folder with today's `.log` in the file manager; with no log folder yet it still opens after creating it |

## Shipped 2026-11-04 — pending confirmation, spacing, top-nav indicator (1.0.9.41)

`BiliLite.Packages_1.0.9.41_x64.msix`, 0 errors, then 38 batches `TOTAL_FAILURES=0` (`webrefresh` 91 → 111).
Every row below is 默认通过 per the maintainer's instruction; the text says what a failure would look like.

| Item | Commit(s) | Confirm on the device |
| --- | --- | --- |
| A foreign pending record no longer blocks renewal | `b9fe695` | sign in as A, interrupt a web refresh so a pending record is left, then 重新登录 into B without logging out: B's cookie refresh must work (playback and any cookie-gated page), and A's record must still be on disk untouched |
| Account switch retires the old record | `b9fe695` | same setup, but reach B through a switch that commits: A's pending record must be gone, and a failed B login must leave it in place |
| Dynamics vertical rhythm | `81705a3` | two consecutive cards are about 24px apart instead of 48; the first card is not clipped against the tab bar, and the reading column and horizontal padding look unchanged |
| Top-nav selection line | `81705a3` | the line under the selected top tab (推荐/热门/番剧…) spans the tab width, sits at the same height, and the other visual states (hover, press, overflow) still look right |

## Shipped 2026-11-05 — settings gating, card title/repost/window (1.0.9.42)

`BiliLite.Packages_1.0.9.42_x64.msix` exists in `Z:\BiliLite.Packages_1.0.9.42_x64_Test` (2026-11-05 00:22).
No probe run is recorded for this build, and none of the rows below has been on a device yet.

| Item | Commit(s) | Confirm on the device |
| --- | --- | --- |
| Codec hints follow the format | `7e628c3` | 设置 → 播放: the HEVC 扩展 note appears only when DASH / HEVC (or M4S / HEVC) is the selected format and the AV1 note only for AV1; switching to H.264/Auto leaves no orphaned note or gap |
| CDN list collapses with replacement | `7e628c3` | with 视频 CDN →「CDN 替换」set to 关闭, the CDN 节点 list and 测试延迟 are not shown at all; turning replacement back on restores the saved node as selected |
| Sampling switch keeps its saved value | `7e628c3` | with 「记录 API 响应结构」off, 「记录正文样本」is visibly disabled and its help line says to enable the former first; toggle the sample switch on, turn capture off, reboot the app: the sample switch is still disabled, and turning capture back on shows the value that was saved — opening the page alone must not rewrite it |
| Pivot re-entry keeps inputs | `7e628c3` | type in a rule/keyword box, switch to another 设置 tab and back: the text is refilled from storage and adding one rule does not add it twice |
| Local help matches the tabs | `7e628c3` | open 设置 → 帮助: the text names only the current tabs and controls, no removed 实时日志 / 编辑 API 参数 / 语言 dropdown |
| One card title | `65fd423` | a video/OPUS card whose preview already prints a title shows it once, not twice; a repost's origin keeps its own title |
| Repost depth | `65fd423` | a three-level repost chain shows the outer card, one inset original and a 查看原动态 row (which opens the right dynamic); a missing original shows no empty frame and keeps the reposter's prose |
| Preview box reserved | `65fd423` | a long feed post with a video/live preview does not jump or reflow when the cover finishes loading, and the 长图/自然比例 single image keeps its shape |
| Article feed and dedupe | `65fd423` | the 专栏 tab loads and pages like 全部/投稿视频; paging or refreshing never shows the same dynamic twice |
| Ten-page window | `65fd423` | scroll past ten response pages of 动态: a 「更早的 N 页已移出」 banner with 重新加载 appears under the tabs, reloading it brings those cards back without duplicating the rows already on screen, the scroll anchor does not jump on expand/collapse, and jumping to an old card from the banner lands on the right one |

## Shipped 2026-11-06 — toast pill, photo grid, attachments, empty player entries (1.0.9.47)

Compiled with `-Fast`, 0 errors; probe batches cover the logic (`toastalign` 41, `cardlayout` 761,
`playertoolbar` 90, `g3` 342, `g5` 200). None of the rows below has been on a device.

| Item | Commit(s) | Confirm on the device |
| --- | --- | --- |
| Toast pill width and centring (BR-54) | `01e51a7` | the progress pill (hold/drag the seek bar) hugs the clock and the digits with a narrow margin instead of a wide band of background, and the icon+text pair sits on the pill's centre line instead of low; check the same at a small window and at 200% text size — volume/brightness pills use the same layout |
| 弹幕列表 hidden without danmaku | `4dc85ce` | open a video whose comment pool has no danmaku: the 弹幕列表 button is absent from the top bar and nothing opens an empty list; open one with danmaku and the button appears after the load; play a downloaded item with a danmaku file and it appears too |
| 选集 button and tab follow the playlist | `4dc85ce` | a single-part video shows no 选集 button and no 选集 tab in the settings pane; a multi-part video shows both; an interaction video shows 节点 instead and its tab lists the nodes |
| 本地轨道 follows the tracks | `4dc85ce` | play an ordinary online video: the 本地轨道 tab is gone from the settings pane; play a local/manifest file that has at least one track and it is back and lists them |
| Hidden tab does not leave a blank pane | `4dc85ce` | open settings on 选集 for a multi-part video, then switch to another episode/source that has no playlist: settings must land on 弹幕 (or the first visible tab), never a blank pane or a tab that is no longer shown |
| Photo grid by count | `798a03a` | 2 images: one row of two; 3: three across when the window is wide, two columns when narrow; 4: 2×2 with a save menu on long-press/right-click (previously missing); 9 and 12: nine squares, the ninth carrying +N, no horizontal overflow at a narrow window |
| Gallery order and +N | `798a03a` | tap the ninth square: the viewer opens on that image with the full set available, previous/next in source order; the +N badge disappears on a card with nine or fewer images |
| Attachment row | `798a03a` | a photo or text post that also carries a vote/reserve shows one quiet row under the body with its kind, title and one description line; 打开 appears only when the source supplied a destination and actually opens it; a post with no attachment shows no empty frame |
| Unknown card kind keeps its author | `1.0.9.50` | a card whose type the build does not know shows the author avatar, name and time, one line saying the kind is unsupported, and the original text selectable; 打开原文 opens that dynamic |
| No dead 打开原文 | `1.0.9.50` | an unsupported card whose id is empty (or a detail page that cannot navigate by id) shows no 打开原文 button at all, and the interaction row is absent on unsupported cards |
| Twelve-image grid stays inside its height | `1.0.9.51` | a 12-image post shows nine squares with +3 on the ninth; nothing bleeds over the card's action row and the card below is not overlapped; tapping the ninth opens the viewer at image 9 of 12 |
| Reload while paging | `1.0.9.51` | scroll to 12+ pages, tap 重新加载 for an earlier page, and keep scrolling while it loads: when it lands, the reinserted page keeps its own number and content (no page-3 items under a page-2 header), and the remaining earlier pages can still be reloaded one by one |
| Live room opens without the null-reference toast | `1.0.9.52` | open any live room (live and not live): no `Object reference not set` toast; if a sub-request still fails, the log has `直播接口返回的数据解析失败 step=…` or `直播详情加载失败 stage=…` naming the step, and the room shows what it could load |
| Bag with a gift missing from the room config | `1.0.9.52` | a room whose gift config lacks one of your bag gifts: the bag opens and shows the rest instead of the whole panel failing |
| Open original of an unknown card | `1.0.9.53` | an unsupported dynamic's 打开原文 opens `t.bilibili.com/<id>` in the browser (or in-app web page), never the native detail page with the same placeholder again; a card without an id shows no button |
| Refresh while an earlier page reloads | `1.0.9.53` | start 重新加载 for an earlier page, refresh the feed (or switch category) before it returns: after the refresh the 更早的页 hint is empty, and reloading later pages uses the new feed's cursors, not the old ones |
| Reserve card looks like a card | `1.0.9.54` | a 预约 dynamic shows the author's text, then a rounded bordered panel: pink 预约 chip, two-line title, two-line 预约说明, and a bottom action with the payload's own label/icon that opens the jump url; a reserve payload without button text shows no action row |
| Long titles show a tooltip | `1.0.9.55` | hover a card title that is cut off at two lines (feed, 预约 panel, video preview, 热门 card): the tooltip shows the whole title; hovering a short title shows the same text and is harmless |
| One list entry in the player | `1.0.9.56` | a multi-P video shows one 目录 button with a `3/12`-style caption and no 选集 button; opening it lists every part and jumps on click; an interaction video still shows 节点 and its tree in the side pane |

## Shipped 2026-11-06 — live guard and legacy web escape (1.0.9.57)

`BiliLite.Packages_1.0.9.57_x64.msix` exists in `Z:\BiliLite.Packages_1.0.9.57_x64_Test` (package directory dated 2026-11-06 22:32) and was compiled. No device row below has run.

| Item | Commit(s) | Confirm on the device |
| --- | --- | --- |
| Live-room sub-request failure no longer throws | `2276f28` | open any live room (live and not live): no `Object reference not set` toast; if a sub-request response fails to parse, the log carries `直播接口返回的数据解析失败 step=…` or `直播详情加载失败 stage=…` naming the step, and the room shows what it could load. |
| Legacy unsupported card opens the web | `2276f28` | an unsupported card produced by the REST fallback shows 打开原文 and opens `t.bilibili.com/<id>`; a card with no id shows no button. |

## Shipped 2026-11-07 — reserve attachment panel and tappable titles (1.0.9.59)

`BiliLite.Packages_1.0.9.59_x64.msix` was installed on the maintainer's device on 2026-11-07; its title-tooltip and
playback paths produced BR-59 and BR-60 (fixed in the next batch), and the rows below were not individually checked
off. Compiled (`-Fast`, 0 errors), `g5` 332 and `localdir` 50 assertions. This batch supersedes the untested
1.0.9.58 package. It also passes `64h.png` to the standalone Reserve panel's icon converter, so that icon no longer
disappears when the payload supplies one.

| Item | Commit(s) | Confirm on the device |
| --- | --- | --- |
| Reserve attachment looks like a card (BR-56) | `a431b4c` | a photo/text dynamic carrying a live reservation shows one rounded bordered panel under the content: pink 预约 chip, the reservation title at 18/26 in at most two lines, the tag line at 14/21 in at most two lines (`11-13 20:00 开播 175人已预约`), and a bottom action with the payload's own label and icon that opens the live URL. A payload with no button text, or with no usable route, shows no action row; a vote or link attachment on the same card keeps the old quiet row. |
| Local playback keeps its picker | `9c8920f` | open a downloaded season or a local collection with 3+ episodes: the 目录 button shows a `2/3`-style caption and opens the drawer with every episode; tapping one plays it and 正在播放 follows; a single local item shows no 目录 button and no empty drawer. |
| Reservation copy and attachment labels (BR-57) | `65ca559` | a live reservation keeps its title and shows the tag line `11-13 20:00 开播 175人已预约` with no brackets anywhere; a `desc3` lottery line becomes a third tag; a goods attachment shows 「商品」 with its title and 打开 and never `ADDITIONAL_TYPE_GOODS`; an attachment with no title, description or link draws no row at all. |
| Long card titles open their tooltip on tap (BR-58) | `65ca559` | hover a truncated 18px title (feed card, reserve panel, attachment row, video/column card) and the full text appears exactly as before; tap or click the title with touch or a mouse and the same full text appears for about four seconds and then closes. Tapping the title starts nothing else, and the title is otherwise unchanged. |
| Text reservation post shows one panel (review `ac6867c`) | `95934db` | open a text-only reservation post: exactly one reservation panel, the title once and the tag line once, with no second panel below it; a photo post carrying the same reservation still shows the panel under its images; a text post that also carries a vote keeps the single panel plus the quiet vote row. |

## Shipped 2026-11-07 — recommend tooltips and progress checkpoints (1.0.9.61)

`BiliLite.Packages_1.0.9.61_x64.msix` exists in `Z:\BiliLite.Packages_1.0.9.61_x64_Test`; built with the plain
script (full Release build, 0 errors), `g5` 335 and `playersess` 203 assertions. This is the `20261107` release
build and supersedes the untested 1.0.9.60 package. No device row below has run.

| Item | Commit(s) | Confirm on the device |
| --- | --- | --- |
| Recommend card titles show a tooltip (BR-59) | `ae90881` | on 推荐, hover a card whose title is cut off (both the grid card and the list card): the full title appears as a tooltip, and a click still opens the card. |
| Playback progress survives closing the app (BR-60) | `ae90881` | play a video for 2+ minutes, then close the window without going back; reopen the app and the same video resumes within ~30 seconds of where it stopped, and 观看记录 carries that progress. Same after minimizing (suspend) and for a season episode. |

## Shipped 2026-11-08 — comment popup dark color values (BR-62, compile only)

Compiled from `Z:\bilibili-uwp-liter` with `build-release-x64.ps1 -Fast -NoVersionBump -SkipProjectReferences`
(0 errors; no MSIX produced). No package and no device run: the maintainer's 2026-11-08 instruction makes the device
report the test, so the rows below say what to look at rather than gate the batch.

| Item | Commit(s) | Confirm on the device |
| --- | --- | --- |
| Comment popup follows the app theme (BR-62) | `44d7f0e` | in 深色, open a comment popup (a dynamic card's comment glyph, or a user page's comment): the panel is the dark `#F21A1A1A` sheet instead of the old white frosted one, the body text is light, separators / reply band / UP badge are white-alpha and visible, and 热门评论/最新评论 use the app's pink (`#fff47bcf`) instead of the OS accent. In 浅色 the window is the old one value for value except those two sort links, which now use `#d0318c` |
| Popup and code brushes agree in 跟随系统 | `44d7f0e` | with the system in 浅色 and the app on 跟随系统, nicknames and like/comment counts are dark text (they used to be the dark-palette `#dddddd` on a light panel); switching the app to 深色 flips the XAML tokens and the `GetBrush` values together, so one window never mixes a light and a dark value |

