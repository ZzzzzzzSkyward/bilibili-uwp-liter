# Bug report — open items only

Branch `dev`. Solved entries were removed on purpose; the fixes and their evidence are one line each in
`git log`, and the reviewer's findings live in `plan/REVIEW_LOG.md`.

**Policy:** the maintainer's device pass counts as passed. A fixed item stays closed until a real bug is
reported; a new report starts a new entry here before any code changes.

## BR-21 — memory usage keeps growing while the app runs (reported 2026-10-31)

Maintainer: memory use grows over time during a session. No log evidence and no measurement yet — this is
the report, not a diagnosis.

Where the growth can plausibly come from, from what the code already does:

- the player keeps one live `PlayerControl` per opened item, and each holds a `PlayerToastService` plus an
  engine (`Player.xaml.cs`, native DASH / FFmpegInterop DASH / SYEngine swap paths) — sessions are only
  released through the existing `Completed`/`SetPlayItem`/`OnUnload` paths;
- the `Home`/`User` dynamic feeds page indefinitely through the web source, so item collections and the
  per-item `RichTextBlock`/image state grow with scroll depth;
- every response goes through `HttpResults`, which keeps the deserialized `JObject` alive as long as
  anything holds its `HttpResults`;
- `Modules/Home/DynamicVM` and `Modules/User/DynamicVM` hold `ObservableCollection` items that are never
  trimmed.

What an actionable report needs: the working set after N minutes for one specific journey (feed paging only
/ playback only / danmaku on), and whether the process is managed-heap bound or native bound. Without that
split, the background-playback and memory work in `plan/PLAN.md` cannot be scoped.

## BR-31 — 文案规范：短文本不写解释，图标不与文字重复（维护者 2026-11-01）

两条硬规则，适用于全部用户可见文案：

1. 短文本不携带解释性描述。反例：`自动连播（分P及下一视频）`、`下一P：<标题>`、`播放列表 · 第7项 · 连播来源，接下来`。标签只说控件做什么；
   解释留给悬浮提示、帮助文本，或者干脆不要。
2. 图标不与文字重复。反例：`[时钟]进度：01：23`。要么图标 + 纯数值，要么文字标签 + 不带图标，不能两边都说同一个词。

新增文案直接按这两条写；一次性的大扫除已经做完，不再保留逐条清单。

## BR-40 — 浏览器 cookie 只按 `bilibili.com` 读，`ac_time_value` 这类主机级 cookie 永远读不到（维护者 2026-11-01 追问能否自动续期）

`Services/CookieService.cs` 的 `WindowsCookieStore.Read` 只查询 `https://bilibili.com` 这一个 origin，写入也只按根域写。B 站登录下发的 `ac_time_value`
（刷新令牌）是 `www.bilibili.com` 的主机级 cookie，不带 `Domain=.bilibili.com`，所以 `GetCookies(bilibili.com)` 看不到它：
`MirrorRefreshToken` 永远读到空串，续期链因为缺令牌根本走不起来。`Modules/User/Account.cs` 自己的 `CookieLoginOrigins()` 已经枚举四个 origin，
两处口径不一致。

触发：扫码/网页登录拿到会话后，cookie 罐里的 SESSDATA 可用（`cookie/info` 10-30 与 10-31 共 12 次全部 code 0），但日志里 `passport-login/web/cookie/correspond`
从没出现过；11-01 的 12 次 `cookie/info` 全部 -101，会话到期后没有任何续期动作，于是画质掉到 480p。

需要的修复：读 cookie 时按同一条 origin 列表查（首次非空为准），写 cookie 落到根域与 www；origin 列表与 `Account` 共用一份，不再各写一套。

## BR-41 — 构建产物把不能装的那个包留在同一个目录，装错就一启动就闪退（维护者 2026-11-01）

`build-release-x64.ps1` 一次全量打包会产出两个 MSIX：可安装的 `Z:\BiliLite.Packages_<版本>_x64_Test\` 和它旁边的
`Z:\BiliLite.UWP_<版本>_x64_Test\`。后者是副产物：根目录是 4.5 MB 的托管主程序、没有 `entrypoint\BiliLiter.exe`，装上之后一启动就闪退。
维护者安装 1.0.9.26 时选中的就是这个副产物，应用一启动即 `0xe0434352` 退出，日志里连一行都没有。

证据：安装目录 `C:\Program Files\WindowsApps\BilibiliUWPLiter_1.0.9.26_x64__wskk75rcr36e6\BiliLiter.exe` 的 SHA-256 前 16 位
`bb9a699166dbe34a`，与 `Z:\BiliLite.UWP_1.0.9.26_x64_Test\BiliLite.UWP_1.0.9.26_x64.msix` 里的 `BiliLiter.exe` 一致，
而可安装包里该文件是 8192 字节的启动器（`0d12e88368a55bb8`），`entrypoint/BiliLiter.exe` 才是 `bb9a699166dbe34a`。整个安装目录没有 `entrypoint\`。

需要的修复：全量打包在交付包布局校验通过之后删掉副产物的 MSIX（连同只剩下它的 `_Test` 目录），`Z:\` 上不再并排留一个装上去会闪退的同名包；
历史遗留的副产物目录一并清掉。

## BR-42 — 评论里的图片加载失败（维护者 2026-11-01）

维护者：评论区里的图片似乎加载不出来。现象未细分，需要先定位是整类（表情/大表情/评论附图）都失败，还是只有某一种域名或某一种引用方式失败，
以及有没有占位图、重试、控制台诊断日志可用。

已知背景：`i0.hdslb.com` 这类图床在没有 `Referer` 时返回 200，所以不是简单的防盗链拒绝；需要看 App 里评论图片是怎么构造 `Image`/`BitmapImage` 的
（URL 是否被规范化、是否带 `@` 缩放后缀、是否是协议相对地址），以及同一套图片控件在别的页面（封面）是否正常。

需要的修复：定位后在源头修，不在渲染处吞错；如果是图片 URL 需要参数或请求头，就补在统一的图片加载入口，而不是每个模板各补一次。

## Notes

- Install a `BiliLite.Packages_*` MSIX (root launcher ≈ 8 KB plus `entrypoint/BiliLiter.exe`). A
  `BiliLite.UWP_*` package whose root `BiliLiter.exe` is ~4.2 MB cannot start this app.
- 诊断 → 「记录 API 响应形状」 writes one NDJSON line per response (endpoint, status, timings, shape, and a
  drift line the first time a field changes type); 「导出诊断包」 collects the recent logs plus those
  captures and a `manifest.json` into one zip to attach to a report.
