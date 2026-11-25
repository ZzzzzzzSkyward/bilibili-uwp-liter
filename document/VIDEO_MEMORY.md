# 视频反复打开、观看、关闭的内存采集

BR-21 仍是 **native-unmeasured**：本轮已取得窄源码所有权修复、独立review与外部采样工具的离线证据，没有在 Windows 重现约 50 个视频后的 RAM 增长。源码探针中的计数、弱引用和模拟资源释放不代表 Windows 私有提交、原生媒体或 GPU 字节。后台归属/播放器整体重写继续以测量为前置。

## 1. 当前证据与边界

| 范围 | 最终已审源码结果 | 证据 |
| --- | --- | --- |
| Season M1 | 终结页/失效VM拒绝迟到metadata/episode与后续页面发布；活页refresh、独立VM重载和fresh owner保留 | [组件PASS](../cloud/rounds/20261125-video-memory/managed/final-review.md) |
| Player R1/R2/R3/RR1 | terminal Dispose/public/Loaded/await链拒绝迟到info/quality；live unload/reload可用，设置每次刷新但自有handler只注册一次 | [组件初审历史](../cloud/rounds/20261125-video-memory/resources/final-review.md)、[残留修复PASS](../cloud/rounds/20261125-video-memory/resources/residual-final-review.md) |
| PF1/FA1/FA2/FA3 | 替代项立即取得轨道/Next可用状态并组合current switch/transition限制；close同步解绑互动child/context，允许有限detached工作；新节点独占pool/segment/initial load lease，旧finally不能unlock；ended在history await后复验原owner而保留原report及current modes | [残留PASS](../cloud/rounds/20261125-video-memory/resources/residual-final-review.md)、[RF1后fresh闭环](../cloud/rounds/20261125-video-memory/rf1-final-audit.md) |
| RF1 | Season初始加载/失败/无可播项时enabled Update入口拒绝null item，不授权工作或抛未处理async-void异常；真正current refresh保持 | [owning](../cloud/rounds/20261125-video-memory/resources/rf1-final-review.md)、[fresh](../cloud/rounds/20261125-video-memory/rf1-final-audit.md) |
| IG1 | Show实际null-safe Next=false；Clear仅当无current local switch才Next=true。保留terminal/labels/overlay，既有tests未改 | [owning](../cloud/rounds/20261125-video-memory/resources/ig1-final-review.md)、[fresh](../cloud/rounds/20261125-video-memory/ig1-final-audit.md) |
| G1 | 五个自有绘制临时对象确定性cleanup；borrowed device/returned image不关闭。完整renderer经后续hash重绑未变 | [graphics PASS](../cloud/rounds/20261125-video-memory/graphics/final-review.md)、[最终不变span](../cloud/rounds/20261125-video-memory/resources/ig1-final-review.md) |
| 外部工具 | 当前 Linux PSCore7.7：46次调用（6次exit0、40次预期exit1）、21组、106检查、55 helper calls/52 synthetic rows PASS；typednull/identity/order/stop/disposal有界行为，不代表PS5.1或native samples | [tool PASS](../cloud/rounds/20261125-video-memory/measurement/final-review.md)、[当前interpreter证据](../cloud/rounds/20261125-video-memory/verification/evidence/interpreter-checks.md) |
| combined local gate | corrected **51/51 logical、52/52 executable PASS、complete_required=true**；source935文件manifest c40d13c9…，results ca43ccc8…；执行时为 dirty7356b948 上的 local bytes；随后按实际 committed blobs 绑定 sourceC | [corrected report](../cloud/rounds/20261125-video-memory/verification/corrected-execution.md)、[exact evidence](../cloud/rounds/20261125-video-memory/verification/evidence/corrected-common-gate.md) |

源码已普通提交/推送为 [source `ff5d2855`](https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/commit/ff5d2855bb62ef6459a724ffae6101b812a7369d)。其唯一 parent 为 `7356b94893377558191b0d70576a52bae704c1f9`，tree 为 `2565a48424882c76acfe3217c7fb610b7b193b91`；[publication receipt](../cloud/rounds/20261125-api-shape-comparison/publication/execution.md) 与 [独立 blob/ref 核对](../cloud/rounds/20261125-api-shape-comparison/publication/root-source-check.md) 证明107个交付路径和935个已测输入的实际 committed blob 与测试时字节一致。测试本身运行于 dirty7356，不把生成的 Git metadata 改称 clean-C run。Hosted：UNVERIFIED：2026-11-25 05:09:51 UTC 的实际 Actions collection 只见旧 run45434541348（head169b2b0、queued/null），没有本次 sourceC 的 hosted result。

第一轮完整gate保留为50/51 logical、51/52 executable PASS/1FAIL（ux1d maintained source assertion）。IG1两处真实可执行assignment修正后，所有tests/assertions字节不变，新的完整run才得到PASS；未删断言、未把失败改成历史通过。
显式 Close/Dispose 终结该 page/player，之后正常用户打开要取得全新所有者。同一仍存活的页面可以刷新、切集、历史返回或视觉 unload/reload；这些与关闭、最小化、后台播放分开。独立 VM 的加载代际可继续复用。Windows 原生缓存若把已终结实例重新交付，应记录真实路径再审查路由，不恢复私有 terminal 标记。

Windows VS/UWP 编译、真实还原后的 Win2D/codec/WinRT、PS5.1、50 个真实解码视频、实际缓存/dispatcher/SMTC/display、像素、managed roots/native heap/GPU、安装签名启动与 live/account/session 门禁在此 Linux 环境均 **FAILED/BLOCKED / UNVERIFIED**。下一步是在 Windows 按下列流程实采；历史维护者默认通过行、账号 P4/P5 hold 和 U2 身份/协议门禁保持。

已发布不可变源码/fixture入口：[PlayerControl](https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/ff5d2855bb62ef6459a724ffae6101b812a7369d/src/BiliLite.UWP/Controls/PlayerControl.xaml.cs)、[SeasonDetailVM](https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/ff5d2855bb62ef6459a724ffae6101b812a7369d/src/BiliLite.UWP/Modules/Detail/SeasonDetailVM.cs)、[playersess fixtures](https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/tree/ff5d2855bb62ef6459a724ffae6101b812a7369d/plan/tests/playersess)、[portable tests](https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/tree/ff5d2855bb62ef6459a724ffae6101b812a7369d/tools/tests/video-memory)、[sampler](https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/ff5d2855bb62ef6459a724ffae6101b812a7369d/tools/measure-video-memory.ps1)。G1只证明CanvasTextFormat、CanvasRenderTarget、CanvasTextLayout、CanvasGeometry与CanvasStrokeStyle这五个自有临时对象在正常/异常路径释放；实际还原的依赖版本、返回像素、native/GPU字节与borrowed device运行仍需Windows证据。完整renderer span SHA256为 `5789d03300d297cb7bc6115ee0622417b5773960544b3b0899ec2bb8167fbcb7`。Win2D1.26.0投影API支持相应Close/Dispose；这不证明Windows实际还原版本，NSDanmaku只给出传递依赖下界。独立runner14与package15不计入52；当前package15及sampler的解释器边界见上表receipt。

## 2. 先固定安装包、进程和行程

Windows 上先按 [BUILD.md](BUILD.md) 验证源码与可安装交付包；编译命令为：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-release-x64.ps1 -Fast -NoVersionBump -Restore
```

它只编译，不生成可安装包。实际发布/安装验证另走 BUILD 的完整流程；记录两个 manifest 的实际版本与交付包 hash，不手改身份、证书、依赖或版本来配合采集。

在测量 shell 记录环境和实际安装清单，再从包/应用/任务管理器 Details 的对应关系选择目标；不要猜进程名，也不要选 ApplicationFrameHost 或短命 launcher。

```powershell
$PSVersionTable
Get-CimInstance Win32_OperatingSystem | Select-Object Caption,Version,BuildNumber
Get-CimInstance Win32_VideoController | Select-Object Name,DriverVersion
Get-AppxPackage | Select-Object Name,PackageFullName,PackageFamilyName,InstallLocation
Get-CimInstance Win32_Process | Select-Object ProcessId,Name,ExecutablePath
# 以下 observed 变量均从实际盘点取得。
$selectedPackage = @(Get-AppxPackage | Where-Object { $_.PackageFullName -eq $observedPackageFullName })
if ($selectedPackage.Count -ne 1) { throw 'Package binding is ambiguous or absent.' }
$installedManifest = $selectedPackage[0] | Get-AppxPackageManifest
$installedManifest.Package.Applications.Application | Select-Object Id,Executable,EntryPoint
$targetProcess = Get-Process -Id $observedProcessId -ErrorAction Stop
$targetProcess | Select-Object Id,ProcessName,StartTime,Path
$boundStartUtc = $targetProcess.StartTime.ToUniversalTime()
Get-FileHash -Algorithm SHA256 -LiteralPath $observedDeliveryArtifactPath
```

核对运行路径确实位于该包安装目录，manifest App Id/executable 与加载目标相符，记录验证方法。权限不足保留错误，用受许可的只读 VS/任务管理器检查；未验证的绑定不能写成通过。进程退出、重启、PID 重用均结束本次 run，新建目录重新绑定。

固定窗口/缩放/字号、tab/NoTab、匿名账号标签、网络/电源、引擎与实际 fallback、画质/codec/resolution/frame rate、音量/倍速、字幕/弹幕、autoplay、API capture 开关、入口/开始位置和磁盘缓存状态。默认 API 样本捕获关闭，普通日志保持原样。准备固定顺序的 50 项 media/part 标签，避免选视频时顺手滚动增长 feed。每次从历史续播打开要重置到同一位置，再开始 30 秒观看计时。

| Journey | 固定操作 |
| --- | --- |
| I | 同一可见 shell 空闲，匹配 S/D 完整用时，无播放器 |
| F | 50 个预声明 feed 滚动/分页步骤，无媒体；记可观察的深度/数量 |
| S / D | 同一个 media/part 重复 / 50 个不同的固定 media/part；每次普通关闭，隐藏弹幕 |
| SD / DD | 与 S/D 完全相同列表和时间，显示弹幕 |
| A | 故意保留一个活动播放器，切固定非播放器视图；匹配 S 用时后显式关闭 |
| M | 一个活动播放器最小化 60 秒后恢复；必要时重复 50 次，单列 suspend/resume |

隐藏弹幕仍会加载分段数据，称为 rendering-off，不能称为 data-free。最小化/挂起不是关闭，既有 suspension GC 会改变实验。I/F/S/D/SD/DD 各筛查一次；对可疑行程与最近对照交替顺序各做三次全新进程重复。保留正常磁盘缓存并记录顺序；不做生产 GC、trim、cache purge 或中途换引擎来制造下降。单独的本地/直播/season/engine-change 测试不能悄悄替代主 S/D。

## 3. Metadata v1 和文件绑定

工具为 [tools/measure-video-memory.ps1](../tools/measure-video-memory.ps1)，四个 Mode：`ValidateMetadata`、`Checkpoint`、`Collect`、`Mark`。没有交付自动 summarizer 或 leak verdict。参数默认 `IntervalSeconds=2`、`DurationSeconds=3600`、`Cycle=0`、`CompletedCycles=0`、`Status=incomplete`、`WindowState=unknown`；本协议显式覆盖采集时长。**没有默认目标进程名或自动选择 PID**：除 ValidateMetadata 外必须显式给出 RunDirectory、TargetProcessId 和 ExpectedProcessStartUtc，且与 metadata 的 process.id/startUtc 一致。

把 UTF-8 `fixture.json` 放在**尚不存在**的 run 目录外。JSON 标量不能用单元素数组代替；时间必须带 `Z` 或 `+00:00`。字段契约：

| 组 | 必填内容 |
| --- | --- |
| 顶层 | `schemaVersion:1`；1–80 位字母/数字/underscore/hyphen `runId`（首位字母/数字）；`evidenceLevel:E2/E3/E4`；`createdUtc`；40-hex `sourceCommit` 或 `unknown`；boolean `sourceDirty` |
| Artifact/runtime | 真实绝对 `artifactPath` 与 64-hex `artifactSha256`；`appVersion/configuration/architecture/runtime` |
| `package` / `process` | 观察到的 `fullName/familyName/appId/executable`；integer `id`、`startUtc/path/verificationMethod` |
| `device` / `profiler` | `osBuild/ram/cpu/gpu/driver/power/display/scaling`；`mode/version` |
| `fixture` 基础 | `journey`、正整数 `replicate/repeatOrder`、50 项字符串数组 `mediaOrder` 与 `mediaListSha256`、严格整数数组 `expectedCycles:[1,...,50]` |
| `fixture` 观察 | `comparisonGroup/shell/accountLabel/network/requestedEngine/actualEngine/requestedQuality/actualQuality/codec/resolution/frameRate/playbackRate/volume/subtitles/danmaku/autoplay/apiCapture/entryRoute/startingPosition/cacheState` |
| `fixture` 时长 | 正有限数 `playbackSeconds:30/betweenCloseSeconds:10/timeoutSeconds:60/warmupCycles:5/warmupIdleSeconds:60`；这些是本协议值，工具仅验证正值 |
| `units` | `privateBytes/workingSetBytes:bytes`；`handleCount/threadCount:count`；`cpuTotalMs:milliseconds` |
| `outputs` / `deviations` | `samples:samples.ndjson/checkpoints:checkpoints.ndjson/events:events.ndjson`；字符串数组 `deviations`（可空） |

Runtime/device/profiler 和 fixture 观察字段可以是非空字符串或 `{ "status":"unavailable", "reason":"具体原因" }`；package/process/artifact 绑定必须实有。未知附加字段不能替代必填项。I/A/M 的 mediaOrder 使用匹配时间/转换步骤标签并写明差别。不要保存凭据、cookie、用户 ID、API bodies 或进程命令行。

列表 hash 是 **PowerShell compact JSON 字符串数组的 UTF-8，无 BOM、无换行**；不是逐行标签文件 hash：

```powershell
$listJson = ConvertTo-Json -InputObject @($preparedOrderedLabels) -Compress
$sha = [Security.Cryptography.SHA256]::Create()
try { $mediaListSha256 = ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($listJson)))).Replace('-','').ToLowerInvariant() }
finally { $sha.Dispose() }
```

ValidateMetadata 也要能读到真实 artifact 并匹配 hash。通过只说明输入类型/字段/hash 合约有效，不证明安装关系、native 采样、视频已开始或 E2/E3/E4 已完成。测试生成器的 synthetic metadata 只供 schema 参考，不能当真实安装样本。

## 4. 采集与 16 个 checkpoint

所有命令均在**观察到操作的时刻**执行；这些命令不会操作 UI。以 repo root 为当前目录，先在 marker shell 验证并初始化：

```powershell
$tool = '.\tools\measure-video-memory.ps1'
$metadataFile = 'C:\captures\fixture.json'
$runDirectory = 'C:\captures\run-01' # 首次调用前必须不存在
$target = @{ TargetProcessId=$observedProcessId; ExpectedProcessStartUtc=$boundStartUtc.ToString('o') }
& $tool -Mode ValidateMetadata -MetadataPath $metadataFile
& $tool -Mode Checkpoint -MetadataPath $metadataFile -RunDirectory $runDirectory @target -Cycle 0 -Phase prewarm
# 第二个 shell：使用完全相同的变量/绑定，在开始 warm-up 前启动。
& $tool -Mode Collect -MetadataPath $metadataFile -RunDirectory $runDirectory @target -IntervalSeconds 2 -DurationSeconds 7200
```

7200 秒是本例的有界采样窗口，覆盖 warm-up、50 次真实 watch/open/close、超时余量和最后 idle；不是自动完成承诺。仅 55×40+60+300 已约 2560 秒，尚未计打开/seek/命令开销，所以无条件 2400 秒示例不足。根据预声明超时/操作预算在开跑前选择足够时长；不要为同一 run 启动第二个 Collect 来补时间。它立即读第一条，按 monotonic cadence 读当前值，超时不补造漏掉的点。

| 顺序 | Cycle / Phase | 实际动作 |
| --- | --- | --- |
| 1 | 0 / `prewarm` | 无播放器，记录冷起点 |
| 2 | 0 / `baseline` | 五次未计数 warm-up，每次观看30秒/关闭后10秒，再前台 idle60；即 warm0 |
| 3–14 | 5、10、25、50，各依次 `opened` → `post-close` → `settled` | seek完成后实际播放30秒；普通关闭且 surface消失/audio停止；起始视图等10秒 |
| 15–16 | 50 / `idle60` → `idle300` | 从最终关闭计时，前台无媒体，不进入设置/最小化 |

例如到 warm0 时执行 `-Mode Checkpoint ... -Cycle 0 -Phase baseline`；到第五次真实观看结束时执行：

```powershell
& $tool -Mode Checkpoint -MetadataPath $metadataFile -RunDirectory $runDirectory @target -Cycle 5 -Phase opened
# 观察真实普通关闭后：
& $tool -Mode Checkpoint -MetadataPath $metadataFile -RunDirectory $runDirectory @target -Cycle 5 -Phase post-close
# 起始视图等待10秒后：
& $tool -Mode Checkpoint -MetadataPath $metadataFile -RunDirectory $runDirectory @target -Cycle 5 -Phase settled
```

在对应动作时换成 10/25/50；最后依次用 `-Cycle 50 -Phase idle60`、`idle300`。所有11种 phase 为 `prewarm/baseline/opened/post-close/settled/idle60/idle300/background/minimized/restored/feed-step`；额外控制 phase 留给对应行程。重复/逆序/错误 cycle、时间倒退被拒；最终 idle 必须在 cycle50 普通/控制状态之后。不要把16条 checkpoint 连续执行成假行程。

每次尝试另记 operator events。以下第一轮示例中的引擎、画质、标签和观察来自实际界面：

```powershell
& $tool -Mode Mark -MetadataPath $metadataFile -RunDirectory $runDirectory @target -Cycle 1 -Phase opened -Action open-request -CompletedCycles 0 -Status incomplete -MediaLabel $preparedMediaLabel -MediaPart $preparedPart
# 如有seek，另在完成时记录 -Action seek-complete。
& $tool -Mode Mark -MetadataPath $metadataFile -RunDirectory $runDirectory @target -Cycle 1 -Phase opened -Action media-start -CompletedCycles 0 -Status active -ActualEngine $observedEngine -ActualQuality $observedQuality
& $tool -Mode Mark -MetadataPath $metadataFile -RunDirectory $runDirectory @target -Cycle 1 -Phase post-close -Action close-action -CompletedCycles 0 -Status incomplete
& $tool -Mode Mark -MetadataPath $metadataFile -RunDirectory $runDirectory @target -Cycle 1 -Phase settled -Action starting-view -CompletedCycles 0 -Status ok -ObservedPlayers 0 -Note 'Operator observed surface gone and audio stopped.'
& $tool -Mode Mark -MetadataPath $metadataFile -RunDirectory $runDirectory @target -Cycle 1 -Phase settled -Action cycle-complete -CompletedCycles 1 -Status ok
```

逐次记录 attempted/completed 计数；`failure/timeout` 保留真实失败，不能偷偷换媒体或把失败补成同一 fixture 的成功。工具要求成功 cycle-complete 从1到50顺序且唯一；失败导致不能完整完成时保留 incomplete run，另开新 run，不伪造 completion。Status 允许 `ok/failed/timeout/incomplete/active/suspended/unavailable`；其他 Action 为 `suspend/resume/observation/run-complete`。未知 visibleTabs/observedPlayers/engine 等为 null，明确传0只代表实际观察到0。

完成50轮且已采最终 idle300 后，才发停止标记：

```powershell
& $tool -Mode Mark -MetadataPath $metadataFile -RunDirectory $runDirectory @target -Cycle 50 -Phase idle300 -Action run-complete -CompletedCycles 50 -Status ok -Note 'Operator journey complete; native acceptance reviewed separately.'
```

它写 `STOPPED.json`（operator-complete），运行中的 collector 在下次观察前停止，后续写入被拒。**先 idle300 checkpoint，再 run-complete**。即使 run-complete 被接受，也仍须人工检查 checkpoint/samples/失败/完整性；Mark 不读 native/UI，在 Linux 也能记录合成标记。Duration 自行结束只结束采样窗口，不代表行程完成。`Ctrl+C` 保留已 flush 数据并释放本工具的 observer/writer/lock；强杀 shell 留锁则保留 incomplete run，新建目录，不删活动锁、不续写 Collect。

## 5. 输出、异常与归因

工具复制原始 `metadata.json`，写 `binding.json`（metadata/tool SHA256、版本、artifact、runId、PID+start）、`samples.ndjson/checkpoints.ndjson/events.ndjson`，必要时 `STOPPED.json`。不要中途编辑 metadata 或脚本；后续偏差记 event note。已有空目录/未绑定目录拒绝，同一绑定重开也必须字节一致。

进程行保留 UTC 和 monotonic ticks/frequency、逐流序号、PID/start 与五项当前值：privateBytes 是私有提交；workingSetBytes 是当前驻留的共享+私有页；handles/threads 是 count；cpuTotalMs 是累计 CPU 毫秒，不是百分比。不是 peak，不相加共享工作集。Process observer 每次 Refresh，使用实际 CLR getter；失败字段保持 null+字段错误/partial，真实0或空线程集合计数0保持有效。身份读失败/退出/PID重用保留 fatal/null 行并停止；输出/metadata/顺序/锁错误非零退出且不截断旧文件。脚本不启动/终止目标，不调用 GC、trim 或 purge。

所有 raw JSONL/log/capture/ETL/profiler snapshot 放 Git 外的专用目录。等所有 writer 结束后对数据和工具作 `Get-FileHash -Algorithm SHA256`，保留命令/exit、source commit+dirty、artifact/tool/runtime/fixture/media-list/run/PID+start 绑定、attempted/completed 与 deviations。用现有 [DIAGNOSTICS.md](DIAGNOSTICS.md) 的问题标记和导出关联 session；标记放 warm-up 前、导出放采集后，避免设置页干扰计时。

报告 warm0、各 active/post-close/settled5/10/25/50 和 idle60/300 的原始点与 elapsed；分开计算 `(M_b-M_a)/(b-a)` 的0→5、5→10、10→25、25→50区间，再列三次重复的 median/range。没有交付自动分析脚本。持续晚期 private-byte 正斜率且 idle/repeats一致，才进入归因；working-set高水位单独不够，平台/空值不能当0。

另做明确标为 intrusive 的同配置/fixture诊断重复：VS2022 UWP Memory Usage 在 warm0、settled5/10/25/50、idle300及A活动正控取快照，记录 profiler 引发的collection，核对旧 page/player/VM/task/timer/event 的 Paths to Root。未支持则记录 runtime/VS/target/错误，不能假定 dotnet-gcdump/counters 能附加这个 UWP runtime。

原生归因先只读发现 `Get-Command wpr.exe,wpa.exe`、`wpr.exe -status/-profiles/-help heap/-help start`；只用本机实际支持的 profile/provider。Store-app heap binding 使用 observed executable leaf、**PackageFullName** 和 manifest AppId。保存原有 HeapTracingConfig，只启停本 run 的录制，在 finally 恢复本 run 改过的配置；不取消他人录制。完整可运行 heap 步骤与官方语法依据见 [Windows protocol](../cloud/rounds/20261125-video-memory/measurement/windows-protocol.md#4-attribute-managed-nativemedia-and-gpu-retention)。检查事件丢失、PID/start、symbols/PDB和 outstanding allocation stacks。WPR heap 不覆盖所有 VirtualAlloc/custom/media/GPU；不能用 privateBytes减managed heap 算 native。GPU只用实际枚举支持的计数/profile，缺失记 null+原因。

| 证据 | 允许结论 |
| --- | --- |
| E1 源码+相同fixture原红后绿 | 具体所有权/行为缺陷；没有Windows字节结论 |
| E2 完整绑定的Windows重复/对照过程曲线 | 该fixture可重现的过程趋势，尚未归因 |
| E3 E2+root/native stack/GPU证据 | 已支持的保留家族归因 |
| E4 同fixture修复前后+native/playback行为 | 该条件下改善/解决；不宣称普遍无泄漏 |

最小结果表每行写 runId/journey/replicate、checkpoint、elapsed、attempted/completed、privateBytes/workingSetBytes/handles/threads/CPU、可观察tabs/players、status/errors/deviations，另附实际取得的roots/native/GPU证据。没有复现、趋势待归因、已归因、修复前后改善四种结论需据实选择；不设任意MB线，也不从离线PASS关闭 BR-21。
