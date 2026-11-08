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

## Notes

- Install a `BiliLite.Packages_*` MSIX (root launcher ≈ 8 KB plus `entrypoint/BiliLiter.exe`). A
  `BiliLite.UWP_*` package whose root `BiliLiter.exe` is ~4.2 MB cannot start this app.
- 诊断 → 「记录 API 响应形状」 writes one NDJSON line per response (endpoint, status, timings, shape, and a
  drift line the first time a field changes type); 「导出诊断包」 collects the recent logs plus those
  captures and a `manifest.json` into one zip to attach to a report.

## BR-53 — 打开任意直播间都报 Object reference not set to an instance（维护者 2026-11-06）

维护者：打开任意直播都会出现 `Object reference not set to an instance`。报告没有任何附加条件——不是某个房间、
某个分区或某个入口，所以按“直播间打开路径上的无条件步骤”处理，而不是按数据差异处理。

现象文字有两处来源，都只说了一半：

- `Modules/Detail/LiveRoomVM.cs:642-648` 的 `LoadLiveRoomDetail` 用 `catch (Exception ex)` 把整段加载包住，最后
  `Utils.ShowMessageToast(ex.Message)` 只弹 `ex.Message`；英文区域下这正是
  `Object reference not set to an instance of an object.`，堆栈不进提示，只进日志（`HandelError`）。
- `App.xaml.cs:158-168` 的异步异常通道会弹「程序出现一个错误，已记录」并把 `e.Exception.ToString()` 作为第二行，
  那里会带 `System.NullReferenceException`。报告里没有这句前缀，更接近第一种。

因此目前能确定的是：`NullReferenceException` 发生在 `LoadLiveRoomDetail` 的
`await` 链里，或紧随其后的 `OnNavigatedTo` 标题段。

已核对的无条件步骤（都在 `LoadLiveRoomDetail` 成功分支里，任何房间都会走）：

- `RoomID/Online/Liveing/LiveInfo` 来自 `data.data.room_info`，缺字段会先在取值处炸，但接口成功时才走；
- `LoadAnchorProfile()`、`GetRoomGiftList()`、`LoadBag()`、`LoadWalletInfo()`、`GetTitles()`、`EntryRoom()`、
  `ReceiveMessage(room_id)` 是 `Liveing` 之外的固定步骤，其中只要有一个读了空模型就会落到同一个 toast；
- `OnNavigatedTo` 的标题段（`Pages/LiveDetailPage.xaml.cs:455-479`）已有 try/catch，弹出的是
  「直播间标题获取失败」，与报告文字不符，但同一处的 `liveRoomVM.LiveInfo.anchor_info.base_info.uname`
  没有 `?.` 保护，若 `LiveInfo` 为空会先抛再被这条 catch 接住。

处理（2026-11-06）：这条路径上所有 `GetData<T>()` 的结果都当非空用——`GetData` 解析失败时返回 null，
于是 `if (data.success)` 本身就炸成一句看不出位置的 `NullReferenceException`，而外层的 `catch` 只弹
`ex.Message`。已改三件事：

1. `LoadLiveRoomDetail` 引入 `stage`，在进入主播资料/播放地址/醒目留言/抽奖/礼物/背包/钱包/头衔/进房/连接
   各步前更新它，`catch` 里连同 `stage` 与房间号一起写 ERROR 日志，toast 文案不变。
2. 新增 `HasParsedData(data, step)`：`data` 为 null 时记一条带步骤名的日志并跳过该步，不再在
   `data.success` 上炸；主信息那一步另外挡 `data.data?.room_info` 为空。
3. 具体空引用一并修掉：背包里存在当前房间礼物配置里没有的礼物时 `_gift.ObjectClone()` 会炸（改为跳过）、
   多个 `data.data["list"]` 在缺键或 data 为空时炸（改为空列表）。

仍缺的一环：还不知道是哪一个接口的响应解析失败——`HasParsedData` 现在会直接把它写进日志。下次出现时
日志里会有 `直播接口返回的数据解析失败 step=…` 或 `直播详情加载失败 stage=…`，把那一行发来即可定位；
本地用真实载荷验过 `getInfoByRoom` 能正常反序列化（`code=0`，`room_info`/`anchor_info` 都在），所以
不是这个接口的形状问题。

处理（2026-11-08，按日志定位）：维护者给的设备日志把这条钉死了。

- `NullReferenceException` 的堆栈是 `at BiliLite.Modules.LiveRoomVM.LoadSuperChat()`，紧跟
  `GET av/v1/SuperChat/getMessageList status=200`，而这条请求没有 `[JSON][FAIL]` —— 解析成功，炸在循环体里：
  `item.user_info.face`（及 `face_frame`/`uname`）没有保护，条目缺 `user_info` 就是一句裸的
  `Object reference not set to an instance of an object.`，正好是 `LoadSuperChat` 自己的 catch 弹出来的文字。
- 同一时刻 `gift/v4/Live/giftConfig` 两次 200 都报
  `Deserialized JSON type 'JArray' is not compatible with expected type 'JObject'. Path 'data'`，
  分别对应 `step=gift-config`（`GetRoomGiftList`）与 `step=bag-gifts`（`LoadBag`）—— 接口的 `data` 已经是数组，
  按 `JObject` 反序列化必然失败，礼物/背包整块不加载。

修复（`c0b7eb1`）：

- 醒目留言：先按「`data` 是数组」或「`data.list`」拆出列表（都认不出就空列表），跳过 `user_info` 为空的条目，
  其余 14 个字段的映射不变。
- 礼物配置：两个 `GiftList` 调用点改为 `GetData<JToken>()` + `ParseGiftList`（数组 / `{"list":[...]}` / null
  统一归一成列表），拼 `Gifts` 时不再放入 null 元素；`bag_list` 与 `gift-room` 的响应形状没有证据表明变化，不动。
- 提示与日志文案保持原样（`step=` 名称不变），没有新增日志。

验收：打开任意直播间 —— 不再弹 `Object reference not set ...`；日志里 `gift/v4/Live/giftConfig` 不再出现
`[JSON][FAIL]`，`step=gift-config`/`step=bag-gifts` 也不再出现；礼物面板按房间礼物配置列出，背包里配置之外的
礼物仍然跳过。

验证（2026-11-08 13:02，设备日志会话 `5399351e6fb34f128b941ea828078b49`，`187fc48` 构建）：通过。

- `gift/v4/Live/giftConfig` 两次都是 200（`http-000042` 155 ms、`http-000045` 10 ms，对应 `LoadBag` 与
  `GetRoomGiftList`），**没有** `[JSON][FAIL]`，也没有 `直播接口返回的数据解析失败 step=gift-config/bag-gifts`。
- 背包那一步走到了 `xlive/app-room/v1/gift/bag_list` 200（`http-000046`）——这行只在礼物配置解析成功后才可能出现。
- 整个会话 167 行里没有 `NullReferenceException`、没有 `LoadSuperChat`、没有「Object reference not set」的 toast；
  钱包（`pay/v2/Pay/myWallet`）、头衔（`rc/v1/Title/getTitle`）、进房（`room_entry_action`）依次 200。
- `api/baseline-https___api_live_bilibili_com_gift_v4_Live_giftConfig.json` 记的形状是 `$.data: "array"`，
  与根因一致；同一目录里 `SuperChat/getMessageList` 的 `$.data.list` 本次是 `null`（房间没有醒目留言），
  所以「缺 `user_info` 的条目」这条保护这次没有被触发——是没有覆盖到，不是失败。
- 会话末尾的 `直播接收包出错 … WebSocketException … without completing the close handshake`（`LiveMessage.Connect`）
  与 12:27 那次相同，是既有噪声，和本条无关。



## BR-54 — 播放器浮层的胶囊太宽，内容块相对背景偏低（维护者 2026-11-06，带截图）

维护者：进度浮层（`00:00:43` + 时钟图标）的胶囊左右各挂了一大截背景，宽度需要收窄到截图红框；另外
「图标和文本都需要移动，图标和文本已经对齐，但和背景不对齐」——即两块彼此的墨迹已在一条线上，
偏低的是整块内容相对胶囊背景。

这与 BR-43 第二轮的前提不同：那一轮把两块的行框钉成同一高度后，偏差出现在两块之间；这一轮两块之间没有问题，
所以不能再用文字单独位移去补，否则会把已经对齐的两块重新拆开。

处理：

- 横向内边距 `BasePaddingX` 16 → 6。胶囊宽度只由内容 + 横向内边距决定，纵向 `BasePaddingY`（8）与
  `ToastRoot.MinHeight`（内容 22 + 上下 16 = 38）都没动，所以只是收窄，没有压扁。
- 新增 `BaseContentOpticalOffset = -1`，通过 `StackPanel` 上的 `ContentOffset` 平移整块内容（图标 + 文字一起），
  随同一个 `scale` 缩放；`BaseIconOpticalOffset` 仍为 0，两块之间的对齐规则不变。
- `plan/tests/toastalign` 从 36 项扩到 41 项：两个位移载体必须分别存在且各只有一个来源、内容常量必须为负、
  横向内边距必须小于纵向，且内边距仍左右/上下对称、仍随 scale 缩放。六项新断言的其中四项在改动前的源码上直接
  失败（`FAIL —— 6 / 41`），说明它们钉的是这次的行为而不是复述现状。

- 已修：`01e51a7`（`toastalign` 41 项，其中 6 项在改动前的源码上失败）。

设备上若仍差一点：宽度只改 `BasePaddingX`，整块上下只改 `BaseContentOpticalOffset`，不要动行框。

## BR-55 — 预约卡只显示普通纯文本，没有样式（维护者 2026-11-06，1.0.9.51）

维护者：预约视频卡片只显示普通纯文本，期望有一定的样式设计、显得更美观。

源码：`Controls/Dynamic/DynamicTemplate.xaml` 的 `Reserve` 模板只有两块裸文字——`OneRowInfo.Title` 与
`OneRowInfo.Desc`，外面一圈 1px 边框；载荷里的按钮文案（`reserve.button.text`）和按钮图标
（`reserve.button.icon_url`）虽然已经映射进 `OneRowInfo.CoverText/Cover`，但对应的 `HyperlinkButton` 被整段注释掉，
注释写着"实际上根本没有办法点击这个按钮"。

处理：改成一块带边框的预约面板——圆角 8、内边距 12、1px `ForegroundBorderColor`；顶部粉色「预约」角标（12px，
与热门页推荐理由标签同一套语言）；标题 18/26 SemiBold 两行省略，说明 14/21 两行省略；底部按钮用载荷给的文案与
图标（图标经 `imageConvert2` 转 `ImageSource`），点击走 `LaunchUrlCommand` + `OneRowInfo.Url`，没有按钮文案时不画按钮。
作者正文仍由 `RichTextControl` 显示在面板之上。

`plan/tests/g5` 新增 7 项钉住这个版式（面板、角标、跳转、按载荷显隐、图标转换器、注释按钮已删除）。

## BR-56 — 图文动态里的直播预约附件仍是两行纯文本（维护者 2026-11-07，带截图）

维护者：预约卡片仍然需要改进。截图是一张图文动态（作者「堇梦D」，正文 + 一张长图），图片下方只有两行：
`预约 直播预约：10月新番OP/ED鉴赏会！` 与 `11-13 20:00 直播 175人预约`，没有面板、角标和按钮。

源码对得上：

- 这条动态的预约在 `modules.module_dynamic.additional.reserve`（直播预约附件），不是独立的
  `DynamicDisplayType.Reserve`。`Controls/Dynamic/DisplayModel.cs` 的 `DynamicAttachmentSummaries.From`
  把它摊成 `DynamicAttachmentSummary`，`Controls/Dynamic/DynamicItemControl.xaml` 的 `AttachmentRow`
  用同一套「安静一行」模板渲染所有附件种类：`KindLabel` + `Title` 同行、`Desc` 一行、可选「打开」。
- `09ee480`（BR-55）只改了 `Controls/Dynamic/DynamicTemplate.xaml` 的 `Reserve` 模板；那个模板只有独立预约动态
  会选中，附件路径不经过它。
- 附件摘要只带 `Title/Desc/Destination`，没有 `reserve.button` 的文案与图标；目的地取的是 `reserve.jump_url`，
  而直播预约的跳转通常在 `reserve.button.jump_url`，所以截图里连「打开」都没有。

处理方向：`Kind == Reserve` 的附件改用 BR-55 的面板语言（圆角 8、内边距 12、1px 边框、粉色「预约」角标、
18/26 标题两行、14/21 说明两行），其它附件种类保持现有安静行；按钮文案/图标取 `reserve.button.text` /
`reserve.button.icon_url`（回退 `icon`），目的地优先 `reserve.button.jump_url`，没有文案或没有可用路由时不画按钮。
作者、时间、正文与图片的既有布局不动。

`plan/tests/g5` 需要钉住：预约附件渲染成面板、投票/链接仍是行、按钮按载荷与路由显隐、图标走 `imageConvert2`；
反向断言在改回旧映射时失败。

## BR-57 — 预约文案要按【时间开播】【人数已预约】排，附件不要打印 wire 类型名（维护者 2026-11-07）

维护者两条要求：

1. 预约卡的说明行改成两个标签（标题行保持 `reserve.title` 不变）：最终形态 `11-13 20:00 开播 175人已预约`
   ——标签不包【】，之间一个空格（维护者后续修正，见 BR-58）。
   时间取 `desc1`（把「直播」改为「开播」），人数取 `reserve_total` / `desc2`
   （「预约」改为「已预约」）；`desc3` 抽奖文案非空时追加第三个标签。
2. 有的卡片会显示 `ADDITIONAL_TYPE_GOODS` 这一行原始类型名。选中方案 A：`GOODS` 显示「商品」、`UGC` 显示
   「合集」、其它未知种类显示「附件」，任何用户可见文案都不再出现 wire 类型名；标题/说明/目的地全空的附件行
   整行不渲染。

源码：`DynamicAttachmentSummaries.From` 把 `desc1/desc2/desc3` 用 `JoinText` 拼成一句写进 `Desc`；
`DynamicAttachmentDisplay.KindLabelOf` 对未知种类直接返回 `RawType`；`AppendUnsupported` 无条件加行。
独立预约卡（`DynamicParse` 的 `Reserve` 分支）同样写 `reserve.desc`，两处要共用同一份文案规则。

处理：新增 `Parsing/Dynamic/ReserveCardCopy.cs`（纯函数，输入 `reserve`，输出标签行），附件摘要与独立预约卡都用它；
`KindLabelOf` 按上表映射，未知种类不再回退 `RawType`；`From` 末尾丢掉无标题、无说明、无目的地的 Unsupported 行。
`plan/tests/g5` 钉住标签格式、时间/人数改写、desc3 追加、无结构化字段时的回退，以及 `KindLabel` 不含
`ADDITIONAL_TYPE_`；旧断言 `Desc == "明天 20:00 1.2万人预约"` 同步改成新文案。

- 已修：`93c69de`（标签映射与空行不渲染）、`65ca559`（去括号，`g5` 265 → 316；同批最终 332）。`ReserveCardCopy` 对
  `直播`/`预约` 只改结尾词，`直播中`/`预约中` 原样保留；`商品`/`合集`/`附件` 三个标签与空行不渲染都有锚点。

## BR-58 — 卡片长标题在设备上没有 tooltip（维护者 2026-11-07，1.0.9.57/.58）

维护者：卡片长标题仍然没有 tooltip。

现状：`09ee480`（BR-55）已经在会被截断的标题上写了 `ToolTipService.ToolTip`——`DynamicItemControl` 的
`CardTitle`、预约面板标题、附件行标题，`DynamicTemplate` 的视频/专栏/预约标题，热门页卡片标题——文本与提示绑定同一份
数据，所以不是绑定漏了；设备上没有出现悬浮提示。`ToolTipService` 只在指针悬停时自动打开，触摸/手柄没有 hover。

处理（只依赖用户能做的动作）：新增附加行为 `Helpers/TapToolTip.cs`，保留原有悬浮路径；`Tapped` 打开，
鼠标 `PointerExited`、`Unloaded` 与 4 秒超时关闭（触摸抬手的 `PointerExited` 会立刻关掉刚打开的提示，忽略）。
每次点击都重新读 `ToolTipService.ToolTip` 的当前值写进点击专用 `ToolTip`，不回写该附加属性——列表回收复用时
绑定还要继续更新，否则会显示上一条的标题。鼠标、触摸、笔都能看到全文。热门页整卡点击会跳转，不挂这个行为。

同批修正 BR-57 的文案：标签不再包【】，标签之间用一个空格分隔（`11-13 20:00 开播 175人已预约`）。

- 已修：`65ca559`（`g5` 316 → 同批最终 332，五个标题元素都有锚点；去掉空格拼接或 `CardTitle` 的附加属性会分别失败）。
  打包 1.0.9.59：`Z:\BiliLite.Packages_1.0.9.59_x64_Test\BiliLite.Packages_1.0.9.59_x64.msix`。

## BR-59 — 推荐卡片的长标题没有 tooltip（维护者 2026-11-07，1.0.9.59）

维护者：推荐卡片仍然不显示 tooltip，其他地方都显示。

源码：`Pages/Home/RecommendPage.xaml` 两个卡片模板的标题都是裸 `TextBlock`——网格卡（`MaxLines="2"`）与
列表卡（`MaxLines="3"`），都没有 `ToolTipService.ToolTip`。动态卡与热门页的标题都有，所以只有推荐页缺。

处理：两个模板各加 `ToolTipService.ToolTip="{x:Bind title}"`，悬停显示全文。推荐卡整张是 ItemClick 跳转，
不加"点击打开 tooltip"，否则点一下既弹提示又跳页。

- 已修：`ae90881`（`g5` 332 → 335，两个模板的 `ToolTipService.ToolTip="{x:Bind title}"` 都有锚点）。

## BR-60 — 关闭应用时正在播放的视频不记进度（维护者 2026-11-07，1.0.9.59）

维护者：记录播放进度的功能失效了。

证据（本机 `LocalState\log\20261107.log`）：
- 15:34:11 打开 870.834s 视频（session `8e7530d4…`）；15:42:00 换成新 session（应用重启）。这 8 分钟里没有任何
  `POST /x/v2/history/report`，本地续播位置也没机会写。
- 全天只有一次 report：15:42:49，49s 视频中途离开页面（走 `Dispose`）。
- 15:49:48 与 16:49:51 又两次打开同一 870.834s 视频，是维护者在反复验证。

源码：进度只在 `PlayerControl.Dispose()`（离开页面）和 `Player_PlayMediaEnded`（播完）上报；
关窗口/退应用不走 `OnNavigatedFrom`，`App.OnSuspending`/`OnExiting` 也没有播放器钩子，所以"看一半直接关应用"
既不上报历史、也不保存续播位置。

处理：播放中每 30s 做一次检查点（同步保存本地位置 + 上报历史），暂停时补存一次；`App.OnSuspending` 在现有
deferral 里 await 一次 flush（最后一次上报能发出去）；`OnExiting` 再同步保存一次位置。`Dispose`/播完的上报保留。检查点走静默上报（`ReportHistory(..., reportErrors: false)`），不会每 30s 弹一次失败提示。

- 已修：`ae90881`（`playersess` 161 → 203，`g5` 335）。`HistoryKey` 同时用于续播读取、`Dispose` 保存与检查点；
  `App.OnSuspending`/`OnExiting` 都在各自的 deferral 里 await flush。发布 1.0.9.61（tag `20261107`）：
  `Z:\BiliLite.Packages_1.0.9.61_x64_Test\BiliLite.Packages_1.0.9.61_x64.msix`。


## BR-61 — 最小化很久后点任务栏图标回不到最大化（维护者 2026-11-07，1.0.9.60）

维护者：窗口缩到任务栏很久以后，点任务栏图标回不到最大化；这个现象一直存在。

本机实测（PID 30616，1.0.9.60，session `4b4ff9b4…`）：

- 失败态：`ApplicationFrameWindow` 18025424 停在 `-32000,-32000`、`showCmd=2`、`flags=2`（语义是应恢复到最大化），
  CoreWindow 3084488 停在 `0,8` 且 DWM cloak=2，进程 61/61 线程 Suspended、`Responding=False`。
- 真实鼠标点可见的任务栏按钮（`哔哩哔哩 UWP`，1077,20）：前台一度变成该 AFW，但窗口仍是最小化，应用日志没有
  `Application resumed` —— 系统只前置了 frame，没有恢复它。MSAA 默认动作点同一个按钮结果相同。
- 同一时刻手动 `ShowWindow(afw, SW_RESTORE)`：窗口立刻回到 `40,72 1216x941`（正常尺寸，不是最大化），日志出现
  `Application resumed`，进程恢复 —— 说明 frame 本身能恢复。
- 对照：系统自带「设置」最小化 45 秒后点任务栏按钮，直接回到最大化（`-8,32 1936x1056`，`zoomed=True`），
  同一套顶置任务栏 + ApplicationFrameHost，说明不是任务栏位置或 frame host 的通用行为。

同类实现的写法（Richasy/Bili.Uwp）：`OnLaunched` 与 `OnActivated` 走同一个方法，结尾无条件 `Window.Current.Activate()`
并重新初始化标题栏；标题栏在控件 `Loaded` 后才 `SetTitleBar`。本仓库 `App.OnActivated` 只处理 Protocol，其它激活
不激活窗口；`OnSuspending` 里 await 一旦抛错，deferral 不会 Complete（系统按挂起失败处理）。

处理：

- `App.OnActivated` 的非 Protocol 分支补 `Window.Current.Activate()`；
- `App.OnAppResuming` 在窗口仍不可见时补一次 `Activate()`，并记一行日志；
- `App.OnSuspending` 的挂起工作加 try/catch，deferral 始终 Complete，并记录耗时与剩余预算；
- `MainPage`/`NoTabMainPage` 在 `Loaded` 后再设一次拖拽区（构造时元素可能还没进可视化树）。

- 待验证：需要下一次复现时看日志。若恢复确实卡在系统侧（没有 `Application resumed`），这些改动只能在应用被恢复
  之后补激活；届时日志里 `Main window is not visible after resume; activating` 是否出现是判断依据。

复现（2026-11-08 13:12，session `5399351e…`）：窗口最小化约 100 秒后点回前台

- `13:12:24.9409 Application resumed` —— 系统确实恢复了进程，**不是**卡在系统侧；
- 同一毫秒 `Main window is not visible after resume; activating`，之后既没有 `Activating the main window failed`，
  也没有 `Main window activated`；
- `13:12:34.9625 Application suspending` —— 10 秒后系统又把应用挂起，说明窗口始终没回到前台。也就是说
  `Window.Current.Activate()` 在挂起恢复路径上对最小化窗口是空操作，上一版的两处 `Activate()` 都无效。

与本机实测也能对上：AFW 当时是 `showCmd=2`、`flags=2`（`WPF_RESTORETOMAXIMIZED`），手动 `ShowWindow(afw, SW_RESTORE)`
能把窗口叫回来，但落在普通尺寸 1216×941 —— 只激活、或只 restore，都不等于「回到最大化」。

处理（`bbfaf7f`）：

- 新增 `Helpers/WindowRestoreHelper.cs`（已加进 `BiliLite.UWP.csproj`）：从 `CoreWindow` 的 `ICoreWindowInterop`
  取 HWND（`CoreWindow` 是 sealed，只能 `(ICoreWindowInterop)(object)` 走运行时 QI），再用 `GetAncestor(hwnd, GA_ROOT)`
  找到顶层框架窗口 —— 被系统最小化的是 `ApplicationFrameWindow`，不是 CoreWindow 子窗口 —— 然后用 `user32`：
  `IsIconic`/`IsZoomed` 读状态、`ShowWindow(SW_SHOWMAXIMIZED|SW_RESTORE)` + `SetForegroundWindow` 还原。取不到 HWND
  （非桌面设备）时全部安静返回。
- `OnSuspending` 记录窗口状态，并用 `GetWindowPlacement` 的 `WPF_RESTORETOMAXIMIZED` 判断「原本是否最大化」：
  最小化窗口的 `WS_MAXIMIZE` 已被清掉，这时 `IsZoomed` 是 false，只有这个标志能回答（正是本机实测的 `flags=2`）。
- `OnAppResuming` 与 `OnActivated` 共用 `RestoreMainWindowIfHiddenAsync()`：窗口不可见时先 `Window.Current.Activate()`，
  200 ms 后仍不可见就按挂起前状态走 Win32 还原并再激活一次，前后状态都写进日志。

验收：把窗口最大化 → 最小化放置 1~2 分钟 → 点任务栏图标，窗口应直接回到最大化。日志里应看到

- `Window state at suspend; hwnd=0x…; root=0x…; iconic=False; zoomed=False; restoreToMaximized=True`
  （`zoomed=False` 与 `restoreToMaximized=True` 同时出现，就是最小化窗口丢失 zoomed 样式的证据）；
- `Main window restored through Win32; visible=True; …; zoomed=True`。

若仍失败，`Main window state before restore` 与 `… restored through Win32` 两行会给出 hwnd / root / iconic / zoomed
的实际值，可直接判断是句柄不对（root 不是 AFW）还是还原命令被系统忽略。


## BR-62 — 暗色模式下评论浮窗仍是浅色色值（维护者 2026-11-08，1.0.9.62）

维护者截图：暗色模式里打开评论浮窗（`Dialogs/CommentDialog`，标题「热门评论」+ 刷新/关闭），面板还是浅色
磨砂、标题深色、正文近黑，与界面其余部分不一套值。

源码定位：

- 浮窗是 `Popup` 的 `Child`（`CommentDialog.xaml.cs` 构造里 `popup.Child = this`），自身没有 `RequestedTheme`；
  根 `Frame` 的 `ElementTheme` 只作用于它的子树，弹窗按 `Application.RequestedTheme`（启动时的系统主题）解析，
  于是深色界面里整窗都取到浅色字典的值。同仓库的其它浮层都显式钉了主题（`MessageToast` `Light`、
  `PlayDirectoryControl` `Dark`、`SendDanmakuDialog` `Dark`、播放器设置 `Pivot` `Dark`），只有这个浮窗没有。
- `Utils.GetBrush` 另算一套：只看 `Window.Current.Content.RequestedTheme`，非 `Light` 一律当 `Dark`；
  `Default`（跟随系统）在浅色系统上拿到深色值，`TransparentLight`(4) 也落到 `Dark`。同一个窗口里
  `ThemeResource` 与代码画刷因此可能来自两套值。
- `App.LoadTheme()` 在启动导航前把 `Application.Resources.ThemeDictionaries` 替换成 `Themes/DefaultTheme.xaml`，
  运行时调色板以该文件为准；`App.xaml` 里同名主题字典只在替换之前存在。

处理（`44d7f0e`）：

- `CommentDialog` 构造时用宿主 `Frame` 的 `ActualTheme` 写 `RequestedTheme`（`ActualTheme` 永不为 `Default`，
  五种设置一次覆盖），浮窗里的主题资源与 `MessageToast`/`PlayDirectoryControl` 同一套做法。
- `Utils.GetBrush` 改用 `Frame.ActualTheme` 取字典，与浮窗同源。
- 浮窗的中性色收成 5 个令牌，深浅两套同时写进 `Themes/DefaultTheme.xaml`（运行时来源）与 `App.xaml`：
  `CommentDialogLayer`（浅色仍是原磨砂；深色 `#1A1A1A`/`0.9`、兜底 `#F21A1A1A`，与播放目录抽屉同面）、
  `CommentDialogBorderColor` `#31808080`/`#31FFFFFF`、`CommentDividerColor` `#33808080`/`#33FFFFFF`、
  `CommentBandColor` `#19AAAAAA`/`#19FFFFFF`、`CommentBadgeColor` `#3C808080`/`#3CFFFFFF`
  —— 深色同 alpha、基色换白。
- 标题两枚排序链接改用 `HighLightTextColor`（浅色 `#d0318c`、深色 `#fff47bcf`），不再跟系统强调色走。

验收：浅色、深色各开一次评论浮窗 —— 深色面板为暗面、正文浅色，分隔线/回复带/UP 徽标可见，昵称与点赞数
和 XAML 同属一套主题；浅色除标题改用 `HighLightTextColor` 外与改动前逐值相同。按维护者 2026-11-08 的指示，
本轮不做设备/运行测试，编译通过后由维护者反馈。

验证（2026-11-08，设备）：通过。维护者确认深色/浅色评论浮窗都取到与界面同一套色值。

## BR-63 — 直播间弹幕断开一次就再也不连（维护者 2026-11-08）

来源是 BR-53 的两份设备日志（2026-11-08 12:27 与 13:02），两次会话里各出现一条：

- `直播接收包出错 | HResult=0x80004005|System.Net.WebSockets.WebSocketException (0xB55B6598): The remote party
  closed the WebSocket connection without completing the close handshake. ---> System.Exception: Exception from
  HRESULT: 0x80072EFE`，紧跟 `at Windows.Networking.Sockets.MessageWebSocketMessageReceivedEventArgs.GetDataReader()`
  与 `at BiliLite.Modules.Live.LiveMessage.Connect(...)`。

这条日志之后接收循环的 `while (... && ws.State == WebSocketState.Open)` 条件为假，`Connect` 直接返回；而
`ReceiveMessage` 只在 `LoadLiveRoomDetail` 的 connect 阶段调用一次，不会再被调用。结果是弹幕、礼物、醒目留言
在本次停留期间全部停住，界面上没有任何提示，只能退出重进。

根因两处：

1. `Connect` 是「连一次、收到死为止」：异常只记一条日志，循环条件依赖 socket 状态，断线即永久停止；而且抛过
   异常的 `ClientWebSocket` 不能复用，重连必须换新实例。
2. 心跳是 `System.Timers.Timer` → `async void HeartBeatTimer_Elapsed` → `ws.SendAsync`：socket 在发送瞬间死亡时
   抛出的异常会从 `async void` 逃逸，UWP 里这会直接终止应用。

处理（`722555b`）：

- `Connect` 改为监督式循环：连接 → 进房 → 心跳 → 起 30 s 心跳定时器 → 收包；收包在取消、服务端 close 帧
  （close 帧不再交给 `ParseData`）或异常时退出内层，仍记原来那条 `直播接收包出错`（文案与级别不变），丢弃旧
  socket、换新 `ClientWebSocket`，按 2 s → 4 s → 8 s → 10 s 封顶退避后重连；连上后把退避重置回 2 s。
- 每次连接前先 `StopHeartBeatTimer()`，定时器不叠加；`SendHeartBeatAsync` 吞掉 `OperationCanceledException` /
  `ObjectDisposedException` / `WebSocketException`，`async void` 不再可能把发送异常带出去。
- `Dispose` 先置标志，再停定时器、释放 socket；重连循环在循环条件、异常后、退避前后都看这个标志，离开页面后
  不会再有重连，也不会在 `Dispose` 之后新建 socket。
- 同一实例只允许一条连接循环（`running` 标志），退避窗口内的重复调用直接返回，避免两条循环抢同一个 `ws`。

验收：进一个弹幕密集的直播间，人为断一次网（锁屏一分钟或关开 Wi-Fi）——日志出现 `直播接收包出错` 后，弹幕列表
应重新出现「弹幕连接成功」并继续滚动，不需要退出重进；连续断线时退避 2→4→8→10 s；离开页面后日志不再出现新的
`直播接收包出错`；期间应用不闪退。

## BR-64 — 打开番剧页面报 Value cannot be null（维护者 2026-11-08，20:38 日志）

现象：进任意番剧页面就抛异常，页面初始化中断——

- `20:38:06 System.ArgumentNullException: Value cannot be null. Parameter name: input`
  `at System.Text.RegularExpressions.Regex.IsMatch(String input)`
  `at BiliLite.Helpers.Utils.ParseArea(String title, Int64 mid)`
  `at BiliLite.Pages.SeasonDetailPage.UpdatePlayInfoToEpisode(Int32 index)` ← `InitializePlayInfo()` ←
  `InitSeasonDetail()` ← `OnNavigatedTo`；
- 前后两次 `pgc/view/web/season` 与 `pgc/view/web/ep/list` 都是 200，没有 `[JSON][FAIL]`，所以不是网络或反序列化失败。

定位：今天实测 `pgc/view/web/ep/list` 的 `result` 只有 `episodes`、`section`、`play_strategy` 三个键（season 29325 /
28747 / 33378 三份一致），没有 `title`、`season_title`、`media_id`、`rating`、`areas`、`stat`、`user_status`。而
`SeasonDetailVM.LoadSeasonDetail` 恰好拿这份 result 造整页 `Detail`，于是 `Detail.title == null`，
`Utils.ParseArea` 里的 `Regex.IsMatch(title, …)` 直接抛 `ArgumentNullException`。同一原因下：

- `SeasonDetailPage.xaml` 里 20 个 `Detail.*` 绑定（标题/封面/简介/评分/发布/统计/地区/关联季/付费/追番状态…）全空；
- `PlayInfo.season_id` 恒为 0，`seasonReviewVM.MediaID = Detail.media_id` 恒为 0（短评按 media 0 去查）；
- 港澳台分区的判定依赖 `Detail.title` 与 `up_info.mid`，标题空时连「僅…地區」也判不出来。

上游同源项目（`ywmoyue/biliuwp-lite`，`ViewModels/Season/SeasonDetailPageViewModel.cs`）是两条请求并行：整页信息取
`pgc/view/web/season`，片单取 `pgc/view/web/ep/list`，`Detail` 只来自前者。本仓库的 `Api/SeasonApi` 只有 `EpList`，
迁移时把两件事并成了一条，才落到现在这个状态。

处理（`b643e77`）：

- `Api/SeasonApi` 新增 `Detail(id, type)`：`{API_BASE_URL}/pgc/view/web/season`，`season_id=` / `ep_id=` 与 `EpList` 同一套；
- `SeasonDetailVM.LoadSeasonDetail` 先取 season 端点（`GetResult<SeasonDetailModel>()`，`result` 信封），
  `Detail = detail ?? season.Detail`；season 解析失败只记一条 `番剧详情解析失败 ep=… endpoint=pgc/view/web/season`，
  不挡播放；`Detail.title` 为空时回落 `season_title`，页面标题/地区判定都有字；
- `Detail.episodes` 仍用 ep/list 的合并列表（含分区预告、按 ep 号去重），下载面板的条目与去重行为不变；只有当 ep/list
  一条都没有时才用 season 自带的那份；
- `Utils.ParseArea` 标题为空时跳过两条 `僅…地區` 正则，其余判定（`mid == 11783021 → hk`、默认 `cn`）不变。

验收：进番剧页面不再出现 `Value cannot be null`；标题、封面、简介、评分、地区、追番状态有内容；短评按正确的
`media_id` 拉取；点下载能看到正片 + 预告；港澳台番剧的 `area` 仍是 `hk`/`tw`（标题含「僅…地區」时）。若整页信息拿不到，
日志应有 `番剧详情解析失败 ep=… endpoint=pgc/view/web/season`，而片单和播放照常。

## BR-65 — 番剧页面不报错但什么都不显示（维护者 2026-11-08，21:57 日志）

现象：BR-64 之后进番剧页面不再抛异常，但页面是空的——标题、封面、简介、剧集列、评论全没有。日志里三次加载都是同一条：

```
21:57:15.5943 ERROR 番剧详情解析失败 ep= endpoint=pgc/view/web/season
21:57:29.5008 ERROR 番剧详情解析失败 ep= endpoint=pgc/view/web/season
21:57:37.8125 ERROR 番剧详情解析失败 ep= endpoint=pgc/view/web/season
```

`ep=` 后面是空的，说明请求根本没带 id：`OnNavigatedTo` 只在参数是 `object[] { seasonId, epId }` 时才填 `ep_id`，
而应用内所有入口（首页番剧、影视、时间表、排行、搜索、追番、历史、换季）都只传 `season_id`。于是
`seasonApi.Detail(ep_id, SeasonIdType.EpId)` 与 `EpList(ep_id, SeasonIdType.EpId)` 都在拿空串去请求，
season 端点回 `code=-400`，ep/list 也拿不到片单，`Detail` 和 `Episodes` 双双为空——页面自然什么都没有。

上游同源项目（`ywmoyue/biliuwp-lite`，`ViewModels/Season/SeasonDetailPageViewModel.cs`）的做法是
`LoadSeasonDetail(string seasonId)`，`Detail(seasonId)` 与 `EpList(seasonId)` 都按 season id 请求，
具体分集在列表到手后按 `ep_id` 选中。本仓库的 `Api/SeasonApi` 两个方法本来就是 `SeasonIdType.SeasonId`
为默认值，是调用方把 id 弄错了。

处理（`5132840`）：

- `SeasonDetailVM.LoadSeasonDetail(season_id)`：`Detail(season_id)`、`EpList(season_id)`，日志改为 `番剧详情解析失败 season=…`；
- `SeasonDetailPage.InitSeasonDetail()` 传 `season_id`；`ep_id` 仍用于在列表里选中初始分集，链接里的分集不在列表时仍从第一集起播；
- `plan/tests/season1` 探针同步：stub 增加 `Detail`、登记 `Endpoint`，断言改成「先 season 再 ep/list，两次都用 `SeasonIdType.SeasonId`」，
  并补了「整页信息失败仍发布片单」的用例。

验收：进番剧/国创/电影页面能看到标题、封面、简介、评分、地区、追番状态与剧集列；换季可用；从 `ep` 链接进入时仍在对应分集起播；
日志不再出现 `ep=` 为空的详情失败行（真的取不到整页信息时应是 `season=<数字>`）。

## BR-66 — 评论里显示 `&quot;` 之类的 HTML 实体（维护者 2026-11-08）

现象：评论区（以及回复、视频简介）把已经转义过的文本又转义了一次，于是页面上看到 `&quot;`、`&amp;`、`&#39;`
这些字符本身，而不是它们代表的标点。

定位：`ControlHelper.StringToRichText` 是评论富文本的唯一出口（`CommentControl.xaml.cs:1012` 的 `Content.text`
getter，主评论与每条回复都走它；`VideoDetailPage.xaml.cs:372` 的视频简介也走它）。它第一步就是
`txt.Replace("&", "&amp;")`——接口给的 `&quot;` 于是变成 `&amp;quot;`，XAML 就把它渲染成六个字符。
短评（`SeasonShortReviewItemModel.content`）是直接绑定的，同样会露出实体。

处理（`ec92550`）：

- `StringToRichText` 在转义之前先 `WebUtility.HtmlDecode(txt)` 解一次（只解一次，后面照旧转义、换行、拼链接/av号/表情）；
- `SeasonReviewVM.GetItems` 对每条短评 `content` 同样解码一次。

验收：评论里引号、`&`、`<`/`>` 显示成字符本身；`<b>` 这类内容仍按文本显示而不是变成加粗；`[表情]`、av/BV 链接照旧；
短评同样不再出现实体。

## BR-67 — 播放器侧栏的「节点」「本地轨道」空标签（维护者 2026-11-08）

现象：播放器设置侧栏会出现「节点」标签但里面一条节点都没有；「本地轨道」也会在没有可选轨道时出现。

定位：`PlayerControl.xaml` 的 `NodePivot` 没有写 `Visibility`，默认就是可见的——只要某一页没走到
`InitializePlayInfo`（例如 BR-65 那会儿番剧页没有可播列表），第一次判定就永远不会执行，空标签就这么露出来。
判定本身也不够严：`PlayerSurfaceVisibility.Decide` 只看「是不是互动视频」，不看节点是否已经取到；
「本地轨道」把视频/音频轨道也算作内容，而那两行下拉只在合集（manifest）播放时才填。

处理（`9fdfbf4`）：

- `NodePivot` 默认 `Collapsed`，第一次判定之前不占位置；
- 判定新增 `NodeCount`（当前这一集真正到手的 `story_list` 条数）与 `IsManifestPlayback`：
  「节点」标签与右下角节点按钮 = 互动视频且节点数 > 0；「本地轨道」= 合集播放且视频/音频轨 > 0，或字幕/弹幕轨 > 0；
- `GetNodes()` 之后补一次判定（首次播放与换节点两处），节点到手才露面；
- 节点列表归属当前分集：换集（包括从一个互动视频换到另一个）重建 `InteractionVideoVM`，上一集的节点不会跟着过来；
- `plan/tests/playertoolbar` 探针补了「互动但节点还没到」「非互动但节点数不为零」「合集外的视频/音频轨」等用例。

验收：普通视频、以及没有任何可播内容的番剧页，侧栏不再出现「节点」「本地轨道」；互动视频在节点列表取到后出现，
换节点后跟着更新，换集后不显示上一集的节点；合集播放里有可选轨道时「本地轨道」出现，没有时消失。
