# Issue 105: close waits for history report before stopping playback

Run with `work/upstream-bugs/unit-tests/batch-008/issue-105/run-dispose-stop-order.sh` from the repository checkout. The runner extracts `PlayerControl.Dispose()` from `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs` and executes the actual method with a recording player and a pending `ReportHistory` task.

The assertions check that player disposal and timer cleanup happen while the history task is pending, and that the captured item, position, and saved resume position are passed through. Current result:

```text
Player disposed while report pending: True
History and resume position captured before report: True
Player disposed after report completed: True
PASS: player cleanup completed before captured history report
```

The fixture confirms the local cleanup ordering and captured values using the extracted target method. It does not establish real API latency, indefinite playback, or exact audio duration. The helper/network boundary is stubbed; no HTTP request is made.
