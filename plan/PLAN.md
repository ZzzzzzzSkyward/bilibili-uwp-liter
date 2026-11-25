# Plan — open work only

Updated 2026-11-01 for `dev`. Everything shipped is one line in the commit history. Finished plan
documents, reports and progress logs were removed on purpose — `git log` has them.

**Verification policy:** the maintainer's device pass counts as passed. A shipped item stays closed
until the maintainer reports an actual bug; only such a report reopens it, and then it is written up
in `bug_report.md` first.

## State

| Fact | Value |
| --- | --- |
| workspace | 路径取当前 checkout；历史 Z:/H: 说明只适用于原维护者工作副本。先核对实际输入是否存在，不把历史 sparse 缺失自动套给完整 checkout |
| branch | `dev` 是唯一开发线；同步状态取实际 git 证据 |
| build | `powershell -NoProfile -ExecutionPolicy Bypass -File ./build-release-x64.ps1 -Fast -NoVersionBump -Restore` 编译循环；不带 `-NoVersionBump` 才自增两份清单。全量发布构建与真实安装启动另行验证；`-SkipProjectReferences` 只在引用输出已存在时使用 |
| probe checks | 显式 Bash；44 目录批次含 g1 根 runner 与 shell-only stab（43 net8.0 项目），另七逻辑探针/八 legacy executables，总计 51/52。精确 SDK/运行时/引用包与命令见 `docs/DEVELOPMENT.md` §6；成功前缀不算完整通过 |
| sync | Local maintainer only: `powershell -NoProfile -ExecutionPolicy Bypass -File sync-dev.ps1` — fetch+rebase+push with TLS retries, stashes the two `Package.appxmanifest` around it (`-NoPull`, `-NoPush`, `-Attempts`, `-RetrySeconds`). Cloud agents follow the applicable serialized grant: ordinary fetch, granted fast-forward-only integration and nonforce push; do not run this rebase/stash script or automatically stash/rewrite concurrent work. Preserve work and record integration blockers |
| review cache | `plan/REVIEW_LOG.md` |
| bug device log | `bug_report.md`, `code_review.md` at the repo root |

## Done

**Current maintenance entries:** the 2026-11-24/25 source/offline sections below state their own unverified native gates; their completion does not imply device acceptance. The dated historical shipping summary remains below.

Newest first; every one of these is device-accepted per the policy above, except the 2026-11-01 entries: BR-44 is
device-confirmed, while P4 step 1, BR-45 and the second BR-43 attempt are compiled and probe-covered only — their
device rows are still open in `plan/VALIDATION.md`. The 2026-11-02 entries are compiled
(`-Fast`, 0 errors, package 1.0.9.35), probe-covered and covered by one unified run of all 35 probe batches
(`TOTAL_FAILURES=0`), but none of them has been on a device. The 2026-11-05 entries (`7e628c3`, `65fd423`) are
compiled into `BiliLite.Packages_1.0.9.42_x64.msix` (package directory dated 2026-11-05 00:22); their device rows
are the 1.0.9.42 section in `plan/VALIDATION.md` and have not been run, and no probe run is recorded for them.
The 2026-11-06 entries (`01e51a7`, `798a03a`, `4dc85ce`) are compiled (`-Fast`, 0 errors) and probe-covered
(`cardlayout` 761, `playertoolbar` 90, `toastalign` 41, `g3` 342, `g5` 200); none has been on a device.

### 2026-11-25 combined memory/API：源码与离线完成，native未验证

[source `ff5d2855`](https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/commit/ff5d2855bb62ef6459a724ffae6101b812a7369d) 已普通提交/推送；唯一parent `7356b94893377558191b0d70576a52bae704c1f9`，tree `2565a48424882c76acfe3217c7fb610b7b193b91`。[publication receipt](../cloud/rounds/20261125-api-shape-comparison/publication/execution.md) 与 [独立 blob/ref 核对](../cloud/rounds/20261125-api-shape-comparison/publication/root-source-check.md)已核对107交付路径与935个已测输入的实际committed blobs，保留执行时dirty7356与生成Git metadata的原始provenance。

| 范围 | 最终有界结果 | 独立证据 |
| --- | --- | --- |
| M1/R1/R2/R3/RR1 | terminal close后的迟到拒绝；fresh owner正常打开；活页refresh/history/unload语义保留；owned settings订阅一次且每次仍刷新 | [managed PASS](../cloud/rounds/20261125-video-memory/managed/final-review.md)、[residual PASS](../cloud/rounds/20261125-video-memory/resources/residual-final-review.md) |
| PF1/FA1–3/RF1/IG1 | 替代轨道/Next按current switch与transition共同限制；close先解绑child；节点拥有pool/segment/lease；history后旧ended suffix拒绝；无item Update不工作；Show/Clear真正null-safe Next赋值 | [RF1 owning](../cloud/rounds/20261125-video-memory/resources/rf1-final-review.md)、[RF1 fresh](../cloud/rounds/20261125-video-memory/rf1-final-audit.md)、[IG1 owning](../cloud/rounds/20261125-video-memory/resources/ig1-final-review.md)、[IG1 fresh](../cloud/rounds/20261125-video-memory/ig1-final-audit.md) |
| G1/tool | 五个自有绘制临时对象确定性cleanup；borrowed device/returned image保留；四模式工具提供typed-null/identity/order/stop/disposal，未测native | [graphics PASS](../cloud/rounds/20261125-video-memory/graphics/final-review.md)、[tool PASS](../cloud/rounds/20261125-video-memory/measurement/final-review.md)、[当前interpreter receipt](../cloud/rounds/20261125-video-memory/verification/evidence/interpreter-checks.md) |
| AS1/AS2/AS3 | parser前保护单query value并保留WBI sanitation；normal present bool控制paging，absent/null/Type21 count`!=`与first-empty/failedappend/retry保持 | [query PASS](../cloud/rounds/20261125-api-shape-comparison/query/final-review.md)、[AS3 PASS](../cloud/rounds/20261125-api-shape-comparison/favorite/final-review.md) |
| AF1/AF2 | distinct reference先存后通知一次，same-reference/null echo不通知；folder的list/default/bool/collected解码先于publication，append拒绝late metadata不先加入row；legacy casts/null/salvage/duplicates与成功发布顺序保留 | [AF1 owning](../cloud/rounds/20261125-api-shape-comparison/favorite/af1-final-review.md)、[AF2 owning](../cloud/rounds/20261125-api-shape-comparison/favorite/af2-final-review.md)、[AF2 fresh](../cloud/rounds/20261125-api-shape-comparison/af2-final-audit.md) |

当前 [corrected combined gate](../cloud/rounds/20261125-video-memory/verification/corrected-execution.md)：**51/51逻辑、52/52可执行 PASS，complete_required=true**，exit0。报告SHA256 `ca43ccc8c2b5fe8b33327965e4f18c4ab7e73c3926e91b09adf9020026575a16`；935输入manifest `c40d13c9428fa910b7066d223ac12a9295f136f213acab7303865cc6e401dc1d`。第一次完整gate的50/51逻辑、51/52可执行PASS与ux1d FAIL保留；IG1两方法修正后才有新的完整PASS，tests/assertions字节未改。原player240/aggregate241、66residual/39lifetime/5graphics groups、season141+497及API兼容断言保留。

独立runner14、package15及sampler不计入52。当前Linux PSCore7.7 portable receipt为46次调用（6exit0/40预期exit1）、21groups、106checks、55helper calls/52synthetic rows；没有自动summarizer，也没有nativePS5.1/PID/视频/RAM样本。Hosted：UNVERIFIED：2026-11-25 05:09:51 UTC 的实际 Actions collection 只见旧 run45434541348（head169b2b0、queued/null），没有本次 sourceC 的 hosted result。

[VIDEO_MEMORY](../document/VIDEO_MEMORY.md)给出真实artifact/source/tool/run/fixture/PID+start绑定、16checkpoint、50视频与对照/归因命令；[API comparison](../document/BILIBILI_API_SHAPE_COMPARISON.md)固定三来源pins/licenses/实质维护与Python继承catalog窄维护限制。新处置见 [BR-68/69](../bug_report.md)。BR-21仍native-unmeasured；后台/SMTC/整体player rewrite仍测量前置。Windows VS/UWP/XAML/PS5.1/Win2D dependency/pixels/cache/codec/root/heap/GPU及cookie/service/write/live/account/session/签名安装启动未验证，按 [新增VALIDATION](VALIDATION.md) 执行，旧设备行不改判。OAuthv2 provenance、账号P4/P5 hold和U2身份/token/hosts/auth/protocol/cancel/session门禁保持。

**后续FI1/FD1只有requirements PASS，代码未实现**：见Open18/19与BR-70/71，不在此sourceC或完整gate修复声明中。须待本批source/docs/archive释放、fresh safe baseline和精确owner grant后实施同fixture原红→新绿及owning/fresh/common gate。

### 2026-11-25 恢复与数据完整性：源码已提交、离线与独立审查完成

以下新修复属于源码/离线范围，保留历史设备默认通过策略和旧 50/51 receipts；不表示本轮 Windows 编译、
真实服务、BackgroundTransfer、安装启动或设备通过。当前 recovery 为单个新批次，完整清单 **51 逻辑 / 52 可执行阶段**。

| 范围 | 已提交行为 | 实际证据与处置 |
| --- | --- | --- |
| BND-A 收藏动作 | `0514966`：Delete/Clean/collect/cancel/sort 只接受 typed code0 回执；malformed/HTTP/transport 失败不删除行、不改标志、不触发 Clean 刷新；Sort 整体异常边界保留正常反馈 | 实际 FavoriteDetailVM + decoder/parser：新 563、原 451 PASS，原源101 genuine failures；[独立边界审查 PASS](../cloud/rounds/20261125-recovery-integrity/boundaries/final-review.md) |
| BND-B/C/D 非账号边界 | `1b6af65`：payload Int32/legacy has_more Int64 越界返回 typed Payload；null season/comment 行仅映射时跳过，保留有效邻居、原始 ReplyCount/游标；六动作、登录前后和 favorite-list 迟到结果归属发起详情，独立 AttentionUP 调用仍可用 | c1 原 186+新 139、season1 141、comments 1089（dialog 153）、g4 179、g3 386+rich-text29/105、g5 335 PASS；真实源 red→green 与[独立 PASS](../cloud/rounds/20261125-recovery-integrity/boundaries/final-review.md)，请求/字段/编码/来源兼容未迁移 |
| S1–S4 历史与本地库 | `bdba9eb`：A–B 首次 Find/Set 前初始化，持久化深值快照按接受顺序写入，写入失败不毒化后续；registry read/modify/grant/stage/replace/retire 共用进程内异步门，reopen 持有到实际 folder 获取后释放 | playersess 原 217+新 23=240，LM02 新 50及原断言、LM03/LM05 PASS；[独立 storage PASS](../cloud/rounds/20261125-recovery-integrity/storage/final-review.md)。保留 live entry、key/schema、staged rollback/typed errors；不承诺跨进程/崩溃恢复 |
| FA-01 registry 边界 | `7fe7a5f`：null/不可用 registry entry 在受控读取中返回 typed registry-read，保留原 bytes/grants、broken-asset 修复语义与后续可用重试 | 同 fixture 214 failures→653 additive PASS，实际 DownloadPage refresh red→green；[FA-01 evidence](../cloud/rounds/20261125-recovery-integrity/storage/implementation-evidence/fa01-red-green.md) 与[最终独立 closure](../cloud/rounds/20261125-recovery-integrity/final-audit.md) |
| 推荐恢复 / FA-03/08/09 | `7fe7a5f`：初始/刷新/追加 single-flight、staged publication、失败保留 rows/banner/continuation、显式重试与 no-progress/空态；反馈只接受 typed acknowledgement 并重算最后一行空态。真实登录 true 为成功，登录及回执后都核对原 collection/object；ShowLoginDialog 仅拥有 show 的调用在 finally 释放标志，fault/cancel 后可在同进程再试 | recovery 最终 348（保留原 312/208/139）及 g5 335 PASS；helper 同 fixture 21 semantic failures→348/348，异常/取消/token/nonowner 控制保持；[owning PASS](../cloud/rounds/20261125-recovery-integrity/recovery/final-review.md) 与[fresh PASS](../cloud/rounds/20261125-recovery-integrity/final-audit.md) |
| 下载观察/扫描/清理 / RF1/RF2/FA-02/04 | `7fe7a5f`：root 扫描失败保留旧完成列表，optional 故障呈部分状态；每 GUID 观察/CTS/退休；删除按已确认的原路径/GUID/范围执行，部分失败保留清理重试，取消失败仍 pending 时不删。creator 与 episode/title subtree 共用 revision/path ownership，准备期与媒体 lifetime 分离；已知取消残留（零/缺媒体/metadata 也包括）不冒充完成项，进度有限且有界 | 原 RF1 actual CTS retry proof 已闭环；partial/read-stamp/current-generation 与有效 sibling 控制 PASS，[独立审查](../cloud/rounds/20261125-recovery-integrity/recovery/final-review.md)。进程内 partial/generation 状态不等于重启/跨进程磁盘完整性判断 |
| 实际下载调用方 / FA-05–07 / CR1 | `7fe7a5f`：MultiFLV 按源序生成 distinct 0.blv/1.blv/… 与 VideoPath，保留 SingleFLV/DASH；required media 空/无效时在 generation/target 变更前拒绝。typed/mixed 失败保留行、正确通知 ErrorMessage，显式重试不重复成功行；批次保存发起 selected-object snapshot，whole-event catch/finally 恢复主按钮并发现已接纳 sibling | 相同最终 caller fixture 43 failures→312 PASS；后续 348保留全部断言。RF1/RF2/FA-01–09/P2-CR1 全部独立 CLOSED，见[最终 fresh audit](../cloud/rounds/20261125-recovery-integrity/final-audit.md)；无账号/凭据/schema 迁移 |

最终 changed-source [完整集成](../cloud/rounds/20261125-recovery-integrity/verification/execution.md)：**51/51 逻辑、52/52 可执行 PASS**，
finished=true、complete_required=true、所有 52 行 PASS，无遗漏/其它状态。报告 SHA256 `b33d402fdf65518ea47805463f82d097bec9e638694750e6031b698baa1b549d`；
915 输入绑定 `0a80ac1cf14e76c9274564a999a88522c23460be6a85e077e093d5e28cd55639`，5073 tool/package bytes 前后稳定。
执行时诚实记录 e928bf0/dirty；最终源码 `7fe7a5ff417d0bc2437b5dc3f0507543f744da9b` 已普通提交/推送，
[publication receipt](../cloud/rounds/20261125-recovery-integrity/recovery/publication.md) 核对其 22 runnable paths 与执行字节相同。
独立14 controls 与 package15 通过各自精确输入绑定沿用，均不计入52；selected PASS 仍为 PARTIAL_PASS。

Hosted **PENDING/UNVERIFIED**：root 于2026-11-25 04:11 Asia/Shanghai 的实际 connected Actions collection 仍只返回
旧 workflow_dispatch run [45434541348](https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/actions/runs/45434541348)，
head 169b2b0、queued/null conclusion；未建立 7fe7a5f的 hosted run/result。不由此推断 outage/权限或重复 dispatch。
Windows/VS/UWP/XAML/native/session/signing-store/设备前置仍 **FAILED/BLOCKED**；具体新增行见 [VALIDATION.md](VALIDATION.md)。
portable PowerShell fixture 不等于 Windows PowerShell/VS 能力。BR-21测量、身份U2、账号 hold 和 parked 范围仍保持原验收与下一步。

### 2026-11-24 维护修复：已提交与离线验收

以下是源码/离线范围的完成，不给历史设备行改判，也不声称本次编译、真实登录会话或安装运行通过。
Linux 上 Windows 编译前置命令退出 127（`powershell: command not found`）；VS/UWP/Windows SDK/运行时不可用。

| ID | 已提交行为 | 实际 owning tests / 独立审查 |
| --- | --- | --- |
| U1 / P0 | `5eec54e`：评论 like/add/reply 分离显式 csrf，含 delete 四个动作将裸 token 编码一次；保留 Add 原文 / Reply 预编码约定、ID/type/root/parent | `comments` 保留旧断言并新增 757 请求断言；旧源码 174 失败 → 856 通过；独立实现审查 PASS |
| PB1 / P1 | `edbd873`：ApiEnvelope 的单一有界整数读取供 data/result、VideoEnvelope、comment-action 使用；越界/BigInteger 返回 Envelope / envelope.code，保留合法拒绝码/消息与各自 payload 规则 | 旧源码 comments 12 + c1 4 异常；不变断言修复后 comments 917、c1 186、season1 117 通过；独立实现审查 PASS |
| U9 / P1 | `7ff548b`：收藏夹 add/edit 的 ts 与 privacy 分隔；公开/私有映射、文本/ID、Android 签名输入顺序不变 | `c2` 链接实际 FavoriteAPI，64 失败 → 451 通过；签名记录器只证明完整输入，不证明密码学；独立实现审查 PASS |
| U5 / P1、U8 / P2 | `f594c9b`：七个 web 搜索 builder 仅在非空 area 分支保留 temp BUVID，直接请求不覆盖保存的身份；app 搜索排序编码后参与原签名 | `api02` 原 12 + 搜索 1258 断言，旧源码 36 失败 → 0；真实 WBI/app signer 与 exact transport/proxy 提取校验，auth1/API01 回归；独立实现审查 PASS |
| E1 / UX-02 / P1 | `1c7f6e4`：评论对话框持有单个发送 Task，等待期间锁编辑/表情/重复发送；仅 typed accepted success 关闭；失败保留草稿、可显式重试，secondary/Closing/Closed 退休后拒绝迟到回调 | `comments` 真实 dialog + 实际 HttpResults/typed parser，153 新检查由 109 失败 → 0，总 1070 通过；独立实现审查 PASS；实际 XAML/焦点/会话仍未验证 |
| QF1 / P1 | `ca99149`：g3、LM05、凭据与 playersess harness 对齐当前实际源码；保留旧断言意图并加强串行上报/最新尾巴负控 | 43/43 目录 + 7/7 逻辑探针 = 50/50 逻辑项、51/51 可执行阶段通过；API01 76、AUTH01B 70+39、playersess 217；独立审查 PASS |
| QF3 / 发布前 P1 | `8f55968`：实际 Packages 布局守卫补根启动器存在且未压缩 Int64 长度 <65,536 字节，保留 entrypoint/companion/fullTrustProcess 与诊断优先级 | 独立 PowerShell probe 的 15 个 ZIP 案例，旧源码 10/15 → 15/15，独立审查 PASS；Core/Linux 证据，不证明 PS5.1/真实 MSIX/安装启动 |

精确执行、red/green 与独立审查：[foundation](../cloud/rounds/20261124-maintenance-foundation/maintenance-plan.md)、
[API/action recovery](../cloud/rounds/20261124-api-request-maintenance/execution-index.md)、
[package guard](../cloud/rounds/20261124-package-layout-guard/final-review.md)。QF2 runner 已实作（`0368569`、默认前缀控制 `f1963e0`，最终 metadata/preflight 修复 `9c33bce`）：
`bash plan/tests/run-all.sh --tools-root PATH` 的 changed-runner 本地完整报告 50/50 逻辑、51/51 可执行阶段 PASS、
`complete_required=true`；独立契约控制 **14/14 PASS**，旧无参链 exit 0。初审 P1 已由真实前置 deadline/signal
red→green 闭环，独立 runner 源码复审 PASS/P1 CLOSED；修正 workflow `c4e9dba` 源码审查 PASS。
真实 corrected workflow_dispatch run [45434541348](https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/actions/runs/45434541348)
绑定 `169b2b0a0205a14cad7d24558920250989720cc2`，2026-11-24 16:17:58 UTC 观察为 **QUEUED、0 jobs/0 artifacts**。
故 hosted CI **PENDING/UNVERIFIED**，不是 PASS、失败 job 或已证明的 outage/permission 问题。旧 HTTP422 是已修配置的历史失败，
不再是当前 run 诊断。精确 [source review](../cloud/rounds/20261124-verification-runner/final-review.md) 与
[hosted receipt](../cloud/rounds/20261124-verification-runner/corrected-hosted-execution.md) 分开；
QF1 sweep、changed-runner local gate 与 hosted 验收也分开。选择/报告/预算/前置详见 DEVELOPMENT §6。Windows 新行见 [VALIDATION.md](VALIDATION.md)。

- **Agent 化整理与 master 移植 (2026-11-24):** `dev` 成为唯一开发线之后，把 `master` 上 8 个提交里仍适用的部分移植过来：新增 `AGENTS.md` + `docs/` 五篇（架构/功能/开发/路线图/索引，按 `dev` 的实际代码重写）；删除死代码 `Helpers/SqlHelper.cs`（只创建一只没人读取的 bili.db）连同 csproj 项与 `App` 调用；首启布局判据移进 `Services/FirstRunDisplayMode.cs`（旧的 `width/height > 16/9` 在 UInt32 上是地板除，实际要求比值 ≥ 2，高为 0 还抛除零），带探针 `plan/tests/firstrun` 15 项（旧判据下 2 项红，本机实跑全绿）；`OnExiting` 的 deferral 改为 `finally` 放行（`OnSuspending` 本仓库早就有 try/catch 与耗时预算，未覆盖）；`VideoPlayHistoryHelper` 的静态表不再可能为 null，`async void` 写盘失败改为记日志；`.gitignore` 补 UWP 产物与探针输出，`.gitattributes` 钉 `*.sh text eol=lf`（CRLF 会让 `set -euo pipefail` 直接失败）。两次同步提交（`086ec90`、`eb1f1df`）的内容经逐文件比对已全部在 `dev` 里（209/318 与 290/317 个 blob 相同，其余是 `dev` 更新的版本），因此没有合并。**未编译、未上设备**：该克隆是 partial clone，缺 `PlayerControl.xaml.cs`、`DynamicVM.cs`、`VideoDetailPage.xaml.cs`、`EFontAwesomeIcon.cs` 与 `BiliLite.gRPC` 的 `dynamic.proto`；实跑的是 `firstrun` 15/15 与 `season1` 117 断言。
- **Playback checkpoint survives a pending report (review `c4aa630`):** `cfc4a15` — `CheckpointHistoryAsync` now writes the local position before any report decision (`PlayerControl.xaml.cs:3404`), and `DrainHistoryReportsAsync` reports the latest pending snapshot until it stops changing, so a pause or the exit flush that lands during a 30 s report still stores and reports the position the slider set. Compile only (`-Fast -NoVersionBump -SkipProjectReferences`, 0 errors).
- **A live room no longer answers with a null reference (BR-53):** `c0b7eb1` — the device log of 2026-11-08 12:27 pinned it to `LoadSuperChat` (a `NullReferenceException` right after a 200 on `SuperChat/getMessageList` with no JSON failure) plus two `[JSON][FAIL]` on `gift/v4/Live/giftConfig` whose `data` is now an array. The superchat list and the gift config are both read as array-or-`{"list":[...]}` (null → empty list), entries without `user_info` are skipped, and `Gifts` no longer receives null elements; `bag_list`/`gift-room` shapes stay as they were. Device pass 2026-11-08 13:02 (session `5399351e…`): both `giftConfig` loads 200 with no JSON failure, `bag_list` 200, no null reference anywhere in the session.
- **A dropped danmaku socket reconnects itself (BR-63):** `722555b` — the receive loop in `Modules/Live/LiveMessage.cs` used to exit as soon as `ws.State` left `Open`, so one remote close killed danmaku, gifts and superchat for the rest of the visit (two device sessions on 2026-11-08 show the same `WebSocketException`). `Connect` now loops: connect → join → heartbeat → receive, replaces the aborted socket, backs off 2→4→8→10 s and resets on success; the heartbeat catches its send failures so the `async void` cannot take the app down, and `Dispose` sets a flag first so nothing reconnects after the page. Compile only (`-Fast`, 0 errors).
- **A minimized window comes back through its frame (BR-61):** `bbfaf7f` — the 2026-11-08 13:12 log showed `Application resumed` followed by a no-op `Window.Current.Activate()`, the window still minimized and a suspension 10 s later. `Helpers/WindowRestoreHelper.cs` (added to the csproj) takes the CoreWindow HWND, resolves the `ApplicationFrameWindow` root, restores it with `ShowWindow(SW_SHOWMAXIMIZED)` when `GetWindowPlacement` reported `WPF_RESTORETOMAXIMIZED`, and logs hwnd/root/iconic/zoomed on both sides. Compile only (`-Fast`, 0 errors).
- **The season page gets its detail back (BR-64):** `b643e77` — the page crashed with `ArgumentNullException` at `Utils.ParseArea` because `SeasonDetailVM` built `Detail` from `pgc/view/web/ep/list`, which only returns `episodes`/`section`/`play_strategy`. `SeasonApi.Detail` (`/pgc/view/web/season`) now supplies the model (title/cover/rating/areas/stat/follow state, so `media_id` and `season_id` stop being 0), `ep/list` keeps the playable list and the merged `Detail.episodes` the download dialog reads, and `ParseArea` no longer runs a regex on a null title. Compile only (`-Fast`, 0 errors).
- **The season page shows its content again (BR-65):** `5132840` — the VM was asking `pgc/view/web/season` and `ep/list` with `ep_id`, which is empty when the page is opened from the feed (only `season_id` is passed), so both answered nothing. It now asks by `season_id` and keeps `ep_id` for the initial episode only; `plan/tests/season1` was re-pinned to that contract.
- **Comment text is decoded before it is escaped (BR-66):** `ec92550` — `ControlHelper.StringToRichText` escaped `&` first, turning API `&quot;` into visible `&amp;quot;`; it now runs one `WebUtility.HtmlDecode` ahead of the escape pass, and short reviews decode their `content` too.
- **Empty player pivots no longer appear (BR-67):** `9fdfbf4` — `节点` was visible before any verdict ran and ignored whether nodes had loaded; `本地轨道` counted video/audio tracks that only have combos in manifest playback. Both are now gated on what the panel would actually render, re-judged after `GetNodes()`, and the interaction VM is rebuilt per episode.
- **Release 1.0.9.61 (2026-11-07):** tag `20261107` on the master sync commit; the release asset is
  `BiliLite.Packages_1.0.9.61_x64_Test.zip` (msix, test certificate, `Install.ps1`/`Add-AppDevPackage.ps1`,
  dependencies). Built with the plain script (full Release build, 0 errors); `g5` 335, `playersess` 203.
- **Recommend card tooltips (BR-59):** `ae90881` — the two card-title TextBlocks in `Pages/Home/RecommendPage.xaml`
  (grid `MaxLines=2`, list `MaxLines=3`) were the only truncated card titles without `ToolTipService.ToolTip`; both
  now bind the full title, and no tap handler is attached because the card is item-click navigated. `g5` 332 → 335.
- **Playback progress survives an app close (BR-60):** `ae90881` — progress was written only from `Dispose` (leaving
  the page) and on media end, so closing the window mid-video lost both the local resume point and the history report
  (log 2026-11-07: 8 minutes of playback, no `history/report`, new session). Now a 30 s `DispatcherTimer` checkpoints
  while playing (local position saved, history posted silently), pause saves once, and `App.OnSuspending`/`OnExiting`
  await `PlayerControl.FlushActivePlaybackAsync()` inside their deferrals. `HistoryKey` is the only key builder and the
  resume read uses it too; `ReportHistory` gained a `reportErrors` flag so checkpoints never toast. `playersess` 161 → 203.
- **Reservation copy and attachment labels (BR-57):** `65ca559` — `Parsing/Dynamic/ReserveCardCopy.cs` formats the
  description as `<time> 开播 <n>人已预约` (+ a third tag for `desc3`), tags separated by one space and no brackets,
  rewriting a trailing 直播 and a pending 预约 while leaving 直播中/预约中 untouched; both the attachment summary and
  the standalone Reserve card use it. Attachment labels map `GOODS`→商品、`UGC`→合集、unknown→附件 and never print a
  wire type; an unsupported row with no title, description or destination is dropped. `g5` 265 → 316.
- **Titles open their tooltip on tap (BR-58):** `65ca559` — `Helpers/TapToolTip.cs` attached behavior; a tap on the
  five truncated dynamic titles opens the same full text a hover shows, for 4 s, and closes on mouse exit/unload.
  It reads `ToolTipService.ToolTip` on every tap (so a recycled list item cannot show the previous title) and never
  writes back the attached property; touch `PointerExited` is ignored because it fires on lift.
- **Text reservation posts keep one panel (review `ac6867c`):** `95934db` — the text branch of
  `ParseOneRowInfo2024` promotes the card to `DynamicDisplayType.Reserve` after `DynamicCardProjection.Apply` already
  projected the reserve into `Attachments`, so the standalone template and the attachment panel drew it twice.
  `DynamicCardProjection.ApplyAttachmentOwnership` now runs right after the parse and drops only the reserve summary
  when the type is `Reserve`; photo posts keep their panel, other kinds stay. `g5` 316 → 332.
- **Local playback keeps its directory (review `dae62b7`):** `9c8920f` — `LocalPlayerPage` builds
  `PlayDirectoryBuilder.FromParts` rows from the `PlayInfos` it is handed (title/cid/duration), reports
  availability only above one item, opens and refreshes the drawer on request, switches Parts rows through
  `ChangePlayIndex(index, PlayTargetStart.Resume)`, refreshes the open drawer from `ChangeEpisodeEvent`, and
  releases its three handlers on navigate-from/close. `plan/tests/localdir` 50 assertions; 28 of its 32 page
  anchors fail against the pre-change body.
- **Reserve attachment panel (BR-56):** `a431b4c` — a reserve attachment (`additional.reserve`) on a photo/text
  dynamic now selects `AttachmentPanelTemplate` through `DynamicAttachmentDataTemplateSelector`: BR-55's bordered
  panel, pink 预约 chip, 18/26 title, 14/21 description and the payload's own button. The summary maps
  `reserve.button.text` / `icon_url` (falling back to the check/uncheck icon) and prefers `button.jump_url` over
  `reserve.jump_url`; vote/link/unknown attachments keep the quiet row. `ImageSourceConvert` treats a missing
  parameter as the original image and the standalone Reserve panel now passes `64h.png`, so its icon no longer
  throws. `g5` 232 → 265 (mapping, panel tokens, selector, button visibility; the pre-fix mapping lands on the
  reservation page instead of the live URL).
- **Package version 1.0.9.57:** `284a714` — both `Package.appxmanifest` files bumped to 1.0.9.57; no other change.
- **Live-room guards and the legacy web escape (BR-53, review `8d496eb`):** `2276f28` — every live-room step that
  parses a response now returns before touching a failed model (no `data.message` on a null), and the ordinary legacy
  unsupported card carries `WebCommand` so it opens `t.bilibili.com`; each failing live step still logs `step`/`stage`.
  `g5` pins both: no `HasParsedData(data, "anchor") && data.success` and at least three `WebCommand = WebCommand`
  branches.
- **One list entry in the player:** `35f6905` — 播放目录 becomes the player's only list entry: `BottomBtnList`,
  `EpisodeList` and `ShowPlaylistButton` are removed, `EpisodePivot` is renamed `NodePivot` and shows only for
  interaction videos, and multi-part switching goes through the directory; `playertoolbar` rewritten.
- **Reserve card and title tooltips (BR-55):** `09ee480` — the 预约 template is one bordered panel with a pink chip,
  two-line title and description and the payload's own button, and every trimmed card title carries a tooltip.

- **Player entries hide when empty:** `4dc85ce` — `Services/PlayerSurfaceVisibility.cs` decides 弹幕列表 / 选集 button /
  选集 tab / 节点 / 本地轨道从 the signals the player already holds (playlist count, interaction, four local track counts,
  loaded danmaku rows for the current item, a local danmaku file, whether the load settled); the danmaku pool carries its
  item so another item's rows cannot show the entry. A selected tab that just became hidden falls back to 弹幕, then to the
  first visible tab. `plan/tests/playertoolbar` (90 checks; 23 fail against the always-visible behaviour).
- **Photo grid and attachment row (CARD_PLAN §6/§7):** `798a03a` — one `DynamicImageGrid` for every count ≥ 2 with
  width-derived tiles (88-DIP fallback to two columns, nine-tile cap with `+N`, shared save menu), geometry in
  `DynamicImageGridLayout`; the card shell gains one attachment row whose 打开 action needs both a destination and a
  command. `plan/tests/cardlayout` 761 checks (40 fail against the old fixed-tile geometry), `g5` 200 after a
  probe-only anchor repair.
- **BR-54 toast pill:** `01e51a7` — horizontal padding 16 → 6, the pill height stays 38, and a negative
  `BaseContentOpticalOffset` moves icon and text together through `ContentOffset`; `toastalign` 41 checks.
- **Feed window and repost bound:** `65fd423` — cards get one shell title slot fed by `major.opus.title` (web and both
  gRPC opus summary sources), so a preview that prints its own title stays single; originals stop at two card-data
  levels with the deeper ancestor collapsed into one 查看原动态 row carrying its dynamic id, and an origin block with
  no card draws no empty frame; previews reserve a 16:9 or natural box before the image loads; the article feed
  parses through the shared web page parser; every publish point dedupes by dynamic id; the list keeps a ten-page
  rolling window with anchor protection and reload checkpoints.
- **Settings gating:** `7e628c3` — the video-format hints render only for HEVC/AV1, the CDN node list and latency test
  collapse while replacement is off, the sample switch disables itself without rewriting the saved value, re-entering
  a pivot refills the page from storage behind a guard instead of re-subscribing every `Loaded` handler, and the
  local `Assets/Text/help.md` is rewritten against the current tabs and controls.
- **Review finding — a login no longer inherits the previous account's token:** `14ac4c3` — `RefreshTokenPolicy`
  decides the owner mismatch before the response-authoritative branch, `PersistCredentials` deletes the previous
  generation's `ac_time_value` when the uid changes, `ReadToken(sessionOwnerId)` refuses a contradictory record,
  `GetRefreshToken` passes the session owner (the only submit path), and a hand-entered token records `manual`
  provenance with the session owner. `auth3` 188 → 218 assertions.
- **Review findings — reused photo body and the reservation lottery line:** `2072d64` — `RichTextBodyState` makes
  the mounted child a function of the current `Content`/`Text` on every change including empty, so a recycled card
  cannot show the previous body; the gRPC mapper maps `desc_text3` and the visible reservation description joins
  all three slots through `JoinText`, which now also drops an exact repeat. `g3` 135 → 176 assertions.
- **Probe truthfulness — BR-31 checked on labels only:** `30bc06a` — `auth1`'s ban on `（ ： ·` covered the whole
  account block and failed on a legitimate help line; it now scans label text and exempts `SettingHelp`/`ToolTip`
  subtrees. A mutation inside a label still fails it.
- **Review finding — a rejected cookie write reported as installed:** `b4cd995` — `Services/CredentialCommitter.cs`
  owns the D2 commit (four-value snapshot/rollback plus per-cookie accounting); a `false` from
  `CookieManager.SetCookie` now counts as a failure exactly like a throw, so `CookieInstalled` is only true when
  at least one write was accepted and none failed and the existing `PartlyCommitted` path reports it. `auth3`
  173 → 188 assertions, with the pre-fix body failing the rejected-jar case.
- **Settings dead controls and the API-key save bug:** `c4c683f` — SETTINGS_PLAN slice 2: the collapsed 语言
  dropdown, the theme-colour panel (`cbColor` was populated before `Loaded`, so its handler could only fire on a
  selection no user could make), the DefaultTheme/示例主题 expander, the disabled `api_platform` combo, the
  注销 accesskey button, the 极验 login test form with its page-private members, and the commented
  外站替换视频 / Akamai 优先 blocks are gone with the code only they reached. Opening the page no longer writes:
  it read `api_web_top` for the `api_web_secondary` box and wrote both keys back on load, which overwrote a
  custom path prefix; only 应用参数 writes now, and it reads the key it displays.
- **Dynamics card layout:** `48c5df5` — CARD_PLAN slice 2: `DynamicItemControl.xaml` is one reading column
  (`MaxWidth` 800, one margin/padding token block at the top of the file) with header | body | content | action
  rows at full width instead of content indented by the 56px pendant column; header collapses to an overflow menu
  under 480 of the card's width, the action row is three star columns, body metrics 16/24, and OneRow's preview
  is a 160-capped media column with a `2*` text column (`CoverWidth` deleted with its seven assignments).
  Ten items need the maintainer's eyes — they are listed in the commit's handoff, not claimed.
- **REFRESH D4/D5 — credential identity:** `75fe40f` — `Services/RefreshCredentialPolicy.cs` holds the only
  decision about the saved web refresh token (`RefreshTokenPolicy.Decide`, a pure function over the saved value,
  its recorded source, the session owner, the cookie candidate and a pending-confirmation flag) plus
  `RefreshTokenMirror`/`AppkeyPolicy`. A response token is authoritative for its session, cookie `ac_time_value`
  adoption only happens when nothing is saved, an owner mismatch refuses the import, and both former mirror
  implementations now call the one unit. `SaveLogin` records which configured appkey acquired the pair (Login for
  password/SMS, Tv for QR, UnknownLegacy otherwise) and `AccountApi.RefreshToken` signs with that same key.
  `auth3` 130 → 173 assertions; `auth1`'s pre-D4 assertion was re-stated as the keep-path plus the migration path.
- **Settings structure (SETTINGS_PLAN slice 1):** `ed5fe9f` — ten tabs in use-case order, controls moved to the tab
  their use case belongs to, each tab grouped with the page's own header/label/help styles, labels stripped of
  parentheticals and duplicated icons with the text moved to the help line. The 直播视频格式 group is deleted:
  it wrote `livevideoformat` that only the page read back, while the live request builds its format from
  `PlayerAPI.videofmt`. Labels, dead-control removal and save errors beyond that one group are the next slice.
- **Dynamics body (CARD_PLAN slice 1):** `2d1c4b3` — web and gRPC mappers keep `additional`, OPUS summary/covers,
  draw pictures, `major.ugc_season`, `no_comment`/`no_forward` and the text-node vocabulary; `Parsing/Dynamic/
  RichTextFlow.cs` joins nodes into one paragraph with the source's own breaks, no character cap and the link
  label kept apart from its target; photo hosts and `ShowContent` read the body; the dead markup builders and the
  reserve `??`/`+` precedence bug are gone. `g3` 73 → 135 assertions, `g5` 44.
- **BR-47 Hot card height budget:** `3ec04aa` — tag on the thumbnail (one ellipsised line, collapsed by its text
  rather than its style object), title plus both metadata lines in a text column, slot 100 → 132 and a sub-480
  stack at 296/320. Layout-visible, so it closes on the device.
- **Probe batches repaired after the unified run:** `488a978` — `g4`'s stub gained the silent `TryGetJObject`
  (the toasting `GetJObject` on top of it), and `ux1c`'s request-builder assertion, which had silently degraded to
  comparing empty strings after `1838108` made the method `async`, now anchors on the surviving signature tail.
- **BR-46 keyboard focus after a switch:** `50bb009` — a `PivotHeaderItem` handles Space itself and every advance funnels through
  `InitializeVideo`, so after auto-next the focus sat on a tab header and Space switched tabs. `PlayerControl.FocusPlayer()`
  is the one entry over the existing `BtnFoucs` sink, called from `InitializeVideo`'s success path only when
  `IsTabStripFocused()` says the focus is on a `Pivot`/`PivotHeaderItem`. `Timer_focus_Tick` keeps its four-type set on
  purpose — a timer that pulls focus off tab headers would break switching tabs from the keyboard. `plan/tests/focus` (33 checks).
- **P4 step 3 — video detail:** `e6c2fb5` — `Models/Api/Video/` + `Parsing/Video/` own web view, related, tags, relation
  stat/attribute, the four state reads, the action endpoints and the favourite-created list. The fallback chain (web → signed
  app → proxied) and the relates merge are decisions over parse results; a failed related read logs its category instead of
  nulling the page; the `relates` setter is null-safe so a wire null no longer throws; `short_link`/`owner_ext`/`pages`
  defaults live in the mapper and `req_user` is never copied from the wire. The `biliproxy` proxy stays as a documented
  last-resort source. `plan/tests/c1` 52→161.
- **Closed-page latch probe refreshed:** `93a8ed3` — `plan/tests/stab` re-pointed at the single-flight paging channel and the
  collection sequence (`AdvanceSequenceAsync`), since `AdvancePlaylistAsync`/`AdvanceUgcSeasonAsync` no longer exist.
  15 checks, 55 assertions, each rewritten anchor shown red with its guard removed.
- **P4 step 5 — comments:** `f03078c` — `Models/Api/Comment/` DTOs + `Parsing/Comment/` (`WebCommentPageParser`,
  `WebCommentActionParser`, `WebCommentPageMapper`). A body without a business code is an envelope failure
  instead of `code = 0` success; the pinned-reply dedupe and the two cursor shapes (`cursor.next_offset` vs the
  legacy `pn`) live in the mapper; `CommentControl` toasts by category and keeps `_page`/cursor source-bound.
  Dead `dataCommentModel`, `CommentItem` and `Comment_New` deleted. `plan/tests/comments` (99 assertions).
- **PLAN item 1 — player session ownership:** `2c3fc09` — `Services/PlayerSessionState.cs` (Begin/Accept/
  Invalidate/Hold/Release/IsCurrent) makes every open carry a session; resources sit in a per-session ledger
  until accepted, an invalidated session releases once, and a resource born after invalidation is released on
  the spot. All 12 open paths are bound, the quality change is awaited, the engine fallback is single in flight,
  and stale continuations/`MediaOpened`/`MediaFailed` are rejected. `plan/tests/playersess` (69 checks).
- **P4 step 2 — playlists:** `1838108` — `Parsing/MediaList/` parsers + mappers for medialist (signed/unsigned),
  favorites and watch later; the service keeps source policy and single-flight but no longer decodes, toasts or
  collapses failures. The toast moved to the publishers through `MediaListFailureText`; an absent `media_list` is
  its own stage and the only case that spends the unsigned retry. `oid = last aid`, `has_more`, `HasKnownTotal`
  and `MediaListPagingState.Append` untouched. `plan/tests/c2` 99→162, `c3` 97→117.
- **P4 step 1 — season detail:** `ed9f248` — `pgc/view/web/ep/list` runs builder → `ApiEnvelope.ReadResult` →
  `Parsing/Season/WebSeasonEpListParser` → mapper, with the field table and the 正片/预告 dedupe rule in the
  parser. `HttpResults.TryGetJObject` is the silent body decode the envelope uses, so a non-JSON body is an
  envelope failure instead of `解析json对象失败`; `GetJObject` keeps its toast for its existing callers on top of
  the same decode. `plan/tests/season1` (109 assertions) pins the envelope, the shapes that used to throw, a
  per-body no-toast check, the dedupe divergence and a refresh that fails after a good load.
- **BR-43 toast alignment, second attempt:** `78a94cb` — the device pass on `a99c3f8` still showed the text lower
  than the icon, so the shared forced line box is gone: both blocks use `TextLineBounds=Tight` with no
  `LineStackingStrategy` and no `LineHeight` at all, the pill keeps its old 38-DIP outer height through a scaled
  `ToastRoot.MinHeight`, and `BaseIconOpticalOffset` is the one residual constant (now 0). `plan/tests/toastalign`
  (36 checks) pins those invariants and exits 1 against `a99c3f8`.
- **BR-44 播放目录 crash:** `a99c3f8` — the fade now targets the button's content
  `StackPanel` and the scale stays on the `CompositeTransform`, so `Storyboard.Begin` no longer throws before
  `DirectoryRequested`; a `Begin` failure is logged instead of swallowing the click. `plan/tests/dirpress`
  (31 checks) reads the XAML names and fails when a `SetTargetProperty` names a property the target type does
  not have — it exits 1 against the pre-fix source. Device-confirmed 2026-11-01.
- **BR-45 list covers:** `433f128` — directory rows, the playlist template, the UGC-season template and the
  live honour icon route their string cover through the existing `imageConvert2` + `'200w'`, so the box gets a
  `BitmapImage` and an empty `Cover` falls back to the placeholder. `DownloadPage` keeps its binding: that model
  already stores a `BitmapImage`.
- **Cookie jar and packaging traps:** `fe335f3` — `Models/CookieOrigins.cs` is the one origin list (read across
  root/www/passport/api, write to root and www) so `ac_time_value` is visible, plus one verdict log line;
  `862e61c` — the build script prints only the installable `BiliLite.Packages_*`, fails before printing
  when it is missing, and deletes the `BiliLite.UWP_*` byproduct that installs into a launch crash.
- **Drawer polish from the device pass:** `5ed8d17` — `Models/DirectoryDrawerMotion.cs` (close token so a
  reopen during the close is never hidden, snap when animations are disabled), a scrim before
  `DirectoryHost` that closes on a tap and swallows the gesture, the 目录 button flashing with an active
  background and one detail-column highlight when the pivot already sits on the target page, a single
  header title (`ShowsSourceTitle` keeps the name only for 正片/预告), crosshair + 正在播放 for the locate
  action, 8px between the playing bar and the ordinal, and tooltips on truncated titles.
- **Login refresh:** `ea7ead8` — `CookieService.NeedsRelogin`/`ReloginRequired`, single-flight
  `RefreshCookieAsync` resetting its slot before releasing waiters, `MirrorRefreshToken` reading
  `ac_time_value` from the jar on every login path, an awaited preflight on both playurl paths, the
  `App.Resuming` hook plus one launch preflight, and 设置 → 账号 with 登录状态/刷新登录/重新登录/退出登录.
- **Collection and playlist navigation (P0–P2 season):** `67b9eae`, `399807d`, `6c9b39c`, `e9e5d2a`,
  `18c519d`, `d905726`, `f68b705`, `1dd359c`, `da62afc`, `4db379c`, `503ceae`, `d0d6bd9`, `0e5b0df` —
  one sequence state for next/previous/auto, a player-hosted 目录 drawer that works in full screen,
  pending/retry/late-result rules, honest counts, one single-flight paging channel with 加载全部,
  search by stable row identity, season 正片/预告 sections, and named queues. Copy rules recorded in
  `bug_report.md` BR-31; the reviewer's open finding became BR-32 and was fixed.

- **Response-evidence workflow:** capture of response shapes and drift behind two diagnostics toggles,
  redacted opt-in samples, one-click zip bundle with `manifest.json`, and `tools/import-capture.ps1`
  turning a bundle into probe fixtures. Finished plans and their references were removed.
- **N1 — player overlay toasts:** `312fa23` — `Controls/PlayerToast.xaml(.cs)` and
  `Services/PlayerToastService.cs`, keyed overlays (volume, brightness, seek, message, acceleration,
  speed) replacing the central `ToolTip` label, one service per `PlayerControl`, timers stopped in
  `Dispose`; build 0 errors.
- **Release tooling:** `build-release-x64.ps1` now captures the whole `-> … .msix` path (spaces
  included), derives the two expected package paths from `OutputDir` + version, refuses to continue
  when neither `BiliLite.UWP_*` nor `BiliLite.Packages_*` is found, and names the `CS2012` holders
  through Restart Manager before retrying. Verified with a full packaging run into `H:\uwp\pkg diag`.
- **BR-16 — quiet load-more:** `e5c97e1`, `261317f` — silent page-2 failure, legacy text in the only
  toast, gRPC cursor preserved so the retry resumes.
- **API-SMART-01:** `797dc3a`, `26d4e57`, `3665bdb` — adaptive gRPC/legacy selection for six dynamic
  paths, persisted `dynamic-api-history.v1.json`.
- **C1/C2/C3:** `92ecd6a`, `34ec312`, `c735a73` (+`18f71dc`) — collections, playlist paging, playlist
  links and continuation.
- **D1/D2:** `5e36ec2`, `e94c843` — danmaku list with copy/like, filter import/export.
- **G1–G5:** `cfcd3de`/`75e8ecb`, `914d32f`, `d4c6345`(+`6ae1981`), `65d5e8c`(+`4fca416`),
  `b39739a`(+`9fb3d16`).
- **Stability 1/3/4:** `96de3fc`, `0668875`, `546b409`.
- **Home-feed REST fallback (BR-13):** `5881dbb`.

## Open

| # | Item | State |
| --- | --- | --- |
| 1 | Player open/replace/close session ownership | **done** — `2c3fc09`; its device rows are 默认通过（维护者 2026-11-04） |
| 2 | Background playback ownership (SMTC + memory budget) | measurement-gated — BR-68窄源码修复完成；BR-21 Windows50-cycle process/root/native/GPU归因仍未测量。先按 [VIDEO_MEMORY](../document/VIDEO_MEMORY.md) 取证，再审background/SMTC/global rewrite |
| 4 | API parsing structure (P4 other features, P5 retire the rest) | P0–P3 done: one `ApiEnvelope` reads every REST response, `WebDynamicFeedParser`/`LegacyDynamicFeedParser` own all dynamic sources, gRPC mapping is protobuf-only. P4 steps 1, 2 and 5 shipped: season detail `ed9f248`, playlists `1838108`, comments `f03078c` — each with a parser/mapper layer, DTOs under `Models/Api/`, a field table beside the parser and its own probe (`season1` 109, `c2` 162 + `c3` 117, `comments` 99). Remaining for P4: **account** only (step 4), which the maintainer is holding back; the video-detail step shipped in `e6c2fb5` and `c1` grew to 161 assertions. Order and traps: `plan/P4_INVENTORY.md` §5 |
| 5 | Collection and playlist navigation / UI | **done** — see the first Done entry; its device rows are 默认通过（维护者 2026-11-04） |
| 6 | Auth refresh defects — `plan/REFRESH_PLAN.md` | **shipped**: D2 `6ee8a81`, D4/D5 `75fe40f`, review findings `b4cd995` (rejected cookie write) and `14ac4c3` (a login is a new credential session), D3 `bba2df4` (explicit, resumable web-refresh transaction with a persisted pending record) and D1 `000bfd0` (cookie-to-app bridge plus one recovery decision table for Home and startup), logout cleanup `e0e454e`, and the review finding `ff26db7` (an account switch drops the saved web token even when the record carries no owner). Device rows are in `VALIDATION.md` and are 默认通过. P4 step 4 (account) stays held back by the maintainer |
| 7 | Hot tab card overflow — `plan/HOT_CARD_PLAN.md` | **shipped** in `3ec04aa` (BR-47). Layout-visible; its device rows are 默认通过（维护者 2026-11-04）, including the 150%/200% text-size case |
| 8 | Settings reorganization — `plan/SETTINGS_PLAN.md` | **shipped through 1.0.9.42**: slices 1–4 (`ed5fe9f`, `c4c683f`, `b9b58c5`, `0f11162`) plus `7e628c3` (HEVC/AV1 hints only for those formats, the CDN list and latency test collapse while replacement is off, the sample switch disables itself without rewriting the saved value, re-entering a pivot refills from storage instead of re-subscribing every `Loaded`, and `Assets/Text/help.md` rewritten against the current tabs). Device rows are the 1.0.9.42 section in `VALIDATION.md` and have not been run |
| 9 | Dynamics card contract — `plan/CARD_PLAN.md` | slices 1–4 and three review findings shipped: `2d1c4b3` (data), `48c5df5` (layout), `2072d64` (body mounting on reuse + the reservation lottery line), `1b51db4` (§4 renderer: paragraph flow, emoji variants, measured expansion, empty-body collapse) and `0b8ad87` (§3: explicit wire kinds, URL normalisation, attachments, comment-target pairs); `65fd423` closed the Opus title slot, the preview ratio box (§6), the two-level repost bound, the article branch through the shared page parser, ID dedupe on append and the ten-page feed window (§9). `798a03a` then shipped §6 (one `DynamicImageGrid` for every count ≥ 2, width-derived tiles, 88-DIP fallback, nine-tile cap with `+N`, the shared save menu) and §7 (one bounded attachment row with an open action only for a real destination). `1.0.9.50` then closed the §2 P1 unknown-kind row (`Parsing/Dynamic/UnsupportedCard.cs` keeps the outer card's author, time, text and id; `Other` renders in the shared shell with 打开原文 only when an id and a command exist). Remaining: the §9 measurement only |
| 10 | Comment popup dark-mode color values — BR-62 | **shipped** — `44d7f0e`. The dialog is a `Popup` child, so it now pins `RequestedTheme` from the host `Frame`'s `ActualTheme` (the reason `MessageToast`/`PlayDirectoryControl` pin one); `Utils.GetBrush` keys its dictionary off the same `ActualTheme`. Five tokens (`CommentDialogLayer`, `CommentDialogBorderColor`, `CommentDividerColor`, `CommentBandColor`, `CommentBadgeColor`) carry the window's neutrals — Light unchanged, Dark the same alpha on white plus the player drawer's `#F21A1A1A` sheet — and the two sort links use `HighLightTextColor`. Compile only (`-Fast -NoVersionBump -SkipProjectReferences`, 0 errors); device-confirmed 2026-11-08 (maintainer) |
| 11 | Playback checkpoint survives a pending history report (review `c4aa630`) | **shipped** — `cfc4a15`. The local position is written before the report gate, only the network report is coalesced, and a single drain loop keeps reporting while the pending snapshot changes; the lifecycle flush awaits that tail. Compile only (`-Fast -NoVersionBump -SkipProjectReferences`, 0 errors) |
| 12 | Live danmaku socket reconnects itself — BR-63 | **shipped** — `722555b`. `Connect` is a supervised loop (connect → join → heartbeat → receive): a dropped or close-framed socket is replaced with a fresh `ClientWebSocket`, the retry backs off 2→4→8→10 s and resets to 2 s on success, the heartbeat's `async void` can no longer throw out of `SendAsync`, and `Dispose` sets a flag first so no reconnect or new socket follows the page. Compile only (`-Fast -NoVersionBump -SkipProjectReferences`, 0 errors) |
| 13 | Minimized window returns maximized (BR-61) | **shipped, awaiting the device** — `bbfaf7f`. The previous `d0e7168` attempt was reproduced on 2026-11-08 13:12: `Application resumed` fired, `Window.Current.Activate()` was a no-op, the window stayed minimized and the system re-suspended the app 10 s later. `Helpers/WindowRestoreHelper.cs` now reads the CoreWindow HWND, resolves the `ApplicationFrameWindow` root with `GetAncestor(GA_ROOT)`, and restores with `ShowWindow(SW_SHOWMAXIMIZED|SW_RESTORE)` + `SetForegroundWindow`; `GetWindowPlacement`'s `WPF_RESTORETOMAXIMIZED` decides maximized-vs-normal because a minimized window reports `IsZoomed == false`. The resume/activation paths log hwnd/root/iconic/zoomed before and after. Compile only (0 errors) |
| 14 | Season page detail comes from `pgc/view/web/season` (BR-64) | **shipped** — `b643e77`. `ep/list` only returns `episodes`/`section`/`play_strategy`, so the whole `Detail` model (title, media_id, rating, areas, stat, user_status) was empty and `Utils.ParseArea(null, …)` crashed the page on `Regex.IsMatch`. `SeasonApi.Detail` now reads the season endpoint, the VM takes `Detail` from it and keeps `ep/list` for the episode/preview lists (and for `Detail.episodes`, the merged list the download dialog needs), `title` falls back to `season_title`, and `ParseArea` skips the 僅…地區 regexes on an empty title. Compile only (0 errors) |
| 15 | Season page requests use the season id (BR-65) | **shipped** — `5132840`. `LoadSeasonDetail` received `ep_id`, which is empty for every in-app entry point (they pass only `season_id`), so both `pgc/view/web/season` and `pgc/view/web/ep/list` were asked with an empty id and the page published an empty `Detail`/`Episodes`. The VM now takes `season_id`, asks both endpoints with `SeasonIdType.SeasonId`, and keeps `ep_id` only to pick the initial episode; the season1 probe follows the new contract. Compile only (0 errors) |
| 16 | Comment text is HTML-decoded before markup (BR-66) | **shipped** — `ec92550`. `ControlHelper.StringToRichText` escaped `&` first, so API text already carrying `&quot;`/`&amp;` rendered as literal entity text; it now decodes once with `WebUtility.HtmlDecode` before the escape pass, and `SeasonReviewVM` decodes short-review `content` the same way. Compile only (0 errors) |
| 17 | Empty player pivots stay hidden (BR-67) | **shipped** — `9fdfbf4` + `b95f2ab`. `NodePivot` was visible by default and the verdict only looked at `IsInteraction`; `Visibility` turned out to be the wrong lever because a `Pivot` draws a header for every entry in `Pivot.Items` regardless of the item's visibility (`LocalTracksPivot` had been `Collapsed` all along and still showed). The tabs are now removed from / inserted back into `Pivot.Items` at slots 0/1 — dropped in the constructor, so nothing shows before the first verdict — gated on the loaded node count and on manifest playback for the video/audio rows, re-judged after `GetNodes()`, with the interaction VM rebuilt per episode. Probe updated. Compile only (0 errors) |
| 18 | FI1 PGC index initial/result ownership — BR-70 | **requirements PASS / UNIMPLEMENTED**；[FI1 amended review](../cloud/rounds/20261125-api-shape-comparison/followup/fi1-review.md)。本批释放/fresh safe baseline/精确SeasonIndexVM+season1 grant后才实施；保留conditions互斥、reentrant通知与grid autofill，samefixture red→green/owning/fresh/common gate |
| 19 | FD1 polymer dynamic offset query value — BR-71 | **requirements PASS / UNIMPLEMENTED**；[FD1 review](../cloud/rounds/20261125-api-shape-comparison/followup/review.md)。本批释放/fresh safe baseline/精确WebDynamicFeedApi+API02 grant后才实施；保留null/empty/source cursor与byte-identical parser/mapper，samefixture red→green/owning/fresh/common gate |

**Order** (dispatcher, 2026-11-02, updated after the parallel wave): the engine-fallback disposal finding
(`0b70113`) is closed; D2 and D4/D5 are in; the first slices of the Hot-card, settings and dynamics plans are in
and were 默认通过. `plan/REFRESH_PLAN.md` is fully shipped (D1 `000bfd0`, D3 `bba2df4`, logout cleanup `e0e454e`), so the auth
row has no code left, and its device rows are 默认通过. `plan/SETTINGS_PLAN.md` slice 3 (`b9b58c5`) and `plan/CARD_PLAN.md` §3/§4 (`1b51db4`, `0b8ad87`) are in and were 默认通过. `7e628c3` then closed the settings row and
`65fd423` closed all but two card items, and `798a03a` closed those two (grid, attachments). `4dc85ce` then
taught the player to hide entries whose content does not exist. Next: `plan/CARD_PLAN.md` §9 measurement with
BR-21 (native50-cycle collection/attribution; narrow source work is recorded above), with FI1/FD1 following current-batch release and a fresh safe grant, then whatever the 1.0.9.4x device pass reports. BR-53 (any live room shows
a `NullReferenceException` message) is recorded in `bug_report.md` and waits for a log line before any live-room
code changes; BR-54 (toast pill width and content centring) is fixed in `01e51a7` and waits only for the device
look.
Item 2 stays parked until BR-21 has measurements; use [VIDEO_MEMORY](../document/VIDEO_MEMORY.md) and preserve the source/measurement distinction; P4 keeps only the account step, still held back. The 1.0.9.42–1.0.9.60 device rows are the next device pass (BR-56, local playback, the BR-57 copy row and the BR-58 tooltip row are the 1.0.9.59 rows; the BR-59 recommend-tooltip row and the BR-60 progress row are the 1.0.9.60 rows). 2026-11-08: BR-62 (comment popup dark color values) is in as `44d7f0e` and device-confirmed by the maintainer. Row 11 (review `c4aa630`) shipped as `cfc4a15` and BR-53 as `c0b7eb1`; BR-53 is device-confirmed (2026-11-08 13:02), row 11 still wants the pause-then-exit check. BR-63 (live danmaku reconnect) shipped as `722555b`; it needs a real disconnect before it counts. BR-61's first attempt failed on the device and `bbfaf7f` replaces it with the Win32 restore.

## 已知债务（2026-11-06，从源码 TODO 收拢）

源码里原先有 10 处 `TODO/FIXME`，已全部清掉。其中三处是真实欠账，记在这里而不是留在代码里：

| 位置 | 欠账 | 为什么现在不做 |
| --- | --- | --- |
| `Controls/Player.xaml.cs` | 播放器类写得过于复杂，需要整体重写 | 重写会动到 BR-1/BR-21/BR-43/BR-54 都已验收过的播放路径，没有设备测量支撑不做；本轮窄修复不解除整体重写门禁，先按 [VIDEO_MEMORY](../document/VIDEO_MEMORY.md) 取证 |
| `Controls/Dynamic/DynamicParse.cs`（`MediaList` 分支） | 旧版合集卡的解析仍是逐字段手写，样例 `https://t.bilibili.com/625835271145782341` | 现代合集已走共享解析器；旧路径只在老接口回退时命中 |
| `Controls/FontAwesome5/FontAwesome.cs` | 多窗口下改字形可能撞线程约束，当前只 `Debug.WriteLine` | 只在多窗口改图标时可能复现，没有设备复现步骤 |

其余 7 处是过时注释或死代码，直接删除：`PlayerControl` 的"本地播放"（该分支已在播）、`VideoPlayHistoryHelper`
的"或许该用数据库"、`RecommendVM` 的重复加载提示（守卫就在下面）、`DynamicVM.OpenTag`（已经会打开话题页）、
`NewPage.xaml` 的折叠"自定义快捷入口"菜单项，以及 `MainPage` 里空的挂起循环与从未订阅的 `GoBack` 处理器。

## 1. Player session ownership

`PlayerControl` opens media in several paths (native DASH, FFmpeg DASH, quality change, engine
fallback) and mutates shared player fields across `await`s. Closing the tab or switching video while
an open is in flight lets the old operation resume against a replaced/cleared player, attach A's
source to B's player, or leak resources created after the close routine finished. `SetQuality` starts
`ChangeQuality` without awaiting it; `Player_ChangeEngine` starts another open on media failure with
no in-flight guard.

Fix shape: one session id per open, new resources kept local until the session is accepted, pending
opens invalidated on close/replacement, resources of an obsolete session disposed, callbacks from
obsolete players rejected. Do not rewrite the player.

## 2. Background playback ownership

`backgroundMediaPlayback` is declared in both manifests and `PlayerControl` wires Play/Pause and
status, so missing permissions are not the issue. The gap is ownership: system media controls are
view-scoped, and `OnUnload` disables them for every control, so a departing tab can disable the
controls of the player that is still playing. `App.OnSuspending` also does no player work and forces a
finalizer wait inside the suspension window.

Fix shape: one active playback owner keeps the SMTC session across tab and visibility changes, optional
visual/danmaku work is released when backgrounded, and suspension work stays bounded. Do not recreate
the player on every foreground return.

This broader background/SMTC design still has no measured Windows evidence. Narrow close/replacement/subscriber/temporary-resource source work is recorded in Done and does not establish the user's RAM trend. Follow [VIDEO_MEMORY](../document/VIDEO_MEMORY.md) to separate ordinary close from active retained playback, tab visibility, minimize/suspend and feed growth before a broader refactor. Native cache delivery and real playback/SMTC remain unverified.

## 4. API parsing structure — names, fields, and methods

**Goal:** give each API response a clear route from transport to an application model. A caller should
know which endpoint produced a field, what that field means, and which method parses it. Start with
the dynamic feeds implicated in BR-17; migrate working features incrementally.

### Responsibilities and locations

The first implementation batch should inventory the existing paths before moving anything:

- `Api/` holds request builders. The general web dynamic feed now lives in
  `Api/User/WebDynamicFeedApi.CreateFeedRequest`, so the old `ArticleV2` name is gone.
- `HttpResults.GetJson/GetJObject/GetData/GetResult` in `Helpers/HttpHelper.cs` still decode responses
  and sometimes show toasts; `ApiDataModel<T>` and `ApiResultModel<T>` describe two different
  response envelopes, and neither is the parse result.
- `Helpers/DynamicGrpcMapper.cs` is protobuf-only again: the web half moved to
  `Parsing/Dynamic/WebDynamicFeedMapper.cs` with the shared scalar readers in `WireScalars.cs`.
  The two feed VMs decode through `Parsing/Dynamic/WebDynamicFeedParser.cs`; legacy REST responses are
  still deserialized inline, which is what P3 separates.

Use these boundaries for migrated features; the paths are proposed destinations, not files added
by this planning change:

| Boundary | Responsibility | Proposed location |
| --- | --- | --- |
| Request | Endpoint, parameters, authentication/signing, request construction | `Api/<Feature>/` |
| Transport | HTTP status/body or gRPC bytes/status; no interpretation of feature fields | Existing `HttpHelper` / `GrpcRequest` |
| Wire DTO | One source's envelope and payload fields; explicit JSON names | `Models/Api/<Feature>/<Source>/` |
| Parser | Decode and check the envelope/field shapes; return data or a parse failure | `Parsing/<Feature>/` |
| Mapper | Convert a source DTO or generated protobuf reply into the existing application model | `Parsing/<Feature>/` |
| Service / VM | Choose sources, own paging and request generations, publish rows and user messages | Existing services and VMs |

Keep the current HTTP library, protobuf generator and renderer. Do not introduce a second transport,
a dependency-injection framework, or a repository-wide rename to achieve these boundaries.

### Naming contract

| Kind | Convention | Dynamic-feed example |
| --- | --- | --- |
| Endpoint/request builder | `<Feature>Api`, with source named when variants coexist; builders are synchronous | `WebDynamicFeedApi.CreateFeedRequest(...)` |
| Network operation | `Get...Async` / `Load...Async`; `Async` means actual asynchronous work | `GetFeedPageAsync(...)` |
| Source DTO | `<Source><Feature><Payload>Dto`; C# properties use PascalCase and explicit wire-name attributes | `WebDynamicFeedItemDto`, `[JsonProperty("id_str")] DynamicId` |
| JSON parsing | `Parse<Source><Payload>` returning `ApiParseResult<T>`; no network or UI work | `ParseWebDynamicFeed(...)` |
| Conditional scalar read | `TryRead<Type>`; failure is distinguishable from a legitimate zero/false | `TryReadInt64`, `TryReadBoolean` |
| Source mapping | `Map<Source><Payload>`; share domain helpers only after source differences are handled | `MapWebDynamicItem`, `MapGrpcDynamicItem` |
| Normalized page | `FeedPage<TItem>` with `Items`, `NextCursor`, `HasMore` and source identity | Existing card models remain the items |

`ApiParseResult<T>` is a proposed small result type containing the parsed value or a failure category
and context. Do not treat `ApiDataModel<T>`, `ApiResultModel<T>`, `ReturnModel<T>` and
`DynamicApiSourceResult` as interchangeable: wire envelopes, parsing results and source-selection
results serve different purposes. Keep compatibility wrappers while callers migrate. Replace names
such as `ArticleV2`, misspelled builders and source-ambiguous mapper methods when their feature is
migrated; do not rename unrelated APIs in the same batch.

### Field contract

Maintain a compact field table beside each migrated parser: logical endpoint/source, wire path,
wire type(s) actually observed, normalized property/type, required/optional status, missing/null
behaviour, and conversion rule. Include the caller or fixture that establishes the rule.

| Field family | Rule |
| --- | --- |
| Envelope | Explicitly select `data` or `result` for the endpoint. A missing or malformed `code` is not implicit success. Normalize `message`/`msg` at this boundary. |
| IDs | Use descriptive names (`DynamicId`, `Aid`, `Cid`, `UserId`) and keep opaque IDs/cursors as strings. Convert to numeric IDs only where the endpoint contract requires it. |
| Nested values | Check object/array shape at every boundary. Missing, JSON null and the wrong token type are distinct; `?.` alone does not make a `JValue` indexable. |
| Numbers and flags | Centralize culture-independent readers. Accept numeric strings or `0`/`1` only where the endpoint's observed contract permits them; never turn a failed required parse into zero/false. |
| Counts and time | Separate display text such as `1.2万` from a numeric count. State timestamp units and distinguish epoch times, durations and presentation labels. |
| Optional content | Apply a documented default or preserve text when optional metadata is unusable, such as a string-valued `emoji`. Unknown card types should retain an existing unsupported-card placeholder. |
| Paging | Keep web `offset`, legacy dynamic IDs and gRPC `historyOffset/updateBaseline` separate and source-bound. A failed append keeps the last successful cursor. Server `has_more`, cursor advancement and filtered rows are separate facts. |

Use DTO deserialization for stable payloads and typed `JToken` readers for genuinely polymorphic
fields. Neither method should require serializing a token back to text merely to parse it again.
Add a shared `ReadObject`/`ReadArray`/scalar helper only when it replaces duplicated rules; keep
endpoint-specific aliases, units and defaults inside that endpoint's parser.

### Parsing and failure contract

1. Transport reports HTTP/gRPC status independently of the response body.
2. Decode once, check the endpoint's envelope, then parse its payload. Separate transport failure,
   server/business rejection and malformed payload; cancellation or a superseded request is not a
   parser failure. A valid empty page is not malformed.
3. Map the source payload into the existing application model. Missing optional metadata may
   degrade one card; a broken required envelope makes that source attempt fail. Record the policy
   for a malformed item instead of silently dropping an entire page.
4. Return the result without toasts or UI mutations. The source-selection service decides whether
   to fall back; the VM alone decides what to publish and which user message to show. Publish rows
   and new paging state only after the page is accepted and its request generation is still current.
5. Attach a logical endpoint/source, stage, field path, expected/actual token type and available
   request ID to parse failures. Reuse the response-evidence workflow in item 3 for captured shapes
   and fixtures rather than creating another diagnostics system.

### Implementation batches and completion criteria

| Batch | Work | Complete when |
| --- | --- | --- |
| P0 — inventory **(done)** | List the dynamic all/video/article, detail, space and repost endpoints, request builders, envelopes, field tables, parsers, mappers and callers. Identify shared rules versus source-specific rules. | Each active response path has a named owner; no existing source or paging cursor is omitted. |
| P1 — minimal contract **(done)** | Define `ApiParseResult<T>` and only the field readers needed by the first feature. Preserve existing callers through adapters. | Success, business rejection, malformed envelope and valid empty payload have distinct, documented results; parsers perform no UI work. |
| P2 — web dynamics **(web half done)** | Extract the web JSON part of `DynamicGrpcMapper` into the web parser/mapper, rename the general feed builder through a compatibility wrapper, and remove direct web-field parsing from the two feed VMs. | All/video/article retain their current rendering and source-bound cursors; the recorded string-emoji failure is handled; malformed pages reach source fallback without partial publication. |
| P3 — other dynamic sources **(done)** | Separate gRPC mapping from legacy REST parsing, reusing the existing application card model. Migrate detail, space and repost callers one path at a time. | Source differences remain explicit; comments, links, empty pages and failed-append retry semantics are preserved. |
| P4 — other features | Apply the same structure to comments, video/season detail, playlists and account responses in separate batches, driven by actual callers. | Each migrated feature has one documented parsing entry point and field contract; untouched features continue through their existing adapters. |
| P5 — retire duplicates **(started: `ClassifyLegacy` removed)** | Remove old parsing wrappers/model aliases only after their callers are gone; update the inventory and field tables. | Source inspection shows no parallel parser for the same response contract or transport/UI work hidden inside a parser. |

Acceptance for each migrated feature: large IDs, optional or mistyped nested fields, unknown cards,
empty pages, separate source cursors, failed append/retry and refresh replacement behave as they do
today.

## Parked

- **Live tiles:** optional; needs the tile service plus a background-task registration whose async
  refresh is awaited before the deferral completes.
- **Real buvid acquisition:** no source-backed flow; app-key headers do not promise it.
- **Alternate danmaku renderer:** nothing needs FrostMaster; keep NSDanmaku.
- **More API/auth variants:** no caller; do not port endpoint classes to mirror the sibling.
- **Local-library expansion:** the five milestones are done; more discovery/formats/artwork needs its
  own request. Keep the current manifest models as the contract.

## 播放器列表入口合并（维护者 2026-11-06 选定方案 A）

维护者指出「选集」按钮与「播放列表/播放目录」功能重叠。核对源码后的实际情况：

- 播放器底栏有三个列表入口：`BottomBtnDirectory`（播放目录，抽屉里带 分P/合集/播放列表/剧集正片/预告、搜索、
  定位正在播放、加载更多、按合集/播放列表连播）、`BottomBtnList`（选集，侧栏只列分P）、`BottomBtnNode`（节点）。
- `PlayerSurfaceVisibility.Decide` 里多P视频会**同时**显示选集与目录，而选集能做的每一件事目录的分P段都能做。

按方案 A 落地：删除 `BottomBtnList`、`EpisodeList`、`EpisodePivot`、`ShowPlaylistButton` 与配套的下标镜像代码；
侧栏只留 弹幕列表 / 节点 / 本地轨道，`EpisodePivot` 改名 `NodePivot` 且只在互动视频出现；分P 切换统一走播放目录
（页面 `DirectoryCaption` 本来就在多P时报告目录可用并给出 `3/12` 这样的位置）。`playertoolbar` 90 项重写，
`--legacy` 基线仍有 22 项失败，说明新断言钉的是这次的行为。
