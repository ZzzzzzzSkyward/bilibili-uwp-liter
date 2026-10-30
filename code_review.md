# Review — `1dcf688bd1ab0171a7df707261b606767fc9df27`

Read `bug_report.md` and reviewed all source changes since `96de3fc673eb5cd03194156657fcfdf97f32e82d` (review commit `bf03273`), using GPT-6-luna reviewers. Static source inspection only; no tests, builds, application execution, package inspection, or other validation.

## Closing a remote playlist during page loading can crash on continuation

`src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs:1027-1030`: let the last loaded queue item finish, then close its tab while the automatic next-page request is pending. `ClosePage()` disposes the player, but a successful response resumes `AdvancePlaylistAsync()`, selects the next row, and calls `InitializeVideo()` on the closed page. There is no check that the tab is still open. The player cleanup sets `dispRequest = null` (`Controls/PlayerControl.xaml.cs:3025-3028`), while the next `InitializePlayInfo()` immediately calls `dispRequest.RequestActive()` (`923-926`). If the subsequent video-detail request succeeds, this produces a null-reference exception through the async selection handler. A direct remote-playlist navigation without a preloaded first page has the same missing close check at `VideoDetailPage.xaml.cs:546-562`.

Record a closed flag or request generation in the page, invalidate it in `ClosePage()`, and stop stale continuations after loading a remote page and after loading video detail, before changing selection or initializing playback. This should cover both the initial fetch and automatic next-page continuation; cancelling the underlying HTTP request is optional.

The previous repost fallback finding is fixed: the first successful REST page replaces the gRPC rows, and later REST pages append normally. No other worthwhile issue found in the playlist models/service, route parsing, or ordinary queue/collection continuation. The bug report still describes the earlier packaging/device round; its reported results were not rerun here.
