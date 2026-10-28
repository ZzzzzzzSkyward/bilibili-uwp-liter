# Bug report — current state

Branch `work`, this increment on top of the round-4 push. Rewritten each turn; history lives in the commits.

## Changed this turn

### BR-13 — layout crash right after the REST-fallback feed rendered (from your log)

Sequence recovered from `LocalState/log/20261028.log`:

```
23:02:01.4205  User dynamic VM stage=rest-fallback reason=... grpc-status=2; bili-status-code=-663
23:02:01.6295  HTTP ... dynamic_svr/v1/dynamic_svr/dynamic_new 200
23:02:01.7413  User dynamic VM stage=rest-feed-items-published page=1 count=20 hasMore=False
23:02:01.8347  ERROR ArgumentException: Value does not fall within the expected range.
               at Windows.UI.Xaml.FrameworkElement.MeasureOverride(Size availableSize)
```

Root cause: a legacy `Text`/`Repost`/`Reserve` card gets `Model.Content` set to a `RichTextBlock` (`Modules/User/DynamicVM.cs:1158`), and **two** owners then try to parent that single element — the generic `<ContentPresenter Content="{x:Bind Model.Content}">` in `DynamicItemControl.xaml:80` and the template's `<RichTextControl Content="{Binding Content}">` (`DynamicTemplate.xaml:9,74,175`). One UIElement cannot have two parents; UWP raises `E_INVALIDARG` during the next layout pass, which is why the stack has no application frame. The gRPC path never hits it because the 2024 converter builds `ContentStr` only — the REST fallback (BR-11) exercised the legacy converter for the first time in this tab.

Fix:
- `DisplayModel.ContentPresenterVisibility` = `ShowContent && !TemplateRendersContent`; `TemplateRendersContent` is true for `Text`, `Repost`, `Reserve` — exactly the types whose templates render `Content` themselves. The generic presenter now yields to the template, so the element has one parent.
- `Photo1x1` and `Photo2x2` no longer bind `Content` into their `RichTextControl` (they keep `Text=Desc`); the body is shown by the generic presenter above the card, matching `Photo3x3`, which is how every other card type already works.

Verified by compile only. `[device]`: opening 动态 with the fallback active is what reproduces it; I cannot run the UI here.

### BR-14 — PowerShell output encoding (method, not just a patch)

The recurring mojibake was a producer problem, not a reader problem: `powershell.exe` writes standard output in the console code page (936/GBK here), so any UTF-8 consumer sees `鎴愬憳`. `build-release-x64.ps1` now sets `[Console]::OutputEncoding`, `[Console]::InputEncoding` and `$OutputEncoding` to a BOM-less UTF-8 **before any output** (inside `try`, because a host without a console handle throws). Verified: `... | grep 快速编译完成` matches with no decoder, `rc=0`.

The method is crystallised in two places:
- `document/BUILD.md` → "Encoding: keeping the Chinese output readable": the in-script fix, the ad-hoc `-Command` wrapper, the `iconv -f GB18030 -t UTF-8 -c` fallback (with the warning that omitting `-c` kills the producing process when a byte is invalid), the ASCII-marker rule for greps of unknown encoding, the stable-log-path rule (`H:/tmp`, because MSYS `/tmp` does not survive between calls), and the `${PIPESTATUS[0]}` rule for pipelines.
- Managed skill `windows-powershell-mojibake`, so the recipe is available in future sessions instead of being rediscovered.

## Status

| ID | State |
|---|---|
| BR-01 | resolved — medialist mapping, 收藏夹/合集 split, ad/banner dropped, compact placeholders |
| BR-02 | resolved — music label |
| BR-03 | resolved — synthetic season cards without author/interaction chrome |
| BR-04 | resolved — request generation + queued refresh + owner-only completion |
| BR-05 | resolved — DP owner, short-viewport trigger, filtered-page completion signal, empty-page cap |
| BR-06 | rejected — no code |
| BR-07 | deferred — label heuristic kept |
| BR-08 | resolved — HTTP/header status preserved, login-required state with retry |
| BR-09 | resolved — cursor restored on live teardown |
| BR-10 | resolved — fan count via `/x/relation/stat`, `HasFans` gating |
| BR-11 | resolved — REST fallback when `DynAll` is rejected (`restFeedMode`, separate cursors, one gRPC retry per refresh) |
| BR-12 | resolved — seven leak sites closed |
| BR-13 | resolved this turn — double-parented `RichTextBlock` that crashed layout on REST-fallback cards |
| BR-14 | done this turn — UTF-8 output in the build script + documented/skilled method |

## Verification

- `-Fast` after the template fix: 0 errors, 98 s.
- Packaging: `Z:\BiliLite.UWP_1.0.9.11_x64_Test\BiliLite.UWP_1.0.9.11_x64.msix` and `Z:\BiliLite.Packages_1.0.9.11_x64_Test\BiliLite.Packages_1.0.9.11_x64.msix`. The first attempt hit the known `CS2012` node lock; the script's detect-and-retry cleaned it and rebuilt, so the log carries both errors and both packages.
- Encoding: `powershell ... -Fast -NoVersionBump | grep 快速编译完成` matched directly (no `iconv`), `rc=0`.

## Needs `[device]` confirmation

Dynamic feed via the REST fallback (the crash scenario), filtered-page pagination, Article signed out, login overlay + retry, cursor after closing a live, fan count on a web-loaded video, and leak counts after opening/closing ~10 video and ~10 live tabs.

## Open for the reviewer

1. BR-11: accept the fallback, or replace it with the real-buvid fix (`/x/frontend/finger/spi` → gRPC metadata)?
2. BR-13 changed how photo cards present their body text (generic presenter above the card instead of the template). If the richer in-template rendering of `Photo1x1`/`Photo2x2` was intentional, the alternative is teaching `RichTextControl` to clone blocks instead of re-parenting the element — say which you prefer.
