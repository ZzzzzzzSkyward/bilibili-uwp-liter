# Bug report — open items only

Branch `work`. Solved entries were removed on purpose; the fixes and their evidence are one line each in
`git log`, and the reviewer's findings live in `plan/REVIEW_LOG.md`.

**Policy:** the maintainer's device pass counts as passed. A fixed item stays closed until a real bug is
reported; a new report starts a new entry here before any code changes.

## BR-21 — memory usage keeps growing while the app runs (reported 2026-10-31)

Maintainer: memory use grows over time during a session. No log evidence and no measurement yet — this is
the report, not a diagnosis.

Where the growth can plausibly come from, from what the code already does:

- the player keeps one live `PlayerControl` per opened item, and each holds a `PlayerToastService` plus an
  engine (`Player.xaml.cs`, native DASH / FFmpegInterop DASH / SYEngine swap paths) — sessions are only
  released through the existing `Completed`/`SetPlayItem`/`OnUnload` paths;
- the `Home`/`User` dynamic feeds page indefinitely through the web source, so item collections and the
  per-item `RichTextBlock`/image state grow with scroll depth;
- every response goes through `HttpResults`, which keeps the deserialized `JObject` alive as long as
  anything holds its `HttpResults`;
- `Modules/Home/DynamicVM` and `Modules/User/DynamicVM` hold `ObservableCollection` items that are never
  trimmed.

What an actionable report needs: the working set after N minutes for one specific journey (feed paging only
/ playback only / danmaku on), and whether the process is managed-heap bound or native bound. Without that
split, the background-playback and memory work in `plan/PLAN.md` cannot be scoped.

## BR-22 — 稍后再看点某一项可能打开别的视频（source review 2026-10-31）

`Pages/User/WatchlaterPage.xaml.cs` 先把 `已失效视频` 过滤成新的 `items`，却把
`watchLaterVM.Videos.IndexOf(data)`（未过滤列表的下标）写进 `VideoPlaylist.Index`。点击项前面只要有失效视频，
下标就偏移到别的视频；末尾的有效项可能越界，而 `Pages/VideoDetailPage.xaml.cs` 直接取
`playlist.Playlist[playlist.Index]`。

需要的修复：在过滤后的队列里按 aid 定位；定位不到或队列为空时不导航，也不要越界。

## BR-23 — 远程播放列表总数未知时显示成“已加载 n / n”（source review 2026-10-31）

`Models/MediaListResources.cs` 的 `MediaListPagingState.Append` 里
`TotalCount = page.TotalCount > 0 ? page.TotalCount : m_rows.Count` 把“服务器没给总数”变成“总数等于已加载数”。
第一页之后即使还有更多页，界面也会显示 `20 / 20`；后面某页又给了总数时也无法区分。

需要的修复：总数未知保持未知；已知总数在后续页缺省时保留。

## BR-24 — 最后一P 时手动“下一P”消失，合集/播放列表里的下一个视频点不到（source review 2026-10-31）

`Controls/PlayerControl.xaml.cs` 的 `BottomBtnNext` 只在 `CurrentPlayIndex != PlayInfos.Count - 1` 时可见，快捷键
`X/M/190` 在最后一P 提示“已经是最后一P了”；而同一个位置上的自动连播（`AllMediaEndEvent`）能继续走合集或播放列表。
只有一个分P 的合集视频因此没有任何手动前进入口，键盘和按钮都到此为止。

需要的修复：按钮、快捷键、自动连播用同一套判定；最后一P → 外层来源（合集/播放列表）的下一行；提示文字写出目标。

## BR-25 — 全屏/全窗口模式下整个外层列表不可达（source review 2026-10-31）

`Pages/VideoDetailPage.xaml.cs` 的 `PlayerControl_FullScreenEvent` / `FullWindowEvent` 把 `RightInfo.Width` 和
`BottomInfo.Height` 置 0，详情列连同合集、播放列表一起消失；播放器的“选集”只列当前视频的 `PlayInfos`，
单P 合集视频连这个按钮都没有。想看合集里的其它视频只能先退出全屏。

需要的修复：播放器里一个“目录”入口，在播放器内部打开覆盖层抽屉；普通窗口下则打开侧栏同一份列表。两个宿主
共享同一份状态和同一套行模板，浏览抽屉不改变播放、也不改变连播来源。

## Notes

- Install a `BiliLite.Packages_*` MSIX (root launcher ≈ 8 KB plus `entrypoint/BiliLiter.exe`). A
  `BiliLite.UWP_*` package whose root `BiliLiter.exe` is ~4.2 MB cannot start this app.
- 诊断 → 「记录 API 响应形状」 writes one NDJSON line per response (endpoint, status, timings, shape, and a
  drift line the first time a field changes type); 「导出诊断包」 collects the recent logs plus those
  captures and a `manifest.json` into one zip to attach to a report.
