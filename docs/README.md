# 文档索引

仓库里的文档分四组：本目录（长期说明）、`document/`（构建、排障与 API 研究）、`plan/`（计划与探针）、
根目录的维护记录。按你要做的事选入口。

## 本目录

| 文档 | 什么时候读 |
| --- | --- |
| [ARCHITECTURE.md](ARCHITECTURE.md) | 想知道代码怎么分层、一次请求怎么走、播放与账号状态存在哪里 |
| [FEATURES.md](FEATURES.md) | 想知道某个功能有没有、入口在哪个文件、哪些是已知缺口 |
| [DEVELOPMENT.md](DEVELOPMENT.md) | 要搭环境、构建、装包、调试、跑探针、排构建故障 |
| [ROADMAP.md](ROADMAP.md) | 要看还剩什么工作、验收口径是什么、卡在哪里 |
| [../AGENTS.md](../AGENTS.md) | 由 agent（或新接手的人）第一次进入仓库时读：目录图、命令、约定、陷阱、禁止改动清单 |

## `document/`

| 文档 | 内容 |
| --- | --- |
| [BUILD.md](../document/BUILD.md) | 构建脚本参数、`CS2012` 锁文件、能启动的 MSIX 布局、快速编译循环、PowerShell 编码、错误分类 |
| [DIAGNOSTICS.md](../document/DIAGNOSTICS.md) | 怎么让用户取日志与诊断包（设置 → 诊断） |
| [BILIBILI_API_RESEARCH.md](../document/BILIBILI_API_RESEARCH.md) | 与姊妹仓库的 pinned 源码对照、当前修复处置与仍需真实身份/会话证据的活动直播鉴权路径 |
| [BILIBILI_API_ENDPOINTS.md](../document/BILIBILI_API_ENDPOINTS.md) | 姊妹仓库的端点目录（203 个 HTTP 方法/路由对 + 8 个 gRPC 路由），标出本仓库已有/没有 |
| [new_version.json](../document/new_version.json) / [entrance.json](../document/entrance.json) | **应用运行时抓取的线上数据**（版本提示 / 首页入口），不是说明书，改它等于给所有客户端推新内容 |

## `plan/`

| 文档 | 内容 |
| --- | --- |
| [PLAN.md](../plan/PLAN.md) | 主计划：State 表（构建/探针/同步命令）、Done 记录、Open 表、已知债务、各专题的落地形状 |
| [VALIDATION.md](../plan/VALIDATION.md) | 设备验收清单：每个批次在真机上要看什么 |
| [REVIEW_LOG.md](../plan/REVIEW_LOG.md) | 评审结论与处置（含「已修」与「决定不修」），避免重复讨论 |
| [P4_INVENTORY.md](../plan/P4_INVENTORY.md) | API 解析重构的接口盘点、字段表与批次顺序 |
| [CARD_PLAN.md](../plan/CARD_PLAN.md) | 动态卡片：数据契约、正文渲染、图片网格、内存与滚动 |
| [HOT_CARD_PLAN.md](../plan/HOT_CARD_PLAN.md) | 热榜卡片的高度预算与响应式布局 |
| [REFRESH_PLAN.md](../plan/REFRESH_PLAN.md) | 登录刷新：cookie/appkey 归属、对应关系与事务 |
| [SETTINGS_PLAN.md](../plan/SETTINGS_PLAN.md) | 设置页十个页签的顺序、文案与失效控件 |
| [offline-probes.yml](../.github/workflows/offline-probes.yml) | dev push/PR 与无输入手动入口的 Ubuntu 离线 controls + 完整 suite；实际 hosted 结论另看对应 SHA 的 run |
| [tests/](../plan/tests) | `bash plan/tests/run-all.sh` 统一运行 43 目录 + 7 逻辑项 / 8 legacy executables（50/51）；Bash/Python/SDK/选择/报告与独立 controls 见 DEVELOPMENT；独立 PowerShell 包布局探针不计入该总数 |

## 根目录

| 文件 | 用途 |
| --- | --- |
| [README.md](../README.md) | 项目简介、功能概览、下载、目录结构、致谢 |
| [bug_report.md](../bug_report.md) | 缺陷报告（BR-nn）：现象、已核对的路径、下一步需要的证据 |
| [code_review.md](../code_review.md) | 当前这一轮外部评审的发现与处置 |
| [cloud/README.md](../cloud/README.md) | 云端执行工作区入口：读顺序、目录用途与规则速记 |
| [cloud/CONTRACT.md](../cloud/CONTRACT.md) | 云端统一执行规范：分支、角色与模型路由、轮次、文件即通道、提交规则 |

## 约定

- 同一件事只写在**一个**文件里：功能状态在 `FEATURES.md`，未完成项在 `ROADMAP.md`，命令在
  `DEVELOPMENT.md`，不变量在 `ARCHITECTURE.md`，操作纪律在 `AGENTS.md`。
- 新增文档用 kebab-case 英文文件名；`plan/` 保持既有 `UPPER_SNAKE.md` 命名，`plan/tests/` 保持批次目录结构。
- 文档里的提交号、版本号、断言数都取自 `git log` 与探针输出；改完行为要顺手改受影响的那一句。
- 分支：`dev` 是开发线（本文档描述它），`master` 是发布线；两线关系与是否合流见
  [ROADMAP.md](ROADMAP.md) §7。
