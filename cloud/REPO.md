# 本仓库执行参数

> 由仓库维护者/首个云端轮次校准；改这里不需要改 `CONTRACT.md`。

- 仓库：`bilibili-uwp-liter`（GitHub `ZzzzzzzSkyward/bilibili-uwp-liter`，本地工作副本 `F:/_cloudprep/bili8`）
- 工作分支：`dev`（唯一开发分支，也是默认分支；`master` 是发布线，新工作不落在那里）
- 远端：`origin`
- 安装依赖：`Visual Studio 2022` + **通用 Windows 平台开发（UWP）**工作负载 + Windows 10 SDK `10.0.19041.0`（最低 18362），.NET SDK 8.0+（探针用），PowerShell 5.1，Git/MSYS2 bash。NuGet 还原由构建脚本的 `-Restore` 完成（`packages.config` 与 `PackageReference` 两种都处理），不要自己调 MSBuild。详见 `document/BUILD.md`
- 测试：`sh plan/tests/firstrun/run.sh`，其它批次同样 `sh plan/tests/<batch>/run.sh`（`plan/tests/` 下 43 个目录批次，每个脚本是一行 `dotnet run --project ... -c Debug`，断言行为而非接线）；单文件探针 `sh plan/tests/run.sh` 需 `UPSTREAM_UNIT_TOOLS` 指向含 `dotnet/sdk/8.0.427` 的 SDK 根（默认 `/workspace/work/upstream-unit-tools`）
- 构建：`powershell -NoProfile -ExecutionPolicy Bypass -File ./build-release-x64.ps1 -Fast -NoVersionBump -Restore`（编译校验，约 1 分钟，不打包、不升版本号）；出可安装包用 `... ./build-release-x64.ps1 -OutputDir Z:\`（约 2 分钟，会把两个 `Package.appxmanifest` 修订号 +1，须与触发它的改动的同一提交）
- 额外验证：按 `document/BUILD.md` 的 MSIX 布局核对——安装包取 `BiliLite.Packages_<version>_x64.msix`，包内必须存在 `entrypoint/BiliLiter.exe` 且根 `BiliLiter.exe` < 64 KB（`BiliLite.UWP_*.msix` 在启动图即退出）；布局/播放/账号/生命周期改动按 `plan/VALIDATION.md` 写「设备上要看什么」，不自行宣称已通过
- 入口文档：`README.md`、`AGENTS.md`、`docs/DEVELOPMENT.md`
- 包管理器/运行时：NuGet（`packages.config` + `PackageReference`）；.NET / UWP，Windows 10 18362+ x64，构建经 VS MSBuild；探针为 `net8.0` 控制台（`dotnet run`）
- CI（若有）：无（仓库没有 `.github/workflows`；验收靠本地构建 + `plan/tests/*/run.sh`）
- 模型路由：专家 `gpt-6.1-sol` / `xhigh`；执行者与主会话 `gpt-6.1-sol` / `high`（详见 `CONTRACT.md §2.1`）

## 本仓库注意事项

- 这是 **partial clone + sparse-checkout**（`--filter=blob:limit=102400`）：29 个 >100 KB 的 blob 不在本地，被标记 skip-worktree，`git status` 仍显示干净。**不要改 sparse 配置，也不要 `git fetch` 这些 blob**。
- **构建可能被缺文件阻塞**：`src/BiliLite.gRPC/bilibili/app/dynamic/v2/dynamic.proto` 等被排除 → `BiliLite.gRPC` 编译失败 → `BiliLite.UWP` 缺 `BiliLite.gRPC.dll`（`WMC1006`）；`CS2001` 指向 `PlayerControl.xaml.cs`、`DynamicVM.cs`、`VideoDetailPage.xaml.cs`、`EFontAwesomeIcon.cs` 也是同一原因。`plan/tests/playersess/run.sh` 会因读被排除的源文件抛 `FileNotFoundException`。**这是环境限制，不是代码缺陷**：记录命令与错误后继续，不要改代码、不要拉 blob。
- 习惯 `-Fast -NoVersionBump` 做编译循环：不带 `-NoVersionBump` 的每次构建都会自增两个 `Package.appxmanifest` 的修订号，并必须与改动同提交。
- `build-release-x64.bat` 结尾有 `pause`，脚本/agent 环境禁用，直接用 `.ps1`。
- `plan/tests/**/run.sh` 必须是 LF：Git Bash 下若报 `set: pipefail: invalid option name` 或路径带 `^M`，先转 LF。
- Windows PowerShell 5.1 按 GBK 输出中文：调用时设 `[Console]::OutputEncoding=[Text.Encoding]::UTF8`，读旧日志用 `iconv -f GB18030 -t UTF-8 -c`。
- `document/new_version.json`、`document/entrance.json` 是**应用运行时抓取的线上数据**，改它等于给所有客户端推新内容——非必要不动。
- `src/BiliLite.WebApi`、`src/ApiTester` 是不随包发布的遗留旁支，不能作为验证依据；`-Fast` 本来也会跳过它们。
- 探针产物（`plan/tests/**/bin|obj`）已在 `.gitignore` 中，不要提交。
