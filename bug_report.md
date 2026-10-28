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

## Status

| ID | State |
|---|---|
| BR-01…BR-12 | resolved in earlier rounds (mapping, pagination, transport status, REST fallback, leaks) |
| BR-13 | resolved — single owner for `Content`; verified on device through the `-663` fallback |
| BR-14 | done — UTF-8 build output, documented + skilled method |
| BR-15 | resolved this turn — splash crash was the non-bootable MSIX layout; layout is now validated |

## Verification this turn

- 1.0.9.13: clean rebuild (retry fired once, succeeded), installed, launched, feed loaded (`Navigating to MainPage` → RecommendPage → dynamic tab).
- 1.0.9.14: full build 62 s with the new validation, `rc=0`; both packages inspected (`root=8192`, `entrypoint=4282880`; Packages additionally `BiliLite.Win32Tools/BiliLite.Win32Tools.exe` + `fullTrustProcess`); installed the Packages msix, booted, `appVersion=1.0.9.14`, dynamic tab loads through the REST fallback.
- Validation logic checked against known artifacts: it flags the crashing 1.0.9.12 UWP package and passes 1.0.9.13/1.0.9.14.

## For the maintainer

Install `Z:\BiliLite.Packages_1.0.9.14_x64_Test\BiliLite.Packages_1.0.9.14_x64.msix` — it boots and carries the `fullTrustProcess` extension that video export needs. `BiliLite.UWP_1.0.9.14_x64.msix` also boots but has no export. Do not install a `BiliLite.UWP_*` package whose root `BiliLiter.exe` is ~4.2 MB; that layout cannot start this app.

## Open for the reviewer

1. Should the shipped UWP-only package exist at all, or should the packaging project's output be the only artifact? The layout race disappears if the app project never packages, but I cannot confirm from this machine whether the packaging project still emits the `BiliLite.UWP_*` file on its own.
2. BR-11 real-buvid follow-up: queue it, or is the fallback final?
