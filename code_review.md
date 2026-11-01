# Review origin/work e9e5d2a1008f4ce16e8209feb52d0f9c4c16fac7

Reviewed all new source since `399807da3a10f7d44c6868c027b4829e270dd736` and reread `bug_report.md`. The new drawer makes loaded collection/playlist rows reachable in full screen. Three worthwhile issues in its interaction remain. Source inspection with Luna reviewers only; no tests, builds, scripts, or application execution. The previous keyboard-index and SDK-restore findings are unchanged by this increment.

## A stale playlist “playing” marker prevents returning from a collection

`src/BiliLite.UWP/Models/PlayDirectory.cs:242–260` marks the playlist's `Index` row as current, regardless of which video is actually playing. That index deliberately stays at the old queue position when a collection video is selected. `Pages/VideoDetailPage.xaml.cs:1370–1375` then treats any `row.IsCurrent` as a reason to ignore activation.

Open playlist row A, choose a collection video X that is absent from that playlist, then browse the drawer's playlist tab. A is still marked “正在播放”; clicking it does nothing, so the user cannot return to A through the directory. The source-switch action also refuses while X is outside the playlist (`VideoDetailPage.xaml.cs:1423–1429`).

Suggested fix: build playlist current markers from the actual playing aid, separately from the saved queue cursor. Only suppress playback when the activated row's identity matches the actual playing item in the appropriate scope. If activating that item chooses a different continuation source, still perform the source switch without restarting playback.

## Full-screen paging and retry still require leaving full screen

`src/BiliLite.UWP/Controls/PlayDirectoryControl.xaml:64–113` exposes rows and source actions, but no load-more/retry control. Its events and model also have no paging action/state. `Models/PlayDirectory.cs:240–291` supplies only the already-loaded playlist rows. The existing manual paging action remains in the detail-column list (`Pages/VideoDetailPage.xaml.cs:606–619`), which full-screen/full-window mode hides.

For a remote list with 20 loaded rows and more available, a user cannot browse item 21 or later in the drawer. If boundary continuation fails, the message still tells them to click “加载更多”, but that button is outside the visible player. They must exit full screen to load or retry; automatic advancement is not a substitute for browsing.

Suggested fix: expose the existing page-owned load-more/retry operation in the drawer, with loaded/loading/failed/exhausted state. Reuse `LoadMediaListPage` and the existing cursor, prevent duplicate requests, and refresh the open directory after loading without starting playback or changing the source.

## Locate-current chooses a part even when the active list is the collection

`src/BiliLite.UWP/Controls/PlayDirectoryControl.xaml.cs:167–178` returns the first `IsCurrent` row across all sections. The combined sections put Parts first (`Models/PlayDirectory.cs:302–305`), and both the current part and outer video retain their current markers. `LocateCurrent` then changes the browsed section to whichever row it found (lines 73–80).

For a multipart video following a collection/playlist, opening the drawer immediately runs this locate action and switches to Parts rather than the active outer list. Clicking “定位正在播放” while viewing the collection repeats the jump. Separately, `SetModel` at lines 56–58 resets the browsed section to `model.ActiveKind` on every redraw, so a part change can pull the user out of another tab they were browsing.

Suggested fix: locate the current row within the browsed section when it has one, otherwise use the active continuation section and its scoped current key. Preserve the browsed section across model refreshes while it still exists; apply the active-source default on a fresh open or when the old section disappears.
