# 本仓库执行参数

- 仓库：`bilibili-uwp-liter`（GitHub `ZzzzzzzSkyward/bilibili-uwp-liter`）
- 工作分支：`dev`（唯一开发分支，也是默认分支；`master` 是发布线，新工作不落在那里）
- 远端：`origin`
- 安装依赖：`Visual Studio 2022` + **通用 Windows 平台开发（UWP）**工作负载 + Windows 10 SDK `10.0.19041.0`（最低 18362），.NET SDK 8.0+（探针用），PowerShell 5.1，Git/MSYS2 bash。NuGet 还原由构建脚本的 `-Restore` 完成（`packages.config` 与 `PackageReference` 两种都处理），不要自己调 MSBuild。详见 `document/BUILD.md`
- 测试：Linux、Bash ≥4、Python ≥3.9，用 `bash plan/tests/run-all.sh --tools-root /absolute/path/to/probe-tools` 跑固定 44 目录（43 net8.0 项目 + shell-only stab）+ 7 逻辑探针 / 8 legacy executables，总计 51 逻辑 / 52 可执行阶段。工具根含 `dotnet/` 的 SDK 8.0.427/runtime 与 ref-pack 8.0.33；`--list` 不跑测试，`--only batch:comments` 等只覆盖所选范围（PARTIAL_PASS）。完整命令/报告/预算/独立 selector 见 `docs/DEVELOPMENT.md` §6；runner controls（14 项）用 `python3 plan/tests/verify-suite-runner.py --tools-root /absolute/path/to/probe-tools`，不计入 51/52
- 构建：`powershell -NoProfile -ExecutionPolicy Bypass -File ./build-release-x64.ps1 -Fast -NoVersionBump -Restore`（编译校验，约 1 分钟，不打包、不升版本号）；出可安装包用 `... ./build-release-x64.ps1 -OutputDir Z:\`（约 2 分钟，会把两个 `Package.appxmanifest` 修订号 +1，须与触发它的改动的同一提交）
- 额外验证：独立布局 probe `powershell -NoProfile -ExecutionPolicy Bypass -File ./plan/tests/package-layout-probe.ps1`（15 内部案例，不计 51/52；Core 可用 pwsh），再按 `document/BUILD.md` 的实际 MSIX 布局核对——安装包取 `BiliLite.Packages_<version>_x64.msix`，包内必须存在 `entrypoint/BiliLiter.exe` 且根 `BiliLiter.exe` < 64 KB（`BiliLite.UWP_*.msix` 在启动图即退出）；布局/播放/账号/生命周期改动按 `plan/VALIDATION.md` 写「设备上要看什么」，不自行宣称已通过
- 入口文档：`README.md`、`AGENTS.md`、`docs/DEVELOPMENT.md`
- 包管理器/运行时：NuGet（`packages.config` + `PackageReference`）；.NET / UWP，Windows 10 18362+ x64，构建经 VS MSBuild；探针为 `net8.0` 控制台（`dotnet run`）
- CI：[offline-probes.yml](../.github/workflows/offline-probes.yml)，dev push/PR 与无输入 workflow_dispatch，Ubuntu 24.04 跑独立 controls + 同一 aggregate 命令。实际 pushed-SHA hosted run 决定 CI 结论，排队/配置/lint/本地通过都不是 hosted PASS；Windows 构建/设备门禁另行执行

## 本仓库注意事项

- 工作副本使用 **partial clone + sparse-checkout**（`--filter=blob:limit=102400`），曾缺 29 个 >100 KB 的 blob，skip-worktree 下 `git status` 仍可干净。先检查当前 checkout 的真实文件；缺失时记录限制，**不要改 sparse 配置，也不要 `git fetch` 这些 blob**。
- **仅在实际缺文件时构建受阻**：`src/BiliLite.gRPC/bilibili/app/dynamic/v2/dynamic.proto` 等被排除 → `BiliLite.gRPC` 编译失败 → `BiliLite.UWP` 缺 `BiliLite.gRPC.dll`（`WMC1006`）；`CS2001` 指向 `PlayerControl.xaml.cs`、`DynamicVM.cs`、`VideoDetailPage.xaml.cs`、`EFontAwesomeIcon.cs` 也是同一原因。`plan/tests/playersess/run.sh` 会因读被排除的源文件抛 `FileNotFoundException`。**这是环境限制，不是代码缺陷**：记录命令与错误后继续，不要改代码、不要拉 blob。
- 习惯 `-Fast -NoVersionBump` 做编译循环：不带 `-NoVersionBump` 的每次构建都会自增两个 `Package.appxmanifest` 的修订号，并必须与改动同提交。
- `build-release-x64.bat` 结尾有 `pause`，脚本/agent 环境禁用，直接用 `.ps1`。
- 探针脚本显式用 Bash：`sh` 可能是 dash，不支持 `BASH_SOURCE`/`pipefail`，LF 文件也会失败。`*.sh` 还必须是 LF；路径/选项带 `^M` 时恢复 LF，换行修复不能替代 Bash。
- Windows PowerShell 5.1 按 GBK 输出中文：调用时设 `[Console]::OutputEncoding=[Text.Encoding]::UTF8`，读旧日志用 `iconv -f GB18030 -t UTF-8 -c`。
- `document/new_version.json`、`document/entrance.json` 是**应用运行时抓取的线上数据**，改它等于给所有客户端推新内容——非必要不动。
- `src/BiliLite.WebApi`、`src/ApiTester` 是不随包发布的遗留旁支，不能作为验证依据；`-Fast` 本来也会跳过它们。
- 探针产物（`plan/tests/**/bin|obj`）已在 `.gitignore` 中，不要提交。
