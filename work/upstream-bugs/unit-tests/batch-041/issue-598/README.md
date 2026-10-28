# Issue 598: no final history report on natural completion

Run with `work/upstream-bugs/unit-tests/batch-041/issue-598/run-natural-end-report.sh` from the repository checkout. The runner extracts the full `Player_PlayMediaEnded` handler from `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs` lines 2374–2451 and executes it for a non-interactive last item in play mode 0. A recording history-report stub makes no network calls.

The assertion expects a final progress report. Current result:

```text
History report calls on natural end: 0
FAIL: natural completion did not report final progress
```

This confirms the target handler has no active final-progress dispatch; the target `Dispose` path also skips history reporting once `PlayState.End` is set. The pinned sibling reports `Player.Duration`, and the API model documents progress in seconds. This unit does not validate Bilibili backend behavior or cross-client resume semantics.
