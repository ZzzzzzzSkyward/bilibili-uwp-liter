# Bug report — open items only

Branch `dev`. Solved entries were removed on purpose; the fixes and their evidence are one line each in
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

## BR-31 — 文案规范：短文本不写解释，图标不与文字重复（维护者 2026-11-01）

两条硬规则，适用于全部用户可见文案：

1. 短文本不携带解释性描述。反例：`自动连播（分P及下一视频）`、`下一P：<标题>`、`播放列表 · 第7项 · 连播来源，接下来`。标签只说控件做什么；
   解释留给悬浮提示、帮助文本，或者干脆不要。
2. 图标不与文字重复。反例：`[时钟]进度：01：23`。要么图标 + 纯数值，要么文字标签 + 不带图标，不能两边都说同一个词。

新增文案直接按这两条写；一次性的大扫除已经做完，不再保留逐条清单。

## BR-43 — 播放器浮层的图标与文本没有垂直居中对齐（维护者 2026-11-01，带截图）

`Controls/PlayerToast.xaml` 里 `IconGlyph`（FontAwesome5 字体）与 `TxtToast`（正文字体）都是
`FontSize="20" VerticalAlignment="Center"`，但两种字体的行框/字形落点不同，胶囊里图标与 `100%` 的墨迹中心对不齐：
截图里数值明显高于图标中心。

需要的修复：两个文本块用同一套行框规则（行框高度算出来是同一份，`ApplyScale` 里一起缩放），图标只允许有
一个命名的补偿常量，不按图标类型写特例；不通过 Padding 上下不对称的办法掩盖。

**真机复测（维护者 2026-11-01，修复 `a99c3f8` 之后）：** 浮层里的图标和文字**比之前更往下偏**了。
上一版是"文字高于图标中心"，这一版把两个文本块的行框统一成 `BlockLineHeight` + `LineHeight = 20 × 1.1`，
并给图标 `TranslateY = -2.4`；维护者的观感是整体下沉，比原来更明显。同批次的 BR-44 播放目录已确认真机正常。

**第二轮修复（2026-11-01，未上真机验收）：** 原因认定是"行框等高 ≠ 墨迹等高"：`BlockLineHeight` 只是把两块
的盒子钉成同一高度，文字字体在行框内自带的下沉留白被一起搬进了对齐里，于是把文字墨迹推得比图标更低——
上一版用统一行框来对齐，方向就是错的。新机制不再钉任何行框：两块都改成 `TextLineBounds="Tight"`、删掉
全部 `LineStackingStrategy` 与 `LineHeight`（XAML 和代码里都不留），盒子包住各自的墨迹，等高即等墨迹。
墨迹收紧后内容变矮，胶囊的外框靠 `ToastRoot.MinHeight` 维持旧尺寸（`BaseContentHeight` = 20 × 1.1 = 22，
加上下内边距后乘同一个 `scale`）；垂直方向的补偿只剩 `BaseIconOpticalOffset` 一个常量，当前为 `0`，
只作 FA 字形墨迹与度量框不对称的残差，真机上若仍差一点就只改它。`plan/tests/toastalign/` 把这个形状钉住，
对 `a99c3f8` 跑是红的（报出 2 处 `LineStackingStrategy` 和 2 处 `LineHeight`）。

## BR-45 — 列表行的封面把字符串直接绑给 `Image.Source`，没有走转换器（内部评审 2026-11-01）

这是一条静态读出来的报告，不是真机复现：列表行的封面是 `string` 的图片地址，绑定却没有声明成
`ImageSource`。评审给出的结果是每行缩略图空白；下面写清楚成立的部分与依赖运行时行为的部分。

事实（源码可证，逐处给出被绑定属性的声明）：

- `Controls/PlayDirectoryControl.xaml:105` 是 `Source="{Binding Cover}"`，而 `Models/PlayDirectory.cs:61`
  的 `PlayDirectoryRow.Cover` 是 `string`（网络模型里就是 `cover`/`arc.pic` 的地址）；
- `Pages/VideoDetailPage.xaml:23`（播放列表）与 `:68`（远程 medialist 行），模板项是
  `VideoPlaylistItem`（`Models/VideoQueue.cs:34` 类，`Cover` 在 `:38` 声明为 `string`）；
- `Pages/VideoDetailPage.xaml:48`（合集分集），模板项是 `VideoUgcSeasonEpisodeView`
  （`Pages/VideoDetailPage.xaml.cs:26` 类，`Cover` 在 `:31` 声明为 `string`）；
- `Pages/LiveDetailPage.xaml:840` 是 `Source="{Binding pic_url}"`，外层 `GridView` 的项源是
  `liveRoomVM.Profile.glory_info`（`Modules/Detail/LiveRoomVM.cs:1815`，
  `List<LiveAnchorProfileGloryInfo>`），该项的 `pic_url` 在 `Modules/Detail/LiveRoomVM.cs:1763` 声明为 `string`；
- 即使绑定引擎把字符串变成图片，得到的也是封面原图：没有 `@200w` 这种尺寸后缀，64×36 的格子会拉一张原图；
- `Cover` 为空时没有任何兜底。`PlayDirectoryBuilder.FromPlaylist` 对空项写 `""`（`Models/PlayDirectory.cs:677`），
  季页对空集写 `""`（`:740`），这两类行就是一个空框。

同一形状但不是缺陷、因此没动的一处：`Pages/DownloadPage.xaml:45` 也是 `Source="{Binding Path=Cover}"`，
但侧栏的数据上下文是 `DownloadedItem`（`Modules/DownloadVM.cs:820` 类），它的
`Cover` 在 `:825` 声明为 `BitmapImage`（本地已下载文件的封面，由 `CoverPath` 读出来后放进模型），
目标类型本来就是 `ImageSource`，再套转换器只会把本地路径当网络地址处理。

依赖运行时行为的部分：`Image.Source` 的类型是 `ImageSource`，`{Binding}` 只对"往字符串转"有文档保证
（[Data binding in depth](https://learn.microsoft.com/en-us/windows/uwp/data-binding/data-binding-in-depth)），
对 `ImageSource` 这种目标类型没有承诺；`{x:Bind}` 会显式生成 `XamlBindingHelper.ConvertValue`，`{Binding}`
不生成。所以空白是真机结论，静态这一侧只能说"没有转换器，全靠运行时自己接"。

需要的修复：改用仓库里现成的 `imageConvert2`（`Converters.ImageSourceConvert`，`App.xaml:327`），参数写
`'200w'`，与 `Pages/SeasonDetailPage.xaml:125` 的封面同一个形式——返回 `BitmapImage`，空/空串直接落到
`ms-appx:///Assets/Thumbnails/Placeholde.png`，空封面行也就有了占位图。不新增转换器、不在 code-behind 里造
`BitmapImage`、不改模型类型；各行原有的格子尺寸（目录 64×36、详情与荣誉图标 56×56）和
`Stretch="UniformToFill"` 保持不变。

## Notes

- Install a `BiliLite.Packages_*` MSIX (root launcher ≈ 8 KB plus `entrypoint/BiliLiter.exe`). A
  `BiliLite.UWP_*` package whose root `BiliLiter.exe` is ~4.2 MB cannot start this app.
- 诊断 → 「记录 API 响应形状」 writes one NDJSON line per response (endpoint, status, timings, shape, and a
  drift line the first time a field changes type); 「导出诊断包」 collects the recent logs plus those
  captures and a `manifest.json` into one zip to attach to a report.

## BR-46 — 连播到下一条视频后按空格切走了标签页（维护者 2026-11-01，真机）

维护者：跳转到下一个播放视频之后再按空格想播放，结果激活了第一个标签页并切了过去；期望焦点留在当前视频上，空格应该播放。

源码里能对上号的几处（真机现象为准，这里只是定位线索）：

- Pivot 表头 `PivotHeaderItem` 自己处理 `Space` 并切页，键被控件吃掉，所以
  `Controls/PlayerControl.xaml.cs:488` 的 `PlayerControl_KeyDown`（`VirtualKey.Space` 分支在 `:534`）
  根本没被调用；
- `PlayerControl` 里有一个"每 2 秒把焦点收回播放器"的 `Timer_focus_Tick`
  (`PlayerControl.xaml.cs:355-362`)，但它的判断只认 `Button`/`AppBarButton`/`HyperlinkButton`/
  `MenuFlyoutItem`，焦点落在页签表头上时不会纠正；
- 页面自己的 `CoreWindow_KeyDown`（`Pages/VideoDetailPage.xaml.cs:79`）同样只处理没被控件吃掉的键。

需要的修复：连播/下一个把当前视频换掉之后，把焦点还给播放器；只在焦点落在页签表头（而不是目录抽屉里的
行、也不是输入框）时纠正，不要靠"定时抢焦点"去覆盖页签的键盘可达性。
