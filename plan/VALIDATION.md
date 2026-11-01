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
| Playlist sources (P4 step 2) | `355e511` | remote playlist pages, both medialist sources and the `bilibili://music/playlist/…` link open and page without a toast on success; a failed page keeps the rows and offers 重试 once; favorites and 稍后再看 lists render (their titles and rows) as before; a very large list still pages from the last loaded row |
| Comments (P4 step 5) | `2c2de3d` | a video's comments load, page and keep 置顶 once; a reply thread opens; like/reply/delete still work and still toast on their own failures; a closed comment section (12002) says so; a broken body shows 解析评论失败 instead of an empty section |
| Player session ownership | `ca0a0e1` | switching P and switching video while a URL is still being fetched never plays the previous video's audio/video and never leaves a stuck player; a quality change no longer needs a second click; a decode failure still falls back to the other engine exactly once; closing the tab mid-load leaves no sound and no error dialog |

