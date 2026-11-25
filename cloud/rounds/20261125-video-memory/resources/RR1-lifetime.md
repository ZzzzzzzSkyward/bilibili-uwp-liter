# RR1 revision evidence and terminal-owner decision

Status: RR1/P1 addressed in revised resource requirements; released for the same independent reviewer. This terminal/fresh-owner choice agrees with root [lifetime-decision.md](../lifetime-decision.md) and the independently traced managed [lifetime-reconciliation.md](../managed/lifetime-reconciliation.md). Managed normative changes remain under its separate author/re-review grant. This is a requirements decision and source/boundary experiment, not a product repair or native validation. R1/R2/R3 acceptance remains mandatory. Only resource Markdown and `/workspace/work/video-memory/resources/rr1-public-boundary/` were written during this revision; original discovery fixture and its logs remain unchanged.

## Chosen public lifetime

**Dispose is terminal for one PlayerControl instance.** A constructor-created live control may initialize/replace its play item repeatedly and may unload/reload while intentionally retained for history/background playback. Once explicit Dispose occurs, no later InitializePlayInfo, Loaded, picker result, quality/info/node result or callback may reauthorize that instance. Public stale entry is a quiet no-op before touching null/disposed resources. There is no reset flag/restart method in this repair.

A later real open obtains a **fresh page/XAML PlayerControl in a fresh frame or in a noncached navigation**. Same-id/same-reference replacement inside an undisposed owner is also legitimate, but is distinguished by an operation generation. No test may simulate legitimate reopen by resetting a private product flag or by treating a SetPlayItem spy as restored services.

This matches actual close/navigation code and avoids adding toast/timer/display/event reconstruction merely to satisfy an unsupported same-instance restart promise. The actual old Dispose permanently disposes the readonly toast service, nulls display/danmaku timer, detaches four XAML player events and has no inverse public operation. Comments saying a cached page can return do not override those actual effects. A still-live cached page returning after forward navigation is a different case: no Dispose occurred.

## Actual source caller/cache preconditions

| Actual caller/source | Observed construction or close rule | Consequence and limit |
| --- | --- | --- |
| MainPage.NavigationHelper_NavigateToPageEvent:96–114 | Every open unconditionally executes `new MyFrame()`, then Navigate and assigns the new frame to a new tab. | Ordinary new tab opens cannot take a PlayerControl out of a closed tab's frame cache. Product XAML constructs the child control for the newly constructed page. This is source construction evidence, not a Windows cache execution. |
| MainPage.ClosePage:191–202 | Clears current visual grid, calls MyFrame.Close, clears tab.Content and removes tab. | Explicit tab close tears down that owner; a later tab open uses the new-frame path above. No disposed-tab resurrection API is present. |
| BasePage.OnNavigatingFrom:15–23, reached through PlayPage:37–45 | On Back or BlankPage, sets `NavigationCacheMode.Disabled` before returning to the platform. Video/Season/Local overrides perform player disposal and call base. | The terminal outgoing page is explicitly excluded from the reusable page cache on these close routes. This was the decisive additional base-class evidence; merely reading page constructors' Enabled assignments would be incomplete. Actual UWP cache eviction/COM lifetime is still unmeasured. |
| NoTabMainPage.xaml:22 and NewInstanceFrame:189–289 | Shell uses NewInstanceFrame. Navigate allocates another frame when the current/backstack already contains the target type; forward navigation pauses an existing PlayPage without disposing it. GoBack disables the outgoing cache, then GoBack+ForwardStack.Clear or Close+removes outgoing frame. | Repeated opening of the same page type uses distinct frame owners; Back removes/uncaches the outgoing closed owner. The retained previous page can resume because it was paused/live, not disposed. |
| MainPage mouse-back:132–142 | Calls current frame.Close then frame.GoBack when possible. | The outgoing page also runs its Back/base cache-disable path; the prior history page was not disposed merely by earlier forward navigation. No page/frame GoForward route is invoked in current product code (WebView.GoForward is unrelated). |
| VideoDetail InitializeVideo:382 | Can redirect to SeasonDetailPage in the same frame, then removes its own VideoDetail backstack entry. | This is the one direct player-page Frame.Navigate call found outside shell/window factories; it is forward navigation, not a disposed-instance restart. Later Back/Blank still uses the base cache-disable rule. |
| NoTab OpenNewWindow:154–174 and MessageCenter.OpenWindow:614–640 | Each creates a new Frame; window consolidation navigates Blank then closes native window. | New standalone opens are fresh owners. Native window/consolidation/cache behavior was not executed here. |

Read-only hashes of all these caller files are in private `caller-bindings.json`. No shell/page/cache edit is required by the selected player contract. If a later feature adds true reentry to a disposed cached page, it must create a fresh player/page under a separate reviewed caller scope; this change must not silently introduce same-instance restoration.

## Authorization at the public boundary

The new PlayerControl terminal latch is monotonic: constructor live → Dispose terminal. InitializePlayInfo must **never** clear it or call a generic Begin that clears it. Loaded also checks terminal state before global registration, settings/timer work or `_activeInstance` assignment. Unloaded still performs existing view-handler cleanup; terminal state does not suppress required detach operations.

Within an undisposed owner, synchronous page/user selection can start a replacement and advance the play-item/work generation. Every async producer captures its original item/generation before awaiting and checks it before resource/guard/session creation. Same PlayInfo object reuse does not make an old operation current. An old page's delayed Initialize cannot reopen a closed instance because that instance's latch is final; it cannot acquire the new frame's control through any current factory path.

For an old request inside a **still-live** page, authorization remains the page's captured original navigation/detail generation checked immediately before calling public Initialize. Existing VideoDetail InitializeVideo has its page/transition identity check; accepted managed M1 must add the corresponding season page/VM checks. Resource code must not replace these checks by recapturing the current identity at completion. No new public Initialize signature or cross-owner registry is needed. This is layered ownership, not permission for page publication after player rejection: season title/model/comment/QR no-late assertions remain mandatory.

## Complete public-boundary fixture

Private project: `/workspace/work/video-memory/resources/rr1-public-boundary/ResourceProbe.csproj`. It reuses byte-identical original `ActualControlMethods.cs` (including real Dispose, SetQuality, ChangeQuality, GetPlayerInfo, subtitle selection/load/clear and subtitle settings) and `ActualClosePlay.cs`; original linked PlayerSessionState/LocalMediaAsyncLoadGuard remain real. New `ActualPublicBoundary.cs` contains complete unchanged actual InitializePlayInfo, SetPlayItem, Loaded, Unloaded, Player_PlayMediaOpened and LoadPlayerSetting bodies. Extraction fails on absent/ambiguous signatures; `public-boundary-bindings.json` records source/method hashes and lines. `prior-proof-bindings.json` binds the unchanged original discovery proofs/logs. No private product state is reset between close and retry.

```bash
python3 /workspace/work/video-memory/resources/rr1-public-boundary/extract-public-boundary.py
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run \
  --project /workspace/work/video-memory/resources/rr1-public-boundary/ResourceProbe.csproj \
  --configfile /workspace/work/video-memory/resources/rr1-public-boundary/NuGet.Config
/workspace/work/maintenance-foundation/quality/dotnet/dotnet \
  /workspace/work/video-memory/resources/rr1-public-boundary/bin/Debug/net8.0/ResourceProbe.dll \
  --require-terminal-public-entry
```

Descriptive baseline exits0; desired-terminal run exits1. Logs `build-run.log` and `desired-terminal-red.log` show:

| Actual-method case | Current source result | Required final result with same assertions/fixture |
| --- | --- | --- |
| Fresh constructed owner, actual Loaded→Initialize→SetPlayItem→quality/info/subtitle, then actual MediaOpened callback |1 engine open,1 Play boundary call,1 subtitle timer,1 danmaku timer,1 RequestActive boundary call. | Same positive behavior; a terminal flag must not suppress all public work. |
| Live owner Unloaded→Loaded, with no Dispose |1 open,1 Play,1 subtitle timer and1 danmaku timer remain usable. | Same; Unloaded must not set the terminal latch. |
| Actual Dispose/Unloaded, then stale public Initialize and Loaded on that instance | Both throw NullReferenceException (display request / danmaku timer); no new SetPlayItem spy is substituted. | Both quiet, no new work/global bindings, no reset/reconstruction of terminal owner. |
| New constructed owner, **same exact PlayInfo object** as disposed first owner |1 open,1 Play,1 subtitle timer,1 danmaku timer. | Same; fresh-object ownership, not identity inequality of media data, authorizes this open. |
| Old owner quality pending→actual close→fresh owner active→old quality returns | Old control issues1 post-close engine open. Fresh control remains1 open/1 subtitle timer. | Old post-close open0; fresh positive still1/1. |
| Undisposed owner starts A with pending quality, starts B using **same exact PlayInfo object**, B completes, A returns |2 total engine opens; B subtitle timer1/Play1. Caller SetPlayItem generation check occurs after old SetQuality already reopened media. |1 total open from B; A does not mutate/mint a replacement. B remains functional. |

The fixture constructs boundary fields/event wiring once for each fresh subject to stand in for XAML-generated InitializeComponent/platform objects. Complete SetPlayItem runs; it is **not** a restoration spy. It traverses real quality/session/info/subtitle methods; the real MediaOpened handler calls the doubled engine's Play method. Network/engine/UI/display/dispatcher/AB-history/danmaku-load boundaries are doubles. Player and subtitle settings loaders are complete actual bodies here; the danmaku settings loader remains a boundary stub (R3 all-three actual-loader coverage remains mandatory in final regression). The persisted-setting boundary supplies autoplay=true to actual LoadPlayerSetting, which sets `_autoPlay` itself; the fixture does not set/reset product lifecycle flags to make a closed control usable. Native media playback, toast visuals, display request accounting and SMTC behavior are not proved by these counts. No same-instance post-Dispose behavior is claimed functional. Current-source exceptions and live stale-quality open are baseline failures, not repair success.

## Cross-scope reconciliation and release

Root has authorized the managed author's exact normative amendment under [managed-revision-dispatch.md](../managed-revision-dispatch.md). This resource contract relies on the same distinction: (a) direct reusable VM reverse-order loads, (b) live-page refresh/selection without Dispose, and (c) terminal close followed by a genuinely fresh page/player. Only (c) covers reopening after close. It must not reset the old page's/player's private closed latch in a test. The managed author/root owns its precise revised normative language and complete caller fixtures; all no-late-title/model/comment/QR checks remain decisive. No player code/test execution grant exists until root reconciles that amendment and the same reviewer accepts RR1.

Private `result.json` binds final boundary logs/fixture hashes and verifies original discovery files plus original product/test source bindings remained unchanged. No conflicting supported same-instance-after-Dispose caller was found. Custody released: resource requirements/evidence/RR1-lifetime are ready for same-reviewer RR1 re-review; no implementation/publication grant is implied.
