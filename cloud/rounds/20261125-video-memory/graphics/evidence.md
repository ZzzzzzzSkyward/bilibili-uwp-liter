# Subtitle graphics ownership evidence

Status: RELEASED for independent requirements review; product/tests/dependencies/tooling/Git refs/index unchanged. Requirements are [requirements.md](requirements.md). Baseline source is `7356b94893377558191b0d70576a52bae704c1f9`; raw downloads/source/metadata/fixtures/logs remain only in `/workspace/work/video-memory/graphics/`.

## G1 — source-proven missing deterministic per-render release

Actual `PlayerControl.xaml.cs:1849–1912` `GenerateSubtitleItem` creates one text format, render target, text layout, geometry and stroke style per invocation. There is no using/Dispose/Close for those values or transfer of them into the returned visual. The local drawing session and memory stream are scoped already. `CanvasDevice.GetSharedDevice` is borrowed. The returned Grid contains an Image with a BitmapImage populated from an encoded PNG stream, not the CanvasRenderTarget/geometry/layout objects.

Ordinary playback reaches it from `SubtitleTimer_Tick:1797–1830` whenever the currently found cue text differs from `currentSubtitleText`; setting refresh reaches it from `UpdateSubtitle:1832–1847`. Both await the renderer before their existing generation/subtitle-reference publication checks. Stale completion can drop an unpublished output, but temporary cleanup is still the renderer's responsibility. Neither caller explicitly disposes the temporary Win2D objects. Complete actual caller methods were inspected; the fixture executes the complete renderer rather than pretending to execute a native timer or XAML player.

This is E1/P2 deterministic-ownership evidence. It does not need a close race or disposal of the player. R1/R2 are separately proven late-close work creation; R3 may amplify renderer invocation on a repeatedly loaded owner, but it is not required for G1. No measurement establishes how often the user's journey renders subtitles or whether those temporaries dominate observed memory.

Upstream `ResourceWrapper::~ResourceWrapper` itself calls `Close` (`winrt/lib/utils/ResourceWrapper.h:47–50`), and the wrappers hold native reference-counted resources. Therefore missing explicit cleanup is **not proof of immortal resources**. Final wrapper destruction/.NET WinRT projection collection/GPU completion/cache reuse timing and native bytes cannot be inferred from this model.

## Actual dependency and API verification

`src/BiliLite.UWP/BiliLite.UWP.csproj` targets UAP10.0.19041.0/min18362, references `Microsoft.NETCore.UniversalWindowsPlatform`6.2.14 and `NSDanmaku`2.2.0, and has no direct Win2D reference. Its declared Toolkit Controls7.1.3 dependency expands into Media7.1.3. Official published nuspecs show:

- [NSDanmaku2.2.0 package](https://api.nuget.org/v3-flatcontainer/nsdanmaku/2.2.0/nsdanmaku.2.2.0.nupkg) → `Win2D.uwp` version1.26.0 lower bound.
- [Toolkit Controls Media7.1.3 nuspec](https://api.nuget.org/v3-flatcontainer/microsoft.toolkit.uwp.ui.controls.media/7.1.3/microsoft.toolkit.uwp.ui.controls.media.nuspec) → Win2D1.25.0 lower bound.
- [Official Win2D.uwp1.26.0 package](https://api.nuget.org/v3-flatcontainer/win2d.uwp/1.26.0/win2d.uwp.1.26.0.nupkg) supplies `lib/uap10.0/Microsoft.Graphics.Canvas.winmd`, projected API XML, native headers/binaries, and `Win2d.githash.txt` identifying official upstream commit `36026efcb3fcd07af1d7a919fdf4201a07f05a20`.

There is no checked-in/restored `project.assets.json` or `packages.lock.json` in this source tree, and no local UWP package cache was found by the focused file scan. Thus1.26.0 is the verified package required by the declared NSDanmaku graph, not a claim that an unobserved Windows restore resolved that exact version. No restore/dependency/project change was made. `dependency-evidence.json` records all18 direct reference versions, official direct nuspec dependency rows and the relevant Toolkit expansion. The eventual Windows executor records actual resolved assets before native compilation/acceptance.

A private .NET8 `System.Reflection.Metadata` reader opens the **actual package WinMD** with `MetadataReaderOptions.None`, without loading native code. Its output `metadata/raw.log` shows:

| Actual WinMD type | Interface/base evidence | Projected API confirmation |
| --- | --- | --- |
| CanvasTextFormat | `Windows.Foundation.IClosable` | Package XML `M:…CanvasTextFormat.Dispose`, “Releases all resources…” |
| CanvasTextLayout | `Windows.Foundation.IClosable` | Package XML `M:…CanvasTextLayout.Dispose` |
| CanvasGeometry | `Windows.Foundation.IClosable` | Package XML `M:…CanvasGeometry.Dispose` |
| CanvasStrokeStyle | `Windows.Foundation.IClosable` | Package XML `M:…CanvasStrokeStyle.Dispose`, “Releases all resources…” |
| CanvasRenderTarget | Base `Microsoft.Graphics.Canvas.CanvasBitmap` | CanvasBitmap implements `Windows.Foundation.IClosable`; package XML documents CanvasBitmap.Dispose. Inherited disposal applies. |
| CanvasDrawingSession | `Windows.Foundation.IClosable` | Package XML documents Dispose. Already scoped. |
| CanvasDevice | `Windows.Foundation.IClosable` | API supports disposal, but shared ownership forbids per-cue disposal. |

The [official IClosable documentation](https://learn.microsoft.com/en-us/uwp/api/windows.foundation.iclosable?view=winrt-19041) states that .NET sees it as `System.IDisposable`. This checks the less-obvious format/style APIs rather than assuming all Win2D-looking classes share a contract. [InMemoryRandomAccessStream docs](https://learn.microsoft.com/en-us/uwp/api/windows.storage.streams.inmemoryrandomaccessstream?view=winrt-19041) explicitly list `System.IDisposable`. [BitmapImage](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.media.imaging.bitmapimage?view=winrt-19041) derives BitmapSource; it is the output image, not an owned closable Win2D target. No speculative Dispose is prescribed for it, Image/Grid or the measuring TextBlock.

Exact-version upstream source/docs are downloaded privately from the package's official commit:

- [CanvasDevice.xml:177–207](https://github.com/microsoft/Win2D/blob/36026efcb3fcd07af1d7a919fdf4201a07f05a20/winrt/docsrc/CanvasDevice.xml#L177) says disposing a shared device can break other sharing components. Keep it open. `CanvasGeometry.cpp:36–40` resets its stored device reference; `CanvasTextLayout.cpp:1832` uses `ClosablePtr.Close`, whose implementation swaps out the pointer rather than invoking device.Close; `CanvasBitmap.h:363–366` resets its device pointer and closes its own resource. Disposing these temporaries does not amount to disposing the shared device.
- [CanvasRenderTarget.xml:24–39](https://github.com/microsoft/Win2D/blob/36026efcb3fcd07af1d7a919fdf4201a07f05a20/winrt/docsrc/CanvasRenderTarget.xml#L24) requires drawing-session closure before SaveAsync because GPU drawing is usually deferred to close. Actual source already has this order.
- `CanvasBitmap.cpp:1334–1358` captures native bitmap/device/stream ComPtrs into the save async action and encodes into the stream. Conservative proposed ownership still keeps the wrapper and stream alive until the operation completes; no early-close assumption is needed.
- [Official BitmapSource.SetSourceAsync example](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.media.imaging.bitmapsource.setsourceasync?view=winrt-19041) opens a stream in a using, awaits `bitmapImage.SetSourceAsync(fileStream)`, sets the Image source, and then disposes the stream. This supports retaining the output while closing its input stream after awaited completion. The current method already follows this contract. It does not prove a specific PNG's native pixels here.

## Allocation, async, failure and proposed scope

The actual operation order is settings/text normalization → shared device → format properties → measurement TextBlock/bold/Measure → target → layout → geometry → drawing session → Clear → style/DashStyle → DrawGeometry → FillGeometry → session close → Image/BitmapImage → stream → await SaveAsync(Png,1.0f) → await SetSourceAsync → stream close → image Width/source/stretch → Grid tag/child → return. There is no await before drawing-session close. A failed acquisition at layout/geometry/drawing/save/source/output assembly currently leaves any already-created unscoped temporaries without deterministic release.

Private `ActualScoped.cs` is a review prototype only. It wraps format/target/layout/geometry in ordinary nested using blocks; format property initialization runs inside the acquired scope. A nullable style local is assigned after Clear, its valid DashStyle is assigned, and a surrounding finally closes it after drawing-session closure and source/output work. This preserves successful allocation/checkpoint/render/save/source order exactly in the fixture, including constructor/setter fault acquisition boundaries, and holds resources conservatively until all consumers finish. Existing stream/drawing scopes stay intact. No product method, package, language level or caller was changed.

## Complete actual-method fixture and exact results

Private files: `extract.py`, `bindings.json`, `ActualOriginal.cs`, `ActualScoped.cs`, `Stubs.cs`, `Program.cs`, `GraphicsProbe.csproj`, empty-source `NuGet.Config`, `restore.log`, `probe.log`, `expected-clean-red.log`; metadata source/logs and official artifacts sit alongside them. `evidence-manifest.sha256.json` binds private evidence; raw artifacts are not committed.

`extract.py` copies the **complete actual** method unmodified (only a surrounding harness class/wrapper is added) and emits a separately transformed candidate. Baseline entire PlayerControl source SHA256 is `18fac7cc1afcdf38f5896364f1606dd05446358965f3aae51cff00a7929b1e4f`; method SHA256 is `8775d4a82d509e92076cfaa0ab54475bbfad4cfaf8f5100a5f9c3929e94d24ea`. The paired fixture uses50 identical cue/setting sequences: center/left/right, regular/bold, font18–24,96/144DPI and CJK/multiline/Latin text. No network/media/account requests occur.

```bash
python3 /workspace/work/video-memory/graphics/extract.py
/workspace/work/maintenance-foundation/quality/dotnet/dotnet restore \
  /workspace/work/video-memory/graphics/GraphicsProbe.csproj \
  --configfile /workspace/work/video-memory/graphics/NuGet.Config
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run \
  --project /workspace/work/video-memory/graphics/GraphicsProbe.csproj --no-restore
/workspace/work/maintenance-foundation/quality/dotnet/dotnet \
  /workspace/work/video-memory/graphics/bin/Debug/net8.0/GraphicsProbe.dll \
  --expect-clean original
```

Main fixture exits0 because it checks baseline evidence, proposed-scope cleanup and controls. The final desired-clean baseline command exits1 **after all50 successful renders**, reporting50 undisposed instances of each owned type and `EXPECTED_CLEAN_FAILED`. A fixture expecting known baseline defects is not a product repair PASS.

| Control | Baseline actual method | Private scoped candidate |
| --- | --- | --- |
|50 complete sequential renders, per-owned-type release |50 format/target/layout/geometry/style acquisitions each;0 explicit disposals for each;50 undisposed each |50 acquisitions/disposals each, exactly once;0 undisposed each |
|Existing drawing-session/stream lifetime |50/50 each closed exactly once |50/50 each closed exactly once |
|All50 returned Grid→Image→BitmapImage outputs retained/read after completion |50 available |50 available after temporary disposal |
|Borrowed shared device, independent consumer |0 device disposals; usable |0 device disposals; usable |
|Pending SaveAsync gate |Session already closed; stream/5 temporaries valid; output pending |Same; no premature close |
|Pending SetSourceAsync gate |Stream/5 temporaries valid; output pending |Same; cleanup after source completion |
|23 fault points |Original injected failure propagates;77 accumulated undisposed resource instances across cases |Original injected failure propagates;0 undisposed resources; each acquired object closes once |
|2 truly suspended async completion failures (save/source) |Stream closes;5 unscoped resources remain undisposed in each case |Stream/5 owned types close once; borrowed device valid |
|50 paired successful operation/parameter traces |Reference |Identical non-disposal allocation/checkpoint/render/save/load traces and output contract |

The23 fault points are color conversion, format constructor/size/alignment/bold setter, measurement, target/layout/geometry/drawing/style acquisition, style setter, Clear/stroke/fill, session-close failure, stream construction, save start/completion, source start/completion and output width/child insertion. Constructor/factory failure is counted as no acquired instance; successful construction followed by setter failure is counted and cleaned. The session-close injected fault occurs after the stub marks its own resource closed; this does **not** simulate native close failing before native release, guarantee arbitrary WinRT teardown success, or fix R4.

Counters are on explicit model objects held by a test-only registry so disposal can be inspected. “Undisposed” is not a WeakReference survivor/native-allocation statistic. Canvas/WinRT/XAML/settings/measure/render/PNG/source boundaries are stubs; fake bitmap content records the input/parameters and a completed read, rather than encoding/decoding or pixels. It cannot show font shaping, outlines, antialiasing, XAML layout fidelity, device loss, WinRT wrapper finalization, GPU bytes or driver/cache reclamation. Tests establish the actual method's disposal calls/order/error behavior only. No RSS, working set, forced production GC or cache purge is used.

## Native gate and next action

`capabilities.json` records Linux x64, exact SDK8.0.427/runtime8.0.33/no workloads, and absence of PATH WPR/WPA/MSBuild/Windows PowerShell. The downloaded Win2D native DLL is a Windows PE32+ x64 binary. There is no runnable Windows UWP/WinRT XAML/GPU substrate here. A portable PowerShell elsewhere does not provide it. Native compile/render/memory acceptance is therefore FAILED/BLOCKED, not passed by metadata inspection or this fixture.

Independent expert requirements review precedes any code grant. If accepted, integrate G1 into the **one resource executor/PlayerControl writer** and existing playersess stage, preserve original240 cases and final51/52 inventory, and rerun baseline/final actual-source fixture with stable source bindings. On a Windows UWP build, record resolved Win2D assets/final package, compile, visually compare multiline/CJK/bold/alignment/color/font/DPI subtitles and repeated cue/setting changes, then run the measurement workstream's warmed same/distinct-video50-cycle subtitles-off/on controls with an idle tail and native/GPU/private-commit attribution. Stable model counters cannot close BR-21; that remains dependent on actual Windows evidence.
