# Issue 598: no final history report on natural completion

Run with `work/upstream-bugs/unit-tests/batch-041/issue-598/run-natural-end-report.sh` from the repository checkout. The runner extracts the complete current `Player_PlayMediaEnded` method from `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs` by its declaration and balanced braces, then executes it for a non-interactive last item in play mode 0. A recording history-report stub makes no network calls. It asserts exactly one call for the same item at `Player.Duration` (100 in the fixture).

This source-level probe does not validate Bilibili backend behavior or cross-client resume semantics.
