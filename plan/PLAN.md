# Plan — open work only

Updated 2026-10-30 for `work`. Everything shipped is one line in the commit history; the offline
probe suite lives in `plan/tests/` and is green. Finished plan documents, reports and progress logs
were removed on purpose — `git log` has them.

**Verification policy:** the maintainer's device pass counts as passed. A shipped item stays closed
until the maintainer reports an actual bug; only such a report reopens it, and then it is written up
in `bug_report.md` first.

## State

| Fact | Value |
| --- | --- |
| branch | `work`, in sync with `origin/work` |
| probes | `plan/tests/<name>/run.sh` — c1 52 · c2 99 · c3 97 · d1 82 · d2 58 · g2 22 · g3 34 · g4 75 · g5 44 · stab 53 · smart1 160, all green |
| build | `powershell -NoProfile -ExecutionPolicy Bypass -File build-release-x64.ps1 -Fast -NoVersionBump` (compile only) or without `-Fast` to package into `Z:\` |
| sync | `powershell -NoProfile -ExecutionPolicy Bypass -File sync-work.ps1` — fetch+rebase+push with TLS retries, stashes the two `Package.appxmanifest` around it (`-NoPull`, `-NoPush`, `-Attempts`, `-RetrySeconds`) |
| dependency plan | `plan/DEPENDENCY_UPGRADE.md` (batch 1 done, batch 2 = protobuf toolchain, batch 3 = migrations) |
| review cache | `plan/REVIEW_LOG.md` |
| bug device log | `bug_report.md`, `code_review.md` at the repo root |

## Done

Newest first; every one of these is device-accepted per the policy above.

- **N1 — player overlay toasts:** `312fa23` — `Controls/PlayerToast.xaml(.cs)` and
  `Services/PlayerToastService.cs`, keyed overlays (volume, brightness, seek, message, acceleration,
  speed) replacing the central `ToolTip` label, one service per `PlayerControl`, timers stopped in
  `Dispose`; build 0 errors.
- **Release tooling:** `build-release-x64.ps1` now captures the whole `-> … .msix` path (spaces
  included), derives the two expected package paths from `OutputDir` + version, refuses to continue
  when neither `BiliLite.UWP_*` nor `BiliLite.Packages_*` is found, and names the `CS2012` holders
  through Restart Manager before retrying. Verified with a full packaging run into `H:\uwp\pkg diag`.
- **BR-16 — quiet load-more:** `e5c97e1`, `261317f` — silent page-2 failure, legacy text in the only
  toast, gRPC cursor preserved so the retry resumes.
- **API-SMART-01:** `797dc3a`, `26d4e57`, `3665bdb` — adaptive gRPC/legacy selection for six dynamic
  paths, persisted `dynamic-api-history.v1.json`.
- **C1/C2/C3:** `92ecd6a`, `34ec312`, `c735a73` (+`18f71dc`) — collections, playlist paging, playlist
  links and continuation.
- **D1/D2:** `5e36ec2`, `e94c843` — danmaku list with copy/like, filter import/export.
- **G1–G5:** `cfcd3de`/`75e8ecb`, `914d32f`, `d4c6345`(+`6ae1981`), `65d5e8c`(+`4fca416`),
  `b39739a`(+`9fb3d16`).
- **Stability 1/3/4:** `96de3fc`, `0668875`, `546b409`.
- **Home-feed REST fallback (BR-13):** `5881dbb`.

## Open

| # | Item | State |
| --- | --- | --- |
| 1 | Player open/replace/close session ownership | not started |
| 2 | Background playback ownership (SMTC + memory budget) | not started, needs device evidence (BR-21 recorded 2026-10-31) |
| 3 | AI subtitle label (#1311) — partially mitigated | needs a sibling-supported discriminator |
| 4 | Response-evidence workflow (shapes, drift, bundle, fixture import) | designed in `plan/DIAGNOSTICS_WORKFLOW.md`, P1 not started |

## 1. Player session ownership

`PlayerControl` opens media in several paths (native DASH, FFmpeg DASH, quality change, engine
fallback) and mutates shared player fields across `await`s. Closing the tab or switching video while
an open is in flight lets the old operation resume against a replaced/cleared player, attach A's
source to B's player, or leak resources created after the close routine finished. `SetQuality` starts
`ChangeQuality` without awaiting it; `Player_ChangeEngine` starts another open on media failure with
no in-flight guard.

Fix shape: one session id per open, new resources kept local until the session is accepted, pending
opens invalidated on close/replacement, resources of an obsolete session disposed, callbacks from
obsolete players rejected. Do not rewrite the player.

## 2. Background playback ownership

`backgroundMediaPlayback` is declared in both manifests and `PlayerControl` wires Play/Pause and
status, so missing permissions are not the issue. The gap is ownership: system media controls are
view-scoped, and `OnUnload` disables them for every control, so a departing tab can disable the
controls of the player that is still playing. `App.OnSuspending` also does no player work and forces a
finalizer wait inside the suspension window.

Fix shape: one active playback owner keeps the SMTC session across tab and visibility changes, optional
visual/danmaku work is released when backgrounded, and suspension work stays bounded. Do not recreate
the player on every foreground return.

This one has no device evidence behind it — only source reading. It needs the maintainer's
observation (minimize vs tab switch, engine, crash details) before it is worth a refactor.

## 3. AI subtitle label (#1311)

Upstream issue #1311 asked for AI-generated subtitles (translated tracks with no language field) to be
labelled as AI rather than shown as a normal language track. The shipped fix (`9443244`) keeps any
language label the server does provide and honours the existing AI-marker heuristic, but **fully
unmarked AI tracks and already-saved labels are unresolved**: the track carries no discriminator and
the saved entries lost the discarded fields, so the app cannot tell them apart. Do not guess an API
field to close it; it needs a sibling-supported signal or the subtitle manifest version that carries
one. Nothing else in that 15-finding upstream series is open — the other 14 are fixed.

## Parked

- **Live tiles:** optional; needs the tile service plus a background-task registration whose async
  refresh is awaited before the deferral completes.
- **Real buvid acquisition:** no source-backed flow; app-key headers do not promise it.
- **Alternate danmaku renderer:** nothing needs FrostMaster; keep NSDanmaku.
- **More API/auth variants:** no caller; do not port endpoint classes to mirror the sibling.
- **Local-library expansion:** the five milestones are done; more discovery/formats/artwork needs its
  own request. Keep the current manifest models as the contract.
