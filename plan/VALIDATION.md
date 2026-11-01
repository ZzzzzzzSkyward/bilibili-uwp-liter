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
