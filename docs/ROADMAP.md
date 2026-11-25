# 路线图

本文是 `plan/` 的**索引**：把 `plan/PLAN.md`、四份专题方案、`document/BILIBILI_API_RESEARCH.md` 与
`bug_report.md` 里的未完成项逐条列出，给出状态、验收口径和下一步动作。详细方案不在这里复制，点链接看原文。

状态：`[ ]` 未完成 · `[~]` 部分完成（等设备或等测量）· `[x]` 已完成。

## 0. 当前基线

| 项 | 值 |
| --- | --- |
| 开发线 | `dev`；可变 tip 与同步状态以实际 git/轮次证据为准 |
| 发布线 | `master`，tip 为 1.0.9.61 同步（2026-11-07）+ 仓库文档化提交 |
| 分叉点 | `e2c7958`（2026-10-31）；`dev` 在此之后 510 个提交，`master` 8 个（两次同步提交的内容 `dev` 已全部包含） |
| 设备验收策略 | 维护者的设备检查「默认通过」；只有真实报告的缺陷才重开（`plan/VALIDATION.md`） |
| 探针 | 44 目录批次（43 net8.0 项目 + shell-only stab）+ 7 逻辑 legacy 探针 / 8 可执行阶段 = 51/52；sourceE完整 gate PASS为旧已绑定快照，详见 §6；新增BR75候选完整gate/独立审查PASS，见1.8。历史 QF1/runner 50/51 仍为原快照证据 |

## 1. 未完成项

### 1.1 [ ] 后台播放归属（P1，阻塞：需要设备测量）

- 来源：[plan/PLAN.md](../plan/PLAN.md) 第 2 项、[bug_report.md](../bug_report.md) BR-21。
- 现状：`backgroundMediaPlayback` 已在两个清单里声明，播放器的播放/暂停与状态也接了；缺的是**归属**——
  系统媒体控制是视图级的，`OnUnload` 会给每个控件都关一次，离开标签页会把仍在播放的那个播放器的控制关掉。
  `OnSuspending` 现在有 deferral 与耗时预算，但仍在挂起窗口里强制等待终结器。
- 验收：一个活动播放所有者跨标签页与可见性变化保持 SMTC 会话；切后台释放可选的视觉/弹幕工作；挂起窗口内
  的等待有界；回到前台不重建播放器。
- 下一步：按 [VIDEO_MEMORY](../document/VIDEO_MEMORY.md) 固定50视频/滚动/弹幕与活动播放/最小化对照，取得过程曲线及managed roots/native/GPU证据，再决定整体重构。BR-68窄源码修复已完成，BR-21的Windows RAM归因仍未测量。

### 1.2 [ ] API 解析结构 P4 剩余：账号（P2，阻塞：维护者保留）

- 来源：[plan/PLAN.md](../plan/PLAN.md) 第 4 项、[plan/P4_INVENTORY.md](../plan/P4_INVENTORY.md) §5。
- 现状：P0–P3 完成；P4 的季节（`ed9f248` 起，`bbf…`/BR-64/65 后继续演进）、播放列表、评论、视频详情已迁移，
  各有 DTO + 解析器 + 字段表 + 探针。只剩**账号**这一步。
- 验收（每个迁移的特性）：大整数 ID、可选/类型错误的嵌套字段、未知卡片、空页、来源各自的游标、追加失败与重试、
  刷新替换，行为与迁移前一致；每个特性有唯一解析入口与字段契约。
- 下一步：等维护者放行账号批次；放行后按 §5 的顺序，一次一个特性。

### 1.3 [~] 设备行：1.0.9.42–1.0.9.62（P1，阻塞：需要真机）

- 来源：[plan/VALIDATION.md](../plan/VALIDATION.md) 的 1.0.9.42 / .59 / .60 段与 2026-11-08 的五个「compile only」段。
- 待确认代表项：设置页十页签、热榜卡片 150%/200% 字号、播放目录作为唯一列表入口后的移动/连播、本地播放分P目录、
  预约卡与标题提示、推荐卡标题提示、关窗后的播放进度、BR-61 的最小化还原、BR-63 的真实断线重连、
  第 11 项「暂停后关窗」的待报尾巴。
- 下一步：维护者按 VALIDATION 逐段走；异常按 `bug_report.md` 新开一条 BR。

### 1.4 [ ] 动态信息流的内存与滚动测量（P2，阻塞：与 1.1 同一批测量）

- 来源：[plan/CARD_PLAN.md](../plan/CARD_PLAN.md) §9、BR-21。
- 现状：代码侧已收口（十页滚动窗口、锚点保护、两级转发上限、卡片模型淘汰），没有代码可写；缺量测。
- 验收：给出滚动深度 vs 工作集曲线，并以root/heap证据判断卡片模型归因。下一步：按 [VIDEO_MEMORY](../document/VIDEO_MEMORY.md) 的F与I/S/D对照，同1.1一起采集；源码所有权修复不代替该量测。

### 1.5 [x] 姊妹仓库 API 研究；已确认缺陷修复与活动 U2 分开

- [原研究](../document/BILIBILI_API_RESEARCH.md) 保留 2026-11-09 pinned 基线与 203 方法/路由对（165 已有、38 无精确对应）；完成研究不等于端点迁移或服务可用。
- **U1/P0 已修 `5eec54e`**：四个评论写动作的单一编码 csrf 字段，保留文本/ID；真实 builder red/green 和独立审查 PASS。
- **U9/P1 已修 `7ff548b`**：收藏夹 ts/privacy 分隔，c2 451 断言与独立审查 PASS。
- **U5/P1、U8/P2 已修 `f594c9b`**：直接 web 搜索保留保存身份，非空 area 的 temp 兼容行为保留；app order 编码进入真实签名，api02 原 12 + 搜索 1258 检查与独立审查 PASS。
- **U2/P1 FAILED/BLOCKED：身份/会话/协议集成证据不可得**。真实调用方是 `LiveRoomVM → Modules/Live/LiveMessage.cs`，目前使用固定 `ServerUrl` 加 JoinRoom；旧 `LiveDanmaku.getConf` 不是活动路径。下一步须先取得可信现有 BUVID 或验证 app 回退、token/host/auth/protocol fixtures 及受控真实 session，再一起迁发现/鉴权并验证取消/断线/离开，保留 BR-63 重连。
- 上述离线修复未进行真实评论/收藏写入或实时搜索会话；Windows/session 跟进见 VALIDATION，不宣称端点可用。

- **2026-11-25 当前字段/发布边界**：见 [三个固定来源与shape比较](../document/BILIBILI_API_SHAPE_COMPARISON.md)。`ff5d2855` 已修AS1/AS2 parser前单值编码、AS3 normal present-boolean优先与兼容fallback、AF1引用通知和AF2解码后发布。原203/165/38研究与端点目录未重算；WBI sanitation、route/source家族、OAuthv2、账号P4/P5 hold和U2前置保持。源码/fixture PASS不代表cookie/service/live/write可用。

### 1.6 [~] 三条只编译过的修复（阻塞：需要真机）

- BR-61 最小化还原（`bbfaf7f`，0 错误）：设备上最小化 ≥10 s 后点任务栏图标应回到原来的最大化状态。
- BR-63 直播弹幕重连（`722555b`）：需要一次真实断线（切网/代理断流）看是否 2→4→8→10 s 退避后重连。
- 第 11 项待报尾巴（`cfc4a15`）：暂停后立即关窗，历史应仍上报一次。

### 1.7 [x] PGC index与opaque cursor：sourceE源码/离线完成

- `d11dbf867c1896bce7d41956a4b78fc2580d5a5c` 交付FI1/FD1/FI2/FI3/SR1：独立conditions/results与reentrant通知所有权、order code优先/旧fallback、仅Integer0终止非空index页、dynamic offset与short-review cursor单值编码。见[当前字段/兼容表](../document/BILIBILI_API_SHAPE_COMPARISON.md)、[BR70–74](../bug_report.md)。
- [最终index owning](../cloud/rounds/20261125-index-dynamic-contracts/later-pgc/final-review.md)、[最终SR1 owning](../cloud/rounds/20261125-index-dynamic-contracts/short-review/final-review.md)、[FD1 owning](../cloud/rounds/20261125-index-dynamic-contracts/dynamic/final-review.md)、[final fresh](../cloud/rounds/20261125-index-dynamic-contracts/final-fresh-audit.md)与[完整gate](../cloud/rounds/20261125-index-dynamic-contracts/verification/execution.md)均PASS；[publication receipt](../cloud/rounds/20261125-index-dynamic-contracts/publication/execution.md) 与 [root独立blob/ref核对](../cloud/rounds/20261125-index-dynamic-contracts/publication/root-source-check.json)已核对最终committed字节身份。Final fresh为PGC140/140、SR13446/3446、118/118、34/34，season790/API028795；fresh的G4179仅精确绑定沿用，后续完整suite正常执行既有G4 stage。
- Windows实际navigation/conditions/selection/grid/order/terminal/feed/review分页与错误重试未验证，按[新增VALIDATION](../plan/VALIDATION.md)跟进。UNVERIFIED：最近已接受的[实际sourceE hosted观察](../cloud/rounds/20261125-index-dynamic-contracts/publication/root-hosted-source-e.md)于2026-11-25 07:35:28UTC以 `head_sha=d11dbf867c1896bce7d41956a4b78fc2580d5a5c` 过滤Actions collection，返回 `total_count=0` / `workflow_runs=[]`，尚未建立E的hosted result，不推断原因。2026-11-25 06:07:01UTC旧run45434541348/head169b2b0（queued/null）仅为历史观察，不能认证E；BR21native-unmeasured、50视频/RAM/root/heap/GPU/PS5.1/playback/安装签名启动、accountP4/P5hold/U2前置不变。

### 1.8 [~] Season review read所有权（BR75，源码/离线完成，native未验证）

- 来源：[BR75](../bug_report.md)、[执行证据](../cloud/rounds/20261125-review-singleflight/execution.md)。public读与Refresh/LoadMore共用单VM claim，覆盖zero-ID、同步通知与异常completion释放；原API/auth/write/paging保持。
- 74新增ownership断言通过；selected season1+api02为2/2 **PARTIAL_PASS**、complete_required=false；本候选[独立实现审查](../cloud/rounds/20261125-review-singleflight/final-review.md)PASS，[实际完整gate](../cloud/rounds/20261125-review-singleflight/verification.md)exit0、51/51logical与52/52executable **PASS**、finished=true、complete_required=true。 原实现duplicate/zero-ID与GetItems-only候选的completion Refresh回归失败已保留，未提交/新CI均未声称。
- 下一步：[新增Windows场景](../plan/VALIDATION.md)快速离开/重入pending review pivot，无duplicate rows/spinner reset；idle refresh/more、error retry与cursor retention。Windows/UWP/XAML、真实service/session/write/account/live、50decoded视频/RAM/roots/heap/GPU、签名安装启动仍未验证；BR21、accountP4/P5hold与U2前置保持，旧device-passed行不重开。
- [实际baselineH hosted观察](../cloud/rounds/20261125-review-singleflight/root-hosted-baseline.md)：2026-11-25 08:58:49UTC对H`86e0e56e67c05aadc34500ee5f65bdcdce3a5277`过滤返回total_count0/workflow_runs[]。这是exactH事实；本次改动/未来checkpoint没有hosted PASS，旧queued run仅历史。

## 2. 已知债务

来源：[plan/PLAN.md](../plan/PLAN.md) § 已知债务（源码内已无 `TODO/FIXME`）。

- [ ] **重写播放器类**（`Controls/Player.xaml.cs`，~175 KB）。阻塞：会动到 BR-1/BR-21/BR-43/BR-54 已验收的
  播放路径，没有设备测量支撑不做；本轮窄修复不解除整体重写门禁，先按 [VIDEO_MEMORY](../document/VIDEO_MEMORY.md) 取证。
- [ ] **旧版合集卡逐字段手写解析**（`Controls/Dynamic/DynamicParse.cs` 的 `MediaList` 分支，样例
  `https://t.bilibili.com/625835271145782341`）。阻塞：现代合集已走共享解析器，旧路径只在老接口回退时命中。
- [ ] **多窗口改字形可能撞线程约束**（`Controls/FontAwesome5/FontAwesome.cs`，当前只 `Debug.WriteLine`）。
  阻塞：只在多窗口改图标时复现，缺设备复现步骤。

## 3. 缺陷（`bug_report.md`）

- [ ] **BR-21 内存随时间增长** — 新行程约50视频；BR-68窄源码修复/完整离线PASS不等于已测RAM。见1.1/1.4与 [VIDEO_MEMORY](../document/VIDEO_MEMORY.md)。
- [x] **BR-31 文案规范** — 两条硬规则已全量改过，新增文案按规则写。
- [x] **BR-53 直播间打开报空引用** — `c0b7eb1` 修好，设备确认 2026-11-08 13:02。
- [~] **BR-61 / BR-63 / BR-54/55/56/57/58/59/60/62/64/65/66/67** — 均已提交，其中 BR-62 已设备确认，
  其余按 §1.3/§1.6 等设备。

- [x] **BR-68/69 源码审计处置** — 播放迟到所有权/RF1/IG1、query/cursor、favorite分页/通知/响应发布已独立PASS并交付sourceC；Windows/native/service仍未验证。
- [x] **BR-70–74 sourceE源码审计处置** — FI1/FD1/FI2/FI3/SR1有界源码与离线独立PASS；真实Windows/UI/service验证仍未运行，见1.7和VALIDATION。

## 4. 停放（明确不做，除非另有需求）

来源：`plan/PLAN.md` § Parked。

- [ ] 直播磁贴（需要 tile service + 后台任务注册，并保证 deferral 完成前 await 到刷新）。
- [ ] 真 buvid 获取（没有可信来源；appkey 头不承诺。注意 U2 要的「真 buvid」会先卡在这里）。
- [ ] 备用弹幕渲染器（FrostMaster 无用例，继续用 NSDanmaku）。
- [ ] 更多 API/鉴权变体（无调用方，不照抄姊妹仓库的端点类；与 §1.5 的「有本地缺陷的差距」区分开）。
- [ ] 本地库扩展（五个里程碑已完成；更多格式/发现/封面需要单独需求）。

## 5. 工程侧欠账（文档化，未改代码）

- [ ] **`Helpers/HttpHelper.cs` 的请求节流是空转的**：`lasttime` 在 `Get` 里被读、从未被写，所以「最小间隔」
  永远不生效（`concurrent_get_count` 也就不可能大于 1）。验收：要么给出上限测量后接回节流（写 `lasttime`），
  要么把这段死逻辑删掉；两者都会改变全局请求节奏，必须先量。
- [ ] **partial/sparse checkout 前置**：历史工作副本曾缺 29 个大 blob（含 dynamic.proto、PlayerControl、DynamicVM、VideoDetailPage、EFontAwesomeIcon、g3/Program）。先检查实际 checkout；源文件存在时不沿用缺失阻塞。缺失时记录命令/路径，不改 sparse 配置或主动拉取排除 blob，见 [DEVELOPMENT.md](DEVELOPMENT.md) §9。
- [x] **Bash 与 LF 分开处理**：`.gitattributes` 钉 `*.sh text eol=lf`；命令须显式 `bash`。`sh` 若是 dash，即使 LF 也不支持 Bash 特性，不能只靠换行修复。
- [x] **PB1/P1 `edbd873`**：业务码越界/BigInteger 转 typed failure；comments/c1/season1 回归与独立审查 PASS。
- [x] **QF1/P1 `ca99149`**：当前源码 harness 对齐与完整 50/51 离线 sweep、独立审查 PASS。
- [~] **QF2/P2**：stage-aware runner/selector/control 已实作，metadata/preflight P1 于 `9c33bce` 修复；changed-runner 完整 50/50 逻辑、51/51 可执行 PASS、complete_required=true，契约控制 **14/14**、旧无参链 exit 0。独立源码复审 **PASS / P1 CLOSED**；修正 workflow `c4e9dba` 源码 PASS，dev push/PR/manual 入口已发布。命令 `bash plan/tests/run-all.sh --tools-root PATH`，选择范围为 PARTIAL_PASS。
  **Hosted PENDING/UNVERIFIED**：[实际 run 45434541348](https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/actions/runs/45434541348)，workflow_dispatch，head `169b2b0a0205a14cad7d24558920250989720cc2`，2026-11-24 16:17:58 UTC 为 QUEUED、0 jobs/0 artifacts。下一步只观察同一 run 至实际 job/step/conclusion/artifact；不将排队当 PASS/失败或已证 outage，不重复派发。旧 HTTP422 配置失败已被修复，作为历史证据保留。本地/source PASS 不替代 hosted 或 Windows 门禁。
- [~] **QF3/发布前 P1 `8f55968`**：Packages 根启动器守卫与独立 15-case probe/实现审查 PASS。**Windows compile/full-package/install/launch FAILED/BLOCKED**：Linux 缺 PowerShell/VS/UWP/Windows SDK。下一步在 Windows 完成实际发布门禁，不以 ZIP fixture 代替。
- [~] **发布签名前置：Windows 证据 BLOCKED**。只读盘点确认仓库 CER（PEM）有效期截至 2024-05-16 22:27:48 GMT，公开 SHA-1 `42621A9CE0525C9351F5142E87F349D063DFE89B`；两个项目当前选择 `3F95D7B0B185635BA827AF4A3E82E31EC3675FCF`，两者不同。未检查 Windows 证书存储/私钥或真实包，因此不能推断当前 signer 过期或已发生签名失败。下一次获授权的发布准备须核对所选证书有效期/私钥/publisher、真实 MSIX 签名及分发证书；不擅改 CER/指纹/身份/密钥。只读证据见 [release-signing-evidence.md](../cloud/rounds/20261124-package-layout-guard/release-signing-evidence.md)。
- [ ] **QF4/P2 依赖兼容性**：完整 shipped graph、SDK/transitive/native 支持与 Windows 播放/导出证据缺失；需单独 owner 做 advisory/pin inventory，禁止批量升级。gRPC 单项目查询不认证全应用。
- [x] **P2 持久化 S1–S4 / FA-01**：已用实际源重现并修复深快照/写入顺序、首次初始化、registry 事务与 admitted reopen；异常 registry 条目返回 typed registry-read 并可在修复后重试。实现/独立审查和平台边界见 §6 与 PLAN；不承诺跨进程或 crash/restart 可靠性。
- [x] **UX-03/04 与恢复源码**：推荐初始/刷新/追加失败保留可用状态、显式重试和空态；下载按实际取消/删除结果反馈、失败保留清理重试，creator/path/subtree/revision 与 caller/lifetime 边界已闭环。当前源码与离线审查完成，见 §6。
- [~] **UX-05/06 Windows 显示/可访问性 FAILED/BLOCKED**：Linux 无 UWP/XAML/native 运行环境。保留既有批量操作、导入导出、完整标题与主题；下一步按 VALIDATION 获取最小窗口/800×600/1280×720、100/150/200% 字号、主题/键盘/Narrator 证据，只按实际缺陷修布局。

## 6. 最近完成

- [x] **2026-11-25 index/dynamic sourceE**：`d11dbf867c1896bce7d41956a4b78fc2580d5a5c`，FI1/FD1/FI2/FI3/SR1；[publication receipt](../cloud/rounds/20261125-index-dynamic-contracts/publication/execution.md) 与 [root独立blob/ref核对](../cloud/rounds/20261125-index-dynamic-contracts/publication/root-source-check.json)、[final fresh](../cloud/rounds/20261125-index-dynamic-contracts/final-fresh-audit.md)与最终owning PASS。当前行为见[API比较](../document/BILIBILI_API_SHAPE_COMPARISON.md)，sourceC/docD及旧ux1dFAIL/corrected52PASS保留历史。
- [x] **当前sourceE完整gate**：[actual receipt](../cloud/rounds/20261125-index-dynamic-contracts/verification/execution.md)，exit0、51/51logical、52/52executable PASS、finished=true、complete_required=true；2026-11-25 06:52:25.306397→06:55:21.987887UTC，176.681秒。Results SHA256`12d25b77236ce59569f831494e8220212c25e30b5c5d871eee6833ff33d0d6d1`；940输入canonical SHA256`3c5dd387072e163c5b99c27fbd17a36bc93f06b3a6f734e433d4e07e205c104e`。实际运行于dirty A`7fc3c524fde4139d01e7ecd362647ca771c64ea8`，随后以实际E committed blobs绑定，不能称clean-E run。All5073quality/732PowerShell/120review-history输入前后稳定；runner14/package15/current sampler独立沿用，不计52stage。
- [~] **当前sourceE hosted/native**：UNVERIFIED：最近已接受的[实际sourceE hosted观察](../cloud/rounds/20261125-index-dynamic-contracts/publication/root-hosted-source-e.md)于2026-11-25 07:35:28UTC以 `head_sha=d11dbf867c1896bce7d41956a4b78fc2580d5a5c` 过滤Actions collection，返回 `total_count=0` / `workflow_runs=[]`，尚未建立E的hosted result，不推断原因。2026-11-25 06:07:01UTC旧run45434541348/head169b2b0（queued/null）仅为历史观察，不能认证E。Windows/UWP/XAML/PS5.1/50decoded/RAM/roots/heap/GPU/playback/cookie/service/write/account/live/session/签名安装启动均未验证；按新增VALIDATION，旧device-passed行与BR21不改判。

- [x] **2026-11-25 combined memory/API源码**：[source `ff5d2855`](https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/commit/ff5d2855bb62ef6459a724ffae6101b812a7369d)，Season M1、player R1/R2/R3/RR1、PF1/FA1–3/RF1/IG1、G1自有临时对象及API AS1–3/AF1/AF2已独立源码/fixture PASS。见 [VIDEO_MEMORY](../document/VIDEO_MEMORY.md)、[API comparison](../document/BILIBILI_API_SHAPE_COMPARISON.md)、[BR-68/69](../bug_report.md)。[publication receipt](../cloud/rounds/20261125-api-shape-comparison/publication/execution.md) 与 [独立 blob/ref 核对](../cloud/rounds/20261125-api-shape-comparison/publication/root-source-check.md)核对107payload及935已测输入的实际committed blobs。
- [x] **sourceC corrected gate（历史）**：[exact receipt](../cloud/rounds/20261125-video-memory/verification/corrected-execution.md)，51/51逻辑、52/52可执行PASS、complete_required=true。Results SHA256 `ca43ccc8c2b5fe8b33327965e4f18c4ab7e73c3926e91b09adf9020026575a16`；935输入manifest `c40d13c9428fa910b7066d223ac12a9295f136f213acab7303865cc6e401dc1d`。执行于dirty7356；随后blob绑定sourceC。首轮50/51逻辑、51/52可执行PASS/1FAIL（ux1d）保留历史，IG1两方法修正后新run PASS、tests不变。runner14/package15/sampler分母独立；[当前interpreter receipt](../cloud/rounds/20261125-video-memory/verification/evidence/interpreter-checks.md)为LinuxPSCore7.7，非PS5.1/native samples。
- [~] **sourceC publication时点 hosted/native（历史）**：UNVERIFIED：2026-11-25 05:09:51 UTC 的实际 Actions collection 只见旧 run45434541348（head169b2b0、queued/null），没有本次 sourceC 的 hosted result。Windows VS/UWP/XAML/PS5.1、实际PID/50decoded视频、cache/codec/WinRT/RAM/root/heap/GPU/pixels、签名安装启动与live/account/session/service/write均未验证；按 [新增VALIDATION](../plan/VALIDATION.md) 和两guide跟进。BR-21仍native-unmeasured，旧设备默认通过不重开。
- [ ] **sourceC/docD时点FI1/FD1计划（历史）**：requirements PASS、代码未实现；见1.7，不在本次修复/gate声明中。

- [x] **2026-11-25 recovery/integrity 源码完成**：`0514966` favorite typed acknowledgements、`1b6af65` payload/null-row/raw-paging 与 detail action/list 归属、`bdba9eb` 深值有序 A–B snapshot/首次初始化/registry 事务与 reopen；最终 `7fe7a5f` 含推荐恢复、下载扫描/每 GUID observer/真实清理重试、creator/subtree/revision 协作、MultiFLV/失败重试/稳定 selection 与 login helper lifetime。RF1/RF2/FA-01–09/CR1 全部独立 CLOSED，范围/red→green/兼容边界见 [PLAN 处置表](../plan/PLAN.md#2026-11-25-恢复与数据完整性源码已提交离线与独立审查完成)、[owning review](../cloud/rounds/20261125-recovery-integrity/recovery/final-review.md)、[fresh audit](../cloud/rounds/20261125-recovery-integrity/final-audit.md)。
- [x] **恢复轮历史完整 gate**：[final integration](../cloud/rounds/20261125-recovery-integrity/verification/execution.md) 为 51/51 逻辑、52/52 可执行 PASS、complete_required=true，recovery 348 / g5 335；源 915 与工具 5073字节绑定最终已推源码 7fe7a5f。14 controls/package15各自独立且输入未变，未重复执行。旧50/51及早期51/52保持历史 provenance。
- [~] **恢复轮 hosted 与 native 记录**：root 2026-11-25 04:11 Asia/Shanghai 实际 Actions collection 只见旧 169b2b0的 run45434541348 queued/null；没有经验证的7fe7a5f hosted run/result。旧head即使最终通过也不能认证新 runnable bytes/51/52 inventory；只观察实际服务结果，不推断原因或重复派发。Windows compile/XAML/BackgroundTransfer/FAL/文件锁/会话/签名与设备仍 FAILED/BLOCKED，下一步按 [新增 Windows 场景](../plan/VALIDATION.md#2026-11-25--恢复与数据完整性的新增-windows-跟进未运行)执行；不更改历史默认验收。

- [x] **2026-11-24 E1/UX-02 `1c7f6e4`**：评论发送 single-flight、等待反馈、保草稿失败重试与关闭后迟到拒绝；真实 dialog/HttpResults/typed parser 的 153 新检查、comments 总 1070 与独立审查 PASS。实际 XAML/焦点/会话仍 FAILED/BLOCKED。
- [x] 本次 U1/PB1/U9/U5/U8/QF1/QF3 的准确行为、测试归属和提交见 [PLAN.md 维护修复表](../plan/PLAN.md#2026-11-24-维护修复已提交与离线验收)。Windows 平台限制保留；账号 P4/P5 仍为维护者 hold；parked 范围不扩张。


- [x] 2026-11-08 批次（全部只编译过，除了 BR-62 已设备确认）：BR-62 评论浮窗暗色令牌（`44d7f0e`）、
  第 11 项待报尾巴（`cfc4a15`）、BR-63 直播弹幕重连（`722555b`）、BR-61 窗口还原（`bbfaf7f`）、
  BR-64/65 番剧页改用 `pgc/view/web/season`（`b643e77`、`5132840`）、BR-66 评论实体解码（`ec92550`）、
  BR-67 播放器空标签（`9fdfbf4`、`b95f2ab`）。
- [x] 2026-11-09：与姊妹仓库的 API 对照与端点目录（`document/BILIBILI_API_RESEARCH.md`、`BILIBILI_API_ENDPOINTS.md`）。
- [x] 2026-11-24 本批次（agent 化整理，从 `master` 的 8 个提交里挑出仍然适用的部分移植到 `dev`）：
  `AGENTS.md` + `docs/` 五篇、死代码 `Helpers/SqlHelper.cs` 删除、首启布局判据 `Services/FirstRunDisplayMode.cs`
  （+ 探针 `plan/tests/firstrun` 15 项）、`OnExiting` 的 deferral 异常安全、`VideoPlayHistoryHelper` 的空表兜底、
  `.gitignore` 与 `*.sh text eol=lf`。其中 `OnSuspending` 的空安全与耗时预算 `dev` 早已有更好的版本，未覆盖。
- 更早的完成记录见 [plan/PLAN.md](../plan/PLAN.md) 的 Done 段；逐条设备说明见
  [plan/VALIDATION.md](../plan/VALIDATION.md)。

## 7. master 与 dev 的关系（留待维护者决定）

`master` 现在是发布线。两条线在 `e2c7958` 分叉后：`dev` 多出 510 个提交，`master` 多出 8 个；
两次同步提交（`086ec90`、`eb1f1df`）的内容已全部在 `dev` 里（逐文件比对：209/318 与 290/317 相同，其余是
`dev` 更新的版本），`337bb45`（README）在 `dev` 有等价补丁，本批次又把文档与三个修复移植到了 `dev`。可选做法：

1. **保持现状**：功能继续进 `dev`，发布时从 `dev` 挑一个点同步到 `master`（现状，推荐）。
2. **在 `master` 上合并 `dev`**：`master` 已经不是 `dev` 的祖先（它自己多 8 个提交），所以**不存在
   fast-forward**；只能 `git merge dev` 产生一个合并提交，或把 `master` 重置到 `dev`（重写发布线历史，
   需要维护者明确同意，且会让 tag `20261107` 之前的血缘变线）。
   合并前还要确认：发布包从 `dev` 构建是否可接受（`dev` 里有若干「只编译过」的修复，见 §1.3/§1.6）。
3. **反向合并**：把本批次（文档 + 修复）再从 `master` 合到 `dev` —— 已经在 2026-11-24 的批次里做过，
   不要重复。

本仓库**没有**动 `master`；是否合流、以哪种方式合流由维护者决定。
