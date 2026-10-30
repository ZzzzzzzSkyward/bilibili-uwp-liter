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
| review cache | `plan/REVIEW_LOG.md` |
| bug device log | `bug_report.md`, `code_review.md` at the repo root |

## Open

| # | Item | State |
| --- | --- | --- |
| 1 | N1 — player overlay toasts | in progress |
| 2 | Player open/replace/close session ownership | not started |
| 3 | Background playback ownership (SMTC + memory budget) | not started, needs device evidence |
| 4 | `build-release-x64.ps1` MSIX artifact path regex | not started |

## 1. N1 — player overlay toasts

Replace the single central label (`PlayerControl.xaml` → `ToolTip`/`TxtToolTip`) with per-action
overlays so volume, brightness, seek, message, held acceleration and speed stop overwriting each
other, and held speed stays visible until release.

- New `Controls/PlayerToast.xaml(.cs)` + `Services/PlayerToastService.cs`; one service per
  `PlayerControl`, no DI container. Keys: Volume, Brightness, Progress, Msg, Accelerating, Speed.
- `Show(key, text)` replaces that key's text and restarts its timeout; a hold API keeps
  Accelerating/held Speed up until release; ordinary keys auto-hide after ~1.5 s.
- The slide/keyboard handlers and the held-speed handler call the service instead of writing the
  central label, keeping every existing step calculation; one action produces one overlay.
- Position from the current player size. Stop all timers and clear overlays in `Dispose`.
- Omit SponsorBlock/skip-segment types; leave the generic `MessageToast` alone; add the `Compile`/
  `Page` entries.

## 2. Player session ownership

`PlayerControl` opens media in several paths (native DASH, FFmpeg DASH, quality change, engine
fallback) and mutates shared player fields across `await`s. Closing the tab or switching video while
an open is in flight lets the old operation resume against a replaced/cleared player, attach A's
source to B's player, or leak resources created after the close routine finished. `SetQuality` starts
`ChangeQuality` without awaiting it; `Player_ChangeEngine` starts another open on media failure with
no in-flight guard.

Fix shape: one session id per open, new resources kept local until the session is accepted, pending
opens invalidated on close/replacement, resources of an obsolete session disposed, callbacks from
obsolete players rejected. Do not rewrite the player.

## 3. Background playback ownership

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

## 4. Release tooling — MSIX artifact path regex

`build-release-x64.ps1` matches artifacts with `->\s+(\S+\.msix)$`, which drops paths containing
spaces and can silently skip the package-layout validation. Capture the whole path, or derive it from
`OutputDir`/version, and never treat a missing artifact as success. Keep the layout checks and the
`CS2012` retry (the retry names its file holders through Restart Manager and waits for them).

## Parked

- **Live tiles:** optional; needs the tile service plus a background-task registration whose async
  refresh is awaited before the deferral completes.
- **Real buvid acquisition:** no source-backed flow; app-key headers do not promise it.
- **Alternate danmaku renderer:** nothing needs FrostMaster; keep NSDanmaku.
- **More API/auth variants:** no caller; do not port endpoint classes to mirror the sibling.
- **Local-library expansion:** the five milestones are done; more discovery/formats/artwork needs its
  own request. Keep the current manifest models as the contract.
