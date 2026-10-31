# Bug report — current state

Branch `work`. This increment fixes the 1.0.9.12 splash-screen crash and verifies the round-5 review fixes on the device. Rewritten each turn; history lives in the commits.

## Fixed this turn

### BR-15 — 1.0.9.12 closed at the splash screen (packaging, not source)

Evidence from the machine, before touching code:

- `LocalState/log/20261028.log` has no session line after 23:39; a 1.0.9.12 launch at 23:54 wrote **nothing at all**. `App()` calls `InitializeComponent()` *before* `LogHelper.Initialize()`, so a startup failure had nowhere to log.
- Application log: Event 1000 × 3 (`BiliLiter.exe`, module `KERNELBASE.dll`, `0xe0434352`) and Event 1026 (`.NET Runtime`, empty stack).
- Reproduced by launching the installed build: process gone in ~2 s, same two events.

Both packages were compared entry by entry:

| package | root `BiliLiter.exe` | `entrypoint/BiliLiter.exe` | boots |
|---|---|---|---|
| 1.0.9.12 (installed, crashed) | 4,282,880 | absent | no |
| 1.0.9.13 / 1.0.9.14 | 8,192 | 4,282,880 | yes |

The bootable layout is the packaging project's: an 8 KB launcher at the root dispatching to `entrypoint\BiliLiter.exe`. The app project's own `-t:Package` writes the ~4.2 MB managed exe to the root with no `entrypoint/`, and because both projects emit **the same file name into the same `Z:\BiliLite.UWP_<version>_x64_Test\` directory**, whichever packaging task finished last decided what got installed. On 1.0.9.12 that was the app project's variant.

Fixes:
- `build-release-x64.ps1` validates both packages after packaging: the UWP package must have `entrypoint/BiliLiter.exe` and a root `BiliLiter.exe` under 64 KB; the Packages package must additionally contain `BiliLite.Win32Tools` and `fullTrustProcess`. On a bad UWP layout it re-runs the packaging project's `-t:Package` once, re-checks, and then **fails the build instead of handing over a package that cannot start**.
- The `CS2012` retry now uses `-t:Rebuild` and waits for the file to be openable exclusively, so a half-written `intermediatexaml` cannot be packaged. The previous fixed `Start-Sleep 3` had already failed twice this session against two different processes.
  The holders are now **named** instead of guessed: a Restart Manager probe caught the build's own `csc.exe`
  writing `obj\x64\Release\intermediatexaml\BiliLiter.exe` while the parent `MSBuild.exe` still held the
  previous copy. The script waits for those handles (`-LockWaitSeconds`, default 90 s), kills only build tools
  if the wait times out, then pauses `-LockRetryDelay` (default 3 s) before the `-t:Rebuild` retry.
  The script carries the same explanation next to the recovery code.
- `App()` now initialises logging **before** `InitializeComponent()` and wraps it in `try/catch` that logs to the app log. A future startup failure will leave a stack trace instead of a silent exit.

### BR-13 — verified on the device

1.0.9.13/1.0.9.14 installed, launched, then the 动态 tab driven through UI Automation (`ControlType.TabItem`, selection invoked). Log:

```
00:10:56  stage=request-start generation=1 itemType=All
00:10:57  stage=rest-fallback reason=请求失败,HTTP 200,服务端拒绝: grpc-status=2; grpc-message=-663; bili-status-code=-663,responseBytes=0
00:10:57  stage=rest-feed-request page=1 offset=
00:10:57  stage=rest-feed-items-published page=1 count=20 hasMore=False
```

That is byte-for-byte the sequence that preceded the `MeasureOverride` `ArgumentException` in 1.0.9.10. App still alive 15 s later, zero Event 1000 entries since 00:09, no `ArgumentException` in the log. The double-parented `RichTextBlock` path is closed for both owners.

### BR-16 — a rejected gRPC page still puts its raw message in front of the user

Reported by the maintainer from a live session (`20261030.log`, session `1a4d50e5…`). Loading page 2 of the
video dynamics feed ended in a toast carrying the gRPC rejection text:

```
22:13:19.5845  Home dynamics stage=home-feed page=2 rpc=dynamic_history page=2 cursor=1264756646249234435
22:13:19.8065  HTTP requestId=http-000012 ... dynamic_history status=404
22:13:19.8065  Dynamic api stage=record where=home-feed page=2 src=legacy outcome=EndpointRejection ms=219
22:13:19.8065  Dynamic api stage=fallback where=home-feed page=2 from=legacy to=grpc outcome=EndpointRejection
22:13:19.8705  Dynamic api stage=record where=home-grpc-fallback src=grpc outcome=EndpointRejection ms=62
22:13:19.8705  ERROR Home dynamics stage=feed-failed page=2 | InvalidOperationException: 请求失败,HTTP 200,服务端拒绝:
               grpc-status=2; grpc-message=-663; bili-status-code=-663,responseBytes=0
```

Page 1 of the same session shows the intended behaviour (`grpc` rejected → `home-rest-fallback` → published
20 cards, no toast). What went wrong on page 2:

- The gRPC rejection is silent on its own — that part works.
- `RequestGrpcSourceAsync` writes `_lastFailureMessage = ex.Message` for the *gRPC* attempt, and because the
  gRPC retry runs **after** the legacy attempt, the gRPC text overwrites the legacy failure text. The toast
  therefore shows `-663` — a server-side code the user cannot act on — even though the path that mattered
  (legacy `dynamic_history`, HTTP 404) is what failed first.
- Falling back from a failed load-more page to gRPC *page 1* is not an alternative page: a success would
  replace the 20 rows already on screen. On `page > 1` the loaded rows must survive a failed append.

Wanted behaviour (maintainer): the raw source error must never be shown; a prompt is allowed only when the
legacy source also failed, and it must describe the legacy failure.

Fix (`e5c97e1`, `Modules/Home/DynamicVM.cs`):

- per-source failure text (`_lastLegacyFailureMessage` / `_lastGrpcFailureMessage`) instead of one shared
  field, so a gRPC rejection can no longer overwrite the legacy message;
- `page > 1` never crosses sources — no gRPC restart from page 1 (that would replace the rows already on
  screen), no toast, `CanLoadMore = false`, rows left untouched;
- `page == 1` keeps the single two-source attempt; the toast fires only when the legacy attempt failed and
  carries the legacy text; `HandelError` no longer feeds the exception text into the toast.

Builds clean (`build-release-x64.ps1 -Fast -NoVersionBump`, 0 errors). Device confirmation is still open:
trigger a 404 on `dynamic_history` and check that only the ERROR log line appears.

## BR-17 — 动态 list stops at the first 20 items (reported 2026-10-30)

Symptom from the maintainer: "动态拉到底下不继续加载" — the list stops after the first screen and
scrolling to the bottom loads nothing.

Log evidence (`LocalState/log/20261030.log`, sessions `cfe242a2`, `1a4d50e5`, `edcee1d5`):

- Primary `动态` (全部), `Modules/User/DynamicVM`:
  `request-start` → gRPC `Dynamic/DynAll`/`DynVideo` rejected
  (`grpc-status=2; grpc-message=-663; bili-status-code=-663`, `responseBytes=0`) →
  `rest-fallback` → `feed-rest-request page=1 offset=` (`dynamic_new`, `type_list=268435455`) →
  `feed-items-published source=rest page=1 count=20 hasMore=False offset=`.
  `CanContinuePaging` sees `has_more=false` and an empty cursor, so `CanLoadMore=false` and the grid
  never asks for page 2. Reproduced three times in one session.
- `视频动态` (`Modules/Home/DynamicVM`):
  page 2 `dynamic_history` → HTTP 404 → gRPC fallback to page 1 → `sources-exhausted` →
  `feed-failed page=2` (BR-16 behaviour: quiet, no rows touched).

So both feed tabs are capped at 20 items because **no available source can paginate any more**:

- `dynamic_svr/v1/dynamic_svr/dynamic_history` is gone (HTTP 404),
- `dynamic_new` succeeds but returns `has_more=0` and no `offset` for the all-types `type_list`,
- the app-key gRPC feed (`DynAll`/`DynVideo`) is refused by the server with `-663` on this account,
  and that happens on the first page too, so it cannot serve paging either.

Fix direction: use the cookie-authenticated web feed
`x/polymer/web-dynamic/v1/feed/all?type=all|video&offset=…` as the paginated source (the 专栏 tab
already pages through the same endpoint with `type=article`), mapping its `items[]`
(`basic` / `modules.module_author` / `modules.module_dynamic.major`) into the existing
`DynamicCardModel2024` renderer. Device confirmation required: the first log after the fix must show a
page-2 `polymer` request with a non-empty `offset` and a growing `count`.

## BR-18 — player overlay toast: rightward shift on fade, clipped icon, volume shown as a fraction

Three reports on the same overlay (N1, `312fa23`/`14995ab`), all from the maintainer on device:

1. "音量弹窗在淡出时向右偏移" — during the hide the toast moves right.
2. "速度弹窗的图标下方被遮挡了约20%" — the speed glyph (`Solid_TachometerAlt`) loses its bottom ~20%.
3. "音量条需要展示百分比0-100而不是一个小数" — the volume overlay must read as an integer percentage.

Cause candidates: `PlayerToast.xaml` animates `ToastRoot.Visibility` through Toolkit implicit animations while the
`PlayerToast` stretches inside the code-created centered `StackPanel` (`Services/PlayerToastService.cs`), so any
width change re-centers a fading element; the glyph is clipped because the FontAwesome line box is shorter than the
glyph ink; the volume text is built with `Player.Volume.ToString("P")` at `PlayerControl.xaml.cs:486` and `:2428`
although `Player.Volume` is 0–1 (`Controls/Player.xaml.cs:151`).

## BR-19 — white borders on the player's own buttons

"播放器内的按钮不需要白色边框": twelve `Background="Transparent"` buttons in `Controls/PlayerControl.xaml` still
carry the default `Button` border, which renders as a light outline over the video.

## BR-20 — two top-right player buttons do the same thing

"播放器右上角两个按钮功能冗余": `TopBtnSettingDanmaku_Click` selects the danmaku pivot and opens the settings
`SplitView`; `TopBtnMore_Click` opens the *same* `SplitView` on the player/other pivot. One of the two is a
duplicate entry point to the same pane.

## BR-21 — memory usage keeps growing while the app runs (reported 2026-10-31)

Maintainer: memory use grows over time during a session. No log evidence and no measurement yet — this is the
report, not a diagnosis.

Where the growth is most likely to come from, from what the code already does:

- the player keeps one live `PlayerControl` per opened item, and each holds a `PlayerToastService` plus an engine
  (`Player.xaml.cs`, native DASH / FFmpegInterop DASH / SYEngine swap paths) — sessions are only released through
  the existing `Completed`/`SetPlayItem`/`OnUnload` paths;
- `Home`/`User` dynamic feeds now page indefinitely through the polymer source (BR-17), so the item collections and
  per-item `RichTextBlock`/image state grow with scroll depth;
- every response goes through `HttpResults`, which keeps the deserialized `JObject` alive as long as anything holds
  its `HttpResults`;
- `Modules/Home/DynamicVM` and `Modules/User/DynamicVM` hold `ObservableCollection` items that are never trimmed.

Getting an actionable report needs numbers, not guesses: the size of the working set after N minutes with a
specific journey (feed paging only / video playback only / danmaku on), plus whether the process is managed-heap
bound or native bound. Without that split the "memory budget" plan item (plan/PLAN.md § 2) cannot be scoped.

## BR-17 follow-up — the tab failed completely after the polymer switch (device log 2026-10-31 08:20)

The log proved the direction was right and the mapper was not:

```
stage=feed-polymer-request page=1 offset= generation=1
stage=feed-request failed | System.InvalidOperationException: Cannot access child value on Newtonsoft.Json.Linq.JValue.
   at DynamicGrpcMapper.MapWebDescription(JObject source) → MapWebCard → MapToUserFeedCards → TryUserFeedPolymerAsync
```

`feed/all` returned the feed; `MapWebDescription` did `node["emoji"]?["icon_url"]` and the real payload carries `emoji` as a
string, so the indexer itself threw (`?.` only guards the result). Because the mapping call sat outside the leg's `try`,
the exception escaped `ServeUserFeedAsync` and the tab retried forever with no rows.

Fixed together: the whole web mapping path now routes nested lookups through one `JObject`-casting accessor (author, stat,
description, major/archive/pgc/draw/opus/live, home-video items), and any polymer-leg failure (decode, map, convert)
returns `Failed(MalformedResponse)` so the chain falls through on page 1 and stops quietly on page > 1. Probes: g3 63
(hostile-field-type fixture, the device payload shape) and g4 114 (failed leg ⇒ fall-through, quiet page-2 stop). The g3
fixture was run against the `f9e27df` mapper and throws the same exception as the device stack.

## Reviewer finding — the legacy feed must never outrank a paginating source

Review of `92e544d`: when the selector preferred `Legacy`, both chains were `Polymer → Legacy → gRPC`, so a temporary
polymer failure let `dynamic_new` win and pin the screen to a source that cannot paginate (`dynamic_history` is 404),
hiding a recovered gRPC. Fixed: both chains now keep `Legacy` **last** and only let the selector order gRPC vs polymer
(`Grpc → Polymer → Legacy` or `Polymer → Grpc → Legacy`). The g4 refresh scenario was re-expressed for the new contract
— after a rejected page, a Refresh retries the paginating chain (gRPC asked again, zero legacy calls) instead of
staying on the legacy feed.

## BR-22 动态里的图片是一个灰色占位，点不开

维持者报告并确认：动态流的图片位置只有灰色占位图（`PlaceholderSource`），点击没有任何反应。

原因链（读代码就能闭环，不需要设备）：

1. 渲染只用 `ImagesInfo`，它由 `Controls/Dynamic/DynamicParse.cs` 的 Photo 分支从
   `modules.module_dynamic.major.draw.items[]`（`src`/`width`/`height`）填充；
2. web 聚合流里图片动态的 major 现在是 `MAJOR_TYPE_OPUS`，图片在 `major.opus.pics[]`（`url`/`width`/`height`），
   而 `DynamicGrpcMapper.MapWebMajor` 把 `opus` 映射成 `article`（只有封面 URL，没有尺寸），`draw` 始终为 null；
3. 于是 `ImagesInfo` 是空列表 → `Photo1x1` 模板把 `ImagesInfo[0].ImageUrlWithPar` 绑到空值 → `ImageEx` 只剩占位图；
   同一条绑定也让 `ImageCommand` 拿到 null 参数，点击自然什么都不发生；
4. 同一个卡片的正文也一起丢了：`module_dynamic.desc` 在 opus 动态里是 null，正文只在 `major.opus.summary`。

修复方向：opus 有图 → 映射成 `draw.items` 并给卡片类型 `DYNAMIC_TYPE_DRAW`（无图 → `DYNAMIC_TYPE_WORD`），
`major.opus.summary` 回填 `module_dynamic.desc`；`src` 为空的图片直接跳过；
`LongImage`/缩略图参数抽成 XAML 无关的规则函数，width/height 缺失时一律走 `@400w.jpg` 而不是 `!header` 头部裁切，
并在模板选择器与 `OpenImage` 上加空值保护。
## Status
## BR-22 状态：已修复

`DynamicGrpcMapper` 现在把 `major.opus.pics` 映射成 `draw.items`（`url`→`src`，带 `width`/`height`），
opus 动态的卡片类型按有没有图定成 `DYNAMIC_TYPE_DRAW` / `DYNAMIC_TYPE_WORD`，并把 `major.opus.summary` 回填到
`module_dynamic.desc`（正文不再丢）。`src` 为空的图片条目在映射层就丢掉，`DynamicParse` 的 Photo 分支同样只收非空 URL，
`Index`/`AllImages` 与过滤后的列表对齐；模板选择器把空 `ImagesInfo` 落到文字模板，`OpenImage` 在 `AllImages` 为空时用当前这张图兜底。
长图判定与缩略图参数搬到 `Helpers/DynamicImageRules.cs`：宽高任一缺失一律 `@400w.jpg`，不会再被判成长图去请求 `!header` 头部裁切。

探针：g3 73（新增 opus 图片/纯文字/空 URL 三条契约，旧映射会失败）、ov 29（尺寸规则）、g4/g5 不回归。

| ID | State |
|---|---|
| BR-01…BR-12 | resolved in earlier rounds (mapping, pagination, transport status, REST fallback, leaks) |
| BR-13 | resolved — single owner for `Content`; verified on device through the `-663` fallback |
| BR-14 | done — UTF-8 build output, documented + skilled method |
| BR-15 | resolved — splash crash was the non-bootable MSIX layout; layout is now validated |
| BR-16 | fixed in `e5c97e1` — load-more failures are silent, the toast carries the legacy message; device check open |
| BR-17 | fixed across `0940c6d` + the mapper-totality commit — device proved `feed/all` returns items but `MapWebDescription` threw on a string `emoji`, so the whole tab failed; the web mapper is now total and a failed polymer leg falls through instead of ending the request |
| BR-18 | fixed in `1f22ec1` — fade collapses only after opacity reaches 0 (stack stretches), glyph moved to a `TextBlock` with `TextLineBounds="Full"`, volume through `PlayerToastOverlay.FormatVolume`; the slider tooltips now use `FormatPercent` through `VolumePercentConverter` |
| BR-19 | fixed in two passes — 11 chrome buttons, then the 9 settings-pane buttons, `BtnOpenDanmakuList` and `BtnReplaceShowMode` once the maintainer reported 弹幕按钮仍有白边 |
| BR-20 | fixed in the same commit — `TopBtnSettingDanmaku` and its handler removed; 弹幕 is pivot 2, reachable from More |
| BR-21 | recorded — memory grows over time; needs a measured journey (feed paging / playback / danmaku) |
| BR-22 | fixed — opus-major image posts lost their pictures (`major.opus.pics` was mapped to an article cover list), so the photo card had no image and no click target; pictures now map to `draw.items`, the opus summary back-fills `desc`, and zero-size images no longer get the long-image crop |

## Verification this turn

- 1.0.9.13: clean rebuild (retry fired once, succeeded), installed, launched, feed loaded (`Navigating to MainPage` → RecommendPage → dynamic tab).
- 1.0.9.14: full build 62 s with the new validation, `rc=0`; both packages inspected (`root=8192`, `entrypoint=4282880`; Packages additionally `BiliLite.Win32Tools/BiliLite.Win32Tools.exe` + `fullTrustProcess`); installed the Packages msix, booted, `appVersion=1.0.9.14`, dynamic tab loads through the REST fallback.
- Validation logic checked against known artifacts: it flags the crashing 1.0.9.12 UWP package and passes 1.0.9.13/1.0.9.14.

## For the maintainer

Install `Z:\BiliLite.Packages_1.0.9.14_x64_Test\BiliLite.Packages_1.0.9.14_x64.msix` — it boots and carries the `fullTrustProcess` extension that video export needs. `BiliLite.UWP_1.0.9.14_x64.msix` also boots but has no export. Do not install a `BiliLite.UWP_*` package whose root `BiliLiter.exe` is ~4.2 MB; that layout cannot start this app.

## Open for the reviewer

1. Should the shipped UWP-only package exist at all, or should the packaging project's output be the only artifact? The layout race disappears if the app project never packages, but I cannot confirm from this machine whether the packaging project still emits the `BiliLite.UWP_*` file on its own.
2. BR-11 real-buvid follow-up: queue it, or is the fallback final?
