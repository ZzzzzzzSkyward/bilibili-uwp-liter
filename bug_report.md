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

## Notes

- Install a `BiliLite.Packages_*` MSIX (root launcher ≈ 8 KB plus `entrypoint/BiliLiter.exe`). A
  `BiliLite.UWP_*` package whose root `BiliLiter.exe` is ~4.2 MB cannot start this app.
- 诊断 → 「记录 API 响应形状」 writes one NDJSON line per response (endpoint, status, timings, shape, and a
  drift line the first time a field changes type); 「导出诊断包」 collects the recent logs plus those
  captures and a `manifest.json` into one zip to attach to a report.
