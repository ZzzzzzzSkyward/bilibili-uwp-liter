# RF1/P1 — update-danmaku before first item escapes async-void boundary

New supported regression on final Control284be647a5f633b4a39532d26b4a00856bab20eb901963ebafd545a8d52367c0, compared to residual baseline8682d150. Source-only, not an observed Windows crash/RAM attribution.

## Actual supported caller and precondition

A newly constructed real PlayerControl has `CurrentPlayItem == null` (auto-property, no initializer; constructor does not assign it). VideoDetailPage.xaml90 and SeasonDetailPage.xaml22 display the control while their detail/network requests are pending. Neither parent disables/collapses the player/settings for Loading; their progress ring is only a centered ring. TopBtnMore_Click opens SplitView with a null-safe item check, and the persistent DanmakuPivot contains enabled `DanmuSettingUpdateDanmaku` at PlayerControl.xaml245, wired to `DanmuSettingUpdateDanmaku_Click`. Thus the ordinary user route is open a detail page, open More → Danmaku, click Update before the first item is initialized (or after initial detail failure). No arbitrary parallel child request or synthetic native callback is required.

## Failure

The complete actual handler at PlayerControl4070 awaits SetDanmaku(true) without a catch. `IsPlaybackWorkCurrent(null,currentGeneration)` returns true when both captured item and CurrentPlayItem are null. The newly inserted SetDanmaku precheck then dereferences `loadingItem.LocalPlayInfo` **before its try**. This faults the task and escapes through the actual async-void click handler as NullReferenceException. Residual baseline8682 dereferences inside its existing try/catch and contains the click failure; final284be moves the new dereference outside that protection. This is a crash-capable UI exception boundary regression.

## Same-fixture safe proof

Private snapshot of all27 bound fixture/read-only dependency inputs; no same-project build or source mutation. Complete actual SetDanmaku and dependent control methods use the final accepted extractor; complete actual click handler is added verbatim. The same synchronized ControlledContext captures posted async-void exceptions deterministically. Only the source argument changes between the final and old8682 extraction. A normal current-item refresh issues two actual method requests and is the positive control; terminal click makes zero requests and no exception; null represents the actual constructor default and changes no terminal/generation flag. All operation tickets and timers settle.

```text
final284be (exit1):
SAFE refresh state=current taskCompleted=True requests=2 unhandled=0 errors=
SAFE refresh state=constructor-empty taskCompleted=True requests=0 unhandled=1 errors=NullReferenceException
SAFE refresh state=terminal taskCompleted=True requests=0 unhandled=0 errors=
SAFETY failures=1

old8682 (exit0):
SAFE refresh state=current taskCompleted=True requests=2 unhandled=0 errors=
SAFE refresh state=constructor-empty taskCompleted=True requests=0 unhandled=0 errors=
SAFE refresh state=terminal taskCompleted=True requests=0 unhandled=0 errors=
SAFETY failures=0
```

The command is `/workspace/work/maintenance-foundation/quality/dotnet/dotnet run --project /workspace/work/video-memory/fresh-residual-audit/empty-refresh/Check.csproj`, after generating with the frozen snapshot extractor using `--source <284be-or-old8682-Control> --out /workspace/work/video-memory/fresh-residual-audit/empty-refresh/generated`. Preparation is `/workspace/work/video-memory/fresh-residual-audit/prepare.py`; it snapshots final284be and generates the final side. No original owner test or expected-safe assertion was changed.

## Narrow required amendment

Reject an absent current item before the new precheck/lease work can dereference it, preserving terminal/current-generation and current-item refresh behavior. A null-safe private current-work rule or an explicit no-item SetDanmaku entry guard can provide this; do not move unrelated failures into silent catches or initialize a fake product item. Add the actual enabled click→SetDanmaku boundary to the owning fixture for the constructor no-item state, a current-item refresh and terminal/closed entry; assert no unhandled UI exception and no unauthorized request/lease/UI publication. Native UI/runtime validation remains separate.

- Private `/workspace/work/video-memory/fresh-residual-audit/empty-refresh/final.log` SHA256 `c3f78d6b35f35c92aa3e0262d795e614b91b108df882f68b148053e446c1ada1`.
- Private `/workspace/work/video-memory/fresh-residual-audit/empty-refresh/old8682.log` SHA256 `884426873e4f8cccf00cdfbf806bbbebe84dad16ebb18a214897933335ffac2b`.
- Private `/workspace/work/video-memory/fresh-residual-audit/prepare.py` SHA256 `ec6c34b33cd5be2347663f3bf70f4c331f84c269178ab7c3bdecdbdb9bf46b47`.
- Private `/workspace/work/video-memory/fresh-residual-audit/empty-refresh/Main.cs` SHA256 `6af3c7c796da87284519420b838115c044ac37b717e63571cc40610a3d495d6c`.
- Private `/workspace/work/video-memory/fresh-residual-audit/empty-refresh/RefreshEntry.g.cs` SHA256 `095f2597148f5646e346eace1005245b514cfd4eb9665babc09e864bd48f554c`.
- Private `/workspace/work/video-memory/fresh-residual-audit/before.json` SHA256 `cd05bacde23261afc83503e4f3422a9e5ccd0b6df505f287d30558b61205f934`.

The final independently captured comparison driver is `python3 /workspace/work/video-memory/fresh-residual-audit/run-empty-comparison.py`, exit0 after enforcing actual expected subprocess exits old8682=0/final284be=1 and unchanged same-fixture bytes. `comparison.json` binds expanded commands, source/fixture/log hashes.
