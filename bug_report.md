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

## BR-26 — 键盘换到下一P后，“下一个”按钮还在重播同一P（review 2026-11-01）

`Controls/PlayerControl.xaml.cs:596-598` 的快捷键直接 `SetPlayItem(EpisodeList.SelectedIndex + 1)`：`SetPlayItem` 只更新
`CurrentPlayIndex`（:1114），不同步 `EpisodeList.SelectedIndex`；而 `BottomBtnNext_Click`（:2719-2725）按
`EpisodeList.SelectedIndex` 判断“还有没有下一P”。于是两分P 视频按 `X` 到 P2 后，按钮仍当成在 P1：点一次重播 P2，
键盘反复按也到不了外层下一个视频，而预览文字已经写的是下一个视频。

需要的修复：当前分P 只留一个来源；分P 变化时同步 `EpisodeList.SelectedIndex`（带重入保护，别触发第二次播放），
按钮、快捷键、预览、可见性都用同一个当前下标。

## BR-27 — `-Restore` 在 SDK 环境就绪之前运行（review 2026-11-01）

`build-release-x64.ps1` 把自动还原放在 SDK 探测之前：带 `-Restore` 时还原仍走认不出 `Microsoft.NET.Sdk` 的 VS MSBuild，
在设置 `MSBuildSDKsPath` 之前就失败，等于这组开关永远无法还原包变更。

需要的修复：把 SDK 探测与 `MSBuildSDKsPath`/工作负载环境设置提前到可选还原之前；`BuildProjectReferences=false`
仍然只留在快速编译分支。

## BR-28 — 播放列表里的“正在播放”标在旧游标上（review 2026-11-01）

`Models/PlayDirectory.cs` 的 `FromPlaylist` 按 `playlist.Index` 标“正在播放”，而这个游标从合集选行后故意不动；`VideoDetailPage`
又拿 `row.IsCurrent` 当“点了也不用播”的依据。结果：从播放列表进站、再点合集里的视频后，播放列表里原来那行仍标着正在播放，
点它什么也不发生，从目录回不到它。

需要的修复：目录里的“正在播放”来自实际在播的 aid，和队列游标分开；激活行的判断按真实身份，只有确实是在播的那一行才不重播。

## BR-29 — 全屏/全窗口里远程列表无法翻页或重试（review 2026-11-01）

抽屉只有已加载行和来源动作，没有“加载更多/重试”；真正的翻页按钮在详情列里，而全屏/全窗口把详情列宽度置 0。
已加载 20 行、后面还有页时，全屏里看不到第 21 项；自动连播失败时提示“点加载更多重试”，可那个按钮正好看不见。

需要的修复：抽屉里复用页面已有的取页操作和游标，带已加载/加载中/失败/已到头状态，禁止重复请求，取回后刷新目录且不起播。

## BR-30 — “定位正在播放”会把浏览分段切回分P（review 2026-11-01）

`PlayDirectoryControl` 的 `LocateCurrent` 取所有分段里第一个 `IsCurrent` 行，而分P分段排在合集/播放列表之前；多分P 视频跟着合集时，
打开抽屉就自动定位并跳到“当前视频分P”。另外 `SetModel` 每次都把浏览分段重置成活动来源，换P 会把用户从正在浏览的页签拉走。

需要的修复：定位优先在当前浏览的分段里找，找不到再用活动来源分段；重画时保留仍存在的浏览分段，只有新打开或旧分段消失才回到活动来源。

## BR-31 — 文案规范：短文本不写解释，图标不与文字重复（维护者 2026-11-01）

两条硬规则，适用于全部用户可见文案：

1. 短文本不携带解释性描述。反例：`自动连播（分P及下一视频）`、`下一P：<标题>`、`播放列表 · 第7项 · 连播来源，接下来`。标签只说控件做什么；
   解释留给悬浮提示、帮助文本，或者干脆不要。
2. 图标不与文字重复。反例：`[时钟]进度：01：23`。要么图标 + 纯数值，要么文字标签 + 不带图标，不能两边都说同一个词。

需要的工作：清点 `src/BiliLite.UWP` 全部可见文案（XAML 与 C# 两侧），按上面两条逐条改正，并把新加的连播/目录文案一起收进这个规范。

## Notes

- Install a `BiliLite.Packages_*` MSIX (root launcher ≈ 8 KB plus `entrypoint/BiliLiter.exe`). A
  `BiliLite.UWP_*` package whose root `BiliLiter.exe` is ~4.2 MB cannot start this app.
- 诊断 → 「记录 API 响应形状」 writes one NDJSON line per response (endpoint, status, timings, shape, and a
  drift line the first time a field changes type); 「导出诊断包」 collects the recent logs plus those
  captures and a `manifest.json` into one zip to attach to a report.
