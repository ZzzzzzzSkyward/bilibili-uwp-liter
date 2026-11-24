# 路线图

本文是 `plan/` 的**索引**：把 `plan/PLAN.md`、四份专题方案、`document/BILIBILI_API_RESEARCH.md` 与
`bug_report.md` 里的未完成项逐条列出，给出状态、验收口径和下一步动作。详细方案不在这里复制，点链接看原文。

状态：`[ ]` 未完成 · `[~]` 部分完成（等设备或等测量）· `[x]` 已完成。

## 0. 当前基线

| 项 | 值 |
| --- | --- |
| 开发线 | `dev`，tip `25795eb`（2026-11-09，「docs: research sibling Bilibili APIs and migration gaps」） |
| 发布线 | `master`，tip 为 1.0.9.61 同步（2026-11-07）+ 仓库文档化提交 |
| 分叉点 | `e2c7958`（2026-10-31）；`dev` 在此之后 510 个提交，`master` 8 个（两次同步提交的内容 `dev` 已全部包含） |
| 设备验收策略 | 维护者的设备检查「默认通过」；只有真实报告的缺陷才重开（`plan/VALIDATION.md`） |
| 探针 | `plan/tests/` 43 个目录批次 + 7 个单文件探针；最近记录的统一运行是 2026-11-02 的 35 个批次（`TOTAL_FAILURES=0`），此后各批次单独跑通 |

## 1. 未完成项

### 1.1 [ ] 后台播放归属（P1，阻塞：需要设备测量）

- 来源：[plan/PLAN.md](../plan/PLAN.md) 第 2 项、[bug_report.md](../bug_report.md) BR-21。
- 现状：`backgroundMediaPlayback` 已在两个清单里声明，播放器的播放/暂停与状态也接了；缺的是**归属**——
  系统媒体控制是视图级的，`OnUnload` 会给每个控件都关一次，离开标签页会把仍在播放的那个播放器的控制关掉。
  `OnSuspending` 现在有 deferral 与耗时预算，但仍在挂起窗口里强制等待终结器。
- 验收：一个活动播放所有者跨标签页与可见性变化保持 SMTC 会话；切后台释放可选的视觉/弹幕工作；挂起窗口内
  的等待有界；回到前台不重建播放器。
- 下一步：先按 BR-21 拿到分层测量（仅滚动动态 / 仅播放 / 开弹幕 三种行程的 N 分钟后工作集，托管堆 vs 原生），
  再决定是否值得重构。

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
- 验收：给出滚动深度 vs 工作集曲线，并说明是否由卡片模型主导。下一步：与 1.1 合并成一次设备行程。

### 1.5 [x] 与姊妹仓库的 API 差距清单（P0/P1 已定级，代码未动）

- 来源：[document/BILIBILI_API_RESEARCH.md](../document/BILIBILI_API_RESEARCH.md)（2026-11-09，对
  `ywmoyue/biliuwp-lite` `ca2cadd` 的源码对照）、[document/BILIBILI_API_ENDPOINTS.md](../document/BILIBILI_API_ENDPOINTS.md)。
- 结论：203 个方法/路由对里 165 个本地已有，38 个没有完全对应的路由。其中两条是**已确认的本地缺陷**：
  - **U1 / P0 — 评论写入的 CSRF 拼装**：`ApiHelper.GetCSRF(bool)` 无论参数都只返回裸 token，而
    `Api/CommentApi.cs` 把它直接拼在 `type=…` / `message=…` 后面，于是 body 变成 `type=1<token>`，且没有
    `csrf` 字段。验收：`POST /x/v2/reply/action` 与 `/x/v2/reply/add` 的 body 只有一个显式 `csrf=<token>`，
    点赞/回复/删除在真实会话下成功。
  - **U2 / P1 — 直播弹幕发现与鉴权**：本地用 `live/room/v1/Danmu/getConf` 且不使用发现 token；
    需要迁到 `xlive/web-room/v1/index/getDanmuInfo`（WBI + 真 buvid）或 app 回退，按 `data.token`/`host_list`
    鉴权。与 BR-63 的重连是两件事：重连修好了，鉴权路径仍是旧的。
- 下一步：U1 是纯请求构造缺陷、可离线断言（构造出的 body 字符串），适合作为下一批的第一个切片；U2 需要
  真实会话与网络，排在 U1 之后。

### 1.6 [~] 三条只编译过的修复（阻塞：需要真机）

- BR-61 最小化还原（`bbfaf7f`，0 错误）：设备上最小化 ≥10 s 后点任务栏图标应回到原来的最大化状态。
- BR-63 直播弹幕重连（`722555b`）：需要一次真实断线（切网/代理断流）看是否 2→4→8→10 s 退避后重连。
- 第 11 项待报尾巴（`cfc4a15`）：暂停后立即关窗，历史应仍上报一次。

## 2. 已知债务

来源：[plan/PLAN.md](../plan/PLAN.md) § 已知债务（源码内已无 `TODO/FIXME`）。

- [ ] **重写播放器类**（`Controls/Player.xaml.cs`，~175 KB）。阻塞：会动到 BR-1/BR-21/BR-43/BR-54 已验收的
  播放路径，没有设备测量支撑不做。
- [ ] **旧版合集卡逐字段手写解析**（`Controls/Dynamic/DynamicParse.cs` 的 `MediaList` 分支，样例
  `https://t.bilibili.com/625835271145782341`）。阻塞：现代合集已走共享解析器，旧路径只在老接口回退时命中。
- [ ] **多窗口改字形可能撞线程约束**（`Controls/FontAwesome5/FontAwesome.cs`，当前只 `Debug.WriteLine`）。
  阻塞：只在多窗口改图标时复现，缺设备复现步骤。

## 3. 缺陷（`bug_report.md`）

- [ ] **BR-21 内存随时间增长** — 2026-10-31 报告，无日志无测量。见 1.1 / 1.4。
- [x] **BR-31 文案规范** — 两条硬规则已全量改过，新增文案按规则写。
- [x] **BR-53 直播间打开报空引用** — `c0b7eb1` 修好，设备确认 2026-11-08 13:02。
- [~] **BR-61 / BR-63 / BR-54/55/56/57/58/59/60/62/64/65/66/67** — 均已提交，其中 BR-62 已设备确认，
  其余按 §1.3/§1.6 等设备。

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
- [ ] **partial clone 的构建前置**：带 `blob:limit=102400` 的克隆缺 29 个大 blob（含
  `BiliLite.gRPC/.../dynamic.proto`、`PlayerControl.xaml.cs`、`DynamicVM.cs`、`VideoDetailPage.xaml.cs`、
  `EFontAwesomeIcon.cs`、`plan/tests/g3/Program.cs`），因此无法编译，`playersess` 与 `g3` 也跑不了。
  下一步：需要完整校验时用完整克隆或换机器，判据见 [DEVELOPMENT.md](DEVELOPMENT.md) §9。
- [x] **`*.sh` 的换行**：`.gitattributes` 钉住 `*.sh text eol=lf`，Windows 克隆里 `sh` 不再因 CRLF 报
  `set: pipefail: invalid option name`。

## 6. 最近完成

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

`master` 现在是发布线，1.0.9.61 的同步内容已全部在 `dev` 里，且 `dev` 多出 510 个提交。可选做法：

1. **保持现状**：功能继续进 `dev`，发布时从 `dev` 挑一个点同步到 `master`（现状）。
2. **让 `master` 快进到 `dev`**：把发布线一次性推进到 `dev` 的 tip，此后 `master` 只做 tag。需要维护者确认
   发布包从 `dev` 构建是否可接受（`dev` 里有若干「只编译过」的修复）。
3. **反向合并**：把本批次（文档 + 修复）留在 `master`，以后再决定是否重复到 `dev`（已由 2026-11-24 批次
   在 `dev` 上完成，不建议重复）。

本仓库当前**没有**做 `master` → `dev` 的快进；两条线的合流由维护者决定。
