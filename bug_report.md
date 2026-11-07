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

1. 预约卡的说明行改成两个标签：`【11-13 20:00 开播】【175人已预约】`（标题行保持 `reserve.title` 不变）。
   选中方案 A：标题不动；时间取 `desc1`（把「直播」改为「开播」），人数取 `reserve_total` / `desc2`
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

- 已修：`93c69de`（`g5` 265 → 293）。`ReserveCardCopy` 对 `直播`/`预约` 只改结尾词，`直播中`/`预约中` 原样保留；
  `商品`/`合集`/`附件` 三个标签与空行不渲染都有锚点。
