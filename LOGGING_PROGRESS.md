# Logging and crash report progress

## Scope and constraints

The goal is to make app failures easier to diagnose from user-provided logs: keep practical persistent logs and crash tracebacks, and provide a simple way for the user to export them for feedback. Keep the solution small and aligned with the existing UWP code. Avoid broad refactors, excessive guards, and changes to unrelated behavior.

Implementation agents must be `gpt-6-luna` subagents. The orchestrator handles coordination; agents should own bounded implementation areas and update this file with each completed step before committing. The app is expected to be built and exercised by the user, who can then return the exported logs.

Use only `/workspace/biliuwp-lite` at pinned commit `ca2caddc597b3faf05b4dac6dcb226aa0694365b` as behavioral reference. Do not consult external documentation or other codebases. Network access is limited to Git setup; do not make API requests. Do not build, restore, run, test, or validate the app. `git diff --check` is the only permitted check.

## Setup and baseline

- Target: `/workspace/bilibili-uwp-liter`, branch `work`.
- Origin: `https://github.com/ZzzzzzzSkyward/bilibili-uwp-liter.git`.
- Initial clean checkout: `dad41d6189576c513fd4e3a428f7a9b8ad55f102`.
- The tracked remote `work` branch was two commits ahead. `git pull --ff-only origin work` fast-forwarded the target to `41356c30fb984dc6ea7396ee66cac2515e364518` (`Fix x64 Release packaging and bump the version on every release`).
- `origin/master` is a separate ancestor of the target branch and was not used as the pull source.
- Reference clone: `/workspace/biliuwp-lite`, shallow clone of `dev`, pinned commit `ca2caddc597b3faf05b4dac6dcb226aa0694365b`.
- No `AGENTS.md` was found under `/workspace`.
- Environment setup context: network proxy policy is enforced, GitHub is allowed, and no VPN is available. Proxy and certificate settings were left unchanged. Network was used only for Git fetch/pull/clone.
- No implementation changes have been made in this setup step. No app build, restore, run, API request, test, or runtime check has been performed.

## Existing logging and crash infrastructure

- `src/BiliLite.UWP/Helpers/LogHelper.cs` exposes `LogHelper.Log` and uses NLog. On first call it creates a file target in `ApplicationData.Current.LocalFolder\\log\\yyyyMMdd.log`, with date, level, logger, thread, message, and exception message/stack trace in each line. It is initialized lazily and the target filename is fixed at initialization time.
- `src/BiliLite.UWP/App.xaml.cs` subscribes to `Application.UnhandledException`; the handler writes the exception through `LogHelper` and marks the event handled. It also shows a toast. The `ExceptionHandlingSynchronizationContext` registration helper is declared but has no call sites in the target tree.
- `src/BiliLite.UWP/Helpers/ExceptionHandler.cs` wraps posted synchronization-context callbacks and raises an event when their callback throws. It is currently disconnected from app startup.
- The app implements the FFmpeg `ILogProvider`, but `Log` only calls `Debug.WriteLine`.
- `src/BiliLite.UWP/Helpers/Utils.cs` has a static `StringBuilder` and `AddALog`, which append request/debug text to the current Settings page's textbox if present. This buffer is trimmed to roughly 10,000 characters by `SettingPage.Log`; it is not persisted independently.
- `src/BiliLite.UWP/Helpers/HttpHelper.cs` centralizes ordinary GET/POST API calls and also provides stream, buffer, and string helpers. POST request bodies are excluded from diagnostics; request endpoints are recorded without query strings.
- `src/BiliLite.UWP/Pages/SettingPage.xaml` and `.xaml.cs` expose the debug text buffer behind the `EnableLog` setting toggle. Its `OpenLog` help link launches the existing local log folder. There is no log bundle or feedback export flow in the inspected code.
- `src/BiliLite.UWP/Modules/IModules.cs` logs unhandled module exceptions and maps network exceptions to a user-facing network error.
- NLog and NLog.Config are already package references in `src/BiliLite.UWP/BiliLite.UWP.csproj`.

## Dynamics tab crash report and source diagnosis

- User report: clicking the Dynamics tab closes the app. The supplied log shows app version 1.0.9.3 on Windows 10 x64. The first process records a successful `dynamic_new` GET immediately before ending; a second process starts about four seconds later. The log has no managed exception or crash traceback. No raw log content or account-specific data is stored here.
- The reviewed source at `be01cbb84625c64f433332f9720ccd28c7510a86` loads the tab through `Pages/Home/DynamicPage.xaml.cs` and `Modules/Home/DynamicVM.cs`. Both the target and pinned sibling request the legacy `dynamic_new` endpoint and extract `data.cards`. The sibling deserializes each video `card` to a flat model with `aid`, `pic`, `title`, `owner`, and `stat`; the target instead deserialized it as `DynamicCardModel2024`, whose properties expect the newer `basic` and `modules` shape. The target's `viewtext` getter contains unguarded nested access in its fallback chain, and the initial video template binds `video.viewtext`. This is a concrete source mapping mismatch and a plausible explanation for a binding-time exception after the response succeeds. The log has no response body or traceback to confirm the failing card.
- Implementation restores the sibling-grounded flat DTO for home video cards, binds owner face/name through that DTO, and adds narrow stage/type/id/count breadcrumbs for request completion, parsing, collection publication, template selection, and video-card conversion. Conversion exceptions retain the original exception and rethrow; no response body is logged, no endpoint behavior changes, and season handling is preserved. Changed paths: `src/BiliLite.UWP/Modules/Home/DynamicVM.cs`, `src/BiliLite.UWP/Pages/Home/DynamicPage.xaml`, and this progress file.
- Target branch was refreshed first with `git pull --ff-only origin work`; starting HEAD was `be01cbb84625c64f433332f9720ccd28c7510a86`. Confidence remains limited because the supplied log has no response payload or traceback. Next user repro: open the Dynamics tab on the updated app, then use Settings → Debug to export the recent logs (after relaunch if the process closes) and return them with the occurrence time.

## Suggested implementation boundaries

1. Improve the existing logger's reliability and traceback capture in `Helpers/LogHelper.cs`, keeping the current NLog dependency and LocalFolder location. Add only lifecycle or metadata hooks needed for diagnosis.
2. Wire app-level and synchronization-context exception capture in `App.xaml.cs` with a small shared logging path. Preserve the app's current user-facing handling behavior unless a concrete issue requires a change.
3. Add a simple export action in `Pages/SettingPage.xaml` and `.xaml.cs` that lets users save or share the recent persistent log files, using existing UWP file/storage patterns. Keep the current open-folder entry point.
4. Review the exported content boundary around `Utils.AddALog` and `HttpHelper` so that request debug text does not silently expose cookies, tokens, or login payloads. Keep request logging changes narrow and avoid logging headers or secrets.

## Resumable plan

- [x] Fetch the configured target and fast-forward branch `work` from its tracked upstream with `--ff-only`.
- [x] Clone and pin the sole behavioral reference locally.
- [x] Inventory existing logger, crash hooks, request breadcrumbs, and settings UI.
- [x] Step 2: improve `LogHelper` session and exception persistence; capture app crash, lifecycle, navigation, and FFmpeg diagnostics in `App.xaml.cs`; persist the existing `Utils.AddALog` trace. Keep all diagnostics additive.
- [x] Step 3: add request IDs, duration, status, and API error breadcrumbs to HTTP/gRPC and high-value player/load failure paths. HTTP results carry a local request ID into JSON parse errors; gRPC diagnostics use an optional callback because the transport is a separate netstandard project; playurl logs include aid/cid/episode/quality, and player logs capture open, opened, failed, and exception stages. No request bodies, headers, cookies, or signed stream query strings are logged. No request contract, status handling, framing, retry, or recovery behavior changed.
- [x] Step 4: add Settings actions to export the latest seven persistent daily log files as a text file and record a timestamped problem marker with an optional note. Keep the existing log-folder action. Add user instructions in `document/DIAGNOSTICS.md`.
- [x] Update this progress record and commit the bounded implementation. Run only `git diff --check`; do not build, restore, run, test, validate, or issue API requests here.
- [ ] User loop: build and run the app, reproduce an issue, optionally mark it in Settings → Debug, relaunch after a crash, export diagnostic logs, then return the file with app/build version, occurrence time, steps, and visible error. No app build, runtime, test, restore, validation, or API request has been performed in this workspace.

## Completed steps and commits

| Commit | Work |
| --- | --- |
| `ad75ed3` | Document setup and inventory existing diagnostics. |
| `3c27b23` | Record the staged logging implementation plan. |
| `b1ca1b2` | Add persistent session logging and app-level diagnostics. |
| `22edc2f` | Add request, gRPC, and player diagnostics. |
| `271ee12` | Add Settings log export, problem marker, and user instructions. |
| `29e0e1c` | Record the Dynamics tab crash report and source-level candidates after refreshing `work` to `be01cbb`. |
| `c4e6909` | Restore legacy home video-card mapping and add focused Dynamics diagnostics. |

Current state: the logging, export, and Dynamics mapping work are committed on `work`. The export includes a session ID, app version, and recent log files with filename headers. Review the text before sharing because logs can include request details. Native process termination may prevent managed exception handlers from recording a complete stack trace. For a Dynamics repro, open that tab on the updated app, then use Settings → Debug to export the recent logs (after relaunch if the process closes) and return them with the occurrence time.

## Batch 3: subtitle setting, duplicate follow buttons, and user profile loading

- User reports: enabling automatic AI subtitles does not take effect; two `关注` buttons overlap on the video-detail page; and the user-info page displays a reading-data failure, possibly because its API usage is outdated.
- Setup: started from a clean `work` checkout at `48f5bdeb119fc55e62853ba449d2c13a6c18c0a7`. `git pull --ff-only origin work` reported `Already up to date`; the branch remains at that HEAD. The sole behavioral reference remains `/workspace/biliuwp-lite` at pinned commit `ca2caddc597b3faf05b4dac6dcb226aa0694365b`. No `AGENTS.md` was found under `/workspace`.
- Bug 1 is completed in `d0b681e`: the setting labeled “默认打开AI字幕” (`PlayerAutoOpenAISubtitle`, default `false`) controls auto-opening AI/automatic tracks; regular subtitles retain their existing default-selection behavior. The player checked only `自动` and uppercase `AI`, while the pinned sibling also checks lowercase `ai`; that omission let lowercase-labeled AI tracks open while the setting was off. Both downloaded/local and API selection paths now recognize lowercase `ai` and log the setting value, candidate track label, and selected-none/track outcome without logging subtitle content. Changed path: `src/BiliLite.UWP/Controls/PlayerControl.xaml.cs`.
- Bug 2 is completed: on the web detail response path, `VideoInfo` and `Loaded` were published before `GetAttentionUp()` initialized a missing `req_user`. The owner follow buttons share a grid cell and bind through that object; a null intermediate path could leave both buttons at their default visible state. The target now initializes a missing request-user model to attention `-999` before publishing it, retaining any API-provided model. A later signed-in lookup writes attention `1` for followed attributes (`2` or `6`) using the model's notifying setter, so bindings switch to “已关注”; an unfollowed, signed-out, or unsuccessful lookup retains `-999`. The sole pinned sibling at `ca2caddc597b3faf05b4dac6dcb226aa0694365b` uses a dedicated attention control with the same mutually exclusive states. This is a source-based diagnosis with no runtime reproduction. Changed path: `src/BiliLite.UWP/Modules/Detail/VideoDetailVM.cs`.
- Bug 3 (user-info reading failure) remains in progress under its own owner; no user-info implementation is included in this fix.
- No app build, runtime test, restore, or validation was performed. Only `git diff --check` is permitted.
- Constraints for this batch: Git network operations only; do not call app APIs or build, restore, run, test, or validate the app. `git diff --check` is the only permitted check.
