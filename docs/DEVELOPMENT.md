# 开发环境与流程

构建、调试、跑探针、排障各一节。构建失败的逐个症状见 [document/BUILD.md](../document/BUILD.md)；
架构见 [ARCHITECTURE.md](ARCHITECTURE.md)。

## 1. 环境要求

| 组件 | 版本 / 说明 |
| --- | --- |
| Windows | 10 版本 18362 或更高，x64（ARM64 无 SYEngine，播放走另一条路径） |
| Visual Studio 2022 | 必须安装 **通用 Windows 平台开发**（UWP）工作负载，含 Windows 10 SDK **10.0.19041.0** |
| .NET SDK | 8.0+（`BiliLite.gRPC`、`BiliLite.WebApi` 是 SDK 风格项目；UWP 项目本身走 MSBuild 而非 dotnet CLI）。批次探针需要 **8.0** 目标包，`plan/tests/run.sh` 还需要 `$UPSTREAM_UNIT_TOOLS` 下的 8.0.427 SDK |
| PowerShell | Windows PowerShell 5.1 即可（脚本自带 UTF-8 输出设置） |
| Bash / Python | 完整离线 runner 用 Linux、Bash ≥4、Python ≥3.9（标准库），以及 awk/sed/grep/cut/cp/mktemp 等现有脚本工具；Windows 的单批次可用 Git/MSYS2 Bash，UWP 构建仍走 Windows |
| 网络 | 拉依赖与 `git push` 需要；本机 GitHub 的 TLS 会间歇性中断。本地维护者可用 `sync-dev.ps1` 重试；云端 agent 不运行该 rebase/stash 脚本，按 §7 的串行授权与普通 Git 流程处理 |

App 的接口基址与 appkey 编译在产品里，不在运行时读取环境变量；离线探针的工具根、PATH 和缓存需要按 §6 设置。

## 2. 分支模型

- `dev` 是开发线：功能、修复、探针、计划都提交到这里，`origin/dev` 是它的远端。
- `master` 是发布线：只收同步提交、tag 与发布说明。发布给用户的包从 `master` 的同步提交构建。
- 两条线在 `e2c7958`（2026-10-31）之后分叉：`dev` 有 510+ 个提交，`master` 有 8 个（两次同步提交
  的内容 `dev` 已经全部包含，其余是本仓库为 agent 化补的文档/修复）。不要指望 `git merge master`
  带来新东西；需要对照时用 `git log origin/master..origin/dev` 与 `git cherry`。

## 3. 首次构建

```powershell
# 1) 还原（packages.config + PackageReference 两种都处理）并出 MSIX
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-release-x64.ps1 -Restore -OutputDir Z:\
```

脚本会自己找 MSBuild（`vswhere`）、还原依赖、按 `Release|x64` 构建，并把产物放到 `-OutputDir`
（默认 `Z:\`，目录必须已存在）。不要直接调 MSBuild：还原、输出目录、版本号递增都在脚本里。

- 每次运行（除 `-NoVersionBump`）都会把 `src/BiliLite.UWP/Package.appxmanifest` 与
  `src/BiliLite.Packages/Package.appxmanifest` 的修订号 +1。这个改动要和触发构建的代码改动进同一个提交；
  不要手改清单版本。
- 日常编译循环：`-Fast -NoVersionBump`（约 1 分钟，只编译 `BiliLite.UWP`，不出包）。
- 依赖或 `obj` 不可信时加 `-Clean`。

## 4. 安装与运行

产物只有一个可安装文件：

```
BiliLite.Packages_<version>_x64_Test\BiliLite.Packages_<version>_x64.msix
```

安装前需要：系统设置里打开「开发人员模式」，核对发布包的实际签名及该发布包随附的安装证书，再按需信任。
不要把仓库内 `src/BiliLite.UWP/BiliLite.UWP.cer` 自动视为当前包的 signer。
发布准备时检查 UWP csproj 与 Packages wapproj 实际选择的 `PackageCertificateThumbprint`，
在目标 Windows 证书存储中核对对应证书的有效期、签名私钥可用性、publisher/manifest 匹配，
再核对实际 MSIX 签名与分发安装证书一致。跟踪 CER 的日期不能证明当前所选 signer 已过期或可用；
没有 Windows 存储/实际包证据时此项仍为 BLOCKED。证书/指纹/清单身份/密钥的调整由维护者在获授权的发布准备中处理。
安装后包名 `BilibiliUWPLiter`。

```powershell
Add-AppxPackage .\BiliLite.Packages_<version>_x64_Test\BiliLite.Packages_<version>_x64.msix
```

- **不要**安装 `BiliLite.UWP_<version>_x64.msix`：app 项目自己 `-t:Package` 产出的那份把 ~4.2 MB 托管 exe
  放在包根，缺少 `entrypoint/`，装上去在启动图就退出且没有任何日志。判断方法见
  [document/BUILD.md](../document/BUILD.md) 的「The MSIX layout that actually boots」。
- 版本号没升就安装等于原地替换，不报错也不升级；要确认装的是新版本，看「设置 → 关于」里的版本。

## 5. 调试

1. VS 打开 `BiliLite.sln`，启动项目设为 `BiliLite.UWP`，配置 `Debug|x64`，F5 部署并挂调试器。
2. 部署失败时先手装一份 MSIX 再「调试 → 附加到进程」到 `BiliLiter.exe`。
3. 应用日志：`%LOCALAPPDATA%\Packages\BilibiliUWPLiter_*\LocalState\log\yyyyMMdd.log`
   （设置 → 诊断 → 「打开日志目录」直达）。每行带 `session=<id>`，会话头在启动时写入。
4. 启动即闪退且没有日志：读 Windows「事件查看器 → 应用程序」的 1000/1026 事件；`e0434352` 通常意味着
   XAML 初始化失败 —— 优先怀疑装错了包（见上一条），而不是源码。
5. 响应形状/漂移排查：设置 → 诊断打开「记录 API 响应形状」（必要时再开采样），复现后「导出诊断包」，
   附件里就带 `log/api/yyyyMMdd.ndjson` 与 `manifest.json`。

## 6. 探针（离线行为测试）

```bash
# UPSTREAM_UNIT_TOOLS 是工具根，里面包含 dotnet/；用实际绝对路径替换
export UPSTREAM_UNIT_TOOLS=/absolute/path/to/probe-tools
export DOTNET_ROOT="$UPSTREAM_UNIT_TOOLS/dotnet"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_HOME="$UPSTREAM_UNIT_TOOLS/cache/dotnet-cli"
export NUGET_PACKAGES="$UPSTREAM_UNIT_TOOLS/cache/nuget"
dotnet --list-sdks
dotnet --list-runtimes
bash plan/tests/season1/run.sh      # 其它目录批次同形
bash plan/tests/run-g1.sh           # g1 的根 runner
bash plan/tests/run.sh              # LM01–LM05、API01、AUTH01B 的旧顺序链
```

工具根需安装 **SDK 8.0.427**、**Microsoft.NETCore.App 8.0.33** 运行时与
`Microsoft.NETCore.App.Ref` **8.0.33** 引用包。
从微软官方 .NET 8 下载安装到该 `dotnet/` 根，确认 `sdk/8.0.427/Roslyn/bincore/csc.dll`、
`packs/Microsoft.NETCore.App.Ref/8.0.33/ref/net8.0` 和运行时真实存在；只把 SDK 放在 PATH 不满足旧 csc runner 的绝对路径约定。
还需 `sdk/8.0.427/Containers/tasks/net8.0/Newtonsoft.Json.dll`。完整 runner 校验该工具根的
`dotnet --version` 必须为 8.0.427、运行时列表含 8.0.33，且 Linux `libcoreclr.so` 存在；不能用其他 SDK 替代。
依赖通过现有项目还原，缓存和构建产物不提交。

### 完整套件与选择执行

工具根准备后，一条命令运行全部必需离线检查：

```bash
bash plan/tests/run-all.sh --tools-root "$UPSTREAM_UNIT_TOOLS"
# --list 不要求 SDK，也不执行测试；须单独使用
bash plan/tests/run-all.sh --list
# --only 可重复，按清单顺序去重执行；这里只覆盖选择范围
bash plan/tests/run-all.sh --tools-root "$UPSTREAM_UNIT_TOOLS" --only batch:comments --only LM05
bash plan/tests/run-all.sh --tools-root "$UPSTREAM_UNIT_TOOLS" --only batch:recovery
bash plan/tests/run-all.sh --tools-root "$UPSTREAM_UNIT_TOOLS" --only AUTH01B
# 独立旧脚本选择：不需要此前阶段成功
bash plan/tests/run.sh --stage LM05
bash plan/tests/run-auth01b.sh --stage loginvm
```

`--tools-root` 优先于 `UPSTREAM_UNIT_TOOLS`；未设时默认 `/workspace/work/upstream-unit-tools`，根下必须有
`dotnet/`。runner 自动为子进程设置该 dotnet 的 PATH/DOTNET_ROOT、工具根内 CLI/NuGet/XDG 缓存，禁用多层 SDK 查找。
入口从脚本位置定位仓库，可从其它 cwd 调用；`--repo-root PATH` 仅用于明确的源码快照/控制夹具根。

ID 区分大小写：目录是 `batch:<name>`（含 g1、stab），单文件是 LM01–LM05、API01、
`AUTH01B.account`、`AUTH01B.loginvm`。逻辑 ID `AUTH01B` 选两个阶段；只选其中一个时该逻辑项不算完整 PASS。
`run.sh --stage all|LM01|LM02|LM03|LM04|LM05|API01|AUTH01B` 与
`run-auth01b.sh --stage all|account|loginvm` 保留无参/all 的旧 fail-fast 顺序。
新 aggregate 顺序执行，普通失败/缺项后仍尝试后续选中项；新增 runnable 目录须先更新受审清单，不能静默遗漏。

`--output PATH` 指向不存在或空的报告目录，非空目录拒绝覆盖。默认在临时目录创建唯一的
`bili-offline-<UTC>-<pid>-...` 目录并打印位置。输出是原子增量 `results.json`、`summary.md` 和逐阶段
`logs/`（每份保留最多 1 MiB 的首尾），记录源 SHA/dirty、工具/命令、完整清单与所选数量、退出码/原因/阶段等。
源码/工具根不可放进输出树；报告、夹具、缓存、bin/obj 都不提交。缺少最终报告或 `finished=false` 不能视为完成。

完整 `PASS` 要求全部 **51 逻辑项 / 52 可执行阶段** 实际通过，且 `complete_required=true`。
选择范围全部通过返回 `PARTIAL_PASS`、exit 0，但 `complete_required=false`；其它未选项仍显示 `NOT_SELECTED`。
缺源码是 `MISSING`，缺精确工具链是 `BLOCKED`（shell-only stab 仍可独立运行）；失败、超时、执行/协议错误保留各自状态。
未知选择/参数或报告设置错误退出 2，任一选中必需项失败/缺失/阻塞退出 1。

`--stage-timeout` 默认 300 秒，`--suite-timeout` 默认 1800 秒，均须正整数。活动子进程按更早的阶段/套件截止终止并清理后代；
阶段超时在套件预算仍有余量时继续后项，套件耗尽则活动项 `TIMEOUT/suite_budget`、后项 `BLOCKED/suite_budget`。
Git/Bash/SDK 元数据前置同样服从套件截止与取消，报告在首个元数据子进程前初始化；中断的来源信息保留 unavailable/unverified。
SIGINT/SIGTERM 尽力清理进程组并完成报告，分别退出 130/143；硬杀/宿主失效不能保证最终报告。

runner 自身的 14 项外部契约控制另跑，**不计入 51/52**：

```bash
python3 plan/tests/verify-suite-runner.py --tools-root "$UPSTREAM_UNIT_TOOLS"
```

它也接受不存在/空的 `--output PATH`；测试临时源码/夹具根与工具/缓存须在报告树之外。
控制失败退出 1，参数/设置错误退出 2；不要因控制失败跳过真实完整 suite，两项结果都须保留。
已发布的 workflow 见 [.github/workflows/offline-probes.yml](../.github/workflows/offline-probes.yml)：
dev push/PR 与无输入 workflow_dispatch，Ubuntu 24.04；运行时步骤通过 GITHUB_ENV 设置独立工具/缓存路径，
先跑控制与全套并分别捕获退出状态，always 上传七天保留的有界报告。真实 CI 结论必须检查实际 pushed SHA 的
GitHub run；排队/尚无 job 是 PENDING，不能由配置、lint、控制或本地 suite PASS 推断 hosted PASS。


- 固定清单为 **44 个目录批次 + 7 个单文件逻辑探针 = 51 个逻辑项 / 52 个可执行阶段**。
  目录批次含 43 个 net8.0 控制台项目和 shell-only `stab`；43 个目录带 `run.sh`，`g1` 使用根
  `run-g1.sh`。项目直接链接真实产品源码，以少量 stub 提供平台/网络边界，不能替代 UWP 编译或真机。
- `batch:recovery` 是一个可执行阶段，覆盖真实推荐、下载与调用方恢复路径；不按内部断言数拆阶段。
- 七个逻辑探针是 LM01–LM05、API01、AUTH01B；最后一个分 Account 与 LoginVM 两个可执行阶段。
  旧无参链遇失败即停，成功前缀不表示后面的阶段执行过。
- **显式用 Bash，并保留 LF**：Linux 的 `sh` 可能是 dash，不支持 `BASH_SOURCE`/`pipefail`，LF 也会失败。
  CRLF 是另一个问题，会产生 `^M` 路径/选项；`.gitattributes` 钉 `*.sh text eol=lf`。
- 单个/选定批次通过仅覆盖其范围；全套通过需要所有必需项实际完成，缺文件、缺 SDK、失败或未执行都不能省略。
- 独立包布局门禁（不计入上述 51/52）：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./plan/tests/package-layout-probe.ps1
# PowerShell Core 可用 pwsh -NoProfile -File ./plan/tests/package-layout-probe.ps1
```

  该门禁从实际构建脚本提取两个函数，对 inert ZIP 运行 **15 个内部案例**，不执行完整发布脚本、不升版本。
  退出 0 为全部通过，1 为断言失败，2 为解析/提取/设置/清理失败。它不证明真实 MSIX 可安装、签名有效或能启动。
  Windows PowerShell 5.1、UWP/XAML 编译、完整打包、安装启动和设备路径仍需各自的平台证据。

## 7. 提交与同步

以下同步脚本仅供本地维护者使用。云端 agent 按适用的串行授权使用普通 fetch、获授权的 fast-forward-only
集成与 nonforce push；不运行 `sync-dev.ps1`，不 rebase，也不自动 stash 或改写其他 owner 的并发工作。
不能安全快进时保留现有工作并记录阻塞，等待相应的集成授权。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\sync-dev.ps1              # 拉 + 推（dev）
powershell -NoProfile -ExecutionPolicy Bypass -File .\sync-dev.ps1 -NoPush       # 只拉
powershell -NoProfile -ExecutionPolicy Bypass -File .\watch-remote.ps1 -Fetch    # 等远端前进
```

- 同步脚本带 TLS 重试、rebase，并把两个 `Package.appxmanifest` 的本地改动 stash 起来。
- 手工推送遇到 `TLS connect error … unexpected eof` 属于网络问题：等 4–6 秒重试，必要时临时取消
  `https_proxy/http_proxy` 再试一次。不要 force push，不要改写 `dev` 或 `master` 的历史。
- 提交信息用 `type(scope): 中文摘要` 的形状（`fix(uwp):`、`docs:`、`chore:`），与仓库现有历史一致。

## 8. 常见问题

| 症状 | 原因与处理 |
| --- | --- |
| `error CS2012 … BiliLiter.exe` + `MSB4181 CompileXaml returned false` | 上一轮编译器进程还占着 `obj\x64\Release\intermediatexaml`。脚本会杀掉持有者、等文件可独占打开、再 `-t:Rebuild` 重试两次；连续失败时用 `-Clean` 并关闭残留 VS |
| `XamlCompiler error WMC1006: Cannot resolve … BiliLite.gRPC.dll / BiliLite.JSBridge.winmd` | 被引用项目还没生成。先跑完整解决方案（不带 `-SkipProjectReferences`），生成后再用 `-Fast -SkipProjectReferences` 提速 |
| `error CS2001: 未能找到源文件 …` 指向 `PlayerControl.xaml.cs`、`DynamicVM.cs`、`VideoDetailPage.xaml.cs`、`EFontAwesomeIcon.cs` | 这是一个 partial clone，>100 KB 的 blob 被按需排除；这些文件在本地不存在。不要改 sparse 配置去拉二进制（见 §9） |
| `sh` 报 Bash 特性/`pipefail` 错误，或路径带 `^M` | 先显式用 `bash`；若仍含 `^M`，另按 §6 恢复 LF |
| `playersess` 批次抛 `FileNotFoundException` | 该批次读 `Controls/PlayerControl.xaml.cs` 做源码接线断言，而该文件被 sparse 排除；环境问题，不是代码问题 |
| `WMC1500` / `WMC1506` / `PRI257` / `NETSDK1138` | 已知可忽略警告（`DropShadowPanel` 弃用、缺失变更通知的 OneWay 绑定、默认语言资源、`netcoreapp3.0` 过期） |
| 装完启动图一闪就退，无日志 | 装成了 `BiliLite.UWP_*.msix`；改装 `BiliLite.Packages_*.msix` |
| 命令行里中文变乱码 | PowerShell 5.1 按 GBK 输出。调用时在命令里设 `[Console]::OutputEncoding=[Text.Encoding]::UTF8`；读旧 GBK 日志用 `iconv -f GB18030 -t UTF-8 -c` |

## 9. partial clone 提醒

历史工作副本曾以 `--filter=blob:limit=102400` 配合 sparse-checkout 排除 29 个大于 100 KB 的 blob。
先检查实际 checkout 的文件是否存在；完整 checkout 不继承这个阻塞。缺失时，
`git status` 仍然干净（它们被标记为 skip-worktree），但**构建会失败**：

- `src/BiliLite.gRPC/bilibili/app/dynamic/v2/dynamic.proto`（117 KB）→ `BiliLite.gRPC` 无法编译，
  `BiliLite.UWP` 因此缺 `BiliLite.gRPC.dll`；
- `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs`、`Modules/User/DynamicVM.cs`、`Pages/VideoDetailPage.xaml.cs`、
  `Controls/FontAwesome5/EFontAwesomeIcon.cs` → 编译器报 `CS2001 未能找到源文件`，`playersess` 探针也读不到源码；
- `plan/tests/g3/Program.cs` → `g3` 批次整个不能跑；
- 以及字体、splash 图、`ffmpeg.7z`、`screenshot/ui.png`、`Assets/JiYan/**` 等二进制。

缺源码时记录实际路径与命令错误，不改 sparse 配置或主动拉取被排除 blob；可继续不依赖缺失输入的离线验证。
任何编译/探针结论都必须说明实际执行范围；Linux 缺 VS/UWP/Windows SDK 另属平台阻塞。

## 10. 一次改动的验收清单

1. `-Fast -NoVersionBump` 编译 0 错误（或在 partial clone 里明确说明无法编译）。
2. 相关探针批次全绿；新行为至少加一条会在旧代码上失败的断言。
3. 影响布局/播放/账号/生命周期的改动，在 `plan/VALIDATION.md` 里写清「设备上要看什么」，不自行宣称已通过。
4. `plan/PLAN.md` 的 Open/Done 状态与 `docs/ROADMAP.md` 同步更新；缺陷留在 `bug_report.md`。
5. 发布前跑一次不带 `-Fast` 的完整构建，确认 Packages 产物路径、entrypoint/companion/fullTrustProcess 与存在且 <65,536 字节的根启动器，再安装启动并走受影响设备路径。
