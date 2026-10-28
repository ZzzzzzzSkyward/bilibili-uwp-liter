# Issue 105: close waits for history report before stopping playback

Run with `work/upstream-bugs/unit-tests/batch-008/issue-105/run-dispose-stop-order.sh` from the repository checkout. The runner extracts `PlayerControl.Dispose()` from `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs` lines 2582–2610 and executes the actual method body with a recording player and a pending `ReportHistory` task.

The assertion expects player disposal before the history task completes. Current result:

```text
Player disposed while report pending: False
Player disposed after report completed: True
FAIL: player stop is blocked by pending history report
```

The fixture proves the source delays its stop path until history reporting returns. `Player.Dispose()` calls `ClosePlay()`, the code's media stop path. The issue reports that audio continues after closing. This probe does not establish real API latency, indefinite playback, or exact audio duration. The helper/network boundary is stubbed; no HTTP request is made.
