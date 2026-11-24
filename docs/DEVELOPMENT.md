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
| Git / MSYS2 bash | 用于 `plan/tests/*/run.sh` 与日常命令行操作 |
| 网络 | 拉依赖与 `git push` 需要；本机 GitHub 的 TLS 会间歇性中断，用 `sync-dev.ps1` 重试 |

不需要预先安装任何环境变量。App 的接口基址与 appkey 编译在产品里，不在运行时读取环境变量。

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

安装前需要：系统设置里打开「开发人员模式」，并信任随包的测试证书（`src/BiliLite.UWP/BiliLite.UWP.cer`，
`PackageCertificateThumbprint` 记录在 `BiliLite.UWP.csproj`）。安装后包名 `BilibiliUWPLiter`。

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
sh plan/tests/<batch>/run.sh      # 目录批次，43 个，例：sh plan/tests/season1/run.sh
sh plan/tests/run.sh              # 单文件探针 LM01–LM05 / API01 / AUTH01B（需要 8.0.427 SDK 根）
```

- 每个目录批次是一个 `net8.0` 控制台项目，`<Compile Include>` 直接链接真实产品源文件
  （`src/BiliLite.UWP/...`），再加少量 stub 顶替 UWP 类型；断言写在 `Program.cs` 里，失败时非零退出并
  打印失败计数。
- 目录批次共 43 个：认证与刷新（`auth1`–`auth4`、`webrefresh`、`api02`）、动态（`g1`–`g5`、`cardlayout`、
  `cap`、`cm1`）、播放器（`playersess`、`playertoolbar`、`localdir`、`focus`、`dirpress`、`toastalign`）、
  列表与分页（`c1`–`c3`、`ux0`、`ux1a`–`ux1d`、`ux2a`/`ux2b`、`ux3`–`ux5`、`uxfix`、`stab`、`smart1`）、
  评论（`comments`）、季节（`season1`）、设置（`settings1`）、启动判据（`firstrun`）、其他（`d1`/`d2`、`ov`）。
- 另有 7 个单文件探针（`LM01`–`LM05` 本地媒体与字幕、`API01`、`AUTH01B`）不建项目：`plan/tests/run.sh`
  用 `$UPSTREAM_UNIT_TOOLS/dotnet/sdk/8.0.427` 的 `csc` 直接编译并运行，末尾还会调用 `run-api01.sh`、
  `run-auth01b.sh`。这个脚本要求那个 SDK 根真实存在（不设时默认 `/workspace/work/upstream-unit-tools`）。
- **脚本必须是 LF**：`.gitattributes` 已钉 `*.sh text eol=lf`；若某个克隆里 `run.sh` 是 CRLF，
  `sh` 会在第一行报 `set: pipefail: invalid option name`，先转换再跑。
- 探针只读产品源码，不改产品源码；修缺陷时优先把断言加进已经拥有该代码的批次。

## 7. 提交与同步

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
| `sh` 报 `set: pipefail: invalid option name` 或路径里带 `^M` | 脚本是 CRLF；按 §6 转成 LF |
| `playersess` 批次抛 `FileNotFoundException` | 该批次读 `Controls/PlayerControl.xaml.cs` 做源码接线断言，而该文件被 sparse 排除；环境问题，不是代码问题 |
| `WMC1500` / `WMC1506` / `PRI257` / `NETSDK1138` | 已知可忽略警告（`DropShadowPanel` 弃用、缺失变更通知的 OneWay 绑定、默认语言资源、`netcoreapp3.0` 过期） |
| 装完启动图一闪就退，无日志 | 装成了 `BiliLite.UWP_*.msix`；改装 `BiliLite.Packages_*.msix` |
| 命令行里中文变乱码 | PowerShell 5.1 按 GBK 输出。调用时在命令里设 `[Console]::OutputEncoding=[Text.Encoding]::UTF8`；读旧 GBK 日志用 `iconv -f GB18030 -t UTF-8 -c` |

## 9. partial clone 提醒

本仓库常用带 `--filter=blob:limit=102400` 的克隆。这种情况下 29 个大于 100 KB 的 blob 不在本地，
`git status` 仍然干净（它们被标记为 skip-worktree），但**构建会失败**：

- `src/BiliLite.gRPC/bilibili/app/dynamic/v2/dynamic.proto`（117 KB）→ `BiliLite.gRPC` 无法编译，
  `BiliLite.UWP` 因此缺 `BiliLite.gRPC.dll`；
- `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs`、`Modules/User/DynamicVM.cs`、`Pages/VideoDetailPage.xaml.cs`、
  `Controls/FontAwesome5/EFontAwesomeIcon.cs` → 编译器报 `CS2001 未能找到源文件`，`playersess` 探针也读不到源码；
- `plan/tests/g3/Program.cs` → `g3` 批次整个不能跑；
- 以及字体、splash 图、`ffmpeg.7z`、`screenshot/ui.png`、`Assets/JiYan/**` 等二进制。

在这种克隆里，代码审查只能做静态阅读，任何「编译通过」的说法都必须先说明这一点。

## 10. 一次改动的验收清单

1. `-Fast -NoVersionBump` 编译 0 错误（或在 partial clone 里明确说明无法编译）。
2. 相关探针批次全绿；新行为至少加一条会在旧代码上失败的断言。
3. 影响布局/播放/账号/生命周期的改动，在 `plan/VALIDATION.md` 里写清「设备上要看什么」，不自行宣称已通过。
4. `plan/PLAN.md` 的 Open/Done 状态与 `docs/ROADMAP.md` 同步更新；缺陷留在 `bug_report.md`。
5. 发布前跑一次不带 `-Fast` 的完整构建，确认产物路径与 `entrypoint/BiliLiter.exe` 布局。
