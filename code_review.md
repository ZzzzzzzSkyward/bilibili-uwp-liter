# Review — `797dc3a75addda681e11e6dc16d03796cb8592cd`

Read the current `bug_report.md` and reviewed all source changes since `5e36ec21b4c80fa75cabadd06bf63274902c207b` (completed review `df790a4`), using GPT-6-luna reviewers. Static source inspection only; no tests, builds, application execution, or other validation. The bug report's earlier device/build results were not rerun here.

## Selecting another video during a detail request can block all later selections

`src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs:284-286`: while video A's detail request is pending, select a different queue or UGC item B. Both selection handlers can call `InitializeVideo()` during loading. It assigns `_id = B` before checking `flag` (`268-269`), then returns because A is still loading. When A finishes, the new `_id != id` guard returns without clearing `flag`. Neither request reaches playback initialization, and every subsequent selection returns at the same loading check. The tab cannot load another video until it is closed and reopened.

If selections during loading should be ignored, check `flag` before assigning `_id`, so a rejected selection cannot invalidate the active request. If the newest selection should replace the old one, use a request generation and make loading-flag cleanup belong to the active generation; each accepted selection must actually start a request.

The previous missing-source finding is resolved: `Services/DynamicApiSelector.cs` is now committed. The selector currently has no production callers, so it does not yet change feed routing. No other worthwhile issue found in its algorithms or the structured gRPC outcome changes.
