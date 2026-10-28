# Bug report — current state

Branch `work`. This increment answers review `56a6d93` (round 5) on top of `22a9814`. Rewritten each turn; history lives in the commits.

## Answered this turn

### BR-13 — ownership, not visibility (review §1)

Correct: collapsing the generic presenter left both consumers holding the same `RichTextBlock`. The presenter now binds `Model.PresenterContent`, which returns **`null`** whenever the template renders the body, so the element is handed to exactly one control:

- `DisplayModel.TemplateRendersContent` — `Text`, `Repost`, `Reserve`, plus `Photo` when the template that will be selected actually binds `Content`.
- `PresenterContent` — `TemplateRendersContent ? null : Content`; `ContentPresenterVisibility` stays as spacing/collapse derived from it.

### BR-13 — photo cards keep their rich body (review §2)

Confirmed the reviewer's premise in code: `DynamicParse.ParseOneRowInfo2024:230` sets `data.Content = Render(item.richtext)` for **every** 2024 card while `DynamicVM:1303` sets `ShowContent = false`, so on the gRPC path the template is the only renderer. Dropping the binding from `Photo1x1`/`Photo2x2` therefore lost hyperlinks and emotes there. Binding restored.

Ownership now follows the selector (`DynamicItemDataTemplateSelector:27-41`), not the enum alone: `ImagesInfo.Count <= 1` → `Photo1x1` and `== 4` → `Photo2x2` both bind `Content` (template owns, presenter gets null); every other count → `Photo3x3`, which binds `Desc` only (presenter owns, unchanged behaviour, no redesign). Outer and reposted cards use the same getters, so a repost and its origin card each resolve their own owner.

### BR-05/11 — an empty first REST page must not stop pagination (review §3)

`DynamicVM.cs:650` published the fallback failure whenever page 1 mapped to zero cards, overriding the `CanContinuePaging` result from the line above. Now the failure is published only when `CanLoadMore == false` **and** the accumulated list is still empty, so an empty page with `has_more=true` and an advancing offset keeps paging, and the message appears only after paging actually stops with nothing to show. Same rule applies past page 1.

BR-11 stays as the reviewer decided: fallback kept, real-buvid metadata left as a separate follow-up.

### Build retry: wait for the lock, do not guess (BR-14 follow-up)

The `CS2012` retry failed twice this turn against different processes (5500, then 24748): killing `MSBuild` and sleeping 3 s is a guess. `build-release-x64.ps1` now parses the locked path out of the `CS2012` message, kills `MSBuild`/`VBCSCompiler`/`csc`, polls `[IO.File]::Open(..., FileShare.None)` until it succeeds (30 s cap), and retries at most twice. Documented in `document/BUILD.md`.

## Status

| ID | State |
|---|---|
| BR-01 | resolved — medialist mapping, 收藏夹/合集 split, ad/banner dropped, compact placeholders |
| BR-02 | resolved — music label |
| BR-03 | resolved — synthetic season cards without author/interaction chrome |
| BR-04 | resolved — request generation + queued refresh + owner-only completion |
| BR-05 | resolved — DP owner, short-viewport trigger, filtered-page completion signal, empty-page cap; empty first page now keeps paging |
| BR-06 | rejected — no code |
| BR-07 | deferred — label heuristic kept |
| BR-08 | resolved — HTTP/header status preserved, login-required state with retry |
| BR-09 | resolved — cursor restored on live teardown |
| BR-10 | resolved — fan count via `/x/relation/stat`, `HasFans` gating |
| BR-11 | resolved, kept per review — REST fallback with separate cursors, one gRPC retry per refresh |
| BR-12 | resolved — seven leak sites closed |
| BR-13 | resolved this turn — single owner for `Content`, photo rich body restored |
| BR-14 | done — UTF-8 build output, documented + skilled method, CS2012 retry now lock-aware |

## Verification

- `-Fast`: 0 errors, 100 s.
- Packaging `1.0.9.12`: `Z:\BiliLite.UWP_1.0.9.12_x64_Test\BiliLite.UWP_1.0.9.12_x64.msix` (69,693,102 B) and `Z:\BiliLite.Packages_1.0.9.12_x64_Test\BiliLite.Packages_1.0.9.12_x64.msix` (95,369,893 B). The first attempt at this version hit `CS2012` twice; the rebuilt run took 62 s and needed no retry.
- No runtime/device claim: I cannot run the UWP app here.

## Needs `[device]` confirmation

REST fallback feed with a text/repost/photo card (the crash path), photo 1x1/2x2 rich body, filtered-page pagination, Article signed out, login overlay + retry, cursor after closing a live, fan count on a web-loaded video, leak counts after ~10 video and ~10 live tabs.

## Open for the reviewer

1. `Photo3x3` still shows the body through the presenter while `Desc` comes from the template; `Photo1x1`/`Photo2x2` now show the body from the template. If you want one shape across all three, that is a separate visual change and I would rather not fold it into this fix.
2. BR-11 real-buvid follow-up: do you want it queued, or is the fallback final?
