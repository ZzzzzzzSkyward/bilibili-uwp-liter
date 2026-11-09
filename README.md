# Bilibili Lite UWP

逍遥橙子开发的https://github.com/xiaoyaocz/biliuwp-lite

是第三方哔哩哔哩UWP客户端

## 更新说明

使用VS2022，Win10 x64 21H2版本升级了所有依赖。最低版本18362。

使用亚克力背景。

签名到2026年过期。

包名换了一个，版本号改了。

## 目录结构

| 路径 | 内容 |
| --- | --- |
| `src/` | 解决方案的各个项目；`BiliLite.UWP` 是应用本体 |
| `plan/` | 计划与验收：`PLAN.md`（条目与顺序）、`VALIDATION.md`（设备清单，默认通过）、`REVIEW_LOG.md`、`P4_INVENTORY.md`，以及四份专题方案 `CARD_PLAN.md`、`HOT_CARD_PLAN.md`、`REFRESH_PLAN.md`、`SETTINGS_PLAN.md` |
| `plan/tests/` | 每个批次的探针，编译产品源码并断言行为；不改产品源码 |
| `document/` | `BUILD.md`（构建与常见构建故障）、`DIAGNOSTICS.md`（日志与反馈） |
| `bug_report.md` | 维护者报告与处理记录（BR-nn） |
| `code_review.md` | 外部评审的当轮结论与处置 |

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
