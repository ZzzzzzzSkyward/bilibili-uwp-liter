**Long-running playback, tabs, and WebView investigation**

2026-10-29. There is a significant remaining leak in the media-tab close path, plus unfinished player operations that can outlive the player they belong to. I would address those before trusting this build for long sessions. They fit parts of the reported symptoms, but the exact cause of the observed video crash is still unknown. Background silence also remains unexplained; the source already declares the permission needed for background playback.

This was source investigation only. No application code, project configuration, manifests, or build scripts were changed. No installs, restores, builds, tests, application runs, memory measurements, or validation commands were performed. Four GPT-6 Luna subagents investigated tabs/WebViews, player ownership, background behavior, and long-session requests/errors; their findings were reconciled against source and callers. Small cleanup concerns without a convincing connection to the symptoms are omitted.

The workspace has unrestricted enforced HTTP access through its supplied proxy. `work` was pulled from GitHub and configured to track `origin/work`. This sets up the source-research workspace; it does not establish a Windows/UWP build environment.

| Source | Revision inspected |
| --- | --- |
| [Target: ZzzzzzzSkyward/bilibili-uwp-liter, work][target] | `59f89eb1812ec30a82ca2512729b97e10b2c9604` |
| [Sibling: ywmoyue/biliuwp-lite, dev][sibling] | `ca2caddc597b3faf05b4dac6dcb226aa0694365b` |

The sibling was absent locally, so it was cloned under `/workspace/work/lifetime-investigation/`. No applicable `AGENTS.md` was found. I interpret “frontend/backend switching” primarily as moving the app between foreground and background; player engine switching is covered too. The existing bug report describes a resolved packaging/startup crash, which is different from the long-session playback crash reported here.

**1. Closing media tabs still leaves pages attached to the window — high confidence, fix first.**

Video, season, and live pages subscribe to the current window's Share event, `DataTransferManager.DataRequested`. They remove that subscription in `OnNavigatedFrom`. However, the tab close button does not navigate away: [MainPage.ClosePage, lines 204–214][tab-close] clears the page's grid, raises a custom `ClosedPage` event, and removes the tab. [MyFrame.Close][frame-close] only raises that event. None of the three page close routines removes its Share subscription.

The reference chain is: **window's Share service → event handler → closed media page → its remaining controls, view model, and collections**. Clearing a grid or disposing a player does not remove this chain. Repeatedly opening and closing media tabs can therefore retain more page objects for the lifetime of the window. This is a concrete source-level retention bug; the amount of retained memory has not been measured, and it does not mean every closed decoder remains active.

The exact subscribe/unsubscribe/close locations are [VideoDetailPage, lines 123–151 and 190–194][video-events], [SeasonDetailPage, lines 70–96 and 129–133][season-events], and [LiveDetailPage, lines 418–448 and 476–480][live-events]. A later Share action can also invoke those old handlers: they read `VideoInfo`, `Detail`, or `liveRoomVM` after close cleanup clears that state. That gives this leak an additional visible failure path.

This is an **incomplete BR-12 fix**, not a claim that the previous work did nothing. Commit [`2a6d2f1`][br12] added useful navigation cleanup, but tab close bypasses that cleanup. The sibling still has equivalent Share subscriptions without matching cleanup in the inspected pages, so copying it will not solve this.

Practical change: use one repeatable cleanup routine for permanent tab close and navigation departure, and remove window-level subscriptions there. Make Share belong to the active page so old or hidden pages cannot answer another page's Share request. No broad navigation rewrite is necessary.

**2. Video opening can finish after close or replacement — high confidence race, strong crash candidate.**

The low-level player changes shared fields while awaiting media creation. For example, [native DASH opening, lines 274–303][native-open] closes the old player, assigns `_playerVideo`, awaits an adaptive source, then uses `_playerVideo` again. [FFmpeg DASH opening, lines 610–653][ffmpeg-open] awaits video/audio source creation and then assigns new players. Neither operation checks whether it still owns the current playback session after those waits.

A concrete sequence is: start opening video A; while its media source is loading, close its tab or start video B; A's unfinished operation then resumes. It can use a cleared/replaced player field, attach A's source to B's player, or create resources after the close routine has already finished. Overlapping FFmpeg opens can overwrite fields and lose the place where an earlier native resource would have been explicitly cleaned up. Already queued player callbacks also read shared current state rather than consistently rejecting events from an old player.

This is reachable through quality/item changes and automatic engine fallback. [SetQuality, line 1798][quality-start] starts `ChangeQuality` without awaiting it; [ChangeQuality, lines 1985–2017][quality-change] can await another URL request and a player open; [Player_ChangeEngine, lines 2892–2917][engine-change] starts another open on media failure without a session or in-flight guard.

There are already useful item-generation checks around subtitle/danmaku work and `SetPlayItem`. They do not protect these lower-level opens or make the unawaited quality operation finish before `SetQuality` returns. The gap remains below those earlier fixes.

Practical change: give each open one owner/session ID, keep newly created resources local until that session is accepted, invalidate pending opens on close, and dispose any resources returned for an obsolete session. Serialize replacement/fallback and reject callbacks from obsolete players. A session ID means simply “this result still belongs to the video now selected.” This is the most useful focused refactor here. It can prevent stale playback and resource ownership mistakes; claiming it fixes the observed hard crash would require device evidence that is outside this investigation.

**3. Separate audio is detached from the wrong element — certain defect, native impact unknown.**

[ClosePlay, lines 1555–1558][player-close] checks whether `mediaPlayerAudio` has a player, then calls `SetMediaPlayer(null)` on **mediaPlayerVideo** again. The audio element therefore keeps its attachment to the old audio player. The separate audio path is real: [FFmpeg DASH playback attaches it at line 770][audio-attach].

The code does explicitly clear and dispose `_playerAudio`, so this alone is not proof of unlimited decoder memory growth. It does leave a UI element referring to a disposed native player, which is a poor lifetime boundary during repeated opens, fallbacks, and closes. The sibling contains the same mistake. Correct the audio detach as part of the player ownership work; stop/detach consumers and clear their sources before releasing decoder resources, with cleanup safe to call more than once.

**4. WebView close can race initialization; native shutdown is less explicit than the sibling's.**

[WebPage_Loaded, lines 27–39][web-init] and `OnNavigatedTo`, lines 67–85 both await `EnsureCoreWebView2Async` and then access the mutable `webView` field. [Close cleanup, lines 59–65][web-close] blanks the WebView, removes it, and sets that field to null. Closing while initialization is outstanding lets a continuation access a field that has already been cleared. The initialization paths have no local closed-session check or failure handling. Closing also attempts navigation even when initialization may still be unfinished.

That is a real timing defect, with possible exceptions or partially completed close operations. It is not evidence that every closed WebView leaks. There is no separate long-lived managed event root identified for the target WebPage, and an unreachable frame/history cycle alone is not a leak.

The target never explicitly calls `WebView2.Close()` here; [the sibling does][sibling-web]. WinUI 2 provides that [Close API][webview-api]. Use one shutdown path that marks the page closed, prevents late initialization/navigation callbacks from changing it, detaches handlers, and explicitly closes the control. Treat persistent browser/native-memory retention as a risk, not a measured finding. Forced `GC.Collect()` is not a substitute for that lifecycle.

**5. Background playback has the basic setup, but ownership and memory handling have gaps.**

Both [app][app-manifest] and [packaging][package-manifest] manifests declare `backgroundMediaPlayback`. [PlayerControl, lines 315–363][smtc-load] enables Play/Pause and handles media buttons, and [lines 2682–2734][smtc-status] update playback status. Missing permission, missing button handlers, and missing status updates are therefore not supported explanations for the muting. There is also no explicit “mute when minimized” code in the inspected paths. An unset `AudioCategory` alone does not establish the cause.

The stronger source lead is that transport-control ownership follows the visual control's load/unload cycle. Every PlayerControl obtains the same view-scoped system media controls. [On unload, lines 301–312][smtc-unload], it removes its handler, clears metadata, and disables those controls; this handler does not stop the underlying player. Moving from a video tab to a nonvideo tab can thus leave playback and the UI-owned media controls with different lifetimes if the tab content unloads. With multiple video controls, a departing control can also disable the shared controls without checking which player currently owns them. Exact tab load/unload ordering was not observed here.

This is a plausible explanation for disappearing controls or inconsistent background playback, especially around tab changes and the FFmpeg path that uses a shared timeline and mixed automatic/manual command management. Microsoft documents [manual system-control integration for timeline/multiple-player scenarios][manual-smtc]. It does **not** establish that this is why the user's audio became silent.

Minimizing the app while the same video tab stays selected is a different case. There is no source evidence that minimizing by itself invokes the tab unload routine. If that is the exact trigger, the Share leak, accumulated media resources, OS media-session behavior, or native playback failure remain candidates; no single cause can be selected from this source pass.

There is a second long-session gap: no foreground/background memory-budget handlers were found. [App.OnSuspending, lines 237–245][suspend] logs and forces a collection/finalizer wait, but does not manage player state or release optional UI resources when entering the background. Microsoft explains that [background media apps must stay within the applicable memory budget][background-guide]. A collection cannot free pages still held by the Share event, and synchronous finalizer waiting can consume the limited suspension time. This is a meaningful resilience gap, not proof that Windows killed this particular process.

Practical change: keep one active playback/session owner responsible for system media controls across tab and app visibility changes. Let background audio continue through that owner, release optional visual/danmaku work and unused page content when backgrounded, and keep suspension work bounded. Do not blindly restart or recreate a player every time the app returns to the foreground.

**What the current error handling can and cannot do**

The app has persistent session logs, playback open/failure breadcrumbs, FFmpeg logging, and exception hooks. Those are useful. However, [both global managed exception handlers][exceptions] mark arbitrary errors handled and show a toast; they do not restore a consistent player/UI state. After an obsolete callback fails halfway through a replacement, keeping the process alive does not undo the partial change. Recover expected network/media failures at their owner, dispose the failed session, and leave the UI able to retry. Global logging is a last resort, not recovery. Simply changing the global handlers to terminate on every error would not solve these ownership defects either.

Native decoder failures, OS termination, and severe memory failures can end the process without a useful managed traceback. The supplied history concerns another startup/package issue, and no crash dump or log for this long-session playback failure was supplied. Therefore this report cannot name a faulting native module, distinguish an OS memory termination from a decoder fault, or claim a reproduced crash.

The useful future evidence would be the existing exported diagnostic log around the failure, the exact action (minimize, switch tab, change quality, or open a new video), selected engine, and Windows crash/termination details. Session age, active/closed tab counts, playback session ID, app memory usage/limit, and lifecycle/close breadcrumbs would make that evidence much more informative. These are recommendations only; no instrumentation or reproduction was performed.

My order of work would be: **finish the Share-event cleanup; make open/replace/close/fallback obey one session owner and fix audio detach; harden WebView shutdown; then give background playback one owner with memory-budget handling.** These changes target actual lifetime problems without requiring a general app rewrite. The sibling offers a useful WebView close example, but it is not a ready-made stability fix for the player or tab leaks.

[target]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/tree/59f89eb1812ec30a82ca2512729b97e10b2c9604
[sibling]: https://github.com/ywmoyue/biliuwp-lite/tree/ca2caddc597b3faf05b4dac6dcb226aa0694365b
[tab-close]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/59f89eb1812ec30a82ca2512729b97e10b2c9604/src/BiliLite.UWP/MainPage.xaml.cs#L204-L214
[frame-close]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/59f89eb1812ec30a82ca2512729b97e10b2c9604/src/BiliLite.UWP/Controls/MyFrame.cs#L8-L12
[video-events]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/59f89eb1812ec30a82ca2512729b97e10b2c9604/src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs#L123-L194
[season-events]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/59f89eb1812ec30a82ca2512729b97e10b2c9604/src/BiliLite.UWP/Pages/SeasonDetailPage.xaml.cs#L70-L133
[live-events]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/59f89eb1812ec30a82ca2512729b97e10b2c9604/src/BiliLite.UWP/Pages/LiveDetailPage.xaml.cs#L418-L480
[br12]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/commit/2a6d2f10dface0bd469e4054bec161572c6227ac
[native-open]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/59f89eb1812ec30a82ca2512729b97e10b2c9604/src/BiliLite.UWP/Controls/Player.xaml.cs#L274-L303
[ffmpeg-open]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/59f89eb1812ec30a82ca2512729b97e10b2c9604/src/BiliLite.UWP/Controls/Player.xaml.cs#L610-L653
[quality-start]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/59f89eb1812ec30a82ca2512729b97e10b2c9604/src/BiliLite.UWP/Controls/PlayerControl.xaml.cs#L1790-L1799
[quality-change]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/59f89eb1812ec30a82ca2512729b97e10b2c9604/src/BiliLite.UWP/Controls/PlayerControl.xaml.cs#L1985-L2017
[engine-change]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/59f89eb1812ec30a82ca2512729b97e10b2c9604/src/BiliLite.UWP/Controls/PlayerControl.xaml.cs#L2892-L2917
[player-close]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/59f89eb1812ec30a82ca2512729b97e10b2c9604/src/BiliLite.UWP/Controls/Player.xaml.cs#L1547-L1579
[audio-attach]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/59f89eb1812ec30a82ca2512729b97e10b2c9604/src/BiliLite.UWP/Controls/Player.xaml.cs#L768-L780
[web-init]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/59f89eb1812ec30a82ca2512729b97e10b2c9604/src/BiliLite.UWP/Pages/WebPage.xaml.cs#L27-L39
[web-close]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/59f89eb1812ec30a82ca2512729b97e10b2c9604/src/BiliLite.UWP/Pages/WebPage.xaml.cs#L59-L105
[sibling-web]: https://github.com/ywmoyue/biliuwp-lite/blob/ca2caddc597b3faf05b4dac6dcb226aa0694365b/src/BiliLite.UWP/Pages/WebPage.xaml.cs#L51-L61
[webview-api]: https://learn.microsoft.com/en-us/windows/winui/api/microsoft.ui.xaml.controls.webview2.close?view=winui-2.8
[app-manifest]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/59f89eb1812ec30a82ca2512729b97e10b2c9604/src/BiliLite.UWP/Package.appxmanifest#L61-L70
[package-manifest]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/59f89eb1812ec30a82ca2512729b97e10b2c9604/src/BiliLite.Packages/Package.appxmanifest#L68-L74
[smtc-load]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/59f89eb1812ec30a82ca2512729b97e10b2c9604/src/BiliLite.UWP/Controls/PlayerControl.xaml.cs#L315-L363
[smtc-status]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/59f89eb1812ec30a82ca2512729b97e10b2c9604/src/BiliLite.UWP/Controls/PlayerControl.xaml.cs#L2682-L2734
[smtc-unload]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/59f89eb1812ec30a82ca2512729b97e10b2c9604/src/BiliLite.UWP/Controls/PlayerControl.xaml.cs#L301-L312
[manual-smtc]: https://learn.microsoft.com/en-us/windows/apps/develop/media-playback/system-media-transport-controls
[suspend]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/59f89eb1812ec30a82ca2512729b97e10b2c9604/src/BiliLite.UWP/App.xaml.cs#L237-L245
[background-guide]: https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/background-audio
[exceptions]: https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter/blob/59f89eb1812ec30a82ca2512729b97e10b2c9604/src/BiliLite.UWP/App.xaml.cs#L66-L104
