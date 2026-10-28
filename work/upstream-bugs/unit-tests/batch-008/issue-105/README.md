# Issue 105: close waits for history report before stopping playback

Run with `work/upstream-bugs/unit-tests/batch-008/issue-105/run-dispose-stop-order.sh` from the repository checkout. The runner extracts `PlayerControl.Dispose()` from `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs` by method and balanced-brace anchors, then executes the actual method with recording player and history boundaries.

The played-item fixture represents an item that has played and is now paused near zero; it checks that player disposal and timer cleanup happen while the history task is pending, and that the captured item, position, and saved resume position are passed through. The unplayed paused-item fixture starts with a restored nonzero position and checks that disposal saves the resume position but skips history while still cleaning up immediately. A natural-end fixture confirms ended playback remains excluded. Current result:

```text
Player disposed while report pending: True
History and resume position captured before report: True
Player disposed after report completed: True
PASS: player cleanup completed before captured history report
Unplayed paused item with restored position was not reported and cleanup ran: True
Natural-end item was not reported and cleanup ran: True
PASS: played history is captured before cleanup; unplayed and natural-end history are skipped
```

The fixture confirms the local cleanup ordering and captured values using the extracted target method. It does not establish real API latency, indefinite playback, or exact audio duration. The helper/network boundary is stubbed; no HTTP request is made.
