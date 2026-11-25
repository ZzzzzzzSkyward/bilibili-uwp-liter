# Managed lifetime reconciliation for RR1

Status: **RELEASED — narrow M1 wording amendment required if the resource contract chooses terminal Dispose**. This is a reconciliation artifact, not an edit to the already PASSed managed requirements/evidence/source-proof, not an execution grant and not a replacement resource contract. Baseline source remains dev7356b94893377558191b0d70576a52bae704c1f9. Read resources-revision-dispatch.md, resources/review.md and the frozen managed review. No product, repository test, build, main document, ref or frozen managed requirement was changed.

## Decision supported by current callers

Use **terminal PlayerControl.Dispose with a fresh page/control for a later closed-item open** as the conservative cross-scope contract. A live page/control can refresh or change item repeatedly **before** Dispose; a reusable SeasonDetailVM can accept later loads independently of any player. Neither proves that a disposed Page's embedded PlayerControl can restart. The current implementation has no such restart boundary: constructor-only display request/timer/toast state is destroyed by Dispose, and complete real public Initialize immediately dereferences the cleared display request. No same-control restoration should be inferred from page-cache comments or a new generation flag.

This choice preserves the actual source shell paths below. It depends on their explicit fresh-frame/cache-disable/removal preconditions; the Linux source/managed doubles do not execute UWP cache or Loaded behavior. If Windows reveals a supported path that actually supplies a terminal Page/control again, record that path and review a specific fresh-owner routing correction or reinitialization scope. Do not silently restore every resource inside Initialize.

## Actual caller and cache trace

| Transition | Actual source path | Owner status and implication |
| --- | --- | --- |
| Tabbed open/new open after closing a tab | MainPage.NavigationHelper_NavigateToPageEvent:95–115 creates **new TabViewItem and new MyFrame**, then Frame.Navigate(type,param). Closed tab path191–202 invokes frame.Close, sets TabItem.Content=null and removes TabItem. | New frame has no prior page cache. Its navigation constructs a page/XAML player owner; old closed frame is detached. New owner's normal constructor initializes display/timers/toasts. Source precondition supports fresh-owner open; actual XAML constructor/cache delivery is not run here. |
| Tabbed mouse Back from a nested page | MainPage.Content_PointerPressed:131–138 calls frame.Close then frame.GoBack. Season/Video OnNavigatingFrom Back closes; BasePage:15–23 sets outgoing NavigationCacheMode.Disabled. | The outgoing player's close is terminal. The previous page in history was retained on forward navigation and is a different live owner. No product Frame.GoForward call is present; the only GoForward search hit is WebView's internal web history. MainPage does not clear its forward stack, so do not claim all native Frame entries are absent. The outgoing page's explicit Disabled policy is the source cache precondition. |
| Single-window/no-tab open same page type | NoTabMainPage.NavigationHelper:130–137 calls NewInstanceFrame.Navigate, then marks resulting page Required. NewInstanceFrame:217–237 checks current/backstack type; duplicate type calls AddFrame (new MyFrame), pauses the previous PlayPage and navigates the new frame. | Repeated same-type open uses a fresh frame/page/player. Previous open history page stays live/paused, not disposed; preserving it is intentional. Required caching for a still-open page is compatible with terminal **close**. |
| No-tab Back within current frame | NewInstanceFrame.GoBack:248–256 first sets outgoing NavigationCacheMode.Disabled, calls frame.GoBack and clears ForwardStack. Season/Video Back invokes Close and base disable. | Outgoing owner is terminal and its forward route is dropped; previous retained owner was never disposed simply because it was navigated away from. |
| No-tab Back removes extra frame | NewInstanceFrame.GoBack:258–268 waits animation, removes its Navigated handler, calls frame.Close and removes frame from Children. | Extra page/player is terminal and the complete frame is detached. A later same-type open creates another frame, not an authorization to revive this object. |
| Multiwindow open/close | NoTabMainPage.OpenNewWindow:154–174 and MessageCenter.OpenWindow:617–639 create new view/new Frame then navigate; Consolidated navigates BlankPage and closes the CoreWindow. Season/Video Blank navigation closes and BasePage disables outgoing cache. | Later open builds a new view/frame/page/control. Closed view owner is terminal. Native view consolidation is unmeasured. |
| Forward navigation or visual unload of an open history page | Season.OnNavigatedFrom:151–156 and Video.OnNavigatedFrom:316–321 release share/frame subscriptions without Dispose. NewInstanceFrame.Navigate pauses a prior PlayPage. The page's Close is restricted to Back/Blank or explicit ClosedPage notification. | Retained page/control lifetime remains live. Reentering it or loading its visuals is not reopening after Dispose. Do not end playback lifetime on ordinary Unloaded, minimize or tab visibility. |
| Season UI refresh | Season.btnRefresh_Click:762–766 rejects while VM.Loading, otherwise awaits InitSeasonDetail. Season XAML82/99/369 wires refresh/menu/error retry to this method. InitSeasonDetail initializes the existing player's list; no Dispose occurs on this path. | A real supported live-page refresh exists. Repeated refresh/retry must work on the same live page/player. Overlapping reverse-order VM tests are a separate generation control; do not remove the UI's existing Loading guard to create that overlap. |
| Season episode/preview change | Season.UpdatePlayInfoToEpisode/Preview:206–267 calls the existing player.InitializePlayInfo while live. PlayerControl_ChangeEpisode and selection handlers do not dispose the owner. SeasonList_SelectionChanged:610–628 opens another Season page via MessageCenter rather than direct same-frame self-navigation. | Reusing the same player and even same PlayInfo identity within a live owner is valid. A newly selected different season takes the shell's new-owner path. |
| Video F5/playlist changes | Video.CoreWindow_KeyDown:80–104 calls InitializeVideo only when current VM is not Loading; successful InitializeVideo:417 calls existing player.InitializePlayInfo. Its Close:153–181 separately disposes. | Live item replacement is distinct from terminal reopen. Existing Video reset/comment text describing cached reentry is not proof a control with null display/timer/disposed toast can restart; Video source is outside M1 and no reset assertion is changed here. |
| Local playback | Local.OnNavigatedTo:98–120 initializes its embedded control on New; Back/Blank:130–139 or ClosedPage:88–95 disposes. | Same fresh-shell/cache rules apply. No actual new LocalPlayerPage/control reconstruction inside disposed initialization is present. |

## Relevant boundary experiment, without rerunning unchanged suites

Private scratch: `/workspace/work/video-memory/managed/lifetime-boundary/`. It references the previously bound product-slice assembly (complete actual PlayerControl.InitializePlayInfo/Dispose and complete actual Season.InitSeasonDetail) plus the separate full-real-SeasonVM assembly. Those source methods are unchanged and source/extraction hashes remain in source-proof.md and the original scratch manifest. No private lifecycle flag is reset. No SetPlayItem spy is called a restored native player.

Exact commands:

```bash
/workspace/work/maintenance-foundation/quality/dotnet/dotnet /workspace/work/maintenance-foundation/quality/dotnet/sdk/8.0.427/Roslyn/bincore/csc.dll @/workspace/work/video-memory/managed/lifetime-boundary/compile.rsp
/workspace/work/maintenance-foundation/quality/dotnet/dotnet /workspace/work/video-memory/managed/lifetime-boundary/LifetimeBoundary.dll
```

Compile0 (no warnings), run0, **6 boundary assertions passed**:

1. Same PlayInfo reference/list initialized twice on one still-live control reaches its actual public initialization twice.
2. Actual Dispose followed by Initialize on that same control throws NullReferenceException before a third SetPlayItem boundary call. This describes the baseline deficiency; the chosen terminal final contract must reject quietly, not preserve the exception.
3. A **new** control accepts that same PlayInfo reference/list after the old owner closes.
4. Complete actual Season initialization executes twice on one live page, yielding two page media/comment initialization effects; then it closes. This confirms the refresh mechanism before terminal Dispose, not native media decoding or click delivery.
5–6. Full real SeasonDetailVM/parser closure accepts A then B on the same VM and retains B with one valid episode. This requires no PlayerControl restart. The previously reviewed reverse-order/invalidated-generation VM fixtures remain necessary and are not rerun unchanged here.

The display/media/timer/Frame dependencies remain managed doubles exactly as documented in source-proof.md. This boundary run neither proves actual video playback nor constructor-loaded native event behavior. The resource revision must independently test complete actual Initialize/Dispose and the required terminal Loaded behavior with a fresh-owner positive path and stale caller identity; M1 does not acquire PlayerControl ownership.

## Precise normative M1 amendments to request later

The following changes are **proposed**, awaiting root's exact amendment grant and independent re-review. All other frozen M1 source scope, original assertions, generation/error requirements, 50-close cases and no-late title/model/review/player/comment/QR acceptance remain mandatory.

1. In the M1 goals paragraph, replace “Reopening/reusing the same page/VM must allow the new request” with: “A still-live page/player supports refresh and item replacement. The same VM may accept a new load generation, including after explicit invalidation. After terminal Page.Close/Player.Dispose, a legitimate user reopen uses a fresh page/player from the shell; no later entry may revive the disposed instance. In every case stale generation A remains rejected.” Do not imply the same VM is transplanted into a fresh page's x:Bind field.
2. In minimal lifetime contract §3, replace “New initialization/reentry may reopen the page” with: “New initialization on a live owner may advance its operation generation; reentry into a retained page that was never closed remains live. Close terminally invalidates this page's embedded player. Freshly constructed page starts open, but a closed page's later callback/entry must not clear its closed state or restore player eligibility. VM InvalidateLoad and subsequent VM load are independent of page/player terminal disposal.” Keep both captured page and VM generation checks; a player boundary guard alone cannot protect page/global title state.
3. Split acceptance §3's ambiguous “old A pending → close → fresh B” into three controls: **(a)** same real VM A pending → InvalidateLoad → B on that VM completes → A success/failure last, preserving B; **(b)** supported live-page refresh/retry on the same page/player before any Dispose, retaining its current UI Loading guard and testing normal/error outcomes; **(c)** old page/player A pending → terminal Close → a distinct fresh page/player B opens (including same content/id/reference where the API supports it) → A success/failure last, with zero old model/title/review/player/comment/QR effects and B unchanged. Also retain closed-page stale-entry rejection: it must not initiate another VM request or become a live owner merely by calling InitSeasonDetail after close.
4. Do **not** require success from InitializePlayInfo on the disposed old player, private-field reopening in a fixture, VM-field replacement that breaks x:Bind, XAML/player reconstruction or automatic resource reattachment. If same-object restart is later explicitly chosen instead, that is a cross-scope restoration amendment needing separate evidence/grant; this narrow M1 generation work does not implement it.
5. A final common fixture must still fail the old page/VM source even if a separate PlayerControl terminal guard suppresses the old NullReferenceException. Zero late title/model/review/comment/QR and zero attempted player initialization are decisive; “the player silently ignored it” is insufficient.

This correction narrows an ambiguous positive acceptance to supported ownership; it does not relax any stale-work safety criterion or original season1 parse/fallback/order/last-good assertions. Keep separate single writers: resource owner for PlayerControl/playersess, managed owner for Season page/VM/season1, later one main-document owner.

## Bindings and release

Source-only current caller/cache policies are the preconditions above, not native test outcomes. Frozen managed requirement/review approval is not edited by this artifact. Windows XAML/COM/cache/Loaded/SMTC/display/decoder/RAM/GPU gates remain unverified. BR-21/global rewrite remains measurement-gated.

- `src/BiliLite.UWP/MainPage.xaml.cs`: `7abe52a5547d2c6764c88a81da70648b72bafa152d09d9d9109eb339a039858c`
- `src/BiliLite.UWP/NoTabMainPage.xaml.cs`: `cda4b43db019f323687ea68f00bbef01538ee4ed7371daea2843b340f5a94577`
- `src/BiliLite.UWP/Pages/BasePage.cs`: `02e11b80834ef2fa90fb5fa39ebb61ae5c30322c484cab2b3b57b467fcbe987a`
- `src/BiliLite.UWP/Pages/SeasonDetailPage.xaml.cs`: `b44706b9156259cc57acda3611023b4a9e0ed96f9345b3576ddaac3148804490`
- `src/BiliLite.UWP/Pages/SeasonDetailPage.xaml`: `ac68653a40f45232eda971e10ad1ae9af02f352efc2e712c846eeead21bc667c`
- `src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs`: `a057e1a6bde0b29f62809e51bf7f691c9ed28a56865cd391b7982a091ef118e3`
- `src/BiliLite.UWP/Pages/LocalPlayerPage.xaml.cs`: `3786a92bde1909cf753162231db16a58618c41fe6eba9711c50e1a6b1853ea97`
- `src/BiliLite.UWP/Helpers/MessageCenter.cs`: `376489239b4be6b4c1c895cb9bed4b984259fecee98b9c3dfce58e1557415085`
- `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs`: `18fac7cc1afcdf38f5896364f1606dd05446358965f3aae51cff00a7929b1e4f`
- `src/BiliLite.UWP/Modules/Detail/SeasonDetailVM.cs`: `a9b84b5512e04f2081d34b9f9d5642a9f5aee902f01b476881996fc9c3c1a07b`

Boundary fixture/result hashes:

- `Program.cs`: `b6cc6a5fe0f4b2baacb36b27774507fe726e830dfa9f73b5032f7babf5d70995`
- `compile.rsp`: `13a518834242da512248e286950a4469a0b8c6edb8ce4ae5545367be71bbc1da`
- `LifetimeBoundary.dll`: `4664fec02aa5e0338fe4054a20a642ce434359198aa1674f9ed69c9a5007d3bd`
- `compile.log`: `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`
- `proof.log`: `35ed733fc867f413975c3e508f9218a5c5ca160404c6562c63c7c5d1f5cc60b2`
- `run-status.json`: `1a61cacb382fe4ecefb97f8ebd964829341e4ada4b0495af3987386c6e834937`
- `ManagedMemoryProof.dll`: `abe19995010135cc45ae0286d3bf6c040f3a220c0e2a4315d055b7bb4cf2a2aa`
- `RealSeasonVMProof.dll`: `1816c326017ec368f3bcc0566e83efcf78ed2b7795d32cebb64d717ee3c0fb22`
- `Newtonsoft.Json.dll`: `8c1dd5c184b4e2e7ead06971ff3ebcb46783be972292d1deb1061744369b4d80`
- `frozen-input-hashes.json`: `d8f1aecfeb011b13e662809b864ab3add2af60bcfc4e999d9747c0ae0611f93d`
- `source-hashes.json`: `a2728f62dd2cc898f81e4f1b5b36c44fb1eadc4a7a413914e3cc94d6ecf3d134`
