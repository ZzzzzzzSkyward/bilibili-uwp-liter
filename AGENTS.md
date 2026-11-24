# AGENTS.md — operating manual for this repository

Third-party Bilibili client for Windows 10/11, written as a C#/XAML UWP application (7 projects,
440 tracked C# files). This file is the entry point for an agent or a new maintainer: it maps the
tree, gives the exact build/verify commands, states the invariants that must not be broken, and
lists the traps that have already cost time once.

Deeper documentation: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md),
[docs/FEATURES.md](docs/FEATURES.md), [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md),
[docs/ROADMAP.md](docs/ROADMAP.md), [docs/README.md](docs/README.md).

## 0. Branch model

- **`dev` is the development line** (default branch for work, 510+ commits ahead of the last shared
  ancestor `e2c7958`). Develop, probe and commit here.
- **`master` is the release line.** It receives sync commits built from a sibling client, tags and
  release notes; it is not where new work lands. Do not force-push or rewrite either branch.

## 1. Build, run, verify

```powershell
# compile only (≈1 min): C# compiler + XAML compiler + PRI, no MSIX, no version bump
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-release-x64.ps1 -Fast -NoVersionBump

# add -Restore when packages changed
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-release-x64.ps1 -Fast -Restore

# full release pipeline (≈2 min): restore + Release|x64 + MSIX, output dir must already exist
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-release-x64.ps1 -OutputDir Z:\

# rebuild from scratch when stale obj/bin confuse the result
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-release-x64.ps1 -Clean
```

- Every run **without** `-NoVersionBump` increments the revision of both
  `src/BiliLite.UWP/Package.appxmanifest` and `src/BiliLite.Packages/Package.appxmanifest` before
  building. The bump belongs to the same commit as the change that triggered it. `-NoVersionBump`
  exists for compile loops and failed retries only.
- `-Fast` skips NuGet restore, MSIX packaging and the WebApi/ApiTester/Win32Tools projects.
- `-SkipProjectReferences` compiles `BiliLite.UWP` only; it requires `BiliLite.gRPC` and
  `BiliLite.JSBridge` outputs to exist already (see §7.1).
- `build-release-x64.bat` is only a double-click wrapper and ends with `pause`; never use it in a
  scripted or agent context.

Behavioural probes (offline, no device, compile the real product sources):

```bash
sh plan/tests/<batch>/run.sh        # 43 directory batches, e.g. sh plan/tests/season1/run.sh
sh plan/tests/run.sh                # the 7 single-file probes (LM01–LM05, API01, AUTH01B)
```

Each directory batch is a `net8.0` console project whose `<Compile Include>` list links product
sources plus stubs; it asserts behaviour, not plumbing, and exits non-zero on a failed assertion.
`plan/tests/run.sh` instead compiles its probes with `csc` from the SDK at
`$UPSTREAM_UNIT_TOOLS/dotnet/sdk/8.0.427`, so that one script needs an explicit .NET 8 SDK root.

Git sync (GitHub TLS drops intermittently on this network; the script retries, rebases, and stashes
the two manifests around the operation):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\sync-dev.ps1            # fetch+rebase+push
powershell -NoProfile -ExecutionPolicy Bypass -File .\watch-remote.ps1 -Fetch # block until remote moves
```

## 2. Repo map

| Path | What it is |
| --- | --- |
| `src/BiliLite.UWP/` | The application. `App.xaml.cs` = startup/lifecycle, `MainPage.xaml` = WinUI `TabView` shell, `Modules/` = view models, `Pages/` = views, `Controls/` = XAML controls (player, cards, comments), `Services/` = non-UI services, `Api/` = request builders, `Parsing/` = envelope/parser/mapper layer, `Models/` = DTOs and app models, `Helpers/` = transport, logging, settings, Win32 window helpers |
| `src/BiliLite.gRPC/` | `netstandard2.0` protobuf/gRPC client. Protos under `bilibili/…` and `Proto/…`; generated at build time by `Grpc.Tools` |
| `src/BiliLite.JSBridge/` | Tiny UWP component (`winmd`) exposing `[AllowForWeb] biliapp` to the WebView login page (`ValidateLogin`, `CloseBrowser`) |
| `src/BiliLite.Win32Tools/` | .NET Framework 4.8 WPF full-trust companion: unpacks `ffmpeg.7z` and runs the export/converter (`AppHelper.LaunchConverter` calls it) |
| `src/BiliLite.Packages/` | MSIX packaging project (`wapproj`). Produces the only installable artifact, `BiliLite.Packages_<version>_x64.msix` |
| `src/BiliLite.WebApi/` | ASP.NET Core `netcoreapp3.0` helper (EF Core SqlServer, Redis). Not referenced by the app and not shipped; treat as a legacy side project |
| `src/ApiTester/` | .NET Framework 4.8 WinForms scratchpad for signing/API experiments. Not shipped |
| `plan/` | Work plan and probe batches. `PLAN.md` = open work + done log, `VALIDATION.md` = device rows, `REVIEW_LOG.md` = review outcomes, `P4_INVENTORY.md` = API-refactor inventory, `CARD_PLAN.md`/`HOT_CARD_PLAN.md`/`REFRESH_PLAN.md`/`SETTINGS_PLAN.md` = feature plans, `tests/` = probes |
| `document/` | `BUILD.md` (build and build-failure playbook), `DIAGNOSTICS.md` (log collection), `BILIBILI_API_RESEARCH.md` + `BILIBILI_API_ENDPOINTS.md` (2026-11-09 sibling comparison and endpoint catalog), `new_version.json` + `entrance.json` (**live data served to the app** from raw.githubusercontent; read `src/BiliLite.UWP/Api/GitApi.cs` before touching them) |
| `bug_report.md` | Defect reports (BR-nn), newest last; an entry stays until the fix is recorded |
| `code_review.md` | Current reviewer findings and their dispositions |
| `tools/import-capture.ps1` | Turns an exported diagnostic bundle into probe fixtures |
| `docs/` | The documentation set indexed by `docs/README.md` |

## 3. Architecture invariants

1. **Data flow is one way:** `Api/*` builds the request → `Helpers/HttpHelper.cs` (or
   `Services/GrpcService.cs`) transports it → `Parsing/*` decodes the envelope and maps the payload →
   `Modules/*VM` publishes rows and owns paging/request generations → `Pages/*` + `Controls/*`
   render. Parsers never touch UI or post toasts; the VM alone decides what to show.
2. **`ApiEnvelope` is the only REST envelope reader.** A missing or malformed `code` is a failure,
   never an implicit success. Endpoints that answer with `data` or `result` say which.
3. **Parse failures are typed values** (`ApiParseResult<T>`), not exceptions and not empty models.
   A failed optional step logs its category and leaves the page usable; a failed required step fails
   that source so the caller can fall back.
4. **Paging state is source-bound.** Web `offset`, legacy dynamic ids and gRPC
   `historyOffset/updateBaseline` never mix; a failed append keeps the last good cursor.
5. **The player has one owner per open.** Every open/replace/close carries a session ticket
   (`Services/PlayerSessionState.cs`); resources born outside an accepted session are released and
   stale continuations are rejected. A player entry exists only when its content does
   (`Services/PlayerSurfaceVisibility.cs`).
6. **Credentials have one commit path** (`Services/CredentialCommitter.cs`,
   `Services/RefreshCredentialPolicy.cs`, `Services/WebRefreshTransaction.cs`). Cookies are written to
   every origin listed in `Models/CookieOrigins.cs`; a refresh token belongs to the session that
   saved it.
7. **Settings are JSON in `ApplicationData.LocalSettings`** through `Helpers/SettingHelper.cs`. Keys
   live in the nested classes of `SettingHelper`; a page must not write a key it does not own on load.
8. **Window/lifecycle fixes go through the helper, not ad-hoc Win32 calls.** `Helpers/WindowRestoreHelper.cs`
   owns the HWND lookups; suspend/close work runs inside the platform deferral and the deferral is
   always completed.

## 4. Conventions

C#
- `PascalCase` types/members, `camelCase` locals, `_camelCase` private fields, `Async` suffix on
  genuinely asynchronous methods. Builders in `Api/` are synchronous and return a request object.
- Wire DTOs live in `Models/Api/<Feature>/<Source>/` with explicit `[JsonProperty("…")]` names;
  scalar readers that distinguish "absent" from "zero" belong in `Parsing/…`.
- No `TODO`/`FIXME` in source. Known debt is recorded in `plan/PLAN.md` (§ 已知债务) or in
  `docs/ROADMAP.md`, not left inline.
- Fire-and-forget work must be exception-safe: `async void` only for event handlers, and every one of
  those swallows-and-logs what it cannot propagate. Platform deferrals are completed in a `finally`.
- Pure rules that a probe must assert live in `Services/` (no XAML types) instead of inside a page or
  control, e.g. `Services/FirstRunDisplayMode.cs`, `Services/PlayerToastOverlay.cs`.

XAML
- One visual owner per element: a recycled list item must recompute its child from the **current**
  item on every change, including empty (`Controls/Dynamic/RichTextControl` is the reference).
- Bind to strings through the existing converters; a converter must not throw on unexpected input,
  it returns a neutral value.
- Layout tokens (margins, sizes, brushes) are declared once at the top of the file as resources or
  named constants instead of being repeated per element.
- Text is user-facing Chinese: short labels carry no explanations, an icon never repeats the word
  next to it (BR-31). Explanations go to tooltips/help text.

## 5. Plan workflow

1. `bug_report.md` is the front door for a user-visible defect (one BR-nn entry with the log or
   screenshot that identifies it).
2. `plan/PLAN.md` holds open items; each gets a scope, a fix shape and an acceptance statement.
   Feature-sized work gets its own `plan/*_PLAN.md` with numbered slices.
3. Each slice lands as one commit that (a) changes source, (b) adds or extends a probe batch, and
   (c) writes the disposition where it belongs (`plan/PLAN.md` Open/Done row, `bug_report.md` answer,
   or `plan/REVIEW_LOG.md` row).
4. `plan/VALIDATION.md` records what a device pass should confirm. Current policy: the maintainer's
   device pass counts as passed; an item reopens only through a new `bug_report.md` entry.
5. `docs/ROADMAP.md` is the index over all of this; keep its status column honest when a slice ships.

## 6. Verification gates

- Compile gate: `-Fast -NoVersionBump` build, 0 errors. `WMC1500`, `WMC1506`, `PRI257`,
  `NETSDK1138` warnings are expected.
- Behaviour gate: the probe batches that cover the touched area exit 0; add assertions to the batch
  that already owns the code instead of creating a new one whenever possible.
- Release gate: one plain `build-release-x64.ps1` run before publishing; check that the emitted
  package is `BiliLite.Packages_<version>_x64_Test\BiliLite.Packages_<version>_x64.msix` and that
  `entrypoint/BiliLiter.exe` exists inside it with a root `BiliLiter.exe` under 64 KB.
- Device gate: only the maintainer can pass it. Do not claim a layout, playback, or account change
  was verified when it was only compiled.

## 7. Traps

1. **`CompileXaml` fails with `WMC1006 … BiliLite.gRPC.dll` / `BiliLite.JSBridge.winmd`** on a fresh
   clone: the referenced project outputs do not exist yet. Build the solution (plain script), or
   let a `-Fast` run build the references first; `-SkipProjectReferences` only works after they exist.
2. **`CS2012` on `BiliLiter.exe`** is a file lock from a compiler process that is still exiting, not a
   source error. The script kills the holders, waits until the file can be opened exclusively, then
   retries with `-t:Rebuild` at most twice. Do not retry blindly and do not "fix" it in source.
3. **The MSIX layout that boots** is the 8 KB launcher at the package root plus
   `entrypoint/BiliLiter.exe`. The app project's own `-t:Package` writes a ~4.2 MB managed exe at the
   root into the *same* filename; installing that one exits at the splash screen with no log. Details:
   `document/BUILD.md`.
4. **Shell scripts must stay LF.** With `core.autocrlf=true` a CRLF `set -euo pipefail` line fails
   under `sh` (`set: pipefail: invalid option name`) and `../..` path arithmetic breaks. `.gitattributes`
   pins `*.sh text eol=lf`; if a clone already has CRLF scripts, rewrite them before running probes.
5. **Console encoding.** Windows PowerShell 5.1 writes GBK on this machine. The build script sets
   UTF-8 for its own output; ad-hoc PowerShell must do the same
   (`[Console]::OutputEncoding=[Text.Encoding]::UTF8; …`), and reading an existing GBK log needs
   `iconv -f GB18030 -t UTF-8 -c` — without `-c` a single bad byte kills the producing process.
6. **Never hand-edit the manifest version.** The build bumps it; a sideloaded package at an unchanged
   version replaces the app silently instead of upgrading it.
7. **Probe projects need a .NET 8 SDK.** `plan/tests/*` target `net8.0`, and `plan/tests/run.sh`
   reads `$UPSTREAM_UNIT_TOOLS/dotnet/sdk/8.0.427` verbatim. With only SDK 9/10 installed, a
   directory batch usually still runs (8.0 targeting pack present), but the csc-based script does not;
   if a batch cannot start, that is an environment gap, not a code defect.
8. **`Helpers/HttpHelper.cs` pacing is inert.** `lasttime` is read in `Get` but never assigned, so the
   intended minimum interval never applies. Do not "fix" it by starting to write `lasttime` without
   measuring: the whole app's request pacing would change at once. Tracked in `docs/ROADMAP.md`.
9. **This clone may be partial.** Large blobs (`>100 KB`) can be sparse-excluded; they are listed in
   §8. A build in such a clone fails on missing sources, and probes that read those files from disk
   (`plan/tests/playersess` reads `PlayerControl.xaml.cs`, `g3` *is* a sparse file) cannot run — that
   is the clone, not the repo.

## 8. Do not touch

- `src/BiliLite.UWP/Assets/Fonts/**`, `Assets/Icon/**`, `Assets/*.png`, `Assets/*.gif`,
  `Assets/JiYan/**`, `src/BiliLite.Packages/Images/**`, `screenshot/ui.png`, `src/BiliLite.Win32Tools/ffmpeg.7z`:
  binary assets, some of them required by the manifest/PRI build. Never `git add -A` over them, never
  rewrite them as text, never delete them because a clone lacks their bytes.
- `src/BiliLite.gRPC/bilibili/**/*.proto`: generated-code contract; regenerated by `Grpc.Tools` at
  build time. Changing a proto changes the wire contract.
- `src/BiliLite.UWP/BiliLite.UWP.cer`, `Package.appxmanifest` `Identity`/`Properties` blocks: signing
  and identity. Version fields are touched only by the build script.
- `document/new_version.json`, `document/entrance.json`: served to installed clients; editing them
  changes every user's update prompt/home entries on the next launch.
- History is append-only for agents: do not rebase, amend, or force-push shared branches, and do not
  move `master` from `dev` work.
