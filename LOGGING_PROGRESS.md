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
- `src/BiliLite.UWP/Helpers/HttpHelper.cs` adds GET/POST breadcrumbs through `Utils.AddALog` and logs caught request failures through `LogHelper`. POST breadcrumbs include the request body, so future export should consider accidental credentials or personal data in request traces.
- `src/BiliLite.UWP/Pages/SettingPage.xaml` and `.xaml.cs` expose the debug text buffer behind the `EnableLog` setting toggle. Its `OpenLog` help link launches the existing local log folder. There is no log bundle or feedback export flow in the inspected code.
- `src/BiliLite.UWP/Modules/IModules.cs` logs unhandled module exceptions and maps network exceptions to a user-facing network error.
- NLog and NLog.Config are already package references in `src/BiliLite.UWP/BiliLite.UWP.csproj`.

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
- [ ] Step 3: add request IDs, duration, status, and API error breadcrumbs to HTTP/gRPC and high-value player/load failure paths. Use the pinned sibling only for any API behavior context; diagnostics must not change API behavior.
- [ ] Step 4: add a simple Settings export/report marker and clear user feedback instructions for sharing the exported logs. Review the export for sensitive request data.
- [ ] At each step, update this progress file and commit the bounded implementation. Run only `git diff --check`; do not build, restore, run, test, validate, or issue API requests here.
- [ ] Hand the user the resulting commit and instructions for building/running the app and returning exported logs; leave build and runtime checks to the user.
