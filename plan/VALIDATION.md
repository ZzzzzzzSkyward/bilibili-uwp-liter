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

## 2026-11-24 — 新修复的 scoped Windows 跟进（本次未编译/未运行）

历史维护者默认通过策略保持。以下仅列新改动需要的 Windows/真实会话证据；不把旧行重开，
也不从离线探针推断 render/focus/session/device PASS。Linux 前置命令
`powershell -NoProfile -ExecutionPolicy Bypass -File ./build-release-x64.ps1 -Fast -NoVersionBump -Restore`
退出 127（`powershell: command not found`），VS/UWP/Windows SDK/运行时不可用，故当前 **FAILED/BLOCKED**。
运行后需记录实际版本、设备、时间、场景/结果；只有新真实缺陷才新增 BR。

| 新改动 | Windows/会话确认范围 |
| --- | --- |
| U1 `5eec54e`、PB1 `edbd873` | 实际登录 cookie 下评论 like/unlike、顶层/子回复、delete；Unicode/换行/特殊字符、正确目标/类型；失败保留已有列表/可重试；typed failure 不变成 false success。离线不证明 cookie jar/wire 或服务写入 |
| U9 `7ff548b` | 受控测试账号新建/编辑收藏夹，公开/私有、空和特殊文本、保存后重新进入确认；记录与清理实际创建的数据，不从签名 recorder 推断服务持久化 |
| U5/U8 `f594c9b` | direct/configured-host 搜索保留现有 BUVID，area/proxy 按选择；page/filter/order 与结果一致；无身份/过期/断网不伤保存会话。需实际 WinRT cookie、网络/代理证据 |
| E1 `1c7f6e4` | 真 ContentDialog 点击/键盘发送空白、延迟发送/连续激活、失败保草稿/显式重试、成功只关一次；secondary/系统关闭/快速新开对话框不受迟到结果污染；busy/progress、表情锁定、编辑焦点与取消后焦点恢复 |
| E1 显示/可访问性 | 实际支持最小窗口及 800×600/1280×720 effective pixels，100/150/200% text，light/dark/follow-system/high contrast；状态不遮操作，键盘可达，Narrator/UIA 名称与 live-region 公告正确 |
| QF3 `8f55968` | Windows PowerShell 5.1 跑独立 15-case probe；实际 UWP/XAML compile，然后获授权的完整版本递增构建；Packages archive 内 entrypoint/companion/fullTrustProcess 与存在且 <65,536 字节根启动器；安装启动、导出/转换器及受影响路径。ZIP fixture 不证明安装/启动 |

## 2026-11-25 — 恢复与数据完整性的新增 Windows 跟进（未运行）

本轮源码/离线与独立审查已完成（[PLAN 处置](PLAN.md#2026-11-25-恢复与数据完整性源码已提交离线与独立审查完成)），
这里列精确新增 native/session 场景。Linux 缺 Windows/VS/UWP/XAML、BackgroundTransfer/FAL/文件锁与真实会话环境，
当前 **FAILED/BLOCKED**；portable pwsh 的包 ZIP fixture 不改变此前 Windows PowerShell 5.1/发布门禁。
先在 Windows 跑 `powershell -NoProfile -ExecutionPolicy Bypass -File ./build-release-x64.ps1 -Fast -NoVersionBump -Restore`，
再记录设备/版本/时间/实际结果；历史默认通过行不重开，发现真实缺陷才新建 BR。

| 新改动 | Windows/真实会话要看什么 |
| --- | --- |
| BND-A 收藏 `0514966` | 受控账号的 Delete/Clean/collect/cancel/sort 普通成功与业务/网络失败：失败保留行/标志、不报排序成功、不误触 Clean 刷新；拖动排序失败后仍可操作，记录实际服务结果而非仅提示。 |
| BND-B/C/D `1b6af65` | season/comment 有有效邻居和可选空项时正常展示/翻页；typed 错误不成为空成功。动作或登录等待期间从视频A切B、同ID重新加载，或让A的收藏列表迟到：B状态/统计/列表不被A覆盖；staff/live的独立关注仍可用。测试代理/fixture若用于异常形状，须区分受控输入与真实服务捕获。 |
| S1/S2 A–B 历史 `bdba9eb` | 冷启动后立即设置A/B、快速更改/取消B、多个视频/剧集交替设置，在写入延迟/失败恢复后保留最后接受状态与原有条目；重启确认落盘结果，播放控件实时A/B行为保持。真实设置存储/启动时序与重启结果单独记录，不把进程内有序队列当crash保证。 |
| S3/S4 / FA-01 本地库 `bdba9eb`/`7fe7a5f` | 并行导入不同集合均保留；同id/root重导入与已开始reopen交错时旧folder可先取得、新token有效；写入/replace失败保留旧registry/grant，后续重试可用。备份后的受控坏registry产生可恢复读取反馈，修复后刷新可用；broken media修复/重导入不丢条目。实际FutureAccessList撤销、文件替换/占用、重启另取证。 |
| 推荐状态/反馈 `7fe7a5f` | 首次断网/空结果、刷新与追加失败/重试、快速连续点击、无新游标；旧rows/banner不消失，空态/重试/自动追加正确。成功/拒绝/失败反馈、移除最后一行；登录成功/取消/异常后再试；登录或回执等待中刷新到同idx的新集合/移除原对象，不向替代行发送或应用旧操作。 |
| 下载扫描与每 GUID observer `7fe7a5f` | 根路径不可读保留原完成列表、子目录/旧metadata故障只标部分状态并保留有效邻居；重扫同CID多个GUID不串CTS/进度/退休。开始/暂停/继续/取消、离页再返、未知总量时进度有限有界；真实BackgroundTransfer callback/UI线程/绑定反馈正确。 |
| 删除/清理/创建并发 `7fe7a5f` | 单集/批量/整标题删除分别确认，期间新增同CID/不同GUID、相邻路径/其它root、未显示子集或创建准备；旧权限不删除新工作。拒绝取消/文件占用/部分删除失败时提示实际结果并保留可重新确认的清理重试；原任务仍pending时不删。准备lease不覆盖媒体lifetime，正常下载不被整段传输锁住。 |
| 已知部分残留 `7fe7a5f` | 取消在首字节前/传输中、清理失败且observer退休，含零长度/缺媒体/不可用episode metadata；完成页不冒出空标题/假完成项，有效同标题sibling仍可见且部分状态明确。新pending/单段成功/失败替代不能清掉旧残留，全required成功才显示新完成；重启后的磁盘判断另记，不能沿用进程内partial 结论。 |
| 实际下载对话框 `7fe7a5f` | 多段MultiFLV按序写不同0.blv/1.blv/…及VideoPath；SingleFLV、DASH有/无音轨、headers/导出保持。空/坏media不改旧目标/metadata；typed失败、mixed成功/失败保留可重试行，ErrorMessage可见，retry不重复成功任务。等待playurl/准备时取消勾选/清空/加选，原选择按序处理且新增行不混入；异常恢复主按钮、已接纳sibling可发现、仅全成功关闭一次。 |
| 显示/可访问性与登录owner `7fe7a5f` | 最小窗口/800×600/1280×720，100/150/200%字号、light/dark/follow-system/high contrast；新状态/重试/清理操作不遮挡且键盘/Narrator可达。受控native登录show fault/cancellation后可再次打开；已有show期间非owner不能解除其占用，不声称新的跨线程串行策略。 |

## 2026-11-25 — combined memory/API新增 Windows与真实会话跟进（未运行）

[source `ff5d2855`](https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/commit/ff5d2855bb62ef6459a724ffae6101b812a7369d) 的新source场景；Windows/native/live均未验证。先Windows执行 `powershell -NoProfile -ExecutionPolicy Bypass -File .\build-release-x64.ps1 -Fast -NoVersionBump -Restore`，记录VS/UWP/SDK/实际resolved依赖和结果；fullpackage/签名安装另按BUILD。不把fixture、Mark、PSCore或51/52PASS当设备通过；旧维护者默认通过行不重开。详细步骤见[VIDEO_MEMORY](../document/VIDEO_MEMORY.md)与[APIcomparison](../document/BILIBILI_API_SHAPE_COMPARISON.md)。

| 新范围 | 实际Windows/UI/会话要看什么 |
| --- | --- |
| Season / terminal / fresh / live | metadata或episode请求/解码期间关闭，迟到结果不能复活旧title/review/player/comment/QR；新打开取得fresh owner。活页refresh/history/unload/reload不能误终结，实际cache交付另记。 |
| Player info / quality / settings | 关闭时迟到info/quality/open不能重建timer/session；活页quality/engine/item fallback保持。反复Loaded后外部设置仍刷新，一次用户设置只产生一次自有subscriber效果。 |
| PF1 / IG1 | A轨道切换等待时接受distinct或same-reference B；B无需等A即可按当前条件使用selectors/Next。B自己的switch与transition各自保有disable权，清除一个不能释放另一个，terminal不启用；label/overlay/keyboard正常。 |
| FA1 / FA2 | close先解绑两个child DataContext；允许有限旧child工作，但不能修改当前绑定视图。新节点独占自己的cid/segment1/pool/settled/lease；旧success/fault/finally不能发布或解锁新lease。记录旧position timer压力、失败重试、manual refresh及再替换为C。 |
| FA3 / history / RF1 | A的ended report等待时close/replace仍保留A report，其后续不能loop/next/toast或修改B。当前ordered/single/list/auto-next/manifest/互动与等待中设置变更保持。Season初始/失败/无可播项时点Update，无item不能throw或开始工作；当前item正常刷新。 |
| G1 / 50 videos | 实际还原的Win2D/codec/display下字幕样式、返回pixels与共享device正常；native heap/GPU单独取证。固定source/artifact/tool/run/fixture/PID+start，5次warm-up、50次真实解码/watch30s/close10s、16checkpoints到idle300，记录attempted/completed与失败。I/F/S/D/SD/DD筛查后，疑似/最近对照各做3次fresh-process重复，A/M另作解释。 |
| AS1 / AS2 | UI搜索`C++`、`A&B`、literal`%2B`、Unicode及原null/empty/tid/order分支；文本不能改写query controls。comment cursor中的`+/%/&/JSON`可用标明synthetic的fixture，真实service cursor另记来源；hot/new/ps/login/source fallback/last-good cursor/retry保持。U1 write form语义不变，不进行未授权真实写。 |
| AS3 / AF1 | normal present bool控制终止/继续，absent/null/Type21保留COUNT；first-empty/missing-info/failed-append/同Page retry保持。notification驱动的initial→Search→Refresh→append绑定跟随distinct collection，same-reference echo/null不递归，旧rows/info语义保持。 |
| AF2 folder publication | 通过initial/Refresh/create/edit/delete后刷新/append/reorder检查实际UI接线；成功次序仍为created引用→default Insert0→flag/Page→collected。迟到的malformed default/bool/collected及append metadata不能提前发布部分row，retry不能复制失败row。保留CLR missing/JSON null/legacy casts/salvage/duplicates/CollectFavorite策略；不是新parser/schema或任意subscriber事务。 |
| PS5.1 / roots / native / service | 先对一个已观察、无害的Windows PID验证采样/identity/error/observer dispose，再采UWP。PS5.1、VS roots、WPR outstanding allocations与GPU只记录实际支持证据，null/error不能填0；真实cookie/account/session/service/write/live门禁独立，U2/P4/P5 hold保持。 |

**sourceC/docD时点范围说明（历史）：** FI1/FD1 requirements PASS但代码未实现，只有未来授权后才新增其实现设备行；本表不声称当前C已修。原始JSONL/log/ETL/snapshots/captures留Git外，记录版本/设备/时间/来源与实际结果；发现真实新缺陷再新增BR。

## 2026-11-25 — sourceE index/dynamic新增Windows与服务跟进（未运行）

以下是`d11dbf867c1896bce7d41956a4b78fc2580d5a5c`新增FI1/FD1/FI2/FI3/SR1场景。源码/离线和synthetic fixture不能代替Windows/UI/service验收。先在Windows执行 `powershell -NoProfile -ExecutionPolicy Bypass -File .\build-release-x64.ps1 -Fast -NoVersionBump -Restore`，记录实际VS/UWP/SDK/还原依赖、设备/版本/时间/source/artifact与结果；full package/签名安装启动另按BUILD。旧维护者device-passed行不重开，实际新缺陷才开BR。

| 范围 | 实际Windows/UI或受控服务要观察 |
| --- | --- |
| FI1 fresh/conditions | 从新index导航开始，conditions与首result各一次；metadata等待时快速Refresh/selection/LoadMore安静拒绝，不重复请求/清owner/制造notice。metadata业务/transport/结构失败后一次合法Refresh重试，usable metadata后首result一次；不设置私有flag准备起点。 |
| FI1 result/grid | Loading/reset通知时重复命令不能抢owner；真实短grid正常page2 autofill，page1 rows/Page已提交，page2等待时旧completion不能清busy。append错误保留rows/同Page retry，正常selection/Refresh保持。 |
| FI2 code/label | 默认3在非首field3目录中仍选code3；显式4、label冲突、展示名变化仍code优先。无code时保留旧label再首项fallback。真实selection/Refresh发送所选keyword；未知opaque code、空白/大小写不规范化，filter/default/签名保持。synthetic目录与真实response分开记录。 |
| FI3 terminal/compatibility | 首/append非空list+JSON Integer0保留全部rows，Page只增一次，无新notice，manual/grid不再追加；integer1/absent仍允许真实page2。null/string0/boolfalse/float0/其他或巨大integer/object/array沿用旧list行为；empty/error/failedappend/retry/terminal后Refresh保持。特殊flags标synthetic，不称live capture。 |
| FD1 dynamic cursor | 真实Home/User all/video/article分页、刷新、failedappend保留last-good source cursor/同Page retry。受控+/%/&/#/duplicate-looking token一次query decode后仍为原值，controls唯一；null省略offset、empty包含一个空offset。REST/legacy/gRPC不混游标，原GET/cookies/identity保持。 |
| SR1 shortreview | 从真实Season detail review入口加载、LoadMore、failedappend/retry/Refresh；Next/total/count/content规则保持。default/null/empty仍包含空cursor；受控opaque值从response→Next→下一request完整roundtrip。配置host、media_id/ps20/sort/default0、可选access_key末尾位置/条件、GET/noCookie/noappWBI保持，不做review写动作。 |

当前本地完整51/52 PASS与上述实际设备结果分开。UNVERIFIED：最近已接受的[实际sourceE hosted观察](../cloud/rounds/20261125-index-dynamic-contracts/publication/root-hosted-source-e.md)于2026-11-25 07:35:28UTC以 `head_sha=d11dbf867c1896bce7d41956a4b78fc2580d5a5c` 过滤Actions collection，返回 `total_count=0` / `workflow_runs=[]`，尚未建立E的hosted result，不推断原因。2026-11-25 06:07:01UTC旧run45434541348/head169b2b0（queued/null）仅为历史观察，不能认证E。特殊cursor/metadata只是synthetic边界覆盖，sanitized真实response来源另记；没有实际服务结果不写可用性PASS。原始JSONL/log/capture/profiler文件留Git外。BR21的50真实视频/RAM/root/heap/GPU/PS5.1/playback/签名安装启动及accountP4/P5hold/U2/live/session前置不因本表解除。

## 2026-11-25 — season review所有权新增Windows跟进（未运行）

[BR75](../bug_report.md)的单VM read claim及existing-season1 synthetic回归不能代替Windows/binding/service。74新增ownership断言通过；selected season1+api02为2/2 **PARTIAL_PASS**、complete_required=false；本候选[独立实现审查](../cloud/rounds/20261125-review-singleflight/final-review.md)PASS，[实际完整gate](../cloud/rounds/20261125-review-singleflight/verification.md)exit0、51/51logical与52/52executable **PASS**、finished=true、complete_required=true。

| 范围 | 实际Windows/UI要观察 |
| --- | --- |
| review pivot pending | 从真实Season detail进入短评，请求等待时快速离开/重入该pivot；只有一份read/rows，spinner不被拒绝调用提前reset，无重复追加。等待中的Refresh/LoadMore不清rows或last-good Next。 |
| idle与失败恢复 | 完成后idle Refresh正常清rows/reset cursor并请求一次，LoadMore继续可用；业务/transport/解码错误后可正常retry，failed append保留rows与last-good cursor。同步binding通知不能重入命令夺owner；completion listener异常只在受控fixture记为synthetic。 |

先按既有Windows build流程记录source/artifact/设备/时间与真实结果，不进行review写动作。Windows/UWP/XAML、真实service/session/write/account/live、50decoded视频/RAM/roots/heap/GPU、签名安装启动仍未验证；BR21、accountP4/P5hold与U2前置保持，旧device-passed行不重开。

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
| Comment popup follows the app theme (BR-62) | `44d7f0e` | **passed 2026-11-08**（维护者确认）: 深色浮窗取深色值，浅色保持原值 |
| Popup and code brushes agree in 跟随系统 | `44d7f0e` | with the system in 浅色 and the app on 跟随系统, nicknames and like/comment counts are dark text (they used to be the dark-palette `#dddddd` on a light panel); switching the app to 深色 flips the XAML tokens and the `GetBrush` values together, so one window never mixes a light and a dark value |

## Shipped 2026-11-08 — checkpoint tail and live-room parse (compile only)

Compiled from `Z:\bilibili-uwp-liter` with `build-release-x64.ps1 -Fast -NoVersionBump -SkipProjectReferences`
(0 errors; no MSIX produced). Per the maintainer's 2026-11-08 instruction there is no runtime or device run from this
side; the rows below say what to look at.

| Item | Commit(s) | Confirm on the device |
| --- | --- | --- |
| Checkpoint survives a pending report (review `c4aa630`) | `cfc4a15` | while a video is playing, seek far forward, pause, and close the app within the 30 s window right after a checkpoint fires; reopen with resume: playback starts at the seeked point, not the pre-seek tick. Same if the window is minimized (suspend) instead of closed. 观看记录 carries the new position |
| Live room opens without a null-reference toast (BR-53) | `c0b7eb1` | **passed 2026-11-08 13:02**, session `5399351e…`: `giftConfig` twice 200 with no `[JSON][FAIL]`, no `step=gift-config`/`step=bag-gifts`, `bag_list` 200, no `NullReferenceException`; the `user_info` guard was not exercised (no superchats in that room) |

## Shipped 2026-11-08 — live danmaku reconnect (BR-63, compile only)

Compiled from `Z:\bilibili-uwp-liter` with `build-release-x64.ps1 -Fast -NoVersionBump -SkipProjectReferences`
(0 errors; no MSIX produced). The drop has only been seen in logs, never forced by hand, so the row below is the
first real test of the reconnect path.

| Item | Commit(s) | Confirm on the device |
| --- | --- | --- |
| A dropped danmaku socket comes back (BR-63) | `722555b` | in a busy room, force one drop (lock the device a minute, or toggle Wi-Fi): the log shows `直播接收包出错`, then a `弹幕连接成功` line returns to the danmaku list and messages resume without leaving the page. Force several drops: the gaps grow 2→4→8→10 s and stay at 10. Leaving the page stops new `直播接收包出错` lines, and nothing crashes while a heartbeat lands on a dead socket |

## Shipped 2026-11-08 — minimized window comes back through its frame (BR-61, compile only)

Compiled from `Z:\bilibili-uwp-liter` with `build-release-x64.ps1 -Fast -NoVersionBump -SkipProjectReferences`
(0 errors; no MSIX produced). The previous attempt (`d0e7168`) was device-reproduced on 2026-11-08 13:12 and failed:
`Application resumed` was logged, `Window.Current.Activate()` ran, the window stayed minimized and the system
re-suspended the app 10 s later. This batch adds the Win32 path and the window-state logs.

| Item | Commit(s) | Confirm on the device |
| --- | --- | --- |
| Maximized window returns maximized after a long minimize (BR-61) | `bbfaf7f` | maximize, minimize for 1–2 minutes, click the taskbar icon: the window should come back maximized straight away. In `20261108.log` expect `Window state at suspend; … iconic=False; zoomed=False; restoreToMaximized=True` at the minimize, then `Main window not visible … activating`, then `Main window restored through Win32; visible=True; …; zoomed=True` at the restore. If it still fails, send those two lines — `hwnd`/`root`/`iconic`/`zoomed` say whether the handle is wrong or the shell refused the command |
| The same window still comes back when the app was left at normal size | `bbfaf7f` | with the window un-maximized, do the same minimize/return: it must come back at the same size (log shows `restoreToMaximized=False` and a `SW_RESTORE` path), and a notification click while the window is visible-but-behind must still front it (`OnActivated` keeps the unconditional `Activate()`) |

## Shipped 2026-11-08 — season page loads its own detail (BR-64, compile only)

Compiled from `Z:\bilibili-uwp-liter` with `build-release-x64.ps1 -Fast -NoVersionBump -SkipProjectReferences`
(0 errors; no MSIX produced). The device log pinned the crash to `Utils.ParseArea(null, …)` fed by a `Detail` model that
was built from `pgc/view/web/ep/list`, whose payload carries only `episodes`/`section`/`play_strategy`.

| Item | Commit(s) | Confirm on the device |
| --- | --- | --- |
| A season page opens without `Value cannot be null` (BR-64) | `b643e77` | open any 番剧/国创/电影 page: no toast, no `ArgumentNullException` in `20261108.log`, and the header shows the real title. The metadata panel has 封面/简介/评分/地区/追番状态; the 短评 tab loads (it used `media_id` = 0 before). Episode switching still works and the 下载 dialog lists both 正片 and 预告 |
| 港澳台 area detection unchanged | `b643e77` | a season whose title carries 僅…香港…地區 still resolves `area=hk` and one with 僅…台灣…地區 resolves `tw`; every other season resolves `cn` (the null-title guard changes nothing for non-empty titles) |

## Shipped 2026-11-08 — season page content, comment entities, empty player pivots (compile only)

Compiled with `build-release-x64.ps1 -Fast -NoVersionBump -SkipProjectReferences` (0 errors, no MSIX).

| Item | Commit(s) | Confirm on the device |
| --- | --- | --- |
| Season page loads its content again (BR-65) | `5132840` | open 番剧 / 国创 / 电影 from the home feed, 时间表, 排行, 搜索, 追番 and 历史: title, cover, intro, rating, area, follow state and the episode column are all filled. Switch season with the season list; open an `/ep` link: playback still starts on that episode. Log shows no `番剧详情解析失败` line with an empty id |
| Comments show characters, not entities (BR-66) | `ec92550` | a comment containing quotes / `&` / `<` renders the characters; a comment whose text contains `&quot;` no longer prints `&quot;`. Replies and the video description behave the same, `[表情]` still renders as images, av/BV ids still link |
| Empty 节点 / 本地轨道 pivots stay hidden (BR-67) | `9fdfbf4` + `b95f2ab` | player settings sidebar on a normal video: no `节点`, no `本地轨道` header. Open the sidebar before playback starts: it opens on 弹幕 with content. On an interactive video the `节点` tab appears once nodes are loaded and follows a branch change; after switching episodes it never shows the previous video's nodes. A manifest/collection playback with tracks shows `本地轨道`, one without hides it |

