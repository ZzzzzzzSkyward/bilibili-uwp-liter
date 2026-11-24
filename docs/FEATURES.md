# 功能清单与状态

状态口径：**完成** = 源码存在且（按 `plan/VALIDATION.md` 的策略）已由维护者的设备检查通过；
**部分** = 主体可用但已知有缺口；**缺失/停放** = 未实现或不打算实现。缺口的原因与下一步都记在
[ROADMAP.md](ROADMAP.md)，不要在本文里另开一份待办。
新修复的“源码/离线完成”不改判历史维护者设备行；其 Windows/真实会话跟进单列在 [VALIDATION.md](../plan/VALIDATION.md)。

## 1. 主界面与浏览

| 功能 | 状态 | 入口 |
| --- | --- | --- |
| 多标签页外壳（WinUI `TabView`，可增删标签） | 完成 | `MainPage.xaml`、`MainPage.xaml.cs` |
| 单页模式（不显示标签栏） | 完成 | `NoTabMainPage.xaml`、设置 `UI.DISPLAY_MODE` |
| 首页：推荐 / 热门 / 动态 / 分区 / 直播 / 影视 | 完成 | `Pages/Home/*`、`Modules/Home/*` |
| 推荐卡片布局（网格/列表两种，首启按屏幕比预设） | 完成 | `Pages/Home/RecommendPage.xaml`、`Modules/Home/RecommendVM.cs`、`Services/FirstRunDisplayMode.cs` |
| 分区、排行榜、标签、话题、找更多 | 完成 | `Pages/RegionDetailPage.xaml`、`Pages/RankPage.xaml`、`Pages/TagDetailPage.xaml`、`Pages/Other/TopicPage.xaml`、`Pages/Other/FindMorePage.xaml` |
| 搜索（综合/视频/番剧/用户/专栏等，标题实体已解码） | 完成 | `Pages/SearchPage.xaml`、`Modules/SearchVM.cs`、`Api/SearchAPI.cs` |
| 通用 Web 页（登录/活动页/网页内容） | 完成 | `Pages/WebPage.xaml` + WebView2 + `BiliLite.JSBridge` |
| 图片查看器（缩放、保存、多图切换） | 完成 | `Pages/Other/ImageViewerPage.xaml`、`Controls/ImageViewerControl.xaml` |
| 深链接/协议激活（`bilibili://` 与站内短链） | 完成 | `Helpers/MessageCenter.cs::HandleUrl`、`App.OnActivated` |
| 最小化后从任务栏回到最大化（BR-61） | 部分：`bbfaf7f` 只编译过，等设备确认 | `Helpers/WindowRestoreHelper.cs`、`App.OnAppResuming` |

推荐恢复的源码/离线行为已完成：首次失败、刷新失败和追加失败各有持久状态与显式重试；刷新/追加失败保留已提交内容，
空页或无新游标停止自动追加，最后一行被成功反馈移除后回到空态。反馈/不感兴趣仅在 typed accepted 回执后改行；
登录成功才继续，且登录和回执等待后仍须属于发起 collection/object。登录窗口异常或取消后，其 show owner 释放标志，
同进程后续重试仍可显示窗口。Windows 真实显示/会话跟进见 VALIDATION。

## 2. 视频

| 功能 | 状态 | 入口 |
| --- | --- | --- |
| 视频详情：信息、标签、相关推荐、状态（点赞/投币/收藏/关注） | 完成（P4 已迁移到解析层） | `Modules/Detail/VideoDetailVM.cs`、`Parsing/Video/*` |
| 点赞 / 投币 / 收藏 / 三连 / 关注 UP | 完成 | `VideoDetailVM.DoLike/DoCoin/DoFavorite/DoTriple/DoAttentionUP` |
| 收藏夹（创建、改名、删除、移动/复制视频、取消收藏） | 完成 | `Pages/User/Favorite*.xaml`、`Dialogs/*Fav*Dialog.xaml` |
| 历史、稍后再看 | 完成 | `Pages/User/HistoryPage.xaml`、`Pages/User/WatchlaterPage.xaml` |
| 播放器：原生 DASH / FFmpegInteropX 软解 / SYEngine 回退 | 完成 | `Controls/Player.xaml.cs`、`Services/PlayerSessionState.cs` |
| 清晰度切换、倍速、音量/亮度浮层、全屏、快捷键 | 完成 | `Controls/PlayerControl.xaml`、`Services/PlayerToastService.cs` |
| 播放目录（分P / 合集 / 播放列表 / 剧集正片与预告、搜索、定位、加载更多、连播） | 完成 | `Controls/PlayDirectoryControl.xaml`、`Models/PlayDirectory.cs` |
| 播放进度记忆与续播、播放历史上报（含关窗/挂起前的落盘与待报尾巴） | 完成（BR-60 起；尾巴部分只编译过） | `Services/PlayerSessionState.cs`、`Modules/Player/*` |
| 侧栏只在有内容时出现条目（弹幕列表 / 节点 / 本地轨道） | 完成（BR-67：从 `Pivot.Items` 增删，而非改可见性） | `Services/PlayerSurfaceVisibility.cs`、`Controls/PlayerControl.xaml` |
| 弹幕（显示、屏蔽词/正则规则、按账号同步规则、导入导出规则文件） | 完成 | `Controls/Player*`、`Modules/DanmuFilterRules.cs`、`Pages/SettingPage.xaml` |
| 弹幕列表（按时间定位、复制、点赞、发送） | 完成 | `Controls/DanmakuListControl.xaml`、`Dialogs/SendDanmakuDialog.xaml` |
| 缓存/下载与导出（含 ffmpeg 合并转码） | 完成 | `Pages/DownloadPage.xaml`、`Modules/DownloadVM.cs`、`BiliLite.Win32Tools` |
| 本地媒体库播放（清单校验、轨道选择、字幕/弹幕轨道） | 完成 | `Pages/LocalPlayerPage.xaml`、`Services/LocalMedia/*` |
| 本地字幕（SRT/VTT/B站 JSON）解析 | 完成 | `Services/LocalMedia/SubtitleTrackParser.cs` |
| 评论：正文先做一次 HTML 解码再转富文本（BR-66），浮窗暗色令牌（BR-62） | 完成 | `Helpers/ControlHelper.cs`、`Dialogs/CommentDialog.xaml` |

新修复的源码/离线行为（[实现与审查](../plan/PLAN.md#2026-11-25-恢复与数据完整性源码已提交离线与独立审查完成)）：

- 收藏夹动作要求真实 typed code0 回执，失败保留已有行/标志，Sort 包含请求/解析异常；详情动作和收藏列表结果归属发起详情，切换视频或同ID重载后不写入替代详情。
- payload 数值越界返回 typed failure；season/comment 映射只跳过 null 行、保留有效邻居，评论原始 ReplyCount 与来源游标不按映射后行数改写。
- A–B 设置先初始化，再以深值快照按接纳顺序落盘；失败不阻断后续写入。本地库导入/重导入/读取共用 registry 事务门，reopen 取得 folder 后释放；不可用 registry entry 是可恢复的 typed registry-read，不静默丢数据。
- 已完成下载扫描在根读取失败时保留旧列表，子项故障显示部分状态；每 GUID 独立观察/取消/退休。取消或永久删除按实际结果反馈，失败项保留重新确认的清理重试；取消失败且原任务未结束时不删除。
- 下载创建准备与清理共享实际路径/子树/revision 保护，媒体传输期间不持有准备 lease；旧清理权限不能删除新工作。进程内已知部分残留（含零/缺媒体）不显示为完成，当前非空 generation 全部成功后才可替代旧部分状态。
- MultiFLV 分段按原序使用不同目的名与元数据，SingleFLV/DASH 命名保持；空或不可用 required media 在目标/generation 变更前拒绝。下载对话框保留 typed/mixed 失败行及 ErrorMessage，显式重试不重复成功项；批次使用发起选择快照，异常后恢复按钮并发现已接纳任务。

以上保持既有请求/表单/metadata/schema/账号兼容，不承诺跨进程、崩溃或重启完整性；native BackgroundTransfer/FAL/文件锁、
Windows/XAML、实际播放/登录/服务写入仍按 [VALIDATION.md](../plan/VALIDATION.md)单独取证，不从离线 PASS 推断。

## 3. 番剧与影视

| 功能 | 状态 | 入口 |
| --- | --- | --- |
| 番剧时间表、索引、排行 | 完成 | `Pages/Bangumi/TimelinePage.xaml`、`AnimeIndexPage.xaml`、`SeasonRankPage.xaml` |
| 剧集详情：`pgc/view/web/season` 取详情、`ep/list` 只取正片/预告（BR-64/BR-65） | 完成 | `Api/SeasonApi.cs`、`Modules/Detail/SeasonDetailVM.cs`、`Parsing/Season/*` |
| 剧集页追番、选集、清晰度 | 完成 | `Pages/SeasonDetailPage.xaml`、`Modules/Season/SeasonIndexVM.cs` |
| 影视（港澳台片单与影视页） | 完成 | `Pages/Home/MoviePage.xaml`、`Api/Home/CinemaAPI.cs` |

## 4. 直播

| 功能 | 状态 | 入口 |
| --- | --- | --- |
| 直播中心、分区、推荐 | 完成 | `Pages/Live/*`、`Api/Live/*` |
| 直播间详情（播放、清晰度、礼物、背包、钱包、醒目留言、粉丝团、头衔） | 完成（BR-53 的解析空引用已修，设备确认 2026-11-08） | `Modules/Detail/LiveRoomVM.cs`、`Pages/LiveDetailPage.xaml` |
| 直播弹幕连接与断线重连（监督循环，2→4→8→10 s 退避，换新 socket） | 部分：`722555b` 只编译过，等一次真实断线 | `Modules/Live/LiveMessage.cs`、`Modules/Live/LiveDanmaku.cs` |

## 5. 动态（t.bilibili.com）

| 功能 | 状态 | 入口 |
| --- | --- | --- |
| 关注动态流：全部/视频/专栏，web 与 gRPC 双源 + 自适应选择 | 完成 | `Modules/Home/DynamicVM.cs`、`Services/DynamicApiSelector.cs`、`Parsing/Dynamic/*` |
| 卡片渲染：文字/图片网格/转发（两级）/视频/专栏/直播/预约/投票/附件 | 完成（CARD_PLAN 切片 1–4 与后续评审修复） | `Controls/Dynamic/*`、`Models/Dynamic/*` |
| 动态详情、用户动态页、话题页 | 完成 | `Pages/User/DynamicDetailPage.xaml`、`Pages/Home/UserDynamicPage.xaml`、`Pages/Other/TopicPage.xaml` |
| 点赞、转发、评论、删除、发布文字/图片动态 | 完成（U1 的四动作 CSRF 拼装已修；真实写入会话仍待验证） | `Modules/User/DynamicVM.cs`、`Modules/User/SendDynamic/*` |
| 信息流滚动窗口（十页滚动窗口、锚点保护、重新加载检查点） | 完成 | `Services/DynamicFeedWindow.cs` |

## 6. 账号

| 功能 | 状态 | 入口 |
| --- | --- | --- |
| 扫码登录 / 密码登录（隔离 cookie jar）/ 手动填写会话 | 完成 | `Dialogs/LoginDialog.xaml`、`Modules/User/LoginVM.cs`、`Services/IsolatedPasswordLoginTransport.cs` |
| cookie 会话校验、单飞刷新、失效提示与重新登录 | 完成 | `Services/CookieService.cs`、`Services/WebRefreshTransaction.cs` |
| 访问密钥（OAuth）续期 | 完成 | `Services/AccessKeyRenewal.cs` |
| 退出登录与清理 | 完成 | `Modules/SettingVM.cs`、`Services/CookieService.cs` |
| 用户空间（信息、投稿、合集、关注/粉丝列表） | 完成 | `Pages/UserInfoPage.xaml`、`Modules/User/UserDetail/*` |

## 7. 设置与诊断

| 功能 | 状态 | 入口 |
| --- | --- | --- |
| 十个页签的设置页（外观与浏览 / 播放 / 视频弹幕 / 直播弹幕 / 下载 / 账号 / 网络与解析 / 诊断 / 帮助 / 关于） | 完成 | `Pages/SettingPage.xaml`、`Modules/SettingVM.cs`、`plan/SETTINGS_PLAN.md` |
| 主题（跟随系统/深/浅、亚克力、标题栏） | 完成 | `App.LoadTheme`、`Themes/*.xaml` |
| 文件日志（每会话 id）与「标记问题」 | 完成 | `Helpers/LogHelper.cs`、`document/DIAGNOSTICS.md` |
| 响应形状采集（端点/状态/耗时/形状/漂移，采样可选、脱敏） | 完成 | `Services/ApiCapture.cs`、`Services/ApiShape.cs`、`Services/HttpLogSanitizer.cs` |
| 诊断包导出（日志 + 采集 + `manifest.json`） | 完成 | `Services/DiagnosticBundle.cs`、`tools/import-capture.ps1` |
| 版本检查与更新提示 | 完成 | `Helpers/Utils.CheckVersion`、`Api/GitApi.cs`、`document/new_version.json` |

## 8. 已知缺口（对应 ROADMAP）

| 缺口 | 现象 | 依据 |
| --- | --- | --- |
| 后台播放归属 | 切标签/最小化后系统媒体控制（SMTC）没有唯一所有者；`OnUnload` 会关掉别的播放器的控制 | `plan/PLAN.md` 第 2 项 |
| 内存随时间增长 | 长会话内存上升，缺分层测量 | `bug_report.md` BR-21 |
| 账号相关的 API 解析（P4 第 4 步） | 账号接口仍在旧的 `GetJson/GetData` 直读路径上 | `plan/PLAN.md` 第 4 项 |
| 与姊妹仓库的 API 差距 | 历史 pinned 对照的 203 个方法/路由对里 38 个无精确对应，不等于 38 个缺失功能；U1/U5/U8/U9 已修，活动直播发现/鉴权 U2 仍受身份/会话证据阻塞 | `document/BILIBILI_API_RESEARCH.md` |
| 直播磁贴、真 buvid、备用弹幕渲染器 | 明确停放，不做 | `plan/PLAN.md` 「Parked」 |
