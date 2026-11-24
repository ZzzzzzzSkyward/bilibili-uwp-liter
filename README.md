# Bilibili Lite UWP

A third-party Bilibili client for Windows 10/11, written in C#/XAML as a UWP application.
`dev` is the development line; `master` carries releases. Code under `src/BiliLite.UWP`,
work plan and offline probes under `plan/`, long-form documentation under `docs/`.

第三方哔哩哔哩 UWP 客户端。上游是 [逍遥橙子](https://github.com/xiaoyaocz/biliuwp-lite) 的 `biliuwp-lite`，
本仓库用 VS2022 与 Windows 10 SDK 10.0.19041（最低 18362）维护，改用亚克力背景、换过包名与版本号，
安装前核对发布包的实际签名与随包证书，说明见 [构建文档](document/BUILD.md)。

## 功能特性

- **浏览**：推荐、热门、动态、分区、影视、直播、排行榜、番剧时间表/索引、搜索、标签与话题页。
- **视频**：DASH 播放（原生 / FFmpegInteropX 软解 / SYEngine 回退）、清晰度与倍速、弹幕（规则过滤、
  按账号同步、导入导出）、播放目录（分P/合集/播放列表/剧集正片与预告、连播）、进度记忆与续播、评论、
  点赞/投币/收藏/三连。
- **番剧**：时间表、索引、排行；剧集页用 `pgc/view/web/season` 取详情、`ep/list` 取正片与预告。
- **动态**：关注流双来源（web 与 gRPC 自适应）、文字/图片网格/转发/视频/专栏/直播/预约/投票卡片、
  发布与互动、十页滚动窗口。
- **直播**：中心/分区/推荐、直播间详情、直播弹幕（监督循环自动重连）。
- **账号**：扫码、密码（隔离 cookie jar）、手动填写会话；cookie 单飞刷新、令牌归属校验、失效提示与重登。
- **本地**：缓存与下载（含 ffmpeg 合并导出）、本地媒体库（清单校验、轨道/字幕/弹幕轨道）。
- **诊断**：文件日志（每会话 id）、响应形状采集（脱敏、可选采样）、一键导出诊断包。

完整清单与状态（完成/部分/缺失）见 [docs/FEATURES.md](docs/FEATURES.md)。

## 快速开始

需要 Visual Studio 2022 + **通用 Windows 平台开发**工作负载 + Windows 10 SDK 10.0.19041，以及 .NET SDK 8.0+（探针用）。

```powershell
# 编译校验（约 1 分钟，不出包、不升版本号）
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-release-x64.ps1 -Fast -NoVersionBump

# 出可安装包（约 2 分钟；每次运行会把清单修订号 +1）
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-release-x64.ps1 -OutputDir Z:\
```

安装 `BiliLite.Packages_<version>_x64_Test\BiliLite.Packages_<version>_x64.msix`（**不要**装
`BiliLite.UWP_*.msix`，那份在启动图就会退出）。完整离线检查在 Linux 上用 Bash ≥4、Python ≥3.9，
准备 8.0.427 SDK/8.0.33 runtime/ref-pack 工具根后运行：

```bash
bash plan/tests/run-all.sh --tools-root /absolute/path/to/probe-tools
```

固定 43 目录 + 7 逻辑探针 = 50 逻辑项 / 51 可执行阶段；选择部分通过只表示 PARTIAL_PASS。
`--list`、选择命令、独立 PowerShell 布局门禁与工具设置见开发文档；
离线 CI 使用同一入口，配置见 [offline-probes.yml](.github/workflows/offline-probes.yml)。
环境、调试、排障与 partial clone 的限制见 [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)，
构建故障逐个症状见 [document/BUILD.md](document/BUILD.md)。

## 目录结构

| 路径 | 内容 |
| --- | --- |
| `src/` | 解决方案的 7 个项目；`BiliLite.UWP` 是应用本体，`BiliLite.Packages` 出 MSIX，`BiliLite.gRPC` 是 protobuf/gRPC 客户端，`BiliLite.JSBridge` 给 WebView 登录页，`BiliLite.Win32Tools` 是完全信任的导出进程，`BiliLite.WebApi` 与 `ApiTester` 是不随包发布的开发期工具 |
| `plan/` | 计划与验收：`PLAN.md`（条目与顺序）、`VALIDATION.md`（设备清单）、`REVIEW_LOG.md`、`P4_INVENTORY.md`，以及专题方案 `CARD_PLAN.md`、`HOT_CARD_PLAN.md`、`REFRESH_PLAN.md`、`SETTINGS_PLAN.md` |
| `plan/tests/` | 43 个目录批次 + 7 个单文件探针，编译产品源码并断言行为；不改产品源码 |
| `document/` | `BUILD.md`（构建与常见构建故障）、`DIAGNOSTICS.md`（日志与反馈）、`BILIBILI_API_RESEARCH.md` + `BILIBILI_API_ENDPOINTS.md`（与姊妹仓库的接口对照）；`new_version.json`、`entrance.json` 是应用抓取的线上数据 |
| `docs/` | [架构](docs/ARCHITECTURE.md)、[功能](docs/FEATURES.md)、[开发](docs/DEVELOPMENT.md)、[路线图](docs/ROADMAP.md)、[索引](docs/README.md) |
| `bug_report.md` | 缺陷报告与处理记录（BR-nn） |
| `code_review.md` | 外部评审的当轮结论与处置 |
| `tools/` | `import-capture.ps1`：把导出的诊断包转成探针夹具 |

## 文档索引

| 文档 | 内容 |
| --- | --- |
| [AGENTS.md](AGENTS.md) | 仓库图、构建/验证命令、架构不变量、C#/XAML 约定、计划流程、陷阱与禁止改动清单 |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | 项目依赖、应用内分层、请求与播放链路、生命周期与窗口、存储位置、扩展点 |
| [docs/FEATURES.md](docs/FEATURES.md) | 面向用户的功能与状态、入口文件 |
| [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) | 环境、分支模型、构建、安装、调试、探针、同步、常见问题 |
| [docs/ROADMAP.md](docs/ROADMAP.md) | 未完成项（含验收口径与下一步）、已知债务、缺陷、停放项、master/dev 关系 |
| [docs/README.md](docs/README.md) | 全部文档的索引与「先读哪一篇」 |

## 当前状态

- 开发线 `dev`：比发布线多 510 个提交（分叉点 `e2c7958`）；最近的批次是 2026-11-08 的 BR-61…BR-67。
- 发布线 `master`：最新发布 `1.0.9.61`（2026-11-07，tag `20261107`）。
- 已知缺口：后台播放归属与内存测量（BR-21）、账号接口的解析重构、与姊妹仓库对照出的评论写入 CSRF
  与直播弹幕发现接口，详见 [docs/ROADMAP.md](docs/ROADMAP.md)。
- 验收策略：编译 + 离线探针 + 维护者设备检查；后者「默认通过」，只有真实报告的缺陷才重开，
  见 [plan/VALIDATION.md](plan/VALIDATION.md)。

## API research

[Bilibili API update and complement backlog](document/BILIBILI_API_RESEARCH.md)
compares this fork with `ywmoyue/biliuwp-lite`, with priorities and pinned source links.
The [endpoint catalog](document/BILIBILI_API_ENDPOINTS.md) lists HTTP and gRPC routes.

## 下载

https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/releases

最新版本：`1.0.9.61`（2026-11-07）

## 其他fork

https://github.com/ywmoyue/biliuwp-lite

## 预览

![screenshot](./screenshot/ui.png)

## 参考

[SYEngine](https://github.com/ShanYe/SYEngine)

[FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX)

[bilibili-API-collect](https://github.com/SocialSisterYi/bilibili-API-collect)

[bilibili-grpc-api](https://github.com/SeeFlowerX/bilibili-grpc-api)

[FontAwesome5](https://github.com/MartinTopfstedt/FontAwesome5)

[waslibs](https://github.com/wasteam/waslibs)
