# User validation checklist — current cycle

The per-batch progress notes were deleted when `plan/` was cleaned, and every batch older than the
collection/playlist UX cycle was removed after the maintainer's device pass. `git log` has them.

Policy in force: the device pass counts as passed. This file is for optional confirmation, not a gate.
A shipped item reopens only when a real bug is reported, and a new top-level `bug_report.md` entry
starts that.

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
