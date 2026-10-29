# Main Dynamics tab crash

## Report and route

The latest user report distinguishes the primary `动态` navigation item from `视频动态`. The primary item routes through `HomeVM.GetAllNavItems()` to `Pages.Home.UserDynamicPage`, which uses `Modules.User.DynamicVM`. The earlier legacy video-card fix and `Home dynamics stage=...` breadcrumbs cover only `Pages.Home.DynamicPage` / `Modules.Home.DynamicVM`.

The supplied log excerpt starts during repeated `x/v2/feed/index` requests and ends at the start of another home-feed request. It is partial and contains no session header, page navigation event, Dynamic request, or exception. Those feed requests come from `RecommendVM`; `MyAdaptiveGridView` auto-loads while the scrollable height is zero, and the same control behavior exists in the pinned sibling. The excerpt does not establish that this traffic caused the crash.

## Source candidate and implementation

Target `UserDynamicPage` originally called `InitializeComponent()` before constructing `dynamicVM`, even though the page XAML binds to the VM and attaches `pivot_SelectionChanged`; that handler reads `dynamicVM.UserDynamicType`. An initial Pivot selection during XAML setup could therefore fail before page navigation reaches `OnNavigatedTo` or the API request. The pinned sibling constructs its viewmodel before `InitializeComponent` and checks the selected type against the current type before loading. Source does not prove the initial event timing, so this is recorded as a plausible lifecycle fault rather than a confirmed crash cause.

The focused change initializes the user-dynamic VM before XAML setup and adds navigation, constructor, Pivot, page-load, background, and VM request/parse/convert/publication breadcrumbs. The concrete stage names include `Home navigation stage=selection/frame-navigate-complete`, `Main dynamics stage=constructor-start/viewmodel-created/xaml-initialized`, `Main dynamics stage=pivot-selection`, `Main dynamics stage=navigated-to/layout-set/load-items-begin`, and `User dynamic VM stage=request-start/response-received/response-parsed/cards-deserialized/card-convert-failed/items-published`. Lifecycle exceptions are logged with their stage and rethrown; API errors continue through the existing VM handler. Logs do not include response bodies. The related `视频动态` 404 and request-frequency report is outside this change.

## Reproduction handoff

Build and launch the updated app, then select the primary `动态` item. If it still closes, relaunch and export recent logs from Settings → Debug. The last `Main dynamics stage=...` or `User dynamic VM stage=...` entry, together with the full exception if present, will locate the failure boundary. No build, test, runtime, or API validation is performed in this workspace.
