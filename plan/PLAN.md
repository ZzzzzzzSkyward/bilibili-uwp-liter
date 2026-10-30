# Remaining feature plan

Updated 2026-10-30 for `work` at `d78a8a4`. Finished batches are one line each — the commits, the
probe under `plan/tests/` and `IMPROVEMENT_PROGRESS.md` carry their history. Only open items keep
detail, and only the detail an implementer actually needs.

## Done

- **G1 — app-key-aware gRPC headers:** `cfcd3de`, `75e8ecb`; report `plan/G1_REPORT.md`.
- **G2 — user-video keyword search:** `914d32f`; probe `plan/tests/g2`.
- **G3 — dynamic detail through the gRPC mapper:** `d4c6345`, review fix `6ae1981`; probe `plan/tests/g3`.
- **G4 — user-space dynamics with their own paging:** `65d5e8c`, timezone fix `4fca416`; probe `plan/tests/g4`.
- **G5 — dynamic repost list:** `b39739a`, page-1 replacement fix `9fb3d16`; probe `plan/tests/g5`.
- **D1 — danmaku list and actions:** `5e36ec2`; probe `plan/tests/d1`.
- **D2 — danmaku filter import/export and the `SHIELD_USER` key fix:** `e94c843`; probe `plan/tests/d2`.
- **C1 — collections on the video detail page:** `92ecd6a`; probe `plan/tests/c1`.
- **C2 — remote playlist paging:** `34ec312`; probe `plan/tests/c2`.
- **C3 — playlist links and continued playback:** `c735a73`, close-race fix `18f71dc`; probe `plan/tests/c3`.
- **API-SMART-01 — adaptive gRPC/legacy source selection:** `797dc3a`, `26d4e57`, `3665bdb`; spec
  `plan/ADAPTIVE_PLAN.md`, probe `plan/tests/smart1`.
- **Stability items 1/3/4** (tab `Share` leak, audio detach, WebPage close race): `96de3fc`, `0668875`, `546b409`; probe `plan/tests/stab`.
- **Home-feed REST fallback and its quiet load-more** (BR-13, BR-16): `5881dbb`, `e5c97e1`, `261317f`; `plan/tests/g4`.

## N1 — Player overlay toasts

Replace the single central label (`PlayerControl.xaml` → `ToolTip`/`TxtToolTip`) with per-action
overlays, so volume, brightness, seek, message, held acceleration and speed no longer overwrite each
other and held speed stays visible until release.

1. New `Controls/PlayerToast.xaml(.cs)` and `Services/PlayerToastService.cs`; one service instance per
   `PlayerControl`, no DI container. Keys: Volume, Brightness, Progress, Msg, Accelerating, Speed.
2. `Show(key, text)` replaces that key's text and restarts its timeout; a hold API keeps
   Accelerating/held Speed up until release. Ordinary keys auto-hide after ~1.5 s.
3. Rewrite `HandleSlideProgressDelta`, `HandleSlideVolumeDelta`, `HandleSlideBrightnessDelta`, the
   keyboard actions and the held-speed handler to call the service, keeping every existing step
   calculation. One action produces one overlay, not both old and new.
4. Position from the current player size, not the sibling's six fixed slots. Stop all timers and clear
   overlays from `Dispose`.
5. Omit SponsorBlock/skip-segment types; leave the generic `MessageToast` for non-player callers; add
   explicit `Compile`/`Page` entries.

## Release tooling — MSIX artifact path regex

`build-release-x64.ps1` matches artifacts with `->\s+(\S+\.msix)$`, which drops paths containing
spaces and can silently skip layout validation. Capture the whole path or derive it from
`OutputDir`/version, and never treat a missing artifact as success. Keep the layout helpers and the
`CS2012` retry (its holder measurement is in `plan/build-tooling.md`).

## Parked work

- **Live tiles:** optional; needs the sibling's tile service plus a background-task registration, and
  its async refresh must be awaited before the deferral completes.
- **Real buvid acquisition:** no source-backed flow established; app-key headers do not promise it.
- **Alternate danmaku renderer:** nothing needs FrostMaster; keep NSDanmaku.
- **More API/auth variants:** no caller; do not port endpoint classes just to mirror the sibling.
- **Local-library expansion:** the five milestones are done; further discovery/formats/artwork needs
  its own request. Keep the current manifest models as the contract.

## Verification boundary

The app builds here (`build-release-x64.ps1 -Fast -NoVersionBump`) and packages to MSIX, but nothing
has been verified on a device except the items recorded in `bug_report.md`. Anything touching XAML,
layout or timing stays "source + build verified" until the maintainer runs it, and reports must say so.
