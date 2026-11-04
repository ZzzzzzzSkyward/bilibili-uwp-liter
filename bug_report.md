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
## BR-47 — 热门页卡片里推荐理由标签把标题和元信息顶出高度预算（维护者报告，源码定位）

维护者：热门页的视频卡片文字溢出/被裁，标题和下面的两行信息看起来挤在一起，带推荐理由的条目最明显。
这里不做真机复现，只记录源码里对得上的高度预算。

现状（`Pages/Home/HotPage.xaml`，`ItemHeight="100"` `DesiredWidth="560"`）：

- 插槽 100 高，但 `GridViewItem` 的样式给每项 `Margin="8"`，内容区只剩 `100 - 8 - 8 = 84`；
- 模板右侧是一个不分行的 `StackPanel`：标题 `FontSize="20" MaxLines="2"`，约 2×24 = 48；
- 推荐理由 `Border` + `FontSize="12"` 的 `TextBlock`，`TextWrapping="Wrap"` 且没有行数上限，
  算上边框和内边距至少一行约 18–20，理由长一点就是两行；
- `right_desc_1` / `right_desc_2` 各一行，约 2×18 = 36；
- 三处 `Margin="0 2 0 0"` 的间距共 6。

按这份预算，需要的文字高度 ≈ `48 + 18 + 36 + 6 = 108`，而内容区只有 84，差 24 以上；
`MaxLines="2"` 只约束标题，`TextTrimming` 对开了换行的理由标签不会把它压成一行。
`StackPanel` 不会让 `ItemHeight` 变大，超出部分由容器裁掉或互相压住。

同一模板还有一个空标签问题：理由 `Border` 的 `Visibility` 绑的是 `rcmd_reason_style` 这个**对象**，
只有内部 `TextBlock` 绑的是文本；对象非空而 `text` 为空/空串时，仍然会画出一个带边框的空胶囊。

修复形状（只改热门页的视频项模板，不动排行榜、推荐页和动态模板）：

- 推荐理由移到封面左上角，`MaxLines="1"` + `NoWrap` + `CharacterEllipsis`，`MaxWidth` 取封面宽减去两侧
  4 的留白（160 − 8 = 152），整个胶囊（含边框）按 `rcmd_reason_style.text` 的字符串可见性收缩；
- 封面右下角原有的 `cover_right_text_1` 胶囊保留，同样改成一行省略号，文本为空时整块收起；
- 标题与两行元信息进入独立的文本列/行：标题 `FontSize="16" FontWeight="SemiBold" MaxLines="2"`
  并按两行预留 `MinHeight="44"`，两行元信息 `FontSize="13" MaxLines="1" NoWrap`，每行预留 18；
- 尺寸从新预算算出来：宽卡 = 6 + 12 + max(封面 90, 文本 88) + 12 + 6 = 132，
  其中文本 88 = 44 + 6 + 18 + 2 + 18；`ItemHeight` 由 100 改为 132，`DesiredWidth` 仍为 560
  （560 的插槽扣掉外层 6×2 和内边距 12×2 后，文本列宽 = 560 − 12 − 24 − 160 − 12 = 352）；
- 容器 `Margin` 归一为 0，6 的外边距移进模板根节点，避免同一段留白算两次；
- 窄窗口回退：`VisualStateGroup` + `AdaptiveTrigger` 在窗口宽小于 480 时切到竖排模板，
  封面 160 高、文本在下方，插槽 6 + 12 + 160 + 12 + 88 + 12 + 6 = 296，`DesiredWidth` 320。

未验证/未覆盖的部分：以上全是静态读源码与算术，真机的裁切、遮挡、竖排堆叠都没有验收；
系统文字大小 150%/200% 的放大没有对应的静态触发器（`AdaptiveTrigger` 只看窗口宽度），
放大后仍可能顶到插槽边界，需要在真机上确认，必要时再引入按实际文本度量的高度。

## BR-48 — 热门页卡片在修好裁切后变得太松，需要恢复紧凑排版（维护者 2026-11-04，带截图）

维护者：热门页的视频卡片排版太宽松，需要恢复原本的紧凑布局。截图里三列布局的每行之间留白明显变大，
卡片内容四周也多出一圈内缩。

来源是 BR-47 的修复：那一轮把插槽从 100 抬到 132，模板根节点加了 `Margin="6" Padding="12"`，
容器 `Margin` 归一为 0，标题从 20 降到 16 并用 `MinHeight="44"` 预留两行。裁切确实修好了，
但每张卡片的垂直装饰从原来的 16 变成 36，标题也变小了，视觉上就是「松」和「小」。

这次的约束是同时满足两件事：

- 密度回到原来看感：容器 `Margin="4"`，模板根节点不再自带外边距和内边距，封面自己保留 4 的留白；
  `ItemHeight` 由预算算出，不再拍数字；标题回到 `FontSize="20" MaxLines="2"`。
- BR-47 的结论不能被推翻：推荐理由仍固定在封面左上角、一行省略号、按文本可见性收缩；
  `cover_right_text_1` 同样一行省略；**内容必须放得进插槽**——这一条是 BR-47 的全部意义，
  所以高度预算要写在文件头注释里，而不是靠 `MinHeight` 撑。

窄窗口（<480）的竖排模板同步重算插槽高度和 `DesiredWidth`，不能宽卡缩小而竖排留在 296。

验收仍是真机：裁切、两行标题的完整性、行间距，以及 150%/200% 系统文字大小下的表现
（`AdaptiveTrigger` 只看窗口宽度，看不到文字大小，这一条和 BR-47 一样需要在设备上确认）。

## BR-49 — 动态卡片底部有一条横贯卡片的白线（维护者 2026-11-04，带截图）

维护者：动态页面有底部白色线条请去除。

来源是卡片外壳自己的分隔线：`Controls/Dynamic/DynamicItemControl.xaml` 的 `CardSurface` 写了
`BorderThickness="0,0,0,1"` 配 `ForegroundBorderColor`（浅色 `#dddddd`，深色 `#111111`）。
卡片本来就有 `CardRoot.Margin` 底部 12 的间距，这条线只是重复表达“分隔”，在深色背景上尤其显眼。

处理：去掉边框与画刷，圆角、内边距和 `CardSurface` 名字保留（`DynamicItemControl.xaml.cs` 还在设置它的 Padding）；
文件头两处描述这条线的注释同步改成现在的样子。转发原动态内部的顶部分隔线与模板里的引用块 1px 边框不动。

## BR-50 — 动态页的直播卡片显示占位符「直播有待修复」（维护者 2026-11-04）

维护者：以前尝试修过动态页面的直播卡片，没成功，现在仍是占位符。

`Controls/Dynamic/DynamicParse.cs` 的现代路径把字段名当成了文案：副标题写成
`"【直播有待修复】parent_area_name" + " · 人气:" + item.viewtext` 和 `"【直播有待修复】area_v2_name"`，
身份取 `item.aid`，链接拼成 `https://b23.tv/live<aid>`。解析层其实早就备好了数据：
web 与 gRPC 两个映射器都往 `major.live` 填了房间号、封面、标题、分区名、人气文案和跳转地址。

链接不只是难看：单行卡把 `OneRowInfo.Url` 交给 `MessageCenter.HandleUrl`，它按 `live(\d+)` 把数字当房间号
送进 `LiveDetailPage`，于是 aid 会打开错误或不存在的直播间。

处理：新增 `Parsing/Dynamic/LiveCardSummary.cs` 统一决策——房间号只在纯数字时采用；目标地址只接受本身
能被识别成直播间的那种，否则用 `https://live.bilibili.com/{房间号}`；分区名与人气文案用 ` · ` 连接并跳过空项；
直播状态三态（1 直播中、0 已结束、其余或缺失为未知），只有确认结束才显示「直播已结束」。
`live_state` 因此改成 `int?`：`live_rcmd` 分支从来没设置过它，而 web 映射器对缺失的键写 0，会被读成“已结束”。
两条旧路径只把目标地址统一到 `live.bilibili.com/{房间号}`，字段读取不动。

## BR-51 — 顶部标签的选中下划线只有 16 像素宽，不与标签等宽（维护者 2026-11-04，带截图）

维护者：顶部标签的下划线需要加长到与标签等宽，并说明这是**全局**的顶部标签，不是动态页自己的页签。

一开始按 `TabView` 找错了地方：WinUI 2.8.7 的 TabView 模板里根本没有下划线部件，选中态是标签底下的
背景路径。真正的那条 3 像素高、16 像素宽的线来自首页顶部的 `winui:NavigationView PaneDisplayMode="Top"`
（推荐/热门/番剧…），它的项模板里 `SelectionIndicator` 写死了 `Width="16"`、外层网格 `HorizontalAlignment="Center"`
并带 `Margin="16,0,16,4"`。

处理：`App.xaml` 里加一份只改这几处的 presenter 模板（指示条改成横向拉伸、左右留 4），高度 3、圆角 2、
主题画刷与全部视觉状态保持原样，再用一份 `NavigationViewItem` 样式把顶部模式的 presenter 指过去。
全应用只有一个 `NavigationView`，所以没有波及其它页面。

## BR-52 — 动态页卡片之间的垂直间距约 48 像素，太松散（维护者 2026-11-04，带截图）

维护者：这个版本还有一个太松散问题，需要减小垂直间距。

两张卡片之间是四段留白相加：卡片下内边距 16 + `CardRoot` 下边距 12 + 列表项 margin 4 + 下一张卡片的上内边距 16 = 48。

处理：纵向节奏改成 8 + 4 + 2 + 2 + 8 = 24 —— 卡片内边距 `16,8`、卡片外边距 `12,0,12,4`、列表项 margin `4,2`、
区块间距 8；横向数值、800 的阅读列宽和列表 `Padding="4"` 不变，窄窗口的 `ApplyWidthState` 与文件头的令牌表同步。
